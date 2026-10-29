using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace Resizer3
{
    public partial class Form1 : Form
    {
        private GitHubReleaseUpdate? pendingUpdate;

        // Contents of the virtual-mode grids.
        private readonly List<InputFile> inputFiles = new();
        private readonly HashSet<string> inputPaths = new(StringComparer.OrdinalIgnoreCase);
        private long inputBytes;
        private readonly List<FailedFile> failedFiles = new();

        // Workers queue failures; the progress timer moves them into the grid in batches.
        private readonly ConcurrentQueue<FailedFile> pendingFailures = new();

        // Batch statistics, updated by worker threads.
        private int succeededCount;
        private int failedCount;
        private int totalFilesProcessing;
        private long processedInputBytes; // input bytes of started files, for MB/s
        private long succeededInputBytes; // input bytes of converted files, for Saved %
        private long afterSize;           // output bytes of converted files

        private readonly System.Windows.Forms.Timer progressTimer = new() { Interval = 50 };
        private int progressTicks;
        private readonly Stopwatch sw = new();
        private CancellationTokenSource? cts;
        private Task?[]? activeWorkers;
        private bool isProcessing;
        private bool isUpdating;
        private bool closeAfterStop;
        private bool closePromptOpen;
        private bool failedTabShown;

        public Form1()
        {
            InitializeComponent();

            textBox1.Text = @"C:\img";
            numericUpDownRes.Value = 100;

            comboBox1.Items.Clear();
            comboBox1.Items.AddRange(new string[]
            {
            "jpg", "png", "webp", "bmp", "gif", "tiff", "ico"
            });
            comboBox1.SelectedItem = "jpg";

            // Setup maxResComboBox
            maxResComboBox.Items.Clear();
            maxResComboBox.Items.AddRange(new string[] { "Off", "4K (2160p)", "1440p", "1080p" });
            maxResComboBox.SelectedItem = "Off";

            // Load saved entries, clamped so an out-of-range value cannot break startup.
            var settings = AppSettings.Load();
            textBox1.Text = settings.SaveLocation;
            numericUpDownQuality.Value = ClampNumeric(settings.Quality, numericUpDownQuality.Minimum, numericUpDownQuality.Maximum);
            numericUpDownThreads.Value = ClampNumeric(settings.ThreadsNumber > 0 ? settings.ThreadsNumber : Environment.ProcessorCount, numericUpDownThreads.Minimum, numericUpDownThreads.Maximum);
            numericUpDownRes.Value = ClampNumeric(settings.Resolution, numericUpDownRes.Minimum, numericUpDownRes.Maximum);
            if (!string.IsNullOrEmpty(settings.Format))
                comboBox1.SelectedItem = settings.Format;
            if (!string.IsNullOrEmpty(settings.MaxRes))
                maxResComboBox.SelectedItem = settings.MaxRes;
            numericUpDownWebpEffort.Value = ClampNumeric(settings.WebpEffort, numericUpDownWebpEffort.Minimum, numericUpDownWebpEffort.Maximum);
            numericUpDownPngCompression.Value = ClampNumeric(settings.PngCompression, numericUpDownPngCompression.Minimum, numericUpDownPngCompression.Maximum);

            progressTimer.Tick += progressTimer_Tick;
            this.Shown += Form1_Shown;
            UpdateFormatOptionsVisibility();

            // Remove Failed Files tab initially
            tabControl.TabPages.Remove(tabPageFailedFiles);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.0";
            Text = "Resizer " + version + "        Batch image converter";
        }

        private async void Form1_Shown(object? sender, EventArgs e)
        {
            await Task.Delay(150);

            await CheckForUpdateAsync();

        }

        private async Task CheckForUpdateAsync()
        {
            update.Enabled = false;
            pendingUpdate = null;

            try
            {
                Version localVersion = GetFileVersion(Application.ExecutablePath);
                pendingUpdate = await GitHubReleaseUpdateService.CheckLatestAsync(localVersion);

                if (pendingUpdate is not null)
                {
                    MessageBox.Show($"Update Available: {pendingUpdate.Version}\n(Local version: {localVersion})");
                    update.Enabled = !isProcessing;
                }
            }
            catch (Exception ex)
            {
                AppLog.TryLog("Update check failed", ex);
                update.Enabled = false;
                pendingUpdate = null;
            }
        }
        private Version GetFileVersion(string path)
        {
            FileVersionInfo info = FileVersionInfo.GetVersionInfo(path);
            return new Version(info.FileVersion ?? "0.0.0.0");
        }

        private async void dataGridView1_DragDropAsync(object sender, DragEventArgs e)
        {
            string[] droppedItems = e.Data?.GetData(DataFormats.FileDrop, false) as string[] ?? Array.Empty<string>();
            if (droppedItems.Length == 0)
                return;

            List<InputFile> found;
            try
            {
                found = await Task.Run(() => InputCollector.Collect(droppedItems));
            }
            catch (Exception ex)
            {
                AppLog.TryLog("Collect dropped files", ex);
                MessageBox.Show(this, "Could not read the dropped items: " + ex.Message, "Resizer3", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!IsDisposed)
                AddInputFiles(found);
        }

        private void AddInputFiles(List<InputFile> files)
        {
            int before = inputFiles.Count;
            foreach (var file in files)
            {
                // Skip files already listed, e.g. when the same folder is dropped twice.
                if (inputPaths.Add(file.FullPath))
                {
                    inputFiles.Add(file);
                    inputBytes += file.Size;
                }
            }

            if (inputFiles.Count != before)
            {
                ClearSortGlyphs(dataGridView1);
                dataGridView1.RowCount = inputFiles.Count;
            }
            UpdateInputSummary();
        }

        private void UpdateInputSummary()
        {
            numFiles.Text = inputFiles.Count.ToString();
            beforeSizeLabel.Text = ((inputBytes / 1024.000 / 1024.000).ToString("N3") + " MB");
        }

        private void dataGridView1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
                e.Effect = DragDropEffects.All;
            else
                e.Effect = DragDropEffects.None;
        }

        private void dataGridView1_CellValueNeeded(object? sender, DataGridViewCellValueEventArgs e)
        {
            if ((uint)e.RowIndex < (uint)inputFiles.Count)
                e.Value = inputFiles[e.RowIndex].FullPath;
        }

        private void dataGridViewFailed_CellValueNeeded(object? sender, DataGridViewCellValueEventArgs e)
        {
            if ((uint)e.RowIndex >= (uint)failedFiles.Count)
                return;

            var failed = failedFiles[e.RowIndex];
            e.Value = e.ColumnIndex == FailedErrorMessage.Index ? failed.ErrorMessage : failed.FilePath;
        }

        private void dataGridView1_ColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            int direction = ToggleSortGlyph(dataGridView1, e.ColumnIndex);
            inputFiles.Sort((a, b) => direction * string.Compare(a.FullPath, b.FullPath, StringComparison.CurrentCultureIgnoreCase));
            dataGridView1.Invalidate();
        }

        private void dataGridViewFailed_ColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            int direction = ToggleSortGlyph(dataGridViewFailed, e.ColumnIndex);
            bool byMessage = e.ColumnIndex == FailedErrorMessage.Index;
            failedFiles.Sort((a, b) => direction * string.Compare(
                byMessage ? a.ErrorMessage : a.FilePath,
                byMessage ? b.ErrorMessage : b.FilePath,
                StringComparison.CurrentCultureIgnoreCase));
            dataGridViewFailed.Invalidate();
        }

        // Virtual-mode grids cannot sort themselves; the lists behind them are sorted instead.
        private static int ToggleSortGlyph(DataGridView grid, int columnIndex)
        {
            var header = grid.Columns[columnIndex].HeaderCell;
            var order = header.SortGlyphDirection == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
            ClearSortGlyphs(grid);
            header.SortGlyphDirection = order;
            return order == SortOrder.Ascending ? 1 : -1;
        }

        private static void ClearSortGlyphs(DataGridView grid)
        {
            foreach (DataGridViewColumn column in grid.Columns)
                column.HeaderCell.SortGlyphDirection = SortOrder.None;
        }

        private async void buttonResize_Click(object sender, EventArgs e)
        {
            if (isProcessing || isUpdating)
                return;

            string outputRoot = textBox1.Text.Trim();
            if (!Path.IsPathFullyQualified(outputRoot))
            {
                MessageBox.Show(this, @"Set Output Path to a full folder path, for example C:\img.", "Resizer3", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Snapshot UI state so workers never touch controls.
            var options = CaptureOptions(outputRoot);
            int threads = Decimal.ToInt32(numericUpDownThreads.Value);
            InputFile[] inputs = inputFiles.ToArray();

            isProcessing = true;
            closeAfterStop = false;
            failedTabShown = false;
            buttonResize.Enabled = false;
            cancelButton.Enabled = true;
            update.Enabled = false;
            buttonResize.Text = "Converting... 0%";

            succeededCount = 0;
            failedCount = 0;
            totalFilesProcessing = 0;
            processedInputBytes = 0;
            succeededInputBytes = 0;
            afterSize = 0;
            progressTicks = 0;

            var runCts = new CancellationTokenSource();
            cts = runCts;
            bool canceled = false;
            sw.Restart();
            progressTimer.Start();
            try
            {
                await RunConversionAsync(inputs, options, threads, runCts);
                canceled = runCts.IsCancellationRequested;
            }
            catch (Exception ex)
            {
                AppLog.TryLog("Conversion", ex);
                if (!closeAfterStop)
                    MessageBox.Show(this, "Conversion stopped with an error: " + ex.Message, "Resizer3", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                sw.Stop();
                progressTimer.Stop();
                UpdateElapsedTime();
                UpdateUI();
                cts = null;
                runCts.Dispose();
                isProcessing = false;
                buttonResize.Enabled = true;
                cancelButton.Enabled = false;
                update.Enabled = pendingUpdate is not null;
                buttonResize.Text = "Convert";
            }

            // With a close prompt open, Form1_FormClosing finishes the close when the prompt returns.
            if (closePromptOpen)
                return;
            if (closeAfterStop)
                Close();
            else if (canceled)
                MessageBox.Show("Operation was Cancelled.");
        }

        private ConversionOptions CaptureOptions(string outputRoot)
        {
            var (maxWidth, maxHeight) = GetMaxResolutionDimensions();
            return new ConversionOptions(
                OutputRoot: outputRoot,
                Format: comboBox1.SelectedItem?.ToString() ?? "jpg",
                PreserveFullFolderStructure: checkBox1.Checked,
                Quality: Decimal.ToInt32(numericUpDownQuality.Value),
                Multiplier: numericUpDownRes.Value / 100m,
                MaxWidth: maxWidth,
                MaxHeight: maxHeight,
                WebpEffort: Decimal.ToInt32(numericUpDownWebpEffort.Value),
                PngCompression: Decimal.ToInt32(numericUpDownPngCompression.Value));
        }

        private async Task RunConversionAsync(InputFile[] inputs, ConversionOptions options, int threads, CancellationTokenSource runCts)
        {
            // Plan output paths off the UI thread. Files whose output would overwrite themselves are
            // rejected; colliding outputs get a numbered suffix.
            var rejected = new List<FailedFile>();
            var (jobs, inputsByName) = await Task.Run(() => (OutputPlanner.Plan(inputs, options, rejected), OutputPlanner.IndexByFileName(inputs)));
            CancellationToken token = runCts.Token;

            totalFilesProcessing = inputs.Length;
            foreach (var failed in rejected)
                pendingFailures.Enqueue(failed);
            Interlocked.Add(ref failedCount, rejected.Count);
            if (jobs.Count == 0)
                return;

            int requestedDegree = threads > 0 ? threads : Environment.ProcessorCount;
            int maxDegree = Math.Clamp(requestedDegree, 1, jobs.Count);
            if (options.UseLibvips)
            {
                // Split cores between file-level workers and libvips' internal pool
                // so small batches of large files still saturate the CPU.
                NetVips.NetVips.Concurrency = Math.Max(1, Environment.ProcessorCount / maxDegree);
            }

            // Workers run CPU-bound loops for the whole batch, so give them dedicated
            // threads; Task.Run would hold thread-pool threads and ramp up slowly
            // past the pool's minimum when high thread counts are requested.
            // Cancellation is checked between files, so no output is left half-written.
            var createdDirs = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            int nextIndex = -1;
            var consumers = new Task?[maxDegree];
            activeWorkers = consumers;
            try
            {
                for (int w = 0; w < maxDegree; w++)
                    consumers[w] = StartWorker();
            }
            catch
            {
                // Could not start every worker (e.g. too many threads): stop the running ones before reporting.
                runCts.Cancel();
                await Task.WhenAll(consumers.OfType<Task>());
                activeWorkers = null;
                throw;
            }

            try
            {
                await Task.WhenAll(consumers.OfType<Task>());
            }
            finally
            {
                activeWorkers = null;
            }

            Task StartWorker() =>
                Task.Factory.StartNew(() =>
                {
                    using var worker = new ConversionWorker(options, createdDirs, inputsByName);
                    while (!token.IsCancellationRequested)
                    {
                        int i = Interlocked.Increment(ref nextIndex);
                        if (i >= jobs.Count)
                            break;

                        var job = jobs[i];
                        Interlocked.Add(ref processedInputBytes, job.InputSize);
                        var result = worker.Process(job, token);
                        if (result.Status == FileStatus.Succeeded)
                        {
                            Interlocked.Add(ref succeededInputBytes, job.InputSize);
                            Interlocked.Add(ref afterSize, result.OutputBytes);
                            Interlocked.Increment(ref succeededCount);
                        }
                        else if (result.Status == FileStatus.Failed)
                        {
                            pendingFailures.Enqueue(new FailedFile(job.InputPath, result.Error ?? "Unknown error"));
                            Interlocked.Increment(ref failedCount);
                        }
                    }
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private void WaitForWorkers(TimeSpan timeout)
        {
            if (activeWorkers is not { } workers)
                return;
            try
            {
                Task.WaitAll(workers.OfType<Task>().ToArray(), timeout);
            }
            catch (Exception ex)
            {
                AppLog.TryLog("WaitForWorkers", ex);
            }
        }

        private void progressTimer_Tick(object? sender, EventArgs e)
        {
            UpdateElapsedTime();
            // Stats and failures refresh every 250 ms so the numbers stay readable.
            if (++progressTicks % 5 == 0)
                UpdateUI();
        }

        private async void update_Click(object sender, EventArgs e)
        {
            if (pendingUpdate is null || isProcessing || isUpdating)
                return;

            var appDir = Path.GetDirectoryName(Application.ExecutablePath) ?? Environment.CurrentDirectory;
            string updatedPath = Path.Combine(appDir, "Resizer3Updated.exe");
            Version localVersion = GetFileVersion(Application.ExecutablePath);
            isUpdating = true;
            update.Enabled = false;
            buttonResize.Enabled = false;

            try
            {
                if (File.Exists(updatedPath))
                    File.Delete(updatedPath);

                await GitHubReleaseUpdateService.DownloadAndVerifyAsync(pendingUpdate, updatedPath, localVersion);

                var startInfo = new ProcessStartInfo
                {
                    FileName = updatedPath,
                    UseShellExecute = false
                };
                startInfo.ArgumentList.Add("--update");
                startInfo.ArgumentList.Add("--pid");
                startInfo.ArgumentList.Add(Environment.ProcessId.ToString(CultureInfo.InvariantCulture));
                startInfo.ArgumentList.Add("--source");
                startInfo.ArgumentList.Add(updatedPath);
                startInfo.ArgumentList.Add("--target");
                startInfo.ArgumentList.Add(Application.ExecutablePath);
                Process.Start(startInfo);

                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Update failed: " + ex.Message);
                isUpdating = false;
                update.Enabled = pendingUpdate is not null;
                buttonResize.Enabled = true;
            }
        }

        private void UpdateElapsedTime()
        {
            double elapsed = sw.Elapsed.TotalSeconds;
            elapsedLabel.Text = elapsed > 0 ? elapsed.ToString("N2") + " s" : "0.00 s";
        }

        private void UpdateUI()
        {
            DrainFailures();

            int succeeded = Volatile.Read(ref succeededCount);
            int failed = Volatile.Read(ref failedCount);
            long size = Interlocked.Read(ref afterSize);
            long convertedInput = Interlocked.Read(ref succeededInputBytes);
            double elapsed = sw.Elapsed.TotalSeconds;
            double filesPerSec = elapsed > 0 ? succeeded / elapsed : 0.0;
            double mbSec = elapsed > 0 ? (Interlocked.Read(ref processedInputBytes) / 1024.0 / 1024.0) / elapsed : 0.0;
            // Compare outputs with the inputs that produced them, so failures do not inflate the figure.
            double saved = convertedInput > 0 ? 100.0 * (1.0 - (double)size / convertedInput) : 0.0;
            int total = totalFilesProcessing;
            int pct = total > 0 ? (int)((succeeded + failed) * 100L / total) : 0;

            completedFiles.Text = failed > 0 ? $"{succeeded} ({failed} failed)" : succeeded.ToString();
            afterSizeLabel.Text = ((size / 1024.000 / 1024.000).ToString("N3") + " MB");
            filesPerSecLabel.Text = filesPerSec.ToString("N2") + " files/s";
            mbPerSecLabel.Text = mbSec.ToString("N2") + " MB/s";
            percentSavedLabel.Text = saved.ToString("N2") + "%";
            processingProgress.Value = Math.Clamp(pct, 0, 100);
            if (isProcessing && cts?.IsCancellationRequested == false)
                buttonResize.Text = $"Converting... {pct}%";
        }

        private void DrainFailures()
        {
            if (pendingFailures.IsEmpty)
                return;

            while (pendingFailures.TryDequeue(out var failed))
                failedFiles.Add(failed);
            ClearSortGlyphs(dataGridViewFailed);
            dataGridViewFailed.RowCount = failedFiles.Count;

            // Show the Failed Files tab, switching to it once per batch.
            if (!tabControl.TabPages.Contains(tabPageFailedFiles))
                tabControl.TabPages.Add(tabPageFailedFiles);
            if (!failedTabShown)
            {
                failedTabShown = true;
                tabControl.SelectedTab = tabPageFailedFiles;
            }
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            if (!isProcessing || cts is null)
                return;

            cts.Cancel();
            buttonResize.Text = "Cancelling...";
            cancelButton.Enabled = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = textBox1.Text,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open output folder: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isProcessing && e.CloseReason is (CloseReason.WindowsShutDown or CloseReason.TaskManagerClosing))
            {
                // No prompt here; give the files in progress a moment so fewer temp files are left behind.
                cts?.Cancel();
                WaitForWorkers(TimeSpan.FromSeconds(3));
            }
            else if (isProcessing)
            {
                // Workers stop between files, so nothing is left half-written; close once they are done.
                // Closing again while they finish offers to leave at once, in case a file is stuck on a slow drive.
                string question = closeAfterStop
                    ? "Files are still finishing. Close now anyway? Files in progress will not be saved."
                    : "A conversion is still running. Stop it and close Resizer?";
                closePromptOpen = true;
                bool yes = MessageBox.Show(this, question, "Resizer3", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2) == DialogResult.Yes;
                closePromptOpen = false;

                if (!isProcessing)
                {
                    // The batch ended while the prompt was open; close unless the user declined both times.
                    if (!yes && !closeAfterStop)
                    {
                        e.Cancel = true;
                        return;
                    }
                }
                else if (!yes)
                {
                    e.Cancel = true;
                    return;
                }
                else if (!closeAfterStop)
                {
                    e.Cancel = true;
                    closeAfterStop = true;
                    cts?.Cancel();
                    buttonResize.Text = "Cancelling...";
                    cancelButton.Enabled = false;
                    return;
                }
            }

            try
            {
                progressTimer.Stop();

                var settings = new AppSettings
                {
                    SaveLocation = textBox1.Text,
                    ThreadsNumber = numericUpDownThreads.Value,
                    Quality = numericUpDownQuality.Value,
                    Resolution = (decimal)numericUpDownRes.Value,
                    Format = comboBox1.SelectedItem as string,
                    MaxRes = maxResComboBox.SelectedItem as string,
                    WebpEffort = Decimal.ToInt32(numericUpDownWebpEffort.Value),
                    PngCompression = Decimal.ToInt32(numericUpDownPngCompression.Value)
                };
                _ = settings.TrySave();
            }
            catch (Exception ex)
            {
                // Never let settings persistence crash shutdown.
                AppLog.TryLog("Form1_FormClosing", ex);
            }
        }


        private void textBox1_DoubleClick(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    textBox1.Text = dialog.SelectedPath;
                }
            }
        }

        private void dataGridViewFailed_DoubleClick(object sender, EventArgs e)
        {
            int index = dataGridViewFailed.CurrentRow?.Index ?? -1;
            if ((uint)index >= (uint)failedFiles.Count)
                return;

            string filePath = failedFiles[index].FilePath;
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selected = comboBox1.SelectedItem?.ToString()?.ToLower() ?? "";

            // Disable quality for formats without quality settings
            if (selected == "png" || selected == "bmp" || selected == "tiff" || selected == "tif" || selected == "ico" || selected == "gif")
            {
                numericUpDownQuality.Enabled = false;
            }
            else
            {
                numericUpDownQuality.Enabled = true;
            }
            UpdateFormatOptionsVisibility();
        }

        private void maxResComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selected = maxResComboBox.SelectedItem?.ToString() ?? "Off";
            if (selected != "Off")
            {
                numericUpDownRes.Enabled = false;
            }
            else
            {
                numericUpDownRes.Enabled = true;
            }
        }

        private void UpdateFormatOptionsVisibility()
        {
            string selected = comboBox1.SelectedItem?.ToString()?.ToLower() ?? "";
            bool showWebp = selected == "webp";
            labelWebpEffort.Visible = showWebp;
            numericUpDownWebpEffort.Visible = showWebp;
            // PNG controls share the WebP effort slot; the formats are mutually exclusive.
            bool showPng = selected == "png";
            labelPngCompression.Visible = showPng;
            numericUpDownPngCompression.Visible = showPng;
        }

        private static decimal ClampNumeric(decimal value, decimal min, decimal max) => Math.Clamp(value, min, max);

        private void clearListButton_Click(object sender, EventArgs e)
        {
            inputFiles.Clear();
            inputPaths.Clear();
            inputBytes = 0;
            dataGridView1.RowCount = 0;
            failedFiles.Clear();
            dataGridViewFailed.RowCount = 0;
            ClearSortGlyphs(dataGridView1);
            ClearSortGlyphs(dataGridViewFailed);
            UpdateInputSummary();
            completedFiles.Text = "0";

            // Remove Failed Files tab
            tabControl.TabPages.Remove(tabPageFailedFiles);

            // Switch back to All Files tab
            tabControl.SelectedTab = tabPageAllFiles;
        }

        private void optimizedButton_Click(object sender, EventArgs e)
        {
            numericUpDownQuality.Value = 85;
            maxResComboBox.SelectedItem = "4K (2160p)";
            comboBox1.SelectedItem = "webp";
            numericUpDownRes.Enabled = false;
            numericUpDownWebpEffort.Value = 2;
        }

        private (int maxWidth, int maxHeight) GetMaxResolutionDimensions()
        {
            string selected = maxResComboBox.SelectedItem?.ToString() ?? "Off";
            return selected switch
            {
                "4K (2160p)" => (3840, 2160),
                "1440p" => (2560, 1440),
                "1080p" => (1920, 1080),
                _ => (int.MaxValue, int.MaxValue)
            };
        }
    }
}
