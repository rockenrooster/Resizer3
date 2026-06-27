# Resizer3

Resizer3 is a Windows desktop batch image converter and resizer. It is built with WinForms on .NET and is designed for high-throughput local image processing with drag-and-drop input, multi-threaded conversion, configurable output quality, resolution controls, progress statistics, and failed-file tracking.

## Documentation

- [Feature Reference](docs/FEATURES.md)
- [User Guide](docs/USER_GUIDE.md)
- [Development Notes](docs/DEVELOPMENT.md)

## Quick Start

1. Launch `Resizer3.exe`.
2. Drag image files or folders onto the file list.
3. Choose an output path.
4. Select an output format.
5. Adjust quality, resolution, max resolution, thread count, and WebP effort if needed.
6. Click `Convert`.

## Build

```powershell
.\build.ps1
```

Release builds write `artifacts/Resizer3.exe` and `artifacts/Resizer3.exe.sha256`.

## Releases And Updates

Updates come from the latest public GitHub Release. Release tags must use `vX.Y.Z.W`, and the release must include these assets:

- `Resizer3.exe`
- `Resizer3.exe.sha256`

Pushing a matching version tag runs the GitHub Actions release workflow.
