using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Resizer3
{
    /// <summary>TurboJPEG 2.x API bindings. C <c>unsigned long</c> is 32-bit on Windows, hence <c>uint</c>.</summary>
    internal static class TurboJpegNative
    {
        private const string DllName = "turbojpeg";
        internal const int TJCS_RGB = 0;
        internal const int TJCS_GRAY = 2;
        internal const int TJCS_CMYK = 3;
        internal const int TJCS_YCCK = 4;

        internal const int TJPF_RGB = 0;
        internal const int TJPF_GRAY = 6;

        internal const int TJSAMP_GRAY = 3;

        internal const int TJERR_WARNING = 0;

        // Abort a decode at the first libjpeg warning (e.g. premature end of data) instead of finishing it.
        internal const int TJFLAG_STOPONWARNING = 8192;

        internal static readonly bool IsAvailable = CheckAvailable();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr tjInitDecompress();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr tjInitCompress();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int tjDestroy(IntPtr handle);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int tjGetErrorCode(IntPtr handle);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int tjDecompressHeader3(IntPtr handle, byte[] jpegBuf, uint jpegSize, out int width, out int height, out int subsamp, out int colorspace);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int tjDecompress2(IntPtr handle, byte[] jpegBuf, uint jpegSize, IntPtr dstBuf, int width, int pitch, int height, int pixelFormat, int flags);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int tjCompress2(IntPtr handle, IntPtr srcBuf, int width, int pitch, int height, int pixelFormat, ref IntPtr jpegBuf, ref uint jpegSize, int jpegSubsamp, int jpegQual, int flags);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int tjDecompressToYUV2(IntPtr handle, byte[] jpegBuf, uint jpegSize, IntPtr dstBuf, int width, int padding, int height, int flags);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int tjCompressFromYUV(IntPtr handle, IntPtr srcBuf, int width, int padding, int height, int jpegSubsamp, ref IntPtr jpegBuf, ref uint jpegSize, int jpegQual, int flags);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint tjBufSizeYUV2(int width, int padding, int height, int jpegSubsamp);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void tjFree(IntPtr buffer);

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
    }

    /// <summary>
    /// One worker's TurboJPEG state: codec handles plus input and scratch buffers reused across files, so JPEG
    /// batches do not churn large allocations. Not thread-safe; dispose when the worker finishes.
    /// </summary>
    internal sealed unsafe class TurboJpegSession : IDisposable
    {
        private const int HeaderPrefixBytes = 128 * 1024;
        private const int YuvPadding = 1;

        // Buffers above this size are freed after each file, so one huge image does not pin memory for the batch.
        private const int RetainedBufferBytes = 64 << 20;

        // Keeps every scratch size well inside 32 bits; tjBufSizeYUV2 returns a 32-bit value on Windows and
        // tjDecompressToYUV2 takes no buffer size, so a header claiming a huge image must never get this far.
        private const long MaxPixels = int.MaxValue / 4;

        private IntPtr decompressor;
        private IntPtr compressor;
        private byte[] input = Array.Empty<byte>();
        private void* scratch;
        private nuint scratchBytes;
        private readonly List<JpegSegment> metadata = new();

        /// <summary>
        /// Re-encodes a JPEG at <paramref name="quality"/> into <paramref name="outPath"/>, keeping its EXIF, XMP,
        /// ICC and IPTC segments and its last-write time. Returns false without creating the output when the input
        /// is not a JPEG, when <paramref name="keepsSize"/> rejects its display size, or when TurboJPEG cannot
        /// decode it cleanly (any libjpeg warning, such as truncated data, leaves the file to libvips to report).
        /// </summary>
        internal bool TryTranscode(string inPath, string outPath, int quality, Func<int, int, bool> keepsSize, out long outputBytes)
        {
            try
            {
                return TryTranscodeCore(inPath, outPath, quality, keepsSize, out outputBytes);
            }
            finally
            {
                if (input.Length > RetainedBufferBytes)
                    input = Array.Empty<byte>();
                if (scratchBytes > RetainedBufferBytes)
                    FreeScratch();
            }
        }

        private bool TryTranscodeCore(string inPath, string outPath, int quality, Func<int, int, bool> keepsSize, out long outputBytes)
        {
            outputBytes = 0;
            using var source = File.OpenHandle(inPath, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.SequentialScan);
            long fileLength = RandomAccess.GetLength(source);
            if (fileLength < 4 || fileLength > int.MaxValue)
                return false;

            int length = (int)fileLength;
            if (input.Length < length)
                input = GC.AllocateUninitializedArray<byte>((int)Math.Min(Array.MaxLength, Math.Max(1L << 20, length + (long)length / 4)));

            // Parse the header from a small prefix so images that need resizing are not read twice.
            int filled = ReadAt(source, 0, Math.Min(length, HeaderPrefixBytes));
            if (filled < 4 || input[0] != 0xFF || input[1] != 0xD8)
                return false;

            if (!EnsureHandles())
                return false;

            if (!TryReadHeader(filled, out int width, out int height, out int subsamp, out int colorspace))
            {
                // SOF can sit past large EXIF/ICC segments.
                if (filled == length)
                    return false;
                filled += ReadAt(source, filled, length - filled);
                if (filled != length || !TryReadHeader(filled, out width, out height, out subsamp, out colorspace))
                    return false;
            }

            if (colorspace == TurboJpegNative.TJCS_CMYK || colorspace == TurboJpegNative.TJCS_YCCK || subsamp < 0
                || width <= 0 || height <= 0 || (long)width * height > MaxPixels)
                return false;

            // A successful header parse means every segment before the first scan is in the buffer.
            int orientation = JpegMetadata.Collect(input.AsSpan(0, filled), metadata);
            var (displayWidth, displayHeight) = ConversionWorker.GetDisplaySize(width, height, orientation);
            if (!keepsSize(displayWidth, displayHeight))
                return false;

            if (filled < length)
            {
                filled += ReadAt(source, filled, length - filled);
                if (filled != length)
                    return false;
            }

            DateTime lastWriteTimeUtc = File.GetLastWriteTimeUtc(source);

            // Raw YUV planes of an RGB-coded JPEG would be re-encoded as YCbCr with wrong colors, so it goes via RGB.
            IntPtr encoded = IntPtr.Zero;
            uint encodedSize = 0;
            bool encodedOk = false, warning = false;
            if (colorspace != TurboJpegNative.TJCS_RGB)
                encodedOk = TryEncodeViaYuv(length, width, height, subsamp, quality, out encoded, out encodedSize, out warning);
            if (!encodedOk && !warning)
                encodedOk = TryEncodeViaRgb(length, width, height, subsamp, colorspace, quality, out encoded, out encodedSize);
            if (!encodedOk)
                return false;

            try
            {
                outputBytes = WriteOutput(outPath, new ReadOnlySpan<byte>((void*)encoded, (int)encodedSize), lastWriteTimeUtc);
                return true;
            }
            finally
            {
                TurboJpegNative.tjFree(encoded);
            }
        }

        public void Dispose()
        {
            if (decompressor != IntPtr.Zero)
            {
                TurboJpegNative.tjDestroy(decompressor);
                decompressor = IntPtr.Zero;
            }
            if (compressor != IntPtr.Zero)
            {
                TurboJpegNative.tjDestroy(compressor);
                compressor = IntPtr.Zero;
            }
            FreeScratch();
            input = Array.Empty<byte>();
        }

        private void FreeScratch()
        {
            NativeMemory.Free(scratch);
            scratch = null;
            scratchBytes = 0;
        }

        private bool EnsureHandles()
        {
            if (decompressor == IntPtr.Zero)
                decompressor = TurboJpegNative.tjInitDecompress();
            if (compressor == IntPtr.Zero)
                compressor = TurboJpegNative.tjInitCompress();
            return decompressor != IntPtr.Zero && compressor != IntPtr.Zero;
        }

        private bool TryReadHeader(int length, out int width, out int height, out int subsamp, out int colorspace)
            => TurboJpegNative.tjDecompressHeader3(decompressor, input, (uint)length, out width, out height, out subsamp, out colorspace) == 0;

        private int ReadAt(SafeFileHandle handle, int offset, int count)
        {
            int total = 0;
            while (total < count)
            {
                int read = RandomAccess.Read(handle, input.AsSpan(offset + total, count - total), offset + total);
                if (read == 0)
                    break;
                total += read;
            }
            return total;
        }

        private void EnsureScratch(nuint bytes)
        {
            if (bytes <= scratchBytes)
                return;
            FreeScratch();
            scratch = NativeMemory.Alloc(bytes);
            scratchBytes = bytes;
        }

        private bool TryEncodeViaYuv(int length, int width, int height, int subsamp, int quality, out IntPtr jpeg, out uint jpegSize, out bool warning)
        {
            jpeg = IntPtr.Zero;
            jpegSize = 0;
            warning = false;

            uint yuvSize = TurboJpegNative.tjBufSizeYUV2(width, YuvPadding, height, subsamp);
            if (yuvSize == 0 || yuvSize == uint.MaxValue)
                return false;
            EnsureScratch(yuvSize);

            if (TurboJpegNative.tjDecompressToYUV2(decompressor, input, (uint)length, (IntPtr)scratch, width, YuvPadding, height, TurboJpegNative.TJFLAG_STOPONWARNING) != 0)
            {
                // A warning (e.g. truncated data) would recur in the RGB path.
                warning = TurboJpegNative.tjGetErrorCode(decompressor) == TurboJpegNative.TJERR_WARNING;
                return false;
            }

            IntPtr buffer = IntPtr.Zero;
            uint size = 0;
            if (TurboJpegNative.tjCompressFromYUV(compressor, (IntPtr)scratch, width, YuvPadding, height, subsamp, ref buffer, ref size, quality, 0) != 0
                || size == 0 || size > int.MaxValue)
            {
                if (buffer != IntPtr.Zero)
                    TurboJpegNative.tjFree(buffer);
                return false;
            }

            jpeg = buffer;
            jpegSize = size;
            return true;
        }

        private bool TryEncodeViaRgb(int length, int width, int height, int subsamp, int colorspace, int quality, out IntPtr jpeg, out uint jpegSize)
        {
            jpeg = IntPtr.Zero;
            jpegSize = 0;

            bool gray = colorspace == TurboJpegNative.TJCS_GRAY;
            int pixelFormat = gray ? TurboJpegNative.TJPF_GRAY : TurboJpegNative.TJPF_RGB;
            int channels = gray ? 1 : 3;
            long bufferSize = (long)width * height * channels;
            if (bufferSize <= 0 || bufferSize > int.MaxValue)
                return false;
            EnsureScratch((nuint)bufferSize);

            int pitch = width * channels;
            if (TurboJpegNative.tjDecompress2(decompressor, input, (uint)length, (IntPtr)scratch, width, pitch, height, pixelFormat, TurboJpegNative.TJFLAG_STOPONWARNING) != 0)
                return false;

            IntPtr buffer = IntPtr.Zero;
            uint size = 0;
            int outSubsamp = gray ? TurboJpegNative.TJSAMP_GRAY : subsamp;
            if (TurboJpegNative.tjCompress2(compressor, (IntPtr)scratch, width, pitch, height, pixelFormat, ref buffer, ref size, outSubsamp, quality, 0) != 0
                || size == 0 || size > int.MaxValue)
            {
                if (buffer != IntPtr.Zero)
                    TurboJpegNative.tjFree(buffer);
                return false;
            }

            jpeg = buffer;
            jpegSize = size;
            return true;
        }

        /// <summary>Writes the encoded JPEG with the source's metadata segments placed after its JFIF header.</summary>
        private long WriteOutput(string outPath, ReadOnlySpan<byte> encoded, DateTime lastWriteTimeUtc)
        {
            int insertAt = JpegMetadata.GetInsertPosition(encoded);
            long total = encoded.Length;
            foreach (var segment in metadata)
                total += segment.Length;

            using var output = File.OpenHandle(outPath, FileMode.Create, FileAccess.Write, FileShare.None, FileOptions.None, preallocationSize: total);
            long position = 0;
            RandomAccess.Write(output, encoded[..insertAt], position);
            position += insertAt;
            foreach (var segment in metadata)
            {
                RandomAccess.Write(output, input.AsSpan(segment.Start, segment.Length), position);
                position += segment.Length;
            }
            RandomAccess.Write(output, encoded[insertAt..], position);
            position += encoded.Length - insertAt;

            try { File.SetLastWriteTimeUtc(output, lastWriteTimeUtc); }
            catch { }
            return position;
        }
    }

    /// <summary>A whole JPEG marker segment (marker, length and payload) inside a buffer.</summary>
    internal readonly record struct JpegSegment(int Start, int Length);

    internal static class JpegMetadata
    {
        private const byte APP1 = 0xE1;
        private const byte APP2 = 0xE2;
        private const byte APP13 = 0xED;

        private static ReadOnlySpan<byte> ExifId => "Exif\0\0"u8;
        private static ReadOnlySpan<byte> XmpId => "http://ns.adobe.com/xap/1.0/\0"u8;
        private static ReadOnlySpan<byte> ExtendedXmpId => "http://ns.adobe.com/xmp/extension/\0"u8;
        private static ReadOnlySpan<byte> IccId => "ICC_PROFILE\0"u8;
        private static ReadOnlySpan<byte> PhotoshopId => "Photoshop 3.0\0"u8;

        /// <summary>
        /// Collects the EXIF, XMP, ICC and Photoshop/IPTC segments before the first scan, in file order,
        /// and returns the EXIF orientation (1 when absent).
        /// </summary>
        internal static int Collect(ReadOnlySpan<byte> jpeg, List<JpegSegment> segments)
        {
            segments.Clear();
            int orientation = 1;
            bool sawExif = false;
            int pos = 2; // past SOI

            while (pos + 4 <= jpeg.Length && jpeg[pos] == 0xFF)
            {
                byte marker = jpeg[pos + 1];
                if (marker == 0xFF)
                {
                    pos++; // fill byte
                    continue;
                }
                if (marker == 0xDA || marker == 0xD9)
                    break; // SOS or EOI
                if (marker == 0x01 || (marker >= 0xD0 && marker <= 0xD7))
                {
                    pos += 2; // standalone marker
                    continue;
                }

                int length = (jpeg[pos + 2] << 8) | jpeg[pos + 3];
                if (length < 2 || pos + 2 + length > jpeg.Length)
                    break;

                ReadOnlySpan<byte> payload = jpeg.Slice(pos + 4, length - 2);
                bool isExif = marker == APP1 && payload.StartsWith(ExifId);
                bool keep = marker switch
                {
                    APP1 => isExif || payload.StartsWith(XmpId) || payload.StartsWith(ExtendedXmpId),
                    APP2 => payload.StartsWith(IccId),
                    APP13 => payload.StartsWith(PhotoshopId),
                    _ => false
                };

                if (keep)
                    segments.Add(new JpegSegment(pos, length + 2));
                if (isExif && !sawExif)
                {
                    sawExif = true;
                    orientation = ReadExifOrientation(payload[ExifId.Length..]);
                }

                pos += 2 + length;
            }

            return orientation;
        }

        /// <summary>Returns the offset just past SOI and any JFIF (APP0) or Adobe (APP14) segment written by the encoder.</summary>
        internal static int GetInsertPosition(ReadOnlySpan<byte> jpeg)
        {
            int pos = 2;
            while (pos + 4 <= jpeg.Length && jpeg[pos] == 0xFF && (jpeg[pos + 1] == 0xE0 || jpeg[pos + 1] == 0xEE))
            {
                int length = (jpeg[pos + 2] << 8) | jpeg[pos + 3];
                if (length < 2 || pos + 2 + length > jpeg.Length)
                    break;
                pos += 2 + length;
            }
            return pos;
        }

        private static int ReadExifOrientation(ReadOnlySpan<byte> tiff)
        {
            if (tiff.Length < 8)
                return 1;

            bool littleEndian;
            if (tiff[0] == (byte)'I' && tiff[1] == (byte)'I')
                littleEndian = true;
            else if (tiff[0] == (byte)'M' && tiff[1] == (byte)'M')
                littleEndian = false;
            else
                return 1;

            if (ReadUInt16(tiff, 2, littleEndian) != 42)
                return 1;

            uint ifd = ReadUInt32(tiff, 4, littleEndian);
            if (ifd > (uint)tiff.Length - 2)
                return 1;

            int count = ReadUInt16(tiff, (int)ifd, littleEndian);
            for (int i = 0; i < count; i++)
            {
                int entry = (int)ifd + 2 + i * 12;
                if (entry + 12 > tiff.Length)
                    break;
                if (ReadUInt16(tiff, entry, littleEndian) != 0x0112)
                    continue;

                int type = ReadUInt16(tiff, entry + 2, littleEndian);
                int value = type switch
                {
                    3 => ReadUInt16(tiff, entry + 8, littleEndian),         // SHORT
                    4 => (int)ReadUInt32(tiff, entry + 8, littleEndian),    // LONG, written by a few tools
                    _ => 1
                };
                return value is >= 1 and <= 8 ? value : 1;
            }

            return 1;
        }

        private static ushort ReadUInt16(ReadOnlySpan<byte> data, int offset, bool littleEndian)
            => littleEndian ? BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]) : BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);

        private static uint ReadUInt32(ReadOnlySpan<byte> data, int offset, bool littleEndian)
            => littleEndian ? BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]) : BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
    }
}
