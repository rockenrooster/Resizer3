# Development Notes

This document summarizes how Resizer3 is built and how the main implementation is organized.

## Project Type

Resizer3 is a Windows Forms application.

Project file:

```text
Resizer3.csproj
```

Important project settings:

| Setting | Value |
| --- | --- |
| SDK | `Microsoft.NET.Sdk` |
| Output type | `WinExe` |
| Target framework | `net10.0-windows7.0` |
| UI framework | Windows Forms |
| Nullable | Enabled |
| Platform target | `x64` |
| Application icon | `Resizer1.ico` |
| Current assembly/file version | See `Resizer3.csproj` |

## Major Dependencies

| Dependency | Purpose |
| --- | --- |
| `PhotoSauce.MagicScaler` | Fallback image processing, resizing, and encoding. |
| `PhotoSauce.NativeCodecs.Libheif` | HEIF/HEIC codec support for MagicScaler. |
| `PhotoSauce.NativeCodecs.Libjxl` | JPEG XL codec support for MagicScaler. |
| `PhotoSauce.NativeCodecs.Libwebp` | WebP codec support for MagicScaler. |
| `NetVips` | Primary high-performance image loading/resizing/saving path. |
| `NetVips.Native` | Native libvips binaries. |
| `turbojpeg.dll` | Native fast path for no-resize JPEG-to-JPEG conversion. |

Single-file publishing bundles the managed and native dependencies, so no assembly-embedding weaver is used.

## Source File Responsibilities

| File | Responsibility |
| --- | --- |
| `Program.cs` | App entry point, unhandled exception hooks, process priority, updater mode. |
| `Form1.cs` | Main UI behavior: drag-and-drop, virtual-mode file grids, starting and cancelling batches, worker threads, progress updates, update check, settings save/load. |
| `ConversionPipeline.cs` | UI-free conversion code: input scanning (`InputCollector`), output path planning (`OutputPlanner`), and per-file processing (`ConversionWorker`) for the libvips, TurboJPEG and MagicScaler paths. |
| `TurboJpeg.cs` | TurboJPEG bindings, the per-worker `TurboJpegSession`, and JPEG metadata segment handling. |
| `GitHubReleaseUpdateService.cs` | GitHub Releases metadata lookup, update download, version check, and SHA256 verification. |
| `Form1.Designer.cs` | WinForms generated control layout. |
| `AppSettings.cs` | JSON settings model and persistence. |
| `AppLog.cs` | Best-effort local logging. |
| `build.ps1` | Version increment and publish script. |

## Build Script

Run:

```powershell
.\build.ps1
```

The script:

1. Reads `Resizer3.csproj`.
2. Increments the fourth component of `AssemblyVersion` unless `-NoIncrement` is supplied.
3. Accepts `-Version x.y.z.w` to set `AssemblyVersion` and `FileVersion` exactly.
4. Runs `dotnet clean`.
5. Publishes Release for `win-x64`.
6. Copies the published executable to `artifacts/Resizer3.exe`.
7. Writes `artifacts/Resizer3.exe.sha256`.

Publish command used by the script:

```powershell
dotnet publish -c Release -r win-x64 /p:PublishSingleFile=true /p:SelfContained=false
```

Release build with an exact version:

```powershell
.\build.ps1 -Version 3.1.6.7 -NoIncrement
```

## Runtime Settings

Settings are stored as JSON:

```text
%LOCALAPPDATA%\Resizer3\settings.json
```

Saved values:

- `SaveLocation`
- `ThreadsNumber`
- `Resolution`
- `Quality`
- `Format`
- `MaxRes`
- `WebpEffort`
- `PngCompression`

Settings load failure returns defaults. Settings save failure is logged and ignored so shutdown can continue.

## Logging

Log file:

```text
%LOCALAPPDATA%\Resizer3\Resizer3.log
```

Logged contexts include:

