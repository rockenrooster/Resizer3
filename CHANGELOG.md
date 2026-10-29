# Changelog

This changelog starts at `v3.1.6.7`; earlier history was not reconstructed.

## Unreleased

- Add upcoming user-visible changes here before tagging a release.

## v3.1.6.14 - 2026-10-28

- Originals are never overwritten: a file whose output path equals the input (same folder and format), including through a mapped drive, subst drive or junction, is skipped and listed in Failed Files.
- Outputs that would share a name (for example `a.jpg` and `a.png` to `a.webp`, or same-named files dropped from different folders) now get ` (2)`, ` (3)` suffixes instead of silently replacing each other. Dropping the same files twice no longer duplicates them.
- Outputs are written to a temporary file and renamed into place when complete, so a failed, canceled or interrupted conversion never leaves a half-written file or destroys an older output.
- The JPEG-to-JPEG fast path now keeps EXIF (orientation, date taken, GPS), XMP, ICC and IPTC metadata, so portrait phone photos no longer come out sideways. RGB-coded JPEGs no longer come out with wrong colours, and malformed headers are refused safely. It is also about 25% faster on large batches.
- Percentage resize and Max Res now use the displayed orientation of EXIF-rotated photos (a 50% resize of a rotated 6000x4000 photo is 2000x3000).
- Truncated or corrupt input files are reported as failed instead of being converted with missing image data. Missing files are reported instead of being skipped silently.
- `Output Full Folder Structure` now uses the selected format's extension.
- Large lists are much faster: dropping tens of thousands of files is near-instant, and Convert no longer freezes the window while re-reading file sizes. The file list is read-only and sorts by clicking the column header.
- Failures are listed in batches, so thousands of failures no longer freeze the window. The progress bar reaches 100% when some files fail, `Completed Files` shows the failed count, and `Saved` compares converted outputs with their own inputs.
- Closing the window during a conversion asks to stop it first, then closes once the current files finish; closing again offers to exit right away. `Update` is disabled while converting.
- `.tif` and `.jfif` files are now accepted. The format list can no longer be edited by typing, and `Output Path` must be a full folder path.
- The updater stages the new executable and swaps it in with a rename, so a failed update can never leave a broken exe. If it fails, it shows the error and restarts the current version instead of exiting silently.
- Out-of-range saved settings no longer crash startup. The log file rolls over at 1 MB.
- About 20% faster startup: removed the redundant Costura.Fody embedding and the unused libpng codec package, and image codecs load on first use.

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
