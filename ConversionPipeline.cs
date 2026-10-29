using System.Collections.Concurrent;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO.Enumeration;
using System.Runtime.InteropServices;
using System.Text;
using NetVips;
using PhotoSauce.MagicScaler;
using PhotoSauce.NativeCodecs.Libheif;
using PhotoSauce.NativeCodecs.Libjxl;
using PhotoSauce.NativeCodecs.Libwebp;
using VipsImage = NetVips.Image;

namespace Resizer3
{
    /// <summary>An input image in the file list.</summary>
    /// <param name="RelativePath">Output-relative path; starts with the dropped folder name for files found in a dropped folder.</param>
    internal readonly record struct InputFile(string FullPath, string RelativePath, long Size);

    internal readonly record struct ConversionJob(string InputPath, string OutputPath, long InputSize);

    internal readonly record struct FailedFile(string FilePath, string ErrorMessage);

    internal sealed record ConversionOptions(
        string OutputRoot,
        string Format,
        bool PreserveFullFolderStructure,
        int Quality,
        decimal Multiplier,
        int MaxWidth,
        int MaxHeight,
        int WebpEffort,
        int PngCompression)
    {
        // libvips doesn't support writing .ico directly; use MagicScaler+GDI for ICO.
        // Some libvips builds omit BMP/GIF save support; use MagicScaler for BMP/GIF as well.
        public bool UseLibvips => Format is not ("ico" or "bmp" or "gif");

        public string MimeType => "image/" + Format;

        public bool PercentageResize => MaxWidth == int.MaxValue && Multiplier != 1m;
    }

    internal enum FileStatus { Succeeded, Failed, Canceled }

    internal readonly record struct FileResult(FileStatus Status, long OutputBytes, string? Error)
    {
        public static FileResult Succeeded(long outputBytes) => new(FileStatus.Succeeded, outputBytes, null);
        public static FileResult Failed(string error) => new(FileStatus.Failed, 0, error);
        public static FileResult Canceled => new(FileStatus.Canceled, 0, null);
    }

    internal static class InputCollector
    {
        private static readonly HashSet<string> AllowedInputExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".jfif", ".png", ".heic", ".heif", ".webp", ".avif", ".jxl",
            ".bmp", ".gif", ".tif", ".tiff", ".ico"
        };

        private static readonly HashSet<string>.AlternateLookup<ReadOnlySpan<char>> AllowedExtensionLookup =
            AllowedInputExtensions.GetAlternateLookup<ReadOnlySpan<char>>();