- Application thread exceptions.
- AppDomain unhandled exceptions.
- Settings persistence failures.
- Dropped-item scanning and unexpected conversion errors.
- Update check failures.
- Updater copy and relaunch failures.

Logging is intentionally best-effort and serialized by a lock. The logging code catches and ignores its own failures. Past 1 MB the log is renamed to `Resizer3.old.log`.

## Conversion Pipeline

The conversion pipeline begins in the `Convert` button handler.

High-level flow:

1. Prevent duplicate starts with `isProcessing` and check that the output path is fully qualified.
2. Snapshot UI state into `ConversionOptions` and copy the input list.
3. Create a new `CancellationTokenSource`, reset counters, and start the single 50 ms UI timer.
4. Plan output paths off the UI thread with `OutputPlanner`: same-path outputs are rejected, clashing outputs get ` (2)`, ` (3)` suffixes.
5. Start dedicated worker threads (`TaskCreationOptions.LongRunning`) based on the thread count, capped at the number of jobs.
6. Each worker owns a `ConversionWorker` (and its TurboJPEG session) and pulls jobs via an atomic index, checking cancellation between files. When an output already exists, the worker compares file IDs (`FileIdentity`) with the job's input and any input of the same name, catching aliases such as mapped drives, `subst` drives and junctions that the planner's string comparison cannot see.
7. Each output is written to `<name>.r3tmp.<ext>`, stamped with the source time, and moved over the final path when complete; failures delete the temporary file.
8. Workers update `Interlocked` counters and queue failures in a `ConcurrentQueue`; the timer drains the queue into the failed grid every 250 ms.
9. In `finally`, stop the timer, run a last UI update, and restore buttons.

Both grids run in virtual mode. `inputFiles` and `failedFiles` in `Form1` are the source of truth; header clicks sort those lists.

## Processing Path Selection

The app chooses the processing path based on output format.

libvips is used unless the selected output is:

- `ico`
- `bmp`
- `gif`

Those formats use the fallback `ProcessSingleFile` path because libvips ICO writing is not supported directly by this app and some libvips builds may omit BMP/GIF save support.

## libvips Path Notes

The libvips path:

- Probes dimensions and EXIF orientation with a header-only `Image.NewFromFile` (sequential access, `failOn: Truncated`).
- Uses max-resolution caps or percentage scaling on the displayed size (width and height swapped for orientations 5-8).
- Uses the file-based `Image.Thumbnail` when dimensions need to change, which enables shrink-on-load (scaled JPEG/WebP decode, HEIF thumbnails) and auto-rotates. Otherwise the probe image itself is saved.
- Writes with format-specific methods where possible (PNG zlib level from the PNG Compression spinner, default 6).
- Sets `NetVips.Concurrency = max(1, cores / worker count)` so large batches avoid nested oversubscription while small batches of large files still saturate the CPU.

Format save methods:

- WebP: `Webpsave`
- JPEG: `Jpegsave`
- PNG: `Pngsave`
- TIFF: `Tiffsave`
- GIF: `Gifsave`
- Fallback: `WriteToFile`

## TurboJPEG Fast Path Notes

The TurboJPEG path is attempted only for no-resize JPEG-to-JPEG output.

It reads a 128 KB prefix, checks the JPEG SOI marker, and parses the header from the prefix (reading the rest only when SOF sits further in) so images that fall through to libvips are not fully read into memory twice.

It is skipped when:

- `turbojpeg.dll` is unavailable.
- The selected output is not JPEG.
- The input extension is not `.jpg`, `.jpeg` or `.jfif`, or the data does not start with a JPEG marker.
- Percentage resize is requested.
- Max-resolution logic would require downscaling the displayed (EXIF-rotated) size.
- The image is CMYK/YCCK, the header claims more than about 536 megapixels, or TurboJPEG reports an error or warning (decodes use `TJFLAG_STOPONWARNING`; for example truncated data); libvips then handles or reports the file.

