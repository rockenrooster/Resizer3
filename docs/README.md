# Resizer3 Documentation

Resizer3 is a Windows desktop batch image converter and resizer. It is built with WinForms on .NET and is designed for high-throughput local image processing with drag-and-drop input, multi-threaded conversion, configurable output quality, resolution controls, progress statistics, and failed-file tracking.

This documentation is organized around how the app is used and what each feature does.

## Documentation Map

- [Feature Reference](FEATURES.md): Detailed feature-by-feature documentation for the app.
- [User Guide](USER_GUIDE.md): Practical walkthroughs for common workflows.
- [Development Notes](DEVELOPMENT.md): Build, packaging, dependencies, settings, and implementation notes.

## Quick Start

1. Launch `Resizer3.exe`.
2. Drag image files or folders onto the file list.
3. Choose an output path. Double-click the output path field to browse, or use the existing value.
4. Select an output format.
5. Adjust quality, resolution, max resolution, thread count, and WebP effort if needed.
6. Click `Convert`.
7. Open the output folder with `Open`.

## Supported Input Files

Resizer3 accepts files with these extensions when they are dropped directly or discovered recursively inside dropped folders:

- `.jpg`
- `.jpeg`
- `.png`
- `.heic`
- `.heif`
- `.webp`
- `.avif`
- `.jxl`
- `.bmp`
- `.gif`
- `.tiff`
- `.ico`

## Supported Output Formats

The current output format selector exposes:

- `jpg`
- `png`
- `webp`
- `bmp`
- `gif`
- `tiff`
- `ico`

## Defaults

- Output path: `C:\img`
- Default format: `jpg`
- Default quality: `95`, unless changed by saved settings
- Default resolution: `100%`
- Default max resolution: `Off`
- Default thread count: processor count, unless changed by saved settings
- Default WebP effort: `2`

## Settings Storage

Resizer3 saves user settings on close to:

```text
%LOCALAPPDATA%\Resizer3\settings.json
```

Runtime errors that reach the app logging layer are written to:

```text
%LOCALAPPDATA%\Resizer3\Resizer3.log
```

## Build

The included build script increments the app version and publishes a Windows x64 single-file executable to `artifacts/Resizer3.exe`:

```powershell
.\build.ps1
```

For more detail, see [Development Notes](DEVELOPMENT.md).

## Releases And Updates

Resizer3 updates come from the latest public GitHub Release. Release tags use `vX.Y.Z.W`, and each release must include:

- `Resizer3.exe`
- `Resizer3.exe.sha256`

The app checks release metadata at startup, enables `Update` when a newer release is available, then downloads and verifies the release asset after the user clicks `Update`.