        private static readonly EnumerationOptions RecursiveFileEnumeration = new()
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            ReturnSpecialDirectories = false,
            AttributesToSkip = 0
        };

        internal static bool IsSupported(ReadOnlySpan<char> path) => AllowedExtensionLookup.Contains(Path.GetExtension(path));

        /// <summary>Expands dropped files and folders (recursively) into supported input files with their sizes.</summary>
        internal static List<InputFile> Collect(IEnumerable<string> droppedItems)
        {
            var files = new List<InputFile>();
            foreach (string item in droppedItems)
            {
                if (Directory.Exists(item))
                {
                    // Include the dropped folder's name in each relative path.
                    string topFolderName = Path.GetFileName(item.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

                    // The enumerator already knows each file's size, so no per-file stat is needed.
                    var entries = new FileSystemEnumerable<InputFile>(
                        item,
                        (ref FileSystemEntry entry) => new InputFile(
                            entry.ToFullPath(),
                            Path.Join(topFolderName, GetRelativeDirectory(ref entry), entry.FileName),
                            entry.Length),
                        RecursiveFileEnumeration)
                    {
                        ShouldIncludePredicate = (ref FileSystemEntry entry) => !entry.IsDirectory && IsSupported(entry.FileName)
                    };
                    files.AddRange(entries);
                }
                else if (IsSupported(item) && File.Exists(item))
                {
                    long size = 0;
                    try { size = new FileInfo(item).Length; }
                    catch { }
                    files.Add(new InputFile(item, Path.GetFileName(item), size));
                }
            }

            return files;
        }

        private static ReadOnlySpan<char> GetRelativeDirectory(ref FileSystemEntry entry)
        {
            ReadOnlySpan<char> directory = entry.Directory;
            return directory.Length > entry.RootDirectory.Length
                ? directory[entry.RootDirectory.Length..].TrimStart(['\\', '/'])
                : ReadOnlySpan<char>.Empty;
        }
    }

    internal static class OutputPlanner
    {
        /// <summary>
        /// Maps inputs to output paths. An input whose output would overwrite itself is rejected; an output that
        /// collides with another input or output of the batch gets a " (2)", " (3)", ... suffix.
        /// </summary>
        internal static List<ConversionJob> Plan(IReadOnlyList<InputFile> inputs, ConversionOptions options, List<FailedFile> rejected)
        {
            var normalizedInputs = new string[inputs.Count];
            var inputPaths = new HashSet<string>(inputs.Count, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < inputs.Count; i++)
            {
                normalizedInputs[i] = NormalizePath(inputs[i].FullPath);
                inputPaths.Add(normalizedInputs[i]);
            }

            var taken = new HashSet<string>(inputs.Count, StringComparer.OrdinalIgnoreCase);
            var nextSuffix = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var jobs = new List<ConversionJob>(inputs.Count);
            for (int i = 0; i < inputs.Count; i++)
            {
                var input = inputs[i];
                string outputPath;
                try
                {
                    outputPath = Path.GetFullPath(BuildOutputPath(input.FullPath, input.RelativePath, options.OutputRoot, options.Format, options.PreserveFullFolderStructure));
                }
                catch (Exception ex)
                {
                    rejected.Add(new FailedFile(input.FullPath, "Invalid output path: " + ex.Message));
                    continue;
                }

                if (string.Equals(outputPath, normalizedInputs[i], StringComparison.OrdinalIgnoreCase))
                {
                    rejected.Add(new FailedFile(input.FullPath, "Skipped: the output would overwrite this file. Choose a different output folder or format."));
                    continue;
                }

                if (inputPaths.Contains(outputPath) || !taken.Add(outputPath))
                {
                    outputPath = MakeUnique(outputPath, inputPaths, taken, nextSuffix);
                    taken.Add(outputPath);
                }

                jobs.Add(new ConversionJob(input.FullPath, outputPath, input.Size));
            }

            return jobs;
        }

        /// <summary>Groups input paths by file name, so workers can check whether an existing output is an input in disguise.</summary>
        internal static ILookup<string, string> IndexByFileName(IReadOnlyList<InputFile> inputs)
            => inputs.ToLookup(input => Path.GetFileName(input.FullPath), input => input.FullPath, StringComparer.OrdinalIgnoreCase);

        internal static string BuildOutputPath(string inPath, string relativePath, string outputRoot, string formatExtension, bool preserveFullFolderStructure)
        {
            string relative;
            if (preserveFullFolderStructure)
            {
                string root = Path.GetPathRoot(inPath) ?? string.Empty;
                relative = !string.IsNullOrEmpty(root) && inPath.StartsWith(root, StringComparison.OrdinalIgnoreCase)
                    ? inPath[root.Length..]
                    : inPath;
                relative = relative.Replace(":", "");
            }
            else
            {
                relative = string.IsNullOrEmpty(relativePath) ? Path.GetFileName(inPath) : relativePath;
            }

            // A leading separator (e.g. after a UNC root) would make Path.Combine drop the output root.
            relative = relative.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string relativeDir = Path.GetDirectoryName(relative) ?? string.Empty;
            string newFileName = Path.GetFileNameWithoutExtension(relative) + "." + formatExtension;

            return string.IsNullOrEmpty(relativeDir)
                ? Path.Combine(outputRoot, newFileName)
                : Path.Combine(outputRoot, relativeDir, newFileName);
        }

        private static string NormalizePath(string path)
        {
            try { return Path.GetFullPath(path); }
            catch { return path; }
        }

        private static string MakeUnique(string path, HashSet<string> inputPaths, HashSet<string> taken, Dictionary<string, int> nextSuffix)
        {
            string directory = Path.GetDirectoryName(path) ?? string.Empty;
            string name = Path.GetFileNameWithoutExtension(path);
            string extension = Path.GetExtension(path);
            for (int n = nextSuffix.GetValueOrDefault(path, 2); ; n++)
            {
                string candidate = Path.Join(directory, $"{name} ({n}){extension}");
                if (!inputPaths.Contains(candidate) && !taken.Contains(candidate))
                {
                    nextSuffix[path] = n + 1;
                    return candidate;
                }
            }
        }
    }

    /// <summary>
    /// Converts files for one worker thread. Owns that thread's TurboJPEG session; dispose when the worker finishes.
    /// Every output is written to a temporary file next to it and renamed into place only after it is complete.
    /// </summary>
    internal sealed class ConversionWorker : IDisposable
    {
        private const int FileStreamBufferSize = 128 * 1024;

        private static readonly Lazy<bool> MagicScalerCodecs = new(() =>
        {
            CodecManager.Configure(codecs =>
            {
                codecs.UseLibjxl();
                codecs.UseLibheif();
                codecs.UseLibwebp();
            });
            return true;
        });

        private readonly ConversionOptions options;
        private readonly ConcurrentDictionary<string, bool> createdDirs;
        private readonly ILookup<string, string>? inputsByName;
        private readonly Func<int, int, bool> keepsSize;
        private TurboJpegSession? turboJpeg;

        /// <param name="inputsByName">The batch's inputs from <see cref="OutputPlanner.IndexByFileName"/>.</param>
        internal ConversionWorker(ConversionOptions options, ConcurrentDictionary<string, bool> createdDirs, ILookup<string, string>? inputsByName = null)
        {
            this.options = options;
            this.createdDirs = createdDirs;
            this.inputsByName = inputsByName;
            keepsSize = KeepsSize;
        }

        public void Dispose()
        {
            turboJpeg?.Dispose();
            turboJpeg = null;
        }

        internal FileResult Process(ConversionJob job, CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();
                // The planner compares path strings; this catches an input reached through another
                // path (mapped drive vs UNC, subst drive, junction or symbolic link).
                if (File.Exists(job.OutputPath) && OverwritesInput(job))
                    return FileResult.Failed("Skipped: the output path is an input file reached through another path (mapped drive, subst drive or link). Choose a different output folder.");

                return options.UseLibvips ? ProcessLibvips(job, token) : ProcessMagicScaler(job, token);
            }
            catch (OperationCanceledException)
            {
                return FileResult.Canceled;
            }
            catch (VipsException ex)
            {
                return FileResult.Failed("libvips error: " + CleanMessage(ex.Message));
            }
            catch (Exception ex)
            {
                return FileResult.Failed(CleanMessage(ex.Message));
            }
        }

        internal static (int width, int height) CalculateTargetDimensions(int imgWidth, int imgHeight, decimal multiplier, int maxWidth, int maxHeight)
        {
            if (imgWidth <= 0 || imgHeight <= 0)
                return (0, 0);

            if (maxWidth != int.MaxValue)
            {
                if (imgWidth <= maxWidth && imgHeight <= maxHeight)
                    return (imgWidth, imgHeight);

                double widthRatio = (double)maxWidth / imgWidth;
                double heightRatio = (double)maxHeight / imgHeight;
                double scale = Math.Min(widthRatio, heightRatio);
                return (Math.Max(1, (int)(imgWidth * scale)), Math.Max(1, (int)(imgHeight * scale)));
            }

            return (Math.Max(1, (int)(imgWidth * multiplier)), Math.Max(1, (int)(imgHeight * multiplier)));
        }

        /// <summary>Returns the size as displayed: EXIF orientations 5-8 rotate the stored image by 90 degrees.</summary>
        internal static (int width, int height) GetDisplaySize(int width, int height, int exifOrientation)
            => exifOrientation is >= 5 and <= 8 ? (height, width) : (width, height);

        private bool OverwritesInput(ConversionJob job)
        {
            if (FileIdentity.IsSameFile(job.InputPath, job.OutputPath))
                return true;
            if (inputsByName is null)
                return false;

            // Aliased folders keep file names, so only inputs with the output's name can match.
            foreach (string input in inputsByName[Path.GetFileName(job.OutputPath)])
            {
                if (FileIdentity.IsSameFile(input, job.OutputPath))
                    return true;
            }
            return false;
        }

        private bool KeepsSize(int width, int height)
        {
            var (targetWidth, targetHeight) = CalculateTargetDimensions(width, height, options.Multiplier, options.MaxWidth, options.MaxHeight);
            return targetWidth == width && targetHeight == height;
        }

        private FileResult ProcessLibvips(ConversionJob job, CancellationToken token)
        {
            string tempPath = GetTempPath(job.OutputPath);
            try
            {
                EnsureOutputDirectory(job.OutputPath);

                // TurboJPEG fast path for no-resize JPEG -> JPEG.
                if (CanUseTurboJpeg(job.InputPath))
                {
                    turboJpeg ??= new TurboJpegSession();
                    if (turboJpeg.TryTranscode(job.InputPath, tempPath, options.Quality, keepsSize, out long fastOutputBytes))
                    {
                        CommitTemp(tempPath, job.OutputPath);
                        return FileResult.Succeeded(fastOutputBytes);
                    }
                }

                token.ThrowIfCancellationRequested();
                // Header-only probe - pixels are not decoded until pulled. Truncated files fail instead
                // of being saved with missing rows.
                using var probe = VipsImage.NewFromFile(job.InputPath, access: Enums.Access.Sequential, failOn: Enums.FailOn.Truncated);
                var (width, height) = GetDisplaySize(probe.Width, probe.Height, GetOrientation(probe));
                var (targetWidth, targetHeight) = CalculateTargetDimensions(width, height, options.Multiplier, options.MaxWidth, options.MaxHeight);

                token.ThrowIfCancellationRequested();
                // File-based Thumbnail enables shrink-on-load (scaled JPEG/WebP decode, HEIF thumbnails);
                // it applies EXIF orientation, so the target size is given in display orientation.
                // Without a resize the probe itself is saved, keeping the orientation tag.
                using var resized = targetWidth != width || targetHeight != height
                    ? VipsImage.Thumbnail(job.InputPath, targetWidth, height: targetHeight, size: Enums.Size.Down, failOn: Enums.FailOn.Truncated)
                    : null;
                Save(resized ?? probe, tempPath);

                long outputBytes = FinishTemp(tempPath, File.GetLastWriteTimeUtc(job.InputPath));
                CommitTemp(tempPath, job.OutputPath);
                return FileResult.Succeeded(outputBytes);
            }
            catch
            {
                TryDelete(tempPath);
                throw;
            }
        }

        private bool CanUseTurboJpeg(string inputPath)
        {
            if (options.Format is not ("jpg" or "jpeg") || options.PercentageResize || !TurboJpegNative.IsAvailable)
                return false;

            ReadOnlySpan<char> extension = Path.GetExtension(inputPath.AsSpan());
            return extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".jfif", StringComparison.OrdinalIgnoreCase);
        }

        private static int GetOrientation(VipsImage image)
            => image.GetTypeOf("orientation") != IntPtr.Zero && image.Get("orientation") is int orientation ? orientation : 1;

        private void Save(VipsImage image, string path)
        {
            switch (options.Format)
            {
                case "webp":
                    image.Webpsave(path, q: options.Quality, lossless: options.Quality == 100, effort: options.WebpEffort);
                    break;
                case "jpg":
                case "jpeg":
                    image.Jpegsave(path, q: options.Quality);
                    break;
                case "png":
                    image.Pngsave(path, compression: options.PngCompression);
                    break;
                case "tiff":
                case "tif":
                    image.Tiffsave(path);
                    break;
                default:
                    // The temp path keeps the target extension, so libvips picks the right saver.
                    image.WriteToFile(path);
                    break;
            }
        }

        private FileResult ProcessMagicScaler(ConversionJob job, CancellationToken token)
        {
            _ = MagicScalerCodecs.Value;
            int imgWidth = 0, imgHeight = 0;

            try
            {
                // FrameInfo sizes already reflect EXIF orientation.
                var info = ImageFileInfo.Load(job.InputPath);
                if (info.Frames.Count > 0)
                {
                    imgWidth = info.Frames[0].Width;
                    imgHeight = info.Frames[0].Height;
                }
            }
            catch
            {
                // Fall back below.
            }

            if (imgWidth == 0 || imgHeight == 0)
            {
                try
                {
                    using var vipsInfo = VipsImage.NewFromFile(job.InputPath, access: Enums.Access.Sequential);
                    (imgWidth, imgHeight) = GetDisplaySize(vipsInfo.Width, vipsInfo.Height, GetOrientation(vipsInfo));
                }
                catch { }
            }

            // MagicScaler applies EXIF orientation, so the target size is given in display orientation.
            var (targetWidth, targetHeight) = CalculateTargetDimensions(imgWidth, imgHeight, options.Multiplier, options.MaxWidth, options.MaxHeight);

            var resizeSettings = new ProcessImageSettings
            {
                Width = targetWidth,
                Height = targetHeight
            };

            // FORMAT-SPECIFIC ENCODER OPTIONS
            if (options.Format == "webp")
            {
                resizeSettings.EncoderOptions = options.Quality >= 100 ? new WebpLosslessEncoderOptions() : new WebpLossyEncoderOptions { Quality = options.Quality };
            }
            else if (options.Format != "ico")
            {
                resizeSettings.EncoderOptions = new LossyEncoderOptions { Quality = options.Quality };
            }

            resizeSettings.TrySetEncoderFormat(options.MimeType);

            string tempPath = GetTempPath(job.OutputPath);
            try
            {
                token.ThrowIfCancellationRequested();
                EnsureOutputDirectory(job.OutputPath);

                if (options.Format == "ico")
                {
                    SaveIcoWithMagicScaler(job.InputPath, tempPath, token);
                }
                else
                {
                    using var outStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, FileStreamBufferSize);
                    MagicImageProcessor.ProcessImage(job.InputPath, outStream, resizeSettings);
                }

                DateTime desiredTime = GetDesiredTimestamp(job.InputPath, tryExifDateTaken: true);
                long outputBytes = FinishTemp(tempPath, desiredTime.ToUniversalTime());
                CommitTemp(tempPath, job.OutputPath);
                return FileResult.Succeeded(outputBytes);
            }
            catch (OperationCanceledException)
            {
                TryDelete(tempPath);
                throw;
            }
            catch (Exception ex)
            {
                TryDelete(tempPath);
                return FileResult.Failed(CleanMessage(ex.Message) + ": Error writing " + job.OutputPath);
            }
        }

        private void EnsureOutputDirectory(string outPath)
        {
            string? dir = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrEmpty(dir))
            {
                createdDirs.GetOrAdd(dir, static directory =>
                {
                    Directory.CreateDirectory(directory);
                    return true;
                });
            }
        }

        internal static string GetTempPath(string outputPath)
        {
            string extension = Path.GetExtension(outputPath);
            return string.Concat(outputPath.AsSpan(0, outputPath.Length - extension.Length), ".r3tmp", extension);
        }

        /// <summary>Stamps the finished temp file with the source time and returns its length.</summary>
        private static long FinishTemp(string tempPath, DateTime lastWriteTimeUtc)
        {
            using var handle = File.OpenHandle(tempPath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
            try { File.SetLastWriteTimeUtc(handle, lastWriteTimeUtc); }
            catch { }
            return RandomAccess.GetLength(handle);
        }

        /// <summary>
        /// Moves a finished temp file over the output, so a failed or interrupted conversion never leaves a
        /// truncated output or destroys an older one. Retries briefly while a scanner or indexer holds the file.
        /// </summary>
        private static void CommitTemp(string tempPath, string outputPath)
        {
            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    File.Move(tempPath, outputPath, overwrite: true);
                    return;
                }
                catch (Exception ex) when (attempt < 6 && ex is IOException or UnauthorizedAccessException)
                {
                    Thread.Sleep(25 * attempt);
                }
            }
        }

        private static void TryDelete(string path)
        {
            try { File.Delete(path); }
            catch { }
        }

        private static string CleanMessage(string message)
            => string.Join("; ", message.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        private static DateTime GetDesiredTimestamp(string inPath, bool tryExifDateTaken)
        {
            if (tryExifDateTaken)
            {
                try
                {
                    using var stream = new FileStream(inPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, FileStreamBufferSize, FileOptions.SequentialScan);
                    using var image = System.Drawing.Image.FromStream(stream, useEmbeddedColorManagement: false, validateImageData: false);
                    if (TryGetExifDateTaken(image, out DateTime dateTaken))
                        return dateTaken;
                }
                catch { }
            }

            try { return File.GetLastWriteTime(inPath); }
            catch { return DateTime.Now; }
        }

        private static bool TryGetExifDateTaken(System.Drawing.Image image, out DateTime dateTaken)
        {
            const int ExifDateTakenPropertyId = 36867;
            dateTaken = default;

            try
            {
                if (image.PropertyIdList?.Contains(ExifDateTakenPropertyId) != true)
                    return false;

                var propItem = image.GetPropertyItem(ExifDateTakenPropertyId);
                if (propItem?.Value == null)
                    return false;

                string rawDate = Encoding.UTF8.GetString(propItem.Value).Trim('\0', ' ', '\t', '\r', '\n');
                return DateTime.TryParseExact(rawDate, "yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out dateTaken);
            }
            catch
            {
                return false;
            }
        }

        private static void SaveIcoWithMagicScaler(string inPath, string outPath, CancellationToken token)
        {
            using var bmpStream = new MemoryStream();
            var bmpSettings = new ProcessImageSettings { Width = 256, Height = 256 };
            bmpSettings.TrySetEncoderFormat("image/bmp");

            MagicImageProcessor.ProcessImage(inPath, bmpStream, bmpSettings);
            token.ThrowIfCancellationRequested();

            bmpStream.Position = 0;
            using var tempImg = new Bitmap(bmpStream);
            SaveAsIco(tempImg, outPath);
        }

        private static void SaveAsIco(System.Drawing.Image img, string path)
        {
            using (var bmp = new Bitmap(img, new System.Drawing.Size(256, 256)))
            {
                // Convert to 32bppArgb
                using (var bmp32 = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format32bppArgb))
                using (Graphics g = Graphics.FromImage(bmp32))
                {
                    g.DrawImage(bmp, 0, 0, bmp.Width, bmp.Height);

                    using (var fs = new FileStream(path, FileMode.Create))
                    using (var bw = new BinaryWriter(fs))
                    {
                        // ICO Header
                        bw.Write((short)0);        // Reserved
                        bw.Write((short)1);        // Type = 1 for icons
                        bw.Write((short)1);        // Number of images

                        // Directory entry
                        bw.Write((byte)0);         // Width 0 = 256
                        bw.Write((byte)0);         // Height 0 = 256
                        bw.Write((byte)0);         // Color palette
                        bw.Write((byte)0);         // Reserved
                        bw.Write((short)1);        // Color planes
                        bw.Write((short)32);       // Bits per pixel
                        int imageDataSize = 40 + bmp32.Width * bmp32.Height * 4; // BITMAPINFOHEADER + pixel data
                        bw.Write(imageDataSize);   // Size of image data
                        bw.Write(6 + 16);          // Offset of image data (header + directory)

                        // BITMAPINFOHEADER
                        bw.Write(40);              // Header size
                        bw.Write(bmp32.Width);
                        bw.Write(bmp32.Height * 2); // height = image + mask (ICO format)
                        bw.Write((short)1);        // Planes
                        bw.Write((short)32);       // Bits per pixel
                        bw.Write(0);               // Compression (BI_RGB)
                        bw.Write(0);               // Image size (can be 0 for BI_RGB)
                        bw.Write(0);               // X pixels per meter
                        bw.Write(0);               // Y pixels per meter
                        bw.Write(0);               // Colors used
                        bw.Write(0);               // Important colors

                        // Write pixel data (BGRA, bottom-up).  Format32bppArgb is already BGRA in memory.
                        var rect = new Rectangle(0, 0, bmp32.Width, bmp32.Height);
                        BitmapData? bits = null;
                        try
                        {
                            bits = bmp32.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                            int stride = bits.Stride;
                            int absStride = Math.Abs(stride);
                            int bytesPerRow = bmp32.Width * 4;
                            byte[] row = new byte[bytesPerRow];

                            for (int y = bmp32.Height - 1; y >= 0; y--)
                            {
                                int sourceOffset = stride > 0
                                    ? y * stride
                                    : (bmp32.Height - 1 - y) * absStride;
                                Marshal.Copy(IntPtr.Add(bits.Scan0, sourceOffset), row, 0, bytesPerRow);
                                bw.Write(row, 0, bytesPerRow);
                            }
                        }
                        finally
                        {
                            if (bits != null)
                                bmp32.UnlockBits(bits);
                        }

                        // AND mask: can be all zero for 32-bit with alpha
                        int maskBytesPerRow = (bmp32.Width + 31) / 32 * 4;
                        for (int y = 0; y < bmp32.Height; y++)
                            for (int b = 0; b < maskBytesPerRow; b++)
                                bw.Write((byte)0);
                    }
                }
            }
        }
    }

    /// <summary>Compares files by volume and file ID, which differ from path strings when a folder is aliased.</summary>
    internal static class FileIdentity
    {
        private const int FileIdInfoClass = 18;

        [StructLayout(LayoutKind.Sequential)]
        private struct FILE_ID_INFO
        {
            public ulong VolumeSerialNumber;
            public ulong FileIdLow;
            public ulong FileIdHigh;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BY_HANDLE_FILE_INFORMATION
        {
            public uint FileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME CreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWriteTime;
            public uint VolumeSerialNumber;
            public uint FileSizeHigh;
            public uint FileSizeLow;
            public uint NumberOfLinks;
            public uint FileIndexHigh;
            public uint FileIndexLow;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetFileInformationByHandleEx(Microsoft.Win32.SafeHandles.SafeFileHandle file, int infoClass, out FILE_ID_INFO info, int size);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetFileInformationByHandle(Microsoft.Win32.SafeHandles.SafeFileHandle file, out BY_HANDLE_FILE_INFORMATION info);

        /// <summary>True only when both paths open the same file; false when that cannot be determined.</summary>
        internal static bool IsSameFile(string first, string second)
        {
            try
            {
                using var a = File.OpenHandle(first, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using var b = File.OpenHandle(second, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                return TryGetId(a, out var idA) && TryGetId(b, out var idB) && idA == idB;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryGetId(Microsoft.Win32.SafeHandles.SafeFileHandle handle, out (ulong Volume, ulong High, ulong Low) id)
        {
            // FileIdInfo has 128-bit IDs (needed on ReFS); older servers only answer the 64-bit query.
            if (GetFileInformationByHandleEx(handle, FileIdInfoClass, out var idInfo, Marshal.SizeOf<FILE_ID_INFO>()))
            {
                id = (idInfo.VolumeSerialNumber, idInfo.FileIdHigh, idInfo.FileIdLow);
                return true;
            }
            if (GetFileInformationByHandle(handle, out var info))
            {
                id = (info.VolumeSerialNumber, 0, ((ulong)info.FileIndexHigh << 32) | info.FileIndexLow);
                return true;
            }
            id = default;
            return false;
        }
    }
}
