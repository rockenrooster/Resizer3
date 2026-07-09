# Changelog

This changelog starts at `v3.1.6.7`; earlier history was not reconstructed.

## Unreleased

- Add upcoming user-visible changes here before tagging a release.

## v3.1.6.13 - 2026-07-09

- Faster downscaling: the libvips path now uses file-based thumbnailing, which enables shrink-on-load (scaled JPEG/WebP decode and HEIF embedded thumbnails).
- Faster JPEG batches with a max-resolution cap: the TurboJPEG fast path now parses headers from a small prefix read instead of reading entire files that libvips will process anyway.
- Added a `PNG Compression` control (0-9, default 6), visible when PNG output is selected and saved with other settings. PNG output previously always used maximum compression (9).
- Conversion workers now run on dedicated threads, so high thread counts reach full parallelism immediately.
- libvips internal concurrency now adapts to the worker count, so small batches of large files use the whole CPU.
- Enabled .NET server garbage collection for better multi-threaded throughput.
- GitHub Release notes are now taken from this changelog: `release.ps1` promotes the `Unreleased` section to the release version, and the release workflow publishes it.

## v3.1.6.12 - 2026-06-27

- Added release script progress output for release start and artifact version verification.

## v3.1.6.10 - 2026-06-27

- Added a release guard that verifies the updater repository settings match `origin`.
- Documented the .NET 10 runtime target and project background.

## v3.1.6.9 - 2026-06-27

- Expanded the README with download, feature, build, release, and update sections.
- Removed the `release.cmd` wrapper.

## v3.1.6.8 - 2026-06-27

- Added `release.ps1` to build, commit, tag, and push releases.
- Documented the scripted release process.

## v3.1.6.7 - 2026-06-27

- Prepared GitHub Releases based updater support.
