# Resizer3

Resizer3 is a Windows desktop batch image converter and resizer built with WinForms and .NET. It is meant for local, high-throughput image conversion with drag-and-drop input, folder recursion, parallel processing, configurable output quality, resize controls, and failed-file tracking.

## Download

Download the latest `Resizer3.exe` from [GitHub Releases](https://github.com/rockenrooster/Resizer3/releases/latest).

Resizer3 is published as a Windows x64, framework-dependent executable. Install the matching .NET Desktop Runtime if Windows cannot launch it.

## Features

- Batch convert files or folders with drag and drop.
- Preserve folder layout for dropped folders.
- Resize by percentage or cap output to 4K, 1440p, or 1080p.
- Convert to `jpg`, `png`, `webp`, `bmp`, `gif`, `tiff`, or `ico`.
- Read common image formats including JPEG, PNG, HEIC/HEIF, WebP, AVIF, JPEG XL, BMP, GIF, TIFF, and ICO.
- Tune quality, WebP effort, and worker thread count.
- Track progress, throughput, before/after size, and failed files.
- Check GitHub Releases for updates from inside the app.

## Quick Start

1. Launch `Resizer3.exe`.
2. Drag image files or folders onto the file list.
3. Choose an output folder.
4. Select an output format.
5. Adjust quality, resolution, max resolution, thread count, and WebP effort if needed.
6. Click `Convert`.

## Documentation

- [Feature Reference](docs/FEATURES.md)
- [User Guide](docs/USER_GUIDE.md)
- [Development Notes](docs/DEVELOPMENT.md)

## Build From Source

Prerequisites:

- Windows
- .NET SDK 10.0.x
- PowerShell

Build a local release artifact:

```powershell
.\build.ps1
```

The build writes:

- `artifacts/Resizer3.exe`
- `artifacts/Resizer3.exe.sha256`

## Release

Releases are created from version tags in the format `vX.Y.Z.W`.

To increment the fourth version component, build, commit, push, tag, and trigger the GitHub Actions release workflow:

```powershell
.\release.ps1
```

Optional overrides:

```powershell
.\release.ps1 -Version 3.1.6.8 -Message "Release v3.1.6.8"
```

The workflow builds `Resizer3.exe`, verifies its `FileVersion`, generates `Resizer3.exe.sha256`, and uploads both files to the GitHub Release.

## Updates

The in-app updater checks the latest public GitHub Release. A release must include:

- `Resizer3.exe`
- `Resizer3.exe.sha256`

The updater does not use GitHub tokens. Private GitHub Releases need a different distribution strategy.
