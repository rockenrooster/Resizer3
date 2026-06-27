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
            for (int i = 0; i < 40; i++)
            {
                try
                {
                    File.Copy(sourcePath, targetPath, overwrite: true);
                    break;
                }
                catch when (i < 39)
                {
                    Thread.Sleep(250);
                }
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = targetPath,
                UseShellExecute = true
            });

            if (cleanupSource)
                ScheduleCleanup(sourcePath);
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
