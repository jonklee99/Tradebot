using SysBot.Base;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SysBot.Pokemon.WinForms;

internal static class Program
{
    public static readonly string WorkingDirectory = Environment.CurrentDirectory = Path.GetDirectoryName(Environment.ProcessPath)!;

    public static string ConfigPath { get; private set; } = Path.Combine(WorkingDirectory, "config.json");

    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main()
    {
        // Catch unhandled exceptions on background threads — these would otherwise silently terminate the process.
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            var msg = ex != null ? $"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}" : e.ExceptionObject?.ToString() ?? "Unknown";
            LogUtil.LogError($"FATAL unhandled exception (terminating={e.IsTerminating}):\n{msg}", "Program");
            try { File.AppendAllText(Path.Combine(WorkingDirectory, "crash.log"), $"[{DateTime.Now}] FATAL:\n{msg}\n\n"); } catch { }
        };

        // Catch unobserved task exceptions (async code that faults without being awaited).
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            LogUtil.LogError($"Unobserved task exception: {e.Exception.Message}\n{e.Exception.StackTrace}", "Program");
            e.SetObserved(); // Prevent process termination
        };

#if NETCOREAPP
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
#endif
        var cmd = Environment.GetCommandLineArgs();
        var cfg = Array.Find(cmd, z => z.EndsWith(".json"));
        if (cfg != null)
            ConfigPath = cmd[0];

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new Main());
    }
}
