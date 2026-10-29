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
- PNG compression level value, visible only for PNG output.
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
- Files already in the list are skipped, so dropping the same folder twice adds nothing.
- The app stores the full file path and size internally; sizes are read once while scanning.
- The visible row shows the input file path. The list is read-only; click the column header to sort it.

When folders are dropped:

- The folder is scanned recursively. Folders that cannot be read are skipped.
- Supported image files in nested folders are added.
- Each file also records a relative path.
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
| `.jpg`, `.jpeg`, `.jfif` | Standard JPEG input. |
| `.png` | Standard PNG input. |
| `.heic`, `.heif` | HEIF-family input through native codecs/libvips support. |
| `.webp` | WebP input. |
| `.avif` | AVIF input is accepted by the file filter. Output AVIF is not exposed in the current format selector. |
| `.jxl` | JPEG XL input through native codecs/libvips support. |
| `.bmp` | Bitmap input. |
| `.gif` | GIF input. |
| `.tif`, `.tiff` | TIFF input. |
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

The output path must be a full folder path, such as `C:\img` or `\\server\share\img`; `Convert` asks you to fix it otherwise.

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
- The drive root (or UNC `\\server\share` root) is removed.
- Colons are removed.
- The resulting path is placed under the output root.
- The output extension is changed to the selected format.

Example:

```text
Input:       D:\Photos\Trip\image.heic
Format:      jpg
Output root: C:\img
Output:      C:\img\Photos\Trip\image.jpg
```

### Output Safety Rules

These rules apply in both modes:

- An input file is never overwritten. If its output path is the input file itself, the file is skipped and listed in `Failed Files`. This also holds when the output folder reaches an input through another path, such as a mapped drive versus its UNC path, a `subst` drive, a junction, or a symbolic link; files are compared by identity when an output already exists.
- If two files in a batch map to the same output path, or an output path is another input file, the later file gets a numbered suffix: `image.webp`, `image (2).webp`, `image (3).webp`, in list order.
- Each output is written to a temporary `<name>.r3tmp.<ext>` file in the same folder and renamed over the final path only when it is complete. A failed conversion deletes its temporary file, an interrupted one can leave only the temporary file behind, and neither damages an existing output.

## Output Formats

The format selector currently exposes:

| Format | Encoder behavior |
| --- | --- |
| `jpg` | Written with JPEG quality. Uses a TurboJPEG fast path for no-resize JPEG-to-JPEG conversion when available; metadata is kept either way. |
| `png` | Written as PNG using the selectable PNG compression level. Quality control is disabled for this format. |
| `webp` | Written as WebP. Uses quality and WebP compression effort. Quality `100` enables lossless WebP behavior. |
| `bmp` | Written through the fallback processing path. Quality control is disabled. |
| `gif` | Written through the fallback processing path. Quality control is disabled. |
| `tiff` | Written as TIFF. Quality control is disabled. |
| `ico` | Written as a 256x256, 32-bit icon. Quality control is disabled. |

AVIF output is not available.

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

Sizes refer to the image as displayed: for a photo whose EXIF orientation rotates it by 90 degrees, width and height are taken after rotation, and the output pixels are rotated upright.

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
- The cap is checked against the displayed orientation, so an EXIF-rotated portrait photo is treated as portrait.

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

## PNG Compression Level

The PNG compression control is visible only when output format is `png`.

Range:

```text
0-9
```

Default:

```text
6
```

This is the zlib compression level. It affects only file size and processing time, never pixel quality: higher levels produce slightly smaller files but take longer to write, and lower levels favor throughput.

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

- Workers run on dedicated threads and pull files from the shared work list.
- For libvips processing, internal libvips concurrency adapts to the worker count so large batches avoid oversubscription while small batches of large files still use the whole CPU.
- The process priority is set below normal at startup.

## Conversion Lifecycle

Clicking `Convert` starts a batch.

The app then:

1. Checks that the output path is a full folder path.
2. Disables `Convert` and `Update`, and enables `Cancel`.
3. Reads the current UI settings and the file list into a snapshot.
4. Plans every output path, applying the output safety rules.
5. Starts worker threads.
6. Updates the UI on a timer while processing.
7. Restores the buttons after completion, cancellation, or an error.

Files that no longer exist are reported in `Failed Files`.

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

Cancellation is cooperative. A file that is already inside a native encoder call may finish before the worker exits. Because outputs are renamed into place only when complete, cancellation never leaves a half-written output.

Closing the window during a conversion asks whether to stop it. Choosing `Yes` cancels the batch and closes the app once the files in progress finish; choosing `No` keeps converting. If a file is stuck, for example on a disconnected network drive, closing the window again offers to exit right away; files still in progress are then not saved.

