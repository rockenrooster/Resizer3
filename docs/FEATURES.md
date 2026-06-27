# Feature Reference

This document describes the feature behavior currently implemented in Resizer3.

## App Overview

Resizer3 is a batch image converter focused on simple high-volume workflows:

- Add many images at once by dragging files or folders into the app.
- Convert those images into a selected output format.
- Resize by percentage or cap images to a maximum display resolution.
- Preserve folder layout for dropped folders.
- Run multiple conversions in parallel.
- Watch progress, throughput, output size, and saved percentage while processing.
- Review failed files separately without stopping the entire batch.

The main window title includes the application version and the description `Batch image converter`.

## Main Window Layout

The UI is split into two main areas.

The left pane contains file tabs:

- `All Files`: the primary input list.
- `Failed Files`: hidden by default and shown only after one or more files fail.

The right pane contains controls and statistics:

- Output path field.
- `Open` button.
- File count and completed count.
- Before and after size labels.
- Saved percentage.
- Elapsed time.
- Files per second.
- MB per second.
- Output folder structure checkbox.
- Format selector.
- Quality value.
- Resolution percentage value.
- Max resolution selector.
- WebP compression effort value, visible only for WebP output.
- Thread count.
- `Optimized Settings`.
- `Convert`.
- `Cancel`.
- `Clear List`.
- `Update`, enabled only when a newer executable is detected.
- Processing progress bar.

## Drag-And-Drop Input

Files and folders can be dropped onto the main file grid.

When files are dropped:

- Each supported image file is added to the `All Files` tab.
- Unsupported files are ignored.
- The app stores the full file path internally.
- The visible row shows the input file path.

When folders are dropped:

- The folder is scanned recursively.
- Supported image files in nested folders are added.
- Each file also receives a hidden relative path.
- The relative path includes the top-level folder name that was dropped.

Example:

```text
D:\Photos\Trip\Day1\image.jpg
```

If `D:\Photos\Trip` is dropped, the stored relative path is:

```text
Trip\Day1\image.jpg
```

This matters because the relative path is used when the app writes output files with normal folder-preserving behavior.

## Supported Input Extensions

The drag-and-drop filter accepts:

| Extension | Notes |
| --- | --- |
| `.jpg`, `.jpeg` | Standard JPEG input. |
| `.png` | Standard PNG input. |
| `.heic`, `.heif` | HEIF-family input through native codecs/libvips support. |
| `.webp` | WebP input. |
| `.avif` | AVIF input is accepted by the file filter. Output AVIF is not exposed in the current format selector. |
| `.jxl` | JPEG XL input through native codecs/libvips support. |
| `.bmp` | Bitmap input. |
| `.gif` | GIF input. |
| `.tiff` | TIFF input. |
| `.ico` | Icon input. |

## Output Path

The output path field controls the root folder where converted images are written.

Default:

```text
C:\img
```

Ways to manage the output path:

- Double-click the output path field to open a folder browser.
- Click `Open` to open the configured output path in Windows Explorer.
- Leave the field as-is to use the saved or default output folder.

The output root is saved when the app closes.

## Output Naming And Folder Layout

Resizer3 has two output path modes.

### Normal Mode

Normal mode is used when `Output Full Folder Structure` is unchecked.

Behavior:

- Output files are written under the configured output root.
- The output extension is changed to the selected format.
- If a file came from a dropped folder, the dropped folder name and nested relative path are preserved.
- If a file was dropped directly, the output file is written directly into the output root.

Example for a dropped folder:

```text
Input:       D:\Photos\Trip\Day1\image.heic
Dropped:     D:\Photos\Trip
Format:      jpg
Output root: C:\img
Output:      C:\img\Trip\Day1\image.jpg
```

Example for a directly dropped file:

```text
Input:       D:\Photos\image.heic
Format:      jpg
Output root: C:\img
Output:      C:\img\image.jpg
```

### Output Full Folder Structure Mode

This mode is used when `Output Full Folder Structure` is checked.

Current behavior:

- The app derives an output path from the full original input path.
- The drive root is removed.
- Colons are removed.
- The resulting path is placed under the output root.
- The original filename and original extension are preserved in the output path.

Example:

```text
Input:       D:\Photos\Trip\image.heic
Output root: C:\img
Output:      C:\img\Photos\Trip\image.heic
```

Important: the selected encoder is still used, but the output path in this mode currently keeps the original extension. For example, selecting `jpg` can write JPEG data to a path ending in the source extension when this mode is enabled.

## Output Formats

The format selector currently exposes:

