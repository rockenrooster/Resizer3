using System.Diagnostics;

namespace Resizer3
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--update")
            {
                RunUpdaterLogic(args);
                return;
            }
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            Application.ThreadException += (s, e) =>
            {
                try { AppLog.TryLog("Application.ThreadException", e.Exception); } catch { }
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                try
                {
                    if (e.ExceptionObject is Exception ex)
                        AppLog.TryLog("AppDomain.UnhandledException", ex);
                }
                catch { }
            };

            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;
            Application.Run(new Form1());
        }
        static void RunUpdaterLogic(string[] args)
        {
            string? source = GetArg(args, "--source");
            string? target = GetArg(args, "--target");
            int? pid = int.TryParse(GetArg(args, "--pid"), out int parsedPid) ? parsedPid : null;

            if (!string.IsNullOrWhiteSpace(source) && !string.IsNullOrWhiteSpace(target))
            {
                if (pid.HasValue)
                    WaitForExit(pid.Value);

                ReplaceAndRelaunch(source, target, cleanupSource: true);
                return;
            }

            RunLegacyUpdaterLogic();
        }

        static void RunLegacyUpdaterLogic()
        {
            string fullExeName = Path.GetFileName(Process.GetCurrentProcess().MainModule?.FileName ?? Application.ExecutablePath);
            string exeBaseName = Path.GetFileNameWithoutExtension(fullExeName);
            string cleanedBaseName = exeBaseName.Replace("Updated", "");

            string currentDir = Path.GetDirectoryName(Application.ExecutablePath) ?? Environment.CurrentDirectory;
            string updatedPath = Path.Combine(currentDir, cleanedBaseName + "Updated.exe");
            string targetPath = Path.Combine(currentDir, cleanedBaseName + ".exe");

            Thread.Sleep(1000);
            ReplaceAndRelaunch(updatedPath, targetPath, cleanupSource: true);
        }

        static void WaitForExit(int pid)
        {
            try
            {
                using Process process = Process.GetProcessById(pid);
                if (!process.HasExited)
                    process.WaitForExit(30000);
            }
            catch { }
        }

        static void ReplaceAndRelaunch(string sourcePath, string targetPath, bool cleanupSource)
        {
            // Copy beside the target, then swap it in with a rename, so a failed copy can never
            // leave a half-written executable to relaunch.
            string stagedPath = targetPath + ".new";
            Exception? copyError = Retry(() => File.Copy(sourcePath, stagedPath, overwrite: true))
                ?? Retry(() => File.Move(stagedPath, targetPath, overwrite: true));

            // On failure the target is still the old version; relaunch it so the app never just disappears.
            if (copyError is not null)
            {
                try { File.Delete(stagedPath); }
                catch { }
                AppLog.TryLog("Updater: could not replace " + targetPath, copyError);
                MessageBox.Show("The update could not be installed, so the current version will start again.\n\n" + copyError.Message,
                    "Resizer3 update", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = targetPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                AppLog.TryLog("Updater: could not relaunch " + targetPath, ex);
                MessageBox.Show("Could not restart Resizer3: " + ex.Message, "Resizer3 update", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            if (cleanupSource)
                ScheduleCleanup(sourcePath);
        }

        // Retries for about 10 seconds while the old process releases the file; returns the last error.
        static Exception? Retry(Action action)
        {
            Exception? lastError = null;
            for (int i = 0; i < 40; i++)
            {
                try
                {
                    action();
                    return null;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    Thread.Sleep(250);
                }
            }
            return lastError;
        }

        static string? GetArg(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            }

            return null;
        }

        static void ScheduleCleanup(string path)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c timeout /t 2 /nobreak > nul & del /q \"{path}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
            }
            catch { }
        }
    }
}
