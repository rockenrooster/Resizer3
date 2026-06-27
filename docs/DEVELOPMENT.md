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
| `PhotoSauce.NativeCodecs.Libpng` | PNG native codec package reference. |
| `PhotoSauce.NativeCodecs.Libwebp` | WebP codec support for MagicScaler. |
| `NetVips` | Primary high-performance image loading/resizing/saving path. |
| `NetVips.Native` | Native libvips binaries. |
| `Costura.Fody` | Embeds managed dependencies for distribution. |
| `turbojpeg.dll` | Native fast path for no-resize JPEG-to-JPEG conversion. |

## Source File Responsibilities

| File | Responsibility |
| --- | --- |
| `Program.cs` | App entry point, unhandled exception hooks, process priority, updater mode. |
| `Form1.cs` | Main UI behavior, drag-and-drop, conversion pipeline, update check, settings save/load, progress updates. |
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
- Timer disposal failures during shutdown.
- Update check failures.

Logging is intentionally best-effort. The logging code catches and ignores its own failures.

## Conversion Pipeline

The conversion pipeline begins in the `Convert` button handler.

High-level flow:

1. Prevent duplicate starts with `isProcessing`.
2. Create a new `CancellationTokenSource`.
3. Reset counters and timers.
4. Snapshot UI state into local variables.
5. Collect valid input rows.
6. Compute total input size.
7. Create a bounded channel.
8. Start consumer tasks based on the thread count.
9. Queue files through a producer task.
10. Process each file with either libvips or fallback logic.
11. Update progress from a UI timer.
12. Stop timers and restore buttons.

## Processing Path Selection

The app chooses the processing path based on output format.

libvips is used unless the selected output is:

- `ico`
- `bmp`
- `gif`

Those formats use the fallback `ProcessSingleFile` path because libvips ICO writing is not supported directly by this app and some libvips builds may omit BMP/GIF save support.

## libvips Path Notes

The libvips path:

- Uses `Image.NewFromFile` with sequential access.
- Reads dimensions before deciding resize behavior.
- Uses max-resolution caps or percentage scaling.
- Uses `ThumbnailImage` only when dimensions need to change.
- Writes with format-specific methods where possible.
- Sets `NetVips.Concurrency = 1` to avoid nested oversubscription.

Format save methods:

- WebP: `Webpsave`
- JPEG: `Jpegsave`
- PNG: `Pngsave`
- TIFF: `Tiffsave`
- GIF: `Gifsave`
- Fallback: `WriteToFile`

## TurboJPEG Fast Path Notes

The TurboJPEG path is attempted only for no-resize JPEG-to-JPEG output.

It is skipped when:

- `turbojpeg.dll` is unavailable.
- The selected output is not JPEG.
- The input is not JPEG.
- Percentage resize is requested.
- Max-resolution logic would require downscaling.

When used, it attempts to preserve ICC profile chunks.

## Fallback Path Notes

The fallback path:

- Reads the input file into memory.
- Uses GDI+ when possible for dimensions and EXIF metadata.
- Uses MagicScaler or libvips for dimension fallback.
- Builds `ProcessImageSettings`.
- Writes ICO with the custom `SaveAsIco` method.
- Writes other fallback formats with MagicScaler.
- Applies EXIF Date Taken or source last-write timestamp to the output file.

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
2. Copies the updated executable over the target executable with retries.
3. Relaunches the target executable.
4. Schedules best-effort cleanup of `Resizer3Updated.exe`.

Legacy `--update` with no extra arguments is still supported.

## Release Process

1. Commit changes.
2. Push `main`.
3. Create a version tag, for example `v3.1.6.7`.
4. Push the tag.
5. GitHub Actions builds `artifacts/Resizer3.exe`, verifies its `FileVersion`, writes `artifacts/Resizer3.exe.sha256`, and creates the GitHub Release.

## Maintenance Notes

- The form designer contains some initial combo box items, but the constructor clears and repopulates the actual format list at runtime.
- AVIF output implementation is commented out and should not be documented as a live output feature.
- The `Output Full Folder Structure` mode currently preserves the source extension in the output path.
- The repository includes generated `bin` and `obj` outputs in the workspace; avoid editing generated files directly.
