using System.Windows;
using Serilog;

namespace AISManager
{
    public partial class App : System.Windows.Application
    {
        public App()
        {
            // Настраиваем вывод ошибок самого Serilog (поможет, если что-то не так с путями)
            Serilog.Debugging.SelfLog.Enable(msg => System.Diagnostics.Debug.WriteLine(msg));

            var logDir = AISManager.AppData.AppPath.LogsPath;
            var logPath = System.IO.Path.Combine(logDir, "log.txt");

            // Создаем папку в AppData, если её нет
            if (!System.IO.Directory.Exists(logDir))
            {
                System.IO.Directory.CreateDirectory(logDir);
            }

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
                .WriteTo.Debug()
                .CreateLogger();

            Log.Information("--- Application Initialize ---");
            Log.Information("Logs are located at: {LogPath}", logPath);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

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