## Progress And Statistics

Resizer3 updates progress and statistics during conversion.

Displayed values:

| Label | Meaning |
| --- | --- |
| `Number of Files` | Number of rows in the input grid. |
| `Completed Files` | Number of files successfully completed in the current batch, followed by the failed count when any file failed. |
| `Before Size` | Total size of listed input files. |
| `After Size` | Total size of successfully written outputs. |
| `Saved` | Percentage reduction of the written outputs versus the inputs that produced them. |
| `Elapsed Time` | Time since conversion started. |
| `files/s` | Completed files per elapsed second. |
| `MB/s` | Input bytes processed per elapsed second. |
| Progress bar | Processed files (completed plus failed) divided by the files in the batch. |

File sizes are read once while scanning dropped items, so large lists do not freeze the interface. Elapsed time refreshes every 50 ms and the other values every 250 ms.

## Failed Files

Failures are recorded without stopping the entire batch.

When a file fails:

- The `Failed Files` tab is added if it is not already visible.
- A row is added with file path and error message. Failures are added in batches with the progress refresh, so thousands of failures do not freeze the window.
- The app switches to the failed-file tab the first time a file fails in a batch.

Click a column header to sort the failed files by path or by error message.

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

libvips and TurboJPEG processing paths:

- Use the source file last-write time.
- Apply that timestamp to the output file after writing.

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

- Reads image dimensions and EXIF orientation using libvips.
- Uses libvips file-based thumbnail processing when resizing is needed, which enables shrink-on-load (scaled JPEG/WebP decode and HEIF embedded thumbnails) and rotates EXIF-oriented images upright.
- Saves the probed image directly when no resize is needed, keeping its metadata and orientation tag.
- Reports truncated input as a failure instead of writing an image with missing rows.
- Writes with format-specific save methods.
- Uses file-level parallelism controlled by the thread setting.

### TurboJPEG Fast Path

Used only when all of these are true:

- Input has a `.jpg`, `.jpeg` or `.jfif` extension and starts with a JPEG marker.
- Output is JPEG.
- TurboJPEG native library is available.
- No percentage resize is requested.
- No max-resolution downscale is required for the displayed (EXIF-rotated) size.

This path:

- Parses the JPEG header from a small prefix read, so files that need libvips processing are not fully read twice.
- Reads the full JPEG bytes only once the fast path is known to apply.
- Transcodes with TurboJPEG, reusing each worker's codec handles and buffers across files.
- Copies the source's EXIF, XMP, ICC and Photoshop/IPTC segments into the output, so orientation, date taken and GPS are kept.

If the fast path cannot be used, the app falls back to the normal libvips JPEG path. Corrupt or truncated JPEGs also fall back, and libvips reports them.

### MagicScaler/GDI Fallback Path

Used for formats where libvips output support is not used by the app:

- `ico`
- `bmp`
- `gif`

This path:

- Reads dimensions with MagicScaler (already in displayed orientation), falling back to libvips for less common input formats.
- Uses GDI+ for the EXIF `Date Taken` timestamp.
- Uses MagicScaler encoder settings for output.
- Uses custom ICO writing for icon output.
- Configures the MagicScaler codecs the first time this path runs, not at startup.

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
- PNG compression level.

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

The logger is designed not to crash the application. If logging itself fails, the failure is ignored. When the log passes 1 MB it is renamed to `Resizer3.old.log` and a new log starts.

## Update Check

After the main form is shown, the app checks the latest public GitHub Release metadata.

Current behavior:

- The app reads the latest release tag and finds the `Resizer3.exe` release asset.
- It compares the release tag version against the local executable version.
- If the release version is newer, it shows an update-available message and enables `Update`. `Update` stays disabled while a conversion runs, and `Convert` is disabled while an update downloads.
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

The updated executable is first copied next to the original as `Resizer3.exe.new` and then renamed over it, so the original is only replaced by a complete file. If the copy or rename keeps failing for about 10 seconds, the updater logs the error, shows it, and relaunches the current version so the app never just disappears.

## Known Behavior And Caveats

- Output AVIF is not currently exposed, even though AVIF input is accepted.
- Cancellation is cooperative and may not interrupt a native encoder immediately.
- The failed-file tab appears only after a failure.
- Very high thread counts can saturate disk, memory bandwidth, or native codecs.
- Existing output files from earlier runs are overwritten, but only after the new file is complete.
- If Windows shuts down mid-batch, a `.r3tmp.` temporary file may be left next to an output; it can be deleted.