| Format | Encoder behavior |
| --- | --- |
| `jpg` | Written with JPEG quality. Uses a TurboJPEG fast path in a narrow no-resize JPEG-to-JPEG case when available. |
| `png` | Written as PNG. Quality control is disabled for this format. |
| `webp` | Written as WebP. Uses quality and WebP compression effort. Quality `100` enables lossless WebP behavior. |
| `bmp` | Written through the fallback processing path. Quality control is disabled. |
| `gif` | Written through the fallback processing path. Quality control is disabled. |
| `tiff` | Written as TIFF. Quality control is disabled. |
| `ico` | Written as a 256x256, 32-bit icon. Quality control is disabled. |

AVIF output code exists only as commented-out implementation and is not available in the UI.

## Quality

The quality field controls lossy output quality where the selected output format supports it.

Range:

```text
1-100
```

Quality is enabled for:

- `jpg`
- `webp`

Quality is disabled for:

- `png`
- `bmp`
- `tiff`
- `ico`
- `gif`

For WebP:

- Quality below `100` uses lossy WebP behavior.
- Quality `100` uses lossless WebP behavior.

## Resolution Percentage

The resolution field scales image dimensions by percentage.

Default:

```text
100
```

Examples:

| Value | Result |
| --- | --- |
| `100` | Keep original dimensions unless max resolution requires downscaling. |
| `50` | Write at half width and half height. |
| `25` | Write at one quarter width and one quarter height. |

The resolution control is disabled whenever `Max Res` is not `Off`.

## Max Resolution

`Max Res` caps image dimensions without enlarging images that are already inside the selected bounds.

Options:

| Option | Maximum dimensions |
| --- | --- |
| `Off` | No max-resolution cap. The percentage resolution field is used. |
| `4K (2160p)` | `3840x2160` |
| `1440p` | `2560x1440` |
| `1080p` | `1920x1080` |

Behavior:

- If the image fits within the selected max width and height, it is not resized by max-res logic.
- If the image exceeds either dimension, it is scaled down proportionally.
- Aspect ratio is preserved.
- The app uses the smaller of the width and height ratios to keep both dimensions inside the cap.

## Optimized Settings

The `Optimized Settings` button applies a preset intended for efficient modern web-style output.

It sets:

- Format: `webp`
- Quality: `85`
- Max Res: `4K (2160p)`
- Resolution percentage: disabled because max resolution is enabled
- WebP effort: `2`

This is a convenience preset. It does not start conversion automatically.

## WebP Compression Effort

The WebP compression effort control is visible only when output format is `webp`.

Range:

```text
0-6
```

Default:

```text
2
```

Higher effort can improve compression at the cost of more processing time. Lower effort can improve throughput at the cost of larger output files.

## Threads

The thread count controls how many worker tasks process files in parallel.

Default:

- The saved value, if one exists.
- Otherwise the machine processor count.

Maximum exposed by the UI:

```text
9999
```

Practical guidance:

- Start with the processor count.
- Reduce threads if the machine becomes unresponsive or disk usage is saturated.
- Increase threads cautiously for fast storage and many small images.

Implementation notes:

- The app uses a bounded channel to feed work to consumers.
- The channel capacity is four times the worker count.
- For libvips processing, internal libvips concurrency is set to `1` so file-level parallelism drives throughput.
- The process priority is set below normal at startup.

## Conversion Lifecycle

Clicking `Convert` starts a batch.

The app then:

1. Disables the `Convert` button.
2. Enables `Cancel`.
3. Reads the current UI settings into local variables.
4. Filters the grid to existing files.
5. Computes total input size for progress and saved percentage.
6. Starts worker tasks.
7. Updates the UI on timers while processing.
8. Restores the `Convert` button after completion or cancellation.

The button text changes during conversion:

```text
Converting... 0%
Converting... 42%
Convert
```

## Cancellation

Clicking `Cancel` requests cancellation through a cancellation token.

Current behavior:

- New work stops being queued.
- Workers stop when they observe cancellation.
- The button text changes to `Cancelling...`.
- The cancel button is disabled while cancellation is being requested.
- A cancellation message is shown when the processing task observes cancellation.
- Already written files are left in the output folder.

Cancellation is cooperative. A file that is already inside a native encoder call may finish before the worker exits.

## Progress And Statistics

Resizer3 updates progress and statistics during conversion.

Displayed values:

| Label | Meaning |
| --- | --- |
| `Number of Files` | Number of rows in the input grid. |
| `Completed Files` | Number of files successfully completed in the current batch. |
| `Before Size` | Total size of listed input files. |
| `After Size` | Total size of successfully written outputs. |
| `Saved` | Percentage reduction based on before size versus after size. |
| `Elapsed Time` | Time since conversion started. |
| `files/s` | Completed files per elapsed second. |
| `MB/s` | Input bytes processed per elapsed second. |
| Progress bar | Completed files divided by total processing files. |

