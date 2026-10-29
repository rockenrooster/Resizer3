# User Guide

This guide covers the main workflows for using Resizer3.

## Convert A Folder Of Photos To WebP

1. Open Resizer3.
2. Drag the folder onto the main file list.
3. Set `Output Path` to the destination folder.
4. Set `Format` to `webp`.
5. Set `Quality` to a value such as `85`.
6. Leave `Max Res` as `Off` if you want percentage scaling.
7. Set `Resolution` to `100` to preserve original dimensions, or lower it to shrink dimensions.
8. Click `Convert`.

The app recursively scans the dropped folder and preserves the dropped folder name in the output layout.

## Use The Optimized WebP Preset

1. Add files or folders.
2. Click `Optimized Settings`.
3. Choose the output folder.
4. Click `Convert`.

The preset selects WebP, quality `85`, max resolution `4K (2160p)`, and WebP effort `2`.

## Resize Images By Percentage

1. Add files or folders.
2. Set `Max Res` to `Off`.
3. Set `Resolution` to the desired percentage.
4. Pick the output format.
5. Click `Convert`.

Examples:

- `100`: keep original dimensions.
- `50`: write half-size images.
- `25`: write quarter-size images.

## Cap Images To A Maximum Resolution

1. Add files or folders.
2. Set `Max Res` to `4K (2160p)`, `1440p`, or `1080p`.
3. Pick the output format.
4. Click `Convert`.

When max resolution is enabled, the percentage `Resolution` field is disabled. Images smaller than the selected cap are not enlarged.

## Keep Folder Layout

For most folder-based workflows, leave `Output Full Folder Structure` unchecked.

Example:

```text
Dropped folder: D:\Photos\Trip
Input file:     D:\Photos\Trip\Day1\image.jpg
Output root:    C:\img
Format:         webp
Output file:    C:\img\Trip\Day1\image.webp
```

Use `Output Full Folder Structure` only when you want output paths derived from the complete original file paths. Like normal mode, it uses the selected format's extension.

Resizer3 never writes over an input file. If an output path would be the input file itself (for example, the output root is the parent of the dropped folder and the format is unchanged), that file is skipped and listed in `Failed Files`. If two files would get the same output name, such as `a.jpg` and `a.png` converted to WebP, the later one is written as `a (2).webp`.

## Handle Failed Files

If some files fail:

1. The `Failed Files` tab appears automatically, and `Completed Files` shows how many failed.
2. Review the file path and error message. Click a column header to sort, for example to group the same errors.
3. Double-click a failed row to open the original file.
4. Fix or remove problem files if needed.
5. Run another conversion.

Truncated or corrupt images, missing files, and files skipped because their output would overwrite them all appear here. A failed file never leaves a partial output behind, and an older output with the same name is kept.

Click `Clear List` to remove both successful input rows and failed-file rows.

## Tune PNG Compression

When `Format` is `png`, a `PNG Compression` control appears (0-9, default 6).

- Lower values write faster with slightly larger files.
- Higher values write smaller files but take longer.
- The setting never affects image quality, only file size and speed.

## Tune Threads

The default thread count uses the processor count unless a saved value exists.

Use lower thread counts when:

- The computer becomes sluggish.
- The source or destination drive is slow.
- You are converting very large images.
- Memory usage climbs too high.

Use higher thread counts when:

- Images are small.
- Storage is fast.
- CPU utilization remains low.

Avoid setting extremely high values unless you are intentionally testing throughput.

## Stop A Running Conversion

Click `Cancel`.

The app stops queuing new work and asks active workers to exit. Files already written remain in the output folder. A file currently inside an encoder may finish before the cancellation completes. Each output is renamed into place only when it is complete, so no half-written files are left.

Closing the window during a conversion asks whether to stop it. If you choose `Yes`, Resizer3 closes as soon as the files in progress finish. If that takes too long, close the window again to exit right away.

## Open The Output Folder

Click `Open` next to the output path. Windows Explorer opens the configured output path.

## Reset The Current List

Click `Clear List`.

This clears:

- Input file rows.
- Failed-file rows.
- Counters.
- Before-size display.

It does not delete output files from disk.
