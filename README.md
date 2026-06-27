# Resizer3

Resizer3 is a Windows desktop batch image converter and resizer built with WinForms and .NET 10. It is meant for local, high-throughput image conversion with drag-and-drop input, folder recursion, parallel processing, configurable output quality, resize controls, and failed-file tracking.

## Download

Download the latest `Resizer3.exe` from [GitHub Releases](https://github.com/rockenrooster/Resizer3/releases/latest).

Resizer3 is published as a Windows x64, framework-dependent executable (.NET 10). Install the matching .NET Desktop Runtime if Windows cannot launch it.

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

## Project Background

I started this project in June 2020. My goal was to have a small, lightweight app that can convert and resize image files efficiently with today's modern multicore CPUs. Other tools are too slow, too bloated, or capped at low thread counts, like FastStone. This tool scales with your CPU core count and is configurable, so converting 40,000 image files with a 5950X, like my system, is much faster than other tools.

I have been using this app myself for over 6 years now, and I think it's ready to share with the world. I only started using AI on my hobby projects in 2025, so it hasn't really affected this project much, but I will use AI, including Codex, to help out my workflow going forward since it's a huge time saver for me. Using AI, I am testing alternate versions of this app, ResizerRust and ResizerC, to see if more performance can be extracted by using different languages.

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


## Updates

The in-app updater checks the latest public GitHub Release. A release must include:

- `Resizer3.exe`
- `Resizer3.exe.sha256`