File size calculations are done off the UI thread where possible so large lists do not freeze the interface.

## Failed Files

Failures are recorded without stopping the entire batch.

When a file fails:

- The `Failed Files` tab is added if it is not already visible.
- A row is added with file path and error message.
- The app switches to the failed-file tab.

The failed-file grid contains:

- `File Path`
- `Error Message`

Double-clicking a failed file row attempts to open the original file with the default Windows shell association.

The failed-file tab is hidden again when `Clear List` is clicked.

## Clear List

`Clear List` resets the current input state.

It clears:

- All input rows.
- Failed-file rows.
- Number of files.
- Completed files.
- Before size.

It also removes the `Failed Files` tab and returns to `All Files`.

## Timestamp Preservation

Resizer3 attempts to preserve useful timestamps on output files.

Fallback processing path:

- Tries to read EXIF `Date Taken` from property ID `36867`.
- If EXIF date parsing fails or is unavailable, uses the source file last-write time.
- Applies the selected timestamp to the output file.
- Retries briefly if the output file is still busy.

libvips processing path:

- Uses the source file last-write time.
- Applies that timestamp to the output file after writing.

## Performance Paths

Resizer3 uses multiple image-processing paths depending on output format and situation.

### libvips Path

Used for most output formats:

- `jpg`
- `png`
- `webp`
- `tiff`
- Other formats supported by libvips default write behavior

This path:

- Reads image dimensions using libvips.
- Uses libvips thumbnail processing when resizing is needed.
- Writes with format-specific save methods.
- Uses file-level parallelism controlled by the thread setting.

### TurboJPEG Fast Path

Used only when all of these are true:

- Input is JPEG.
- Output is JPEG.
- TurboJPEG native library is available.
- No percentage resize is requested.
- No max-resolution downscale is required.

This path:

- Reads the JPEG bytes.
- Extracts header data.
- Preserves ICC profile data when possible.
- Transcodes with TurboJPEG.
- Writes the output directly.

If the fast path cannot be used, the app falls back to the normal libvips JPEG path.

### MagicScaler/GDI Fallback Path

Used for formats where libvips output support is not used by the app:

- `ico`
- `bmp`
- `gif`

This path:

- Reads the input into memory.
- Uses GDI+ where possible for dimensions and EXIF metadata.
- Falls back to MagicScaler/libvips dimension detection for less common input formats.
- Uses MagicScaler encoder settings for output.
- Uses custom ICO writing for icon output.

## ICO Output

ICO output is written by a custom helper.

Current behavior:

- Output is resized to `256x256`.
- Output uses 32-bit BGRA pixel data with alpha support.
- The ICO contains one image entry.
- The AND mask is written as all zeroes for 32-bit alpha icons.

## Settings Persistence

On app close, Resizer3 saves:

- Output path.
- Thread count.
- Quality.
- Resolution.
- Format.
- Max resolution.
- WebP effort.

Settings file:

```text
%LOCALAPPDATA%\Resizer3\settings.json
```

If loading fails, the app silently falls back to defaults.

If saving fails, the error is logged and the app still closes.

## Logging

The app has a best-effort log writer.

Log file:

```text
%LOCALAPPDATA%\Resizer3\Resizer3.log
```

The logger is designed not to crash the application. If logging itself fails, the failure is ignored.

## Update Check

After the main form is shown, the app checks the latest public GitHub Release metadata.

Current behavior:

- The app reads the latest release tag and finds the `Resizer3.exe` release asset.
- It compares the release tag version against the local executable version.
- If the release version is newer, it shows an update-available message and enables `Update`.
- If no newer version is detected, `Update` remains disabled.
- If the check fails, the error is logged and `Update` remains disabled.

Clicking `Update`:

1. Downloads `Resizer3.exe` beside the running app as `Resizer3Updated.exe`.
2. Verifies the downloaded file is nonempty, newer than the local executable, and matches SHA256 when release checksum data is available.
3. Starts the downloaded executable with explicit `--update` arguments.
3. Exits the current app.
4. The updater process waits for the original executable to exit.
5. It copies the updated executable over the original executable.
6. It relaunches the updated executable.

## Known Behavior And Caveats

- Output AVIF is not currently exposed, even though AVIF input is accepted.
- `Output Full Folder Structure` preserves original filename and extension in the output path.
- Cancellation is cooperative and may not interrupt a native encoder immediately.
- The failed-file tab appears only after a failure.
- Very high thread counts can saturate disk, memory bandwidth, native codecs, or the Windows UI message loop.
- Existing output files with the same name are overwritten.