RGB-coded JPEGs (no YCbCr transform) are decoded to RGB pixels instead of raw YUV planes, because re-encoding their raw planes as YCbCr would shift the colours.

When used, it copies the source's APP1 EXIF/XMP, APP2 ICC and APP13 Photoshop/IPTC segments into the output after the encoder's JFIF header. Each worker's `TurboJpegSession` keeps its codec handles, input buffer and native scratch buffer between files, frees any buffer larger than 64 MB after the file that needed it, and frees everything on dispose. The TurboJPEG bindings declare C `unsigned long` as `uint`, which is 32-bit on Windows.

## Fallback Path Notes

The fallback path:

- Configures the MagicScaler codecs (libjxl, libheif, libwebp) on first use.
- Reads dimensions with `ImageFileInfo` (already in displayed orientation), falling back to libvips.
- Builds `ProcessImageSettings`.
- Writes ICO with the custom `SaveAsIco` method.
- Writes other fallback formats with MagicScaler.
- Applies EXIF Date Taken (read with GDI+) or source last-write timestamp to the output file.

## Update Mechanism

Normal startup checks the latest public GitHub Release through:

```text
https://api.github.com/repos/{OWNER}/Resizer3/releases/latest
```

The release tag must use `vX.Y.Z.W`, and the release asset name is `Resizer3.exe`. A companion `Resizer3.exe.sha256` asset is used when GitHub's asset digest is unavailable.

The updater is unauthenticated and is intended for a public repository. Private GitHub Releases require a different distribution strategy; do not embed GitHub tokens in the desktop app.

The manual `Update` button downloads the selected release asset beside the current application as `Resizer3Updated.exe`, verifies its file version and SHA256 when available, and starts it with:

```text
--update --pid <current process id> --source "<path to Resizer3Updated.exe>" --target "<path to current Resizer3.exe>"
```

Updater mode:

1. Waits for the original process id to exit when provided.
2. Copies the updated executable to `<target>.new`, then renames it over the target, each with retries (40 attempts, 250 ms apart).
3. If either step keeps failing, deletes the staged copy, logs and shows the error; the target is still the old version.
4. Relaunches the target executable.
5. Schedules best-effort cleanup of `Resizer3Updated.exe`.

Legacy `--update` with no extra arguments is still supported.

## Release Process

Run:

```powershell
.\release.ps1
```

The script increments the fourth version component, builds locally, verifies the artifact version, promotes the `Unreleased` section of `CHANGELOG.md` to a `## vX.Y.Z.W - date` section, commits pending changes with a generated file-change summary, pushes the current branch, creates tag `vX.Y.Z.W`, and pushes the tag. GitHub Actions then builds `artifacts/Resizer3.exe`, verifies its `FileVersion`, writes `artifacts/Resizer3.exe.sha256`, and creates the GitHub Release using that changelog section as the release notes (falling back to auto-generated notes when the changelog has no entry for the tag).

Changelog convention: keep user-visible changes as bullets under `## Unreleased` in `CHANGELOG.md`. The placeholder line under `Unreleased` is ignored during promotion; if only the placeholder is present, the changelog is left untouched and the release uses auto-generated notes.

Optional overrides:

```powershell
.\release.ps1 -Version 3.1.6.8 -Message "Release v3.1.6.8"
```

## Maintenance Notes

- The form designer contains some initial combo box items, but the constructor clears and repopulates the actual format list at runtime.
- AVIF output is not implemented and should not be documented as a live output feature.
- Saved numeric settings are clamped to each control's range when loaded.
- `AllowUnsafeBlocks` is enabled for the native buffers in `TurboJpeg.cs`.
- `ConversionPipeline.cs` and `TurboJpeg.cs` do not reference WinForms controls, so they can be compiled into a test harness directly.
- The repository includes generated `bin` and `obj` outputs in the workspace; avoid editing generated files directly.
