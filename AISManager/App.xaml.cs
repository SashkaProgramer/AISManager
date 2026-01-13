using System.Windows;
using Serilog;

namespace AISManager
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var logPath = System.IO.Path.Combine(AISManager.AppData.AppPath.LogsPath, "log.txt");
            var logDir = AISManager.AppData.AppPath.LogsPath;
            if (!System.IO.Directory.Exists(logDir))
            {
                System.IO.Directory.CreateDirectory(logDir);
            }

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
                .WriteTo.Debug()
                .CreateLogger();

            Log.Information("Application Starting... Log file: {LogPath}", logPath);

            var mainWindow = new MainWindow();
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("Application Exiting...");
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }
}
