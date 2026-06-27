using PhotoSauce.MagicScaler;
using PhotoSauce.NativeCodecs.Libheif;
using PhotoSauce.NativeCodecs.Libjxl;
using PhotoSauce.NativeCodecs.Libwebp;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Threading.Channels;
using System.Collections.Concurrent;
using System.Threading;
using System.Runtime.InteropServices;
using NetVips;
//using SixLabors.ImageSharp;
//using NeoSolve.ImageSharp.AVIF;



namespace Resizer3
{
    public partial class Form1 : Form
    {

        private const int FileStreamBufferSize = 128 * 1024;
        private GitHubReleaseUpdate? pendingUpdate;
        private static readonly HashSet<string> AllowedInputExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".heic", ".heif", ".webp", ".avif", ".jxl",
            ".bmp", ".gif", ".tiff", ".ico"
        };

        private static readonly EnumerationOptions RecursiveFileEnumeration = new()
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            ReturnSpecialDirectories = false,
            AttributesToSkip = 0
        };

        private readonly record struct ConversionItem(string InputPath, string OutputPath, long InputSize);

        public Form1()
        {
            InitializeComponent();

            textBox1.Text = @"C:\img";
            numericUpDownRes.Value = 100;




            CodecManager.Configure(codecs =>
            {
                codecs.UseLibjxl();
                codecs.UseLibheif();
                codecs.UseLibwebp();
                //codecs.UseLibpng();
            });
            comboBox1.Items.Clear();
            comboBox1.Items.AddRange(new string[]
            {
            "jpg", "png", "webp", "bmp", "gif", "tiff", "ico"
            });
            comboBox1.SelectedItem = "jpg";

            // Setup maxResComboBox
            maxResComboBox.Items.Clear();
            maxResComboBox.Items.AddRange(new string[] { "Off", "4K (2160p)", "1440p", "1080p" });
            maxResComboBox.SelectedItem = "Off";

            // Load Saved entries
            var settings = AppSettings.Load();
            textBox1.Text = settings.SaveLocation;
            numericUpDownQuality.Value = settings.Quality;
            numericUpDownThreads.Value = settings.ThreadsNumber > 0 ? settings.ThreadsNumber : Environment.ProcessorCount;
            numericUpDownRes.Value = settings.Resolution;
            if (!string.IsNullOrEmpty(settings.Format))
                comboBox1.SelectedItem = settings.Format;
            if (!string.IsNullOrEmpty(settings.MaxRes))
                maxResComboBox.SelectedItem = settings.MaxRes;
            numericUpDownWebpEffort.Value = ClampNumeric(settings.WebpEffort, numericUpDownWebpEffort.Minimum, numericUpDownWebpEffort.Maximum);

            this.Shown += Form1_Shown;
            AddRelativePathColumn();
            UpdateWebpLibvipsOptionsVisibility();

            // Remove Failed Files tab initially
            tabControl.TabPages.Remove(tabPageFailedFiles);
        }

        private void AddFailedFile(string filePath, string errorMessage)
        {
            // Show Failed Files tab if it's not visible
            if (!tabControl.TabPages.Contains(tabPageFailedFiles))
            {
                tabControl.TabPages.Add(tabPageFailedFiles);
            }

            // Add failed file to dataGridViewFailed
            var row = new DataGridViewRow();
            row.CreateCells(dataGridViewFailed);
            row.Cells[0].Value = filePath;
            row.Cells[1].Value = errorMessage;
            dataGridViewFailed.Rows.Add(row);

            // Switch to Failed Files tab
            tabControl.SelectedTab = tabPageFailedFiles;
        }

        private int fileNum;
        long afterSize = 0;
        private void Form1_Load(object sender, EventArgs e)
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.0";
            Text = "Resizer " + version + "        Batch image converter";
        }

        private async void Form1_Shown(object? sender, EventArgs e)
        {
            await Task.Delay(150);

            await CheckForUpdateAsync();

        }
        private void AddRelativePathColumn()
        {
            if (!dataGridView1.Columns.Contains("RelativePath"))
            {
                var relCol = new DataGridViewTextBoxColumn
                {
                    Name = "RelativePath",
                    HeaderText = "RelativePath",
                    Visible = false // hide it
                };
                dataGridView1.Columns.Add(relCol);
            }
        }



        public void SaveAsIco(System.Drawing.Image img, string path)
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
        /*
        public void SaveAsAvif(string inPath, string outPath, int quality)
        {
            int cqLevel = (int)(63 * (100 - quality) / 100m);
            var encoder = new AVIFEncoder
            {
                CQLevel = cqLevel,
                Lossless = (quality == 100),

            };

            using var image = SixLabors.ImageSharp.Image.Load(inPath);

            image.Save(outPath, encoder);
        }
        */


        private async Task CheckForUpdateAsync()
        {
            update.Enabled = false;
            pendingUpdate = null;

            try
            {
                Version localVersion = GetFileVersion(Application.ExecutablePath);
                pendingUpdate = await GitHubReleaseUpdateService.CheckLatestAsync(localVersion);

                if (pendingUpdate is not null)
                {
                    MessageBox.Show($"Update Available: {pendingUpdate.Version}\n(Local version: {localVersion})");
                    update.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                AppLog.TryLog("Update check failed", ex);
                update.Enabled = false;
                pendingUpdate = null;
            }
        }
        private Version GetFileVersion(string path)
        {
            FileVersionInfo info = FileVersionInfo.GetVersionInfo(path);
            return new Version(info.FileVersion ?? "0.0.0.0");
        }

        private async void dataGridView1_DragDropAsync(object sender, DragEventArgs e)
        {
            var data = e.Data;
            if (data == null)
                return;
            string[] droppedItems = data.GetData(DataFormats.FileDrop, false) as string[] ?? Array.Empty<string>();

            List<(string fullPath, string relativePath)> validFiles = await Task.Run(() =>
            {
                var allFiles = new List<(string, string)>();

                foreach (var item in droppedItems)
                {
                    if (Directory.Exists(item))
                    {
                        string topFolderName = Path.GetFileName(item.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

                        var filesInDir = Directory.EnumerateFiles(item, "*.*", RecursiveFileEnumeration)
                                                  .Where(f => AllowedInputExtensions.Contains(Path.GetExtension(f)));

                        foreach (var file in filesInDir)
                        {
                            // include top folder in relative path
                            string relativePath = Path.Combine(topFolderName, Path.GetRelativePath(item, file));
                            relativePath = relativePath.Replace('/', Path.DirectorySeparatorChar); // normalize separators
                            allFiles.Add((file, relativePath));
                        }
                    }
                    else if (File.Exists(item))
                    {
                        if (AllowedInputExtensions.Contains(Path.GetExtension(item)))
                            allFiles.Add((item, Path.GetFileName(item)));
                    }
                }

                return allFiles;
            });

            dataGridView1.SuspendLayout();
            int relColIndex = dataGridView1.Columns["RelativePath"]?.Index ?? -1;
            foreach (var (fullPath, relativePath) in validFiles)
            {
                object[] rowValues = new object[dataGridView1.Columns.Count];
                rowValues[0] = fullPath;
                if (relColIndex >= 0)
                    rowValues[relColIndex] = relativePath;
                dataGridView1.Rows.Add(rowValues);
            }
            dataGridView1.ResumeLayout();

            dataGridView1_Changed();
        }






        private void dataGridView1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
                e.Effect = DragDropEffects.All;
            else
                e.Effect = DragDropEffects.None;
        }

        private int comfileNum;

        private readonly ConcurrentDictionary<string, bool> createdDirs = new();

        private System.Windows.Forms.Timer? uiUpdateTimer;
        private System.Windows.Forms.Timer? elapsedTimeTimer;

        // Processing statistics
        private long totalBeforeSize = 0; // total input size in bytes
        private int totalFilesProcessing = 0;
        private long processedInputBytes = 0; // cumulative input bytes processed

        public int threads;
        public int t = 0;
        public string? format;
        Stopwatch sw = new Stopwatch();
        private bool isProcessing = false;

        private async void buttonResize_Click(object sender, EventArgs e)
        {
            if (isProcessing)
                return;
            isProcessing = true;
            buttonResize.Enabled = false;
            cancelButton.Enabled = true;

            cts = new();
            dataGridView1_Changed();
            sw.Reset();
            sw.Start();

            afterSize = 0;
            Interlocked.Exchange(ref processedInputBytes, 0);
            Interlocked.Exchange(ref totalBeforeSize, 0);
            totalFilesProcessing = 0;
            createdDirs.Clear();
            t = 0;
            threads = Decimal.ToInt32(numericUpDownThreads.Value);
            comfileNum = 0;
            format = "image/" + (comboBox1.SelectedItem?.ToString() ?? "jpg");

            //Thread myThread = new Thread(() => ThreadHandler2Async());
            //myThread.Start();
            buttonResize.Text = "Converting... 0%";
            // Start a periodic UI updater (batches UI work to reduce cross-thread overhead)
            uiUpdateTimer = new System.Windows.Forms.Timer();
            uiUpdateTimer.Interval = 250;
            uiUpdateTimer.Tick += (s, ev) => UpdateUI();
            uiUpdateTimer.Start();

            elapsedTimeTimer = new System.Windows.Forms.Timer();
            elapsedTimeTimer.Interval = 33;
            elapsedTimeTimer.Tick += (s, ev) => UpdateElapsedTime();
            elapsedTimeTimer.Start();

            try
            {
                await ThreadHandler2Async();
            }
            finally
            {
                isProcessing = false;
                buttonResize.Enabled = true;
                cancelButton.Enabled = false;
                buttonResize.Text = "Convert";
            }
            return;
        }

        private async Task ThreadHandler2Async()
        {
            try
            {
                // Snapshot UI state to avoid cross-thread access during processing
                var rows = dataGridView1.Rows.Cast<DataGridViewRow>()
                    .Where(r => r.Cells.Count > 0 && r.Cells[0].Value != null)
                    .Select(r => (inPath: r.Cells[0].Value?.ToString() ?? string.Empty, relativePath: r.Cells["RelativePath"].Value?.ToString() ?? string.Empty))
                    .ToList();

                string outputRoot = textBox1.Text;
                string formatLocal = format ?? "image/jpg";
                string formatExtension = GetFormatExtension(formatLocal);
                bool preserveFullFolderStructure = checkBox1.Checked;
                int quality = Decimal.ToInt32(numericUpDownQuality.Value);
                decimal multiplier = numericUpDownRes.Value / 100m;
                // libvips doesn't support writing .ico directly; use MagicScaler+GDI for ICO.
                // Some libvips builds omit BMP/GIF save support; use MagicScaler for BMP/GIF as well.
                bool useLibvips = formatLocal != "image/ico" && formatLocal != "image/bmp" && formatLocal != "image/gif";
                bool webpLossless = quality == 100;
                int webpEffort = Decimal.ToInt32(numericUpDownWebpEffort.Value);

                // Capture max resolution dimensions on UI thread before spawning workers
                var (maxWidth, maxHeight) = GetMaxResolutionDimensions();

                // Filter to existing files and compute totals for progress and throughput
                var filteredRows = new List<ConversionItem>(rows.Count);
                long sumBefore = 0;
                foreach (var r in rows)
                {
                    if (string.IsNullOrEmpty(r.inPath) || !File.Exists(r.inPath))
                        continue;

                    long inputSize = GetFileLength(r.inPath);
                    sumBefore += inputSize;
                    string outPath = BuildOutputPath(r.inPath, r.relativePath, outputRoot, formatExtension, preserveFullFolderStructure);
                    filteredRows.Add(new ConversionItem(r.inPath, outPath, inputSize));
                }

                totalFilesProcessing = filteredRows.Count;
                Interlocked.Exchange(ref totalBeforeSize, sumBefore);

                int requestedDegree = threads > 0 ? threads : Environment.ProcessorCount;
                int maxDegree = Math.Max(1, Math.Min(requestedDegree, Math.Max(1, filteredRows.Count)));
                ThreadPool.GetMinThreads(out int minWorker, out int minIO);
                int desiredMinWorkers = Math.Min(maxDegree + 2, Math.Max(Environment.ProcessorCount * 2, 8));
                if (minWorker < desiredMinWorkers)
                {
                    ThreadPool.SetMinThreads(desiredMinWorkers, minIO);
                }
                if (useLibvips)
                {
                    // Avoid oversubscription: let outer file-level parallelism drive throughput.
                    NetVips.NetVips.Concurrency = 1;
                }

                int channelCapacity = Math.Max(1, Math.Min(Math.Max(1, filteredRows.Count), maxDegree * 4));
                var channel = Channel.CreateBounded<ConversionItem>(
                    new BoundedChannelOptions(channelCapacity)
                    {
                        SingleWriter = true,
                        SingleReader = false,
                        FullMode = BoundedChannelFullMode.Wait
                    });

                // Start consumers
                var consumers = Enumerable.Range(0, maxDegree).Select(_ => Task.Run(async () =>
                {
                    await foreach (var item in channel.Reader.ReadAllAsync(cts.Token).ConfigureAwait(false))
                    {
                        try
                        {
                            if (useLibvips)
                            {
                                ProcessSingleFileLibvips(item.InputPath, item.OutputPath, item.InputSize, formatLocal, quality, multiplier, maxWidth, maxHeight, webpLossless, webpEffort, cts.Token);
                            }
                            else
                            {
                                ProcessSingleFile(item.InputPath, item.OutputPath, item.InputSize, formatLocal, quality, multiplier, maxWidth, maxHeight, cts.Token);
                            }
                        }
                        catch (OperationCanceledException) { break; }
                    }
                }, cts.Token)).ToArray();

                var producer = Task.Run(async () =>
                {
                    try
                    {
                        foreach (var file in filteredRows)
                        {
                            cts.Token.ThrowIfCancellationRequested();
                            await channel.Writer.WriteAsync(file, cts.Token).ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException) { }
                    finally
                    {
                        channel.Writer.TryComplete();
                    }
                }, cts.Token);

                await Task.WhenAll(consumers.Append(producer));

                sw.Stop();
                // ensure final UI update and stop timer
                elapsedTimeTimer?.Stop();
                uiUpdateTimer?.Stop();
                UpdateUI();
                try
                {
                    double finalElapsed = sw.Elapsed.TotalSeconds;
                    string elapsedText = finalElapsed > 0 ? finalElapsed.ToString("N2") + " s" : "0.00 s";
                    double finalFilesPerSec = finalElapsed > 0 ? comfileNum / finalElapsed : 0.0;

                    // Update summary labels in the main UI instead of showing a MessageBox
                    elapsedLabel.Text = elapsedText;
                    filesPerSecLabel.Text = finalFilesPerSec.ToString("N2") + " files/s";
                    buttonResize.Text = "Convert";
                }
                catch { }
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Operation was Cancelled.");
            }
            finally
            {
                sw.Stop();
                cts.Dispose();
            }
        }

        private static string GetFormatExtension(string formatLocal)
        {
            int slashIndex = formatLocal.LastIndexOf('/');
            return slashIndex >= 0 ? formatLocal[(slashIndex + 1)..] : formatLocal;
        }

        private static long GetFileLength(string path)
        {
            try { return new FileInfo(path).Length; }
            catch { return 0; }
        }

        private static string BuildOutputPath(string inPath, string relativePath, string outputRoot, string formatExtension, bool preserveFullFolderStructure)
        {
            if (preserveFullFolderStructure)
            {
                string root = Path.GetPathRoot(inPath) ?? string.Empty;
                string safeFullPath = !string.IsNullOrEmpty(root) && inPath.StartsWith(root, StringComparison.OrdinalIgnoreCase)
                    ? inPath[root.Length..]
                    : inPath;
                safeFullPath = safeFullPath.Replace(":", "");
                return Path.Combine(outputRoot, safeFullPath);
            }

            string relative = string.IsNullOrEmpty(relativePath) ? Path.GetFileName(inPath) : relativePath;
            string relativeDir = Path.GetDirectoryName(relative) ?? string.Empty;
            string newFileName = Path.GetFileNameWithoutExtension(relative) + "." + formatExtension;

            return string.IsNullOrEmpty(relativeDir)
                ? Path.Combine(outputRoot, newFileName)
                : Path.Combine(outputRoot, relativeDir, newFileName);
        }

        private static (int width, int height) CalculateTargetDimensions(int imgWidth, int imgHeight, decimal multiplier, int maxWidth, int maxHeight)
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

        private void ReportFailure(string filePath, string errorMessage)
        {
            try
            {
                if (IsDisposed || !IsHandleCreated)
                    return;

                if (InvokeRequired)
                    BeginInvoke((System.Windows.Forms.MethodInvoker)(() => AddFailedFile(filePath, errorMessage)));
                else
                    AddFailedFile(filePath, errorMessage);
            }
            catch { }
        }

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

        private static void TrySetOutputTimestamp(string outPath, DateTime desiredTime, CancellationToken token, bool retry)
        {
            int attempts = retry ? 8 : 1;
            for (int attempt = 0; attempt < attempts; attempt++)
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    File.SetLastWriteTime(outPath, desiredTime);
                    return;
                }
                catch (IOException) when (retry) { }
                catch (UnauthorizedAccessException) when (retry) { }
                catch { return; }

                Thread.Sleep(50);
            }
        }

        private static long GetOutputFileLength(string outPath, CancellationToken token, bool retry)
        {
            int attempts = retry ? 8 : 1;
            for (int attempt = 0; attempt < attempts; attempt++)
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    var fi = new FileInfo(outPath);
                    if (fi.Exists && fi.Length > 0)
                        return fi.Length;
                }
                catch { }

                if (retry)
                    Thread.Sleep(30);
            }

            try { return new FileInfo(outPath).Length; }
            catch { return 0; }
        }

        private void SaveIcoWithMagicScaler(string inPath, string outPath, CancellationToken token)
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

        private void ProcessSingleFile(string inPath, string outPath, long inputSize, string formatLocal, int quality, decimal multiplier, int maxWidth, int maxHeight, CancellationToken token)
        {
            if (string.IsNullOrEmpty(inPath))
                return;
            token.ThrowIfCancellationRequested();

            try
            {
                token.ThrowIfCancellationRequested();
                Interlocked.Add(ref processedInputBytes, inputSize);
                int imgWidth = 0, imgHeight = 0;

                try
                {
                    var info = ImageFileInfo.Load(inPath);
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
                        using var vipsInfo = NetVips.Image.NewFromFile(inPath, access: Enums.Access.Sequential);
                        imgWidth = vipsInfo.Width;
                        imgHeight = vipsInfo.Height;
                    }
                    catch { }
                }

                var (targetWidth, targetHeight) = CalculateTargetDimensions(imgWidth, imgHeight, multiplier, maxWidth, maxHeight);

                var resizeSettings = new ProcessImageSettings
                {
                    Width = targetWidth,
                    Height = targetHeight
                };

                // FORMAT-SPECIFIC ENCODER OPTIONS
                if (formatLocal == "image/webp")
                {
                    resizeSettings.EncoderOptions = quality >= 100 ? new WebpLosslessEncoderOptions() : new WebpLossyEncoderOptions { Quality = quality };
                }
                else if (formatLocal != "image/ico")
                {
                    resizeSettings.EncoderOptions = new LossyEncoderOptions { Quality = quality };
                }

                resizeSettings.TrySetEncoderFormat(formatLocal);

                // Write output
                try
                {
                    token.ThrowIfCancellationRequested();
                    EnsureOutputDirectory(outPath);

                    if (formatLocal == "image/ico")
                    {
                        SaveIcoWithMagicScaler(inPath, outPath, token);
                    }
                    else
                    {
                        using (var outStream = new FileStream(outPath, FileMode.Create, FileAccess.Write, FileShare.None, FileStreamBufferSize))
                        {
                            MagicImageProcessor.ProcessImage(inPath, outStream, resizeSettings);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ReportFailure(inPath, ex.Message + ": Error writing " + outPath);
                    return;
                }

                DateTime desiredTime = GetDesiredTimestamp(inPath, tryExifDateTaken: true);
                TrySetOutputTimestamp(outPath, desiredTime, token, retry: true);
                long added = GetOutputFileLength(outPath, token, retry: true);

                Interlocked.Increment(ref comfileNum);
                Interlocked.Add(ref afterSize, added);

                // UI updates handled by the periodic timer.
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                ReportFailure(inPath, ex.Message);
            }
        }

        private void ProcessSingleFileLibvips(string inPath, string outPath, long inputSize, string formatLocal, int quality, decimal multiplier, int maxWidth, int maxHeight, bool webpLossless, int webpEffort, CancellationToken token)
        {
            if (string.IsNullOrEmpty(inPath))
                return;
            token.ThrowIfCancellationRequested();

            string ext = GetFormatExtension(formatLocal).ToLowerInvariant();
            NetVips.Image? processedImage = null;
            bool disposeProcessed = false;
            try
            {
                token.ThrowIfCancellationRequested();
                // Try TurboJPEG fast-path for no-resize JPEG -> JPEG
                if ((ext == "jpg" || ext == "jpeg") &&
                    TryTurboJpegFastPath(inPath, outPath, quality, multiplier, maxWidth, maxHeight, out long fastInputSize, out long outputSize))
                {
                    try { Interlocked.Add(ref processedInputBytes, fastInputSize); } catch { }
                    Interlocked.Increment(ref comfileNum);
                    Interlocked.Add(ref afterSize, outputSize);
                    return;
                }

                Interlocked.Add(ref processedInputBytes, inputSize);

                // Load image header with NetVips - fast and avoids full decode
                using var image = NetVips.Image.NewFromFile(inPath, access: Enums.Access.Sequential);
                int imgWidth = image.Width;
                int imgHeight = image.Height;
                var (targetWidth, targetHeight) = CalculateTargetDimensions(imgWidth, imgHeight, multiplier, maxWidth, maxHeight);

                // Resize only if needed (use ThumbnailImage for fast decode+resize; downscale-only)
                if (targetWidth != imgWidth || targetHeight != imgHeight)
                {
                    token.ThrowIfCancellationRequested();
                    processedImage = image.ThumbnailImage(
                        targetWidth,
                        height: targetHeight,
                        size: Enums.Size.Down);
                    disposeProcessed = true;
                }
                else
                {
                    processedImage = image;
                }

                // Ensure output directory exists
                EnsureOutputDirectory(outPath);

                token.ThrowIfCancellationRequested();
                switch (ext.ToLower())
                {
                    case "webp":
                        processedImage.Webpsave(outPath, q: quality, lossless: webpLossless, effort: webpEffort);
                        break;
                    case "jpg":
                    case "jpeg":
                        processedImage.Jpegsave(outPath, q: quality);
                        break;
                    case "png":
                        processedImage.Pngsave(outPath, compression: 9);
                        break;
                    case "bmp":
                        processedImage.WriteToFile(outPath);
                        break;
                    case "tiff":
                    case "tif":
                        processedImage.Tiffsave(outPath);
                        break;
                    case "gif":
                        processedImage.Gifsave(outPath);
                        break;
                    default:
                        // Fallback to default write
                        processedImage.WriteToFile(outPath);
                        break;
                }

                // Preserve Date Taken metadata - simplified (no blocking retries)
                DateTime desiredTime = GetDesiredTimestamp(inPath, tryExifDateTaken: false);
                TrySetOutputTimestamp(outPath, desiredTime, token, retry: false);

                // Get file length - simplified (no blocking retries)
                long added = GetOutputFileLength(outPath, token, retry: false);

                Interlocked.Increment(ref comfileNum);
                Interlocked.Add(ref afterSize, added);

                // UI updates handled by the periodic timer.
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                ReportFailure(inPath, $"libvips error: {ex.Message}");
            }
            finally
            {
                if (disposeProcessed)
                    processedImage?.Dispose();
            }
        }






        public void Convert(string inPath, string outPath, ProcessImageSettings Settings, System.Drawing.Image myImage)
        {
            EnsureOutputDirectory(outPath);

            try
            {
                /*
                if (format == "image/avif")
                {
                    SaveAsAvif(inPath, outPath, Decimal.ToInt32(numericUpDownQuality.Value));
                }
                */
                if (format == "image/ico")
                {
                    SaveAsIco(myImage, outPath);
                }
                else
                {
                    using (var outStream = new FileStream(outPath, FileMode.Create, FileAccess.Write, FileShare.None, FileStreamBufferSize))
                    {
                        MagicImageProcessor.ProcessImage(inPath, outStream, Settings);
                    }
                }
            }
            catch (Exception ex)
            {
                Task.Run(() =>
                {
                    MessageBox.Show(ex.Message + ": Error writing " + outPath);
                });
            }

            DateTime desiredTime = TryGetExifDateTaken(myImage, out DateTime dateTaken)
                ? dateTaken
                : GetDesiredTimestamp(inPath, tryExifDateTaken: false);
            TrySetOutputTimestamp(outPath, desiredTime, CancellationToken.None, retry: false);

            // Update counters atomically; UI will be refreshed by the periodic updater.
            try
            {
                Interlocked.Increment(ref comfileNum);
                long len = 0;
                try { len = new FileInfo(outPath).Length; } catch { len = 0; }
                Interlocked.Add(ref afterSize, len);
            }
            catch { }

            //Thread updateCompletedLabelThread = new Thread(() => updateCompletedLabel(comfileNum));
            //updateCompletedLabelThread.Start();

            t--;
            //Dispose();
            myImage.Dispose();
        }

        private async void update_Click(object sender, EventArgs e)
        {
            if (pendingUpdate is null)
                return;

            var appDir = Path.GetDirectoryName(Application.ExecutablePath) ?? Environment.CurrentDirectory;
            string updatedPath = Path.Combine(appDir, "Resizer3Updated.exe");
            Version localVersion = GetFileVersion(Application.ExecutablePath);
            update.Enabled = false;

            try
            {
                if (File.Exists(updatedPath))
                    File.Delete(updatedPath);

                await GitHubReleaseUpdateService.DownloadAndVerifyAsync(pendingUpdate, updatedPath, localVersion);

                var startInfo = new ProcessStartInfo
                {
                    FileName = updatedPath,
                    UseShellExecute = false
                };
                startInfo.ArgumentList.Add("--update");
                startInfo.ArgumentList.Add("--pid");
                startInfo.ArgumentList.Add(Environment.ProcessId.ToString(CultureInfo.InvariantCulture));
                startInfo.ArgumentList.Add("--source");
                startInfo.ArgumentList.Add(updatedPath);
                startInfo.ArgumentList.Add("--target");
                startInfo.ArgumentList.Add(Application.ExecutablePath);
                Process.Start(startInfo);

                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Update failed: " + ex.Message);
                update.Enabled = pendingUpdate is not null;
            }
        }

        private void UpdateElapsedTime()
        {
            try
            {
                double elapsed = sw.Elapsed.TotalSeconds;
                string elapsedText = elapsed > 0 ? elapsed.ToString("N2") + " s" : "0.00 s";
                elapsedLabel.Text = elapsedText;
            }
            catch { }
        }

        private void UpdateUI()
        {
            if (InvokeRequired)
            {
                BeginInvoke((System.Windows.Forms.MethodInvoker)delegate { UpdateUI(); });
                return;
            }
            try
            {
                var completed = Interlocked.CompareExchange(ref comfileNum, 0, 0);
                var size = Interlocked.Read(ref afterSize);
                double elapsed = sw.Elapsed.TotalSeconds;
                double filesPerSec = elapsed > 0 ? completed / elapsed : 0.0;
                double mbSec = elapsed > 0 ? (Interlocked.Read(ref processedInputBytes) / 1024.0 / 1024.0) / elapsed : 0.0;
                double inBytes = Interlocked.Read(ref totalBeforeSize);
                double saved = inBytes > 0 ? 100.0 * (1.0 - (double)size / inBytes) : 0.0;
                int pct = totalFilesProcessing > 0 ? (int)(completed * 100L / totalFilesProcessing) : 0;
                completedFiles.Text = completed.ToString();
                afterSizeLabel.Text = ((size / 1024.000 / 1024.000).ToString("N3") + " MB");
                filesPerSecLabel.Text = filesPerSec.ToString("N2") + " files/s";
                mbPerSecLabel.Text = mbSec.ToString("N2") + " MB/s";
                percentSavedLabel.Text = saved.ToString("N2") + "%";
                processingProgress.Value = Math.Max(0, Math.Min(100, pct));
                if (isProcessing)
                {
                    buttonResize.Text = $"Converting... {pct}%";
                }
            }
            catch { }
        }
        CancellationTokenSource cts = new();
        private void cancelButton_Click(object sender, EventArgs e)
        {
            if (!isProcessing)
                return;
            try
            {
                if (!cts.IsCancellationRequested)
                    cts.Cancel();
                buttonResize.Text = "Cancelling...";
                cancelButton.Enabled = false;
            }
            catch (ObjectDisposedException) { }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = textBox1.Text,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open output folder: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                // Stop timers early to reduce shutdown-time surprises.
                try { uiUpdateTimer?.Stop(); } catch (Exception ex) { AppLog.TryLog("Form1_FormClosing.uiUpdateTimer.Stop", ex); }
                try { uiUpdateTimer?.Dispose(); } catch (Exception ex) { AppLog.TryLog("Form1_FormClosing.uiUpdateTimer.Dispose", ex); }
                uiUpdateTimer = null;

                try { elapsedTimeTimer?.Stop(); } catch (Exception ex) { AppLog.TryLog("Form1_FormClosing.elapsedTimeTimer.Stop", ex); }
                try { elapsedTimeTimer?.Dispose(); } catch (Exception ex) { AppLog.TryLog("Form1_FormClosing.elapsedTimeTimer.Dispose", ex); }
                elapsedTimeTimer = null;

                var settings = new AppSettings
                {
                    SaveLocation = textBox1.Text,
                    ThreadsNumber = numericUpDownThreads.Value,
                    Quality = numericUpDownQuality.Value,
                    Resolution = (decimal)numericUpDownRes.Value,
                    Format = comboBox1.SelectedItem as string,
                    MaxRes = maxResComboBox.SelectedItem as string,
                    WebpEffort = Decimal.ToInt32(numericUpDownWebpEffort.Value)
                };
                _ = settings.TrySave();
            }
            catch (Exception ex)
            {
                // Never let settings persistence crash shutdown.
                AppLog.TryLog("Form1_FormClosing", ex);
            }
        }


        private void textBox1_DoubleClick(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    textBox1.Text = dialog.SelectedPath;
                }
            }
        }

        private int fileSizeRefreshVersion;

        private async void dataGridView1_Changed()
        {
            // Compute file sizes off the UI thread to avoid blocking the UI for large lists.
            int refreshVersion = Interlocked.Increment(ref fileSizeRefreshVersion);
            var paths = new List<string>();
            for (int i = 0; i < dataGridView1.Rows.Count; i++)
            {
                var val = dataGridView1.Rows[i].Cells[0].Value;
                var s = val?.ToString();
                if (!string.IsNullOrEmpty(s))
                    paths.Add(s);
            }
            int visibleFileCount = paths.Count;

            long beforeSize = await Task.Run(() =>
            {
                long sum = 0;
                foreach (var p in paths)
                {
                    sum += GetFileLength(p);
                }
                return sum;
            });

            if (refreshVersion != Volatile.Read(ref fileSizeRefreshVersion) || IsDisposed || !IsHandleCreated)
                return;

            fileNum = visibleFileCount;
            try
            {
                BeginInvoke((System.Windows.Forms.MethodInvoker)(() =>
                {
                    if (refreshVersion != Volatile.Read(ref fileSizeRefreshVersion))
                        return;

                    numFiles.Text = fileNum.ToString();
                    beforeSizeLabel.Text = ((beforeSize / 1024.000 / 1024.000).ToString("N3") + " MB");
                }));
            }
            catch { }
        }

        private void dataGridView1_MouseLeave(object sender, EventArgs e)
        {
            dataGridView1_Changed();
        }

        private void dataGridViewFailed_DoubleClick(object sender, EventArgs e)
        {
            if (dataGridViewFailed.CurrentRow == null)
                return;

            var cellValue = dataGridViewFailed.CurrentRow.Cells[0].Value;
            if (cellValue == null)
                return;

            string filePath = cellValue.ToString() ?? string.Empty;
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selected = comboBox1.SelectedItem?.ToString()?.ToLower() ?? "";

            // Disable quality for formats without quality settings
            if (selected == "png" || selected == "bmp" || selected == "tiff" || selected == "tif" || selected == "ico" || selected == "gif")
            {
                numericUpDownQuality.Enabled = false;
            }
            else
            {
                numericUpDownQuality.Enabled = true;
            }
            UpdateWebpLibvipsOptionsVisibility();
        }

        private void maxResComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selected = maxResComboBox.SelectedItem?.ToString() ?? "Off";
            if (selected != "Off")
            {
                numericUpDownRes.Enabled = false;
            }
            else
            {
                numericUpDownRes.Enabled = true;
            }
        }

        private void UpdateWebpLibvipsOptionsVisibility()
        {
            bool show = (comboBox1.SelectedItem?.ToString()?.ToLower() == "webp");
            labelWebpEffort.Visible = show;
            numericUpDownWebpEffort.Visible = show;
        }

        private static decimal ClampNumeric(int value, decimal min, decimal max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private bool TryTurboJpegFastPath(string inPath, string outPath, int quality, decimal multiplier, int maxWidth, int maxHeight, out long inputSize, out long outputSize)
        {
            inputSize = 0;
            outputSize = 0;
            if (!TurboJpegNative.IsAvailable)
                return false;

            if (multiplier != 1m && maxWidth == int.MaxValue)
                return false;

            byte[] jpegData;
            try
            {
                jpegData = File.ReadAllBytes(inPath);
            }
            catch
            {
                return false;
            }

            inputSize = jpegData.LongLength;

            if (!TurboJpegNative.TryGetHeader(jpegData, out int width, out int height, out int subsamp, out int colorspace))
                return false;

            bool noResize = maxWidth == int.MaxValue
                ? multiplier == 1m
                : (width <= maxWidth && height <= maxHeight);
            if (!noResize)
                return false;

            byte[]? icc = JpegIcc.TryExtract(jpegData);

            if (!TurboJpegNative.TryTranscode(jpegData, width, height, subsamp, colorspace, quality, out byte[] output))
                return false;

            if (icc != null && icc.Length > 0)
            {
                if (!JpegIcc.TryInsert(output, icc, out byte[] withIcc))
                    return false;
                output = withIcc;
            }

            if (output.Length == 0)
                return false;

            EnsureOutputDirectory(outPath);

            File.WriteAllBytes(outPath, output);
            outputSize = output.LongLength;

            DateTime desiredTime = File.GetLastWriteTime(inPath);
            try { File.SetLastWriteTime(outPath, desiredTime); } catch { }

            return true;
        }

        private static class TurboJpegNative
        {
            private const string DllName = "turbojpeg";
            private const int TJCS_GRAY = 2;
            private const int TJCS_CMYK = 3;
            private const int TJCS_YCCK = 4;

            private const int TJPF_RGB = 0;
            private const int TJPF_GRAY = 6;

            private const int TJSAMP_GRAY = 3;

            private static readonly ThreadLocal<IntPtr> s_decompress = new(() => IntPtr.Zero);
            private static readonly ThreadLocal<IntPtr> s_compress = new(() => IntPtr.Zero);

            internal static readonly bool IsAvailable = CheckAvailable();

            [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
            private static extern IntPtr tjInitDecompress();

            [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
            private static extern IntPtr tjInitCompress();

            [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
            private static extern int tjDestroy(IntPtr handle);

            [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
            private static extern int tjDecompressHeader3(IntPtr handle, byte[] jpegBuf, int jpegSize, out int width, out int height, out int subsamp, out int colorspace);

            [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
            private static extern int tjDecompress2(IntPtr handle, byte[] jpegBuf, int jpegSize, IntPtr dstBuf, int width, int pitch, int height, int pixelFormat, int flags);

            [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
            private static extern int tjCompress2(IntPtr handle, IntPtr srcBuf, int width, int pitch, int height, int pixelFormat, ref IntPtr jpegBuf, ref uint jpegSize, int jpegSubsamp, int jpegQual, int flags);

            [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
            private static extern int tjDecompressToYUV2(IntPtr handle, byte[] jpegBuf, int jpegSize, IntPtr dstBuf, int width, int padding, int height, int flags);

            [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
            private static extern int tjCompressFromYUV(IntPtr handle, IntPtr srcBuf, int width, int padding, int height, int jpegSubsamp, ref IntPtr jpegBuf, ref uint jpegSize, int jpegQual, int flags);

            [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
            private static extern ulong tjBufSizeYUV2(int width, int padding, int height, int jpegSubsamp);

            [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
            private static extern IntPtr tjAlloc(nint bytes);

            [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
            private static extern void tjFree(IntPtr buffer);

            private static bool CheckAvailable()
            {
                try
                {
                    IntPtr handle = tjInitDecompress();
                    if (handle == IntPtr.Zero)
                        return false;
                    tjDestroy(handle);
                    return true;
                }
                catch (DllNotFoundException)
                {
                    return false;
                }
                catch (EntryPointNotFoundException)
                {
                    return false;
                }
                catch (BadImageFormatException)
                {
                    return false;
                }
            }

            private static IntPtr GetDecompressHandle()
            {
                if (!IsAvailable)
                    return IntPtr.Zero;
                IntPtr handle = s_decompress.Value;
                if (handle == IntPtr.Zero)
                {
                    handle = tjInitDecompress();
                    s_decompress.Value = handle;
                }
                return handle;
            }

            private static IntPtr GetCompressHandle()
            {
                if (!IsAvailable)
                    return IntPtr.Zero;
                IntPtr handle = s_compress.Value;
                if (handle == IntPtr.Zero)
                {
                    handle = tjInitCompress();
                    s_compress.Value = handle;
                }
                return handle;
            }

            internal static bool TryGetHeader(byte[] jpegData, out int width, out int height, out int subsamp, out int colorspace)
            {
                width = 0;
                height = 0;
                subsamp = 0;
                colorspace = 0;
                IntPtr handle = GetDecompressHandle();
                if (handle == IntPtr.Zero)
                    return false;
                return tjDecompressHeader3(handle, jpegData, jpegData.Length, out width, out height, out subsamp, out colorspace) == 0;
            }

            internal static bool TryTranscode(byte[] jpegData, int width, int height, int subsamp, int colorspace, int quality, out byte[] output)
            {
                if (TryTranscodeYuv(jpegData, width, height, subsamp, colorspace, quality, out output))
                    return true;
                return TryTranscodeRgb(jpegData, width, height, subsamp, colorspace, quality, out output);
            }

            private static bool TryTranscodeYuv(byte[] jpegData, int width, int height, int subsamp, int colorspace, int quality, out byte[] output)
            {
                output = Array.Empty<byte>();
                try
                {
                    IntPtr dec = GetDecompressHandle();
                    IntPtr comp = GetCompressHandle();
                    if (dec == IntPtr.Zero || comp == IntPtr.Zero)
                        return false;

                    if (colorspace == TJCS_CMYK || colorspace == TJCS_YCCK)
                        return false;

                    const int padding = 1;
                    ulong yuvSize = tjBufSizeYUV2(width, padding, height, subsamp);
                    if (yuvSize == 0 || yuvSize > int.MaxValue)
                        return false;

                    IntPtr yuvBuf = tjAlloc((nint)yuvSize);
                    if (yuvBuf == IntPtr.Zero)
                        return false;

                    try
                    {
                        if (tjDecompressToYUV2(dec, jpegData, jpegData.Length, yuvBuf, width, padding, height, 0) != 0)
                            return false;

                        IntPtr outBuf = IntPtr.Zero;
                        uint outSize = 0;
                        if (tjCompressFromYUV(comp, yuvBuf, width, padding, height, subsamp, ref outBuf, ref outSize, quality, 0) != 0)
                            return false;

                        try
                        {
                            if (outSize == 0 || outSize > int.MaxValue)
                                return false;
                            byte[] managed = new byte[outSize];
                            Marshal.Copy(outBuf, managed, 0, (int)outSize);
                            output = managed;
                            return true;
                        }
                        finally
                        {
                            if (outBuf != IntPtr.Zero)
                                tjFree(outBuf);
                        }
                    }
                    finally
                    {
                        tjFree(yuvBuf);
                    }
                }
                catch (EntryPointNotFoundException)
                {
                    return false;
                }
                catch (DllNotFoundException)
                {
                    return false;
                }
                catch (BadImageFormatException)
                {
                    return false;
                }
            }

            private static bool TryTranscodeRgb(byte[] jpegData, int width, int height, int subsamp, int colorspace, int quality, out byte[] output)
            {
                output = Array.Empty<byte>();
                IntPtr dec = GetDecompressHandle();
                IntPtr comp = GetCompressHandle();
                if (dec == IntPtr.Zero || comp == IntPtr.Zero)
                    return false;

                if (colorspace == TJCS_CMYK || colorspace == TJCS_YCCK)
                    return false;

                int pixelFormat = colorspace == TJCS_GRAY ? TJPF_GRAY : TJPF_RGB;
                int channels = colorspace == TJCS_GRAY ? 1 : 3;
                long bufSize = (long)width * height * channels;
                if (bufSize <= 0 || bufSize > int.MaxValue)
                    return false;

                IntPtr rgbBuf = tjAlloc((nint)bufSize);
                if (rgbBuf == IntPtr.Zero)
                    return false;

                try
                {
                    int pitch = width * channels;
                    if (tjDecompress2(dec, jpegData, jpegData.Length, rgbBuf, width, pitch, height, pixelFormat, 0) != 0)
                        return false;

                    IntPtr outBuf = IntPtr.Zero;
                    uint outSize = 0;
                    int outSubsamp = colorspace == TJCS_GRAY ? TJSAMP_GRAY : subsamp;
                    if (tjCompress2(comp, rgbBuf, width, pitch, height, pixelFormat, ref outBuf, ref outSize, outSubsamp, quality, 0) != 0)
                        return false;

                    try
                    {
                        if (outSize == 0 || outSize > int.MaxValue)
                            return false;
                        byte[] managed = new byte[outSize];
                        Marshal.Copy(outBuf, managed, 0, (int)outSize);
                        output = managed;
                        return true;
                    }
                    finally
                    {
                        if (outBuf != IntPtr.Zero)
                            tjFree(outBuf);
                    }
                }
                finally
                {
                    tjFree(rgbBuf);
                }
            }
        }

        private static class JpegIcc
        {
            private const byte MarkerPrefix = 0xFF;
            private const byte SOI = 0xD8;
            private const byte SOS = 0xDA;
            private const byte EOI = 0xD9;
            private const byte APP2 = 0xE2;
            private const int IccOverheadLen = 14;
            private const int IccMaxBytesInMarker = 65533 - IccOverheadLen;
            private static readonly byte[] IccId = Encoding.ASCII.GetBytes("ICC_PROFILE");

            internal static byte[]? TryExtract(byte[] jpeg)
            {
                if (jpeg.Length < 4 || jpeg[0] != MarkerPrefix || jpeg[1] != SOI)
                    return null;

                int index = 2;
                int? totalSegments = null;
                var segments = new Dictionary<int, byte[]>();

                while (index + 1 < jpeg.Length)
                {
                    if (jpeg[index] != MarkerPrefix)
                    {
                        index++;
                        continue;
                    }

                    while (index < jpeg.Length && jpeg[index] == MarkerPrefix)
                        index++;
                    if (index >= jpeg.Length)
                        break;

                    byte marker = jpeg[index++];
                    if (marker == SOS || marker == EOI)
                        break;

                    if (index + 1 >= jpeg.Length)
                        break;

                    int length = (jpeg[index] << 8) | jpeg[index + 1];
                    if (length < 2 || index + length > jpeg.Length)
                        break;

                    int payloadStart = index + 2;
                    int payloadLength = length - 2;

                    if (marker == APP2 && payloadLength >= IccOverheadLen)
                    {
                        if (IsIccMarker(jpeg, payloadStart, payloadLength, out int seq, out int total))
                        {
                            int dataOffset = payloadStart + IccOverheadLen;
                            int dataLen = payloadLength - IccOverheadLen;
                            if (dataLen > 0)
                            {
                                byte[] chunk = new byte[dataLen];
                                Buffer.BlockCopy(jpeg, dataOffset, chunk, 0, dataLen);
                                segments[seq] = chunk;
                                totalSegments ??= total;
                            }
                        }
                    }

                    index += length;
                }

                if (segments.Count == 0 || totalSegments is null)
                    return null;

                int totalCount = totalSegments.Value;
                for (int i = 1; i <= totalCount; i++)
                {
                    if (!segments.ContainsKey(i))
                        return null;
                }

                int totalSize = segments.Values.Sum(s => s.Length);
                byte[] icc = new byte[totalSize];
                int offset = 0;
                for (int i = 1; i <= totalCount; i++)
                {
                    byte[] chunk = segments[i];
                    Buffer.BlockCopy(chunk, 0, icc, offset, chunk.Length);
                    offset += chunk.Length;
                }

                return icc;
            }

            internal static bool TryInsert(byte[] jpeg, byte[] icc, out byte[] output)
            {
                output = Array.Empty<byte>();
                if (jpeg.Length < 2 || icc.Length == 0)
                    return false;
                if (jpeg[0] != MarkerPrefix || jpeg[1] != SOI)
                    return false;

                int numSegments = (icc.Length + IccMaxBytesInMarker - 1) / IccMaxBytesInMarker;
                int extra = numSegments * (2 + 2 + IccOverheadLen) + icc.Length;
                int newSize = jpeg.Length + extra;
                if (newSize <= 0)
                    return false;

                byte[] outBuf = new byte[newSize];
                int insertPos = 2;
                Buffer.BlockCopy(jpeg, 0, outBuf, 0, insertPos);
                int outPos = insertPos;
                int offset = 0;

                for (int seg = 0; seg < numSegments; seg++)
                {
                    int chunk = icc.Length - offset;
                    if (chunk > IccMaxBytesInMarker)
                        chunk = IccMaxBytesInMarker;

                    outBuf[outPos++] = MarkerPrefix;
                    outBuf[outPos++] = APP2;
                    int length = IccOverheadLen + chunk + 2;
                    outBuf[outPos++] = (byte)((length >> 8) & 0xFF);
                    outBuf[outPos++] = (byte)(length & 0xFF);

                    Buffer.BlockCopy(IccId, 0, outBuf, outPos, IccId.Length);
                    outPos += IccId.Length;
                    outBuf[outPos++] = 0;
                    outBuf[outPos++] = (byte)(seg + 1);
                    outBuf[outPos++] = (byte)numSegments;

                    Buffer.BlockCopy(icc, offset, outBuf, outPos, chunk);
                    outPos += chunk;
                    offset += chunk;
                }

                Buffer.BlockCopy(jpeg, insertPos, outBuf, outPos, jpeg.Length - insertPos);
                output = outBuf;
                return true;
            }

            private static bool IsIccMarker(byte[] data, int start, int length, out int seq, out int total)
            {
                seq = 0;
                total = 0;
                if (length < IccOverheadLen)
                    return false;

                for (int i = 0; i < IccId.Length; i++)
                {
                    if (data[start + i] != IccId[i])
                        return false;
                }
                if (data[start + IccId.Length] != 0)
                    return false;

                seq = data[start + IccId.Length + 1];
                total = data[start + IccId.Length + 2];
                if (seq <= 0 || total <= 0)
                    return false;
                return true;
            }
        }

        private void clearListButton_Click(object sender, EventArgs e)
        {
            dataGridView1.Rows.Clear();
            dataGridViewFailed.Rows.Clear();
            numFiles.Text = "0";
            completedFiles.Text = "0";
            beforeSizeLabel.Text = "0 MB";
            fileNum = 0;

            // Remove Failed Files tab
            tabControl.TabPages.Remove(tabPageFailedFiles);

            // Switch back to All Files tab
            tabControl.SelectedTab = tabPageAllFiles;
        }

        private void optimizedButton_Click(object sender, EventArgs e)
        {
            numericUpDownQuality.Value = 85;
            maxResComboBox.SelectedItem = "4K (2160p)";
            comboBox1.SelectedItem = "webp";
            numericUpDownRes.Enabled = false;
            numericUpDownWebpEffort.Value = 2;
        }

        private (int maxWidth, int maxHeight) GetMaxResolutionDimensions()
        {
            string selected = maxResComboBox.SelectedItem?.ToString() ?? "Off";
            return selected switch
            {
                "4K (2160p)" => (3840, 2160),
                "1440p" => (2560, 1440),
                "1080p" => (1920, 1080),
                _ => (int.MaxValue, int.MaxValue)
            };
        }
    }
}
