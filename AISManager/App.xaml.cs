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

            if (CheckDownloadPath())
            {
                var mainWindow = new MainWindow();
                mainWindow.Show();
            }
            else
            {
                Shutdown();
            }
        }

        private bool CheckDownloadPath()
        {
            var config = AISManager.AppData.Configs.AppConfig.Instance;
            config.Load();

            // Проверяем, пустой ли путь или указывает на несуществующую папку
            if (string.IsNullOrWhiteSpace(config.DownloadPath) || !System.IO.Directory.Exists(config.DownloadPath))
            {
                while (string.IsNullOrWhiteSpace(config.DownloadPath) || !System.IO.Directory.Exists(config.DownloadPath))
                {
                    var result = System.Windows.MessageBox.Show(
                        "Необходимо указать корректный путь к папке для загрузки фиксов.\n\nУказать сейчас?",
                        "Первоначальная настройка",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
                        {
                            dialog.Description = "Выберите папку для загрузки фиксов";
                            dialog.UseDescriptionForTitle = true;

                            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                            {
                                config.DownloadPath = dialog.SelectedPath;
                                config.Save();
                            }
                        }
                    }
                    else
                    {
                        Log.Information("User declined to set download path. Shutting down.");
                        return false;
                    }
                }
            }
            return true;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("Application Exiting...");
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }
}
