using AISManager.App.Configs;
using AISManager.App.WinFormSink;
using AISManager.Services;
using AISManager.Ui.Tray;
using HardDev.CoreUtils.Config;
using HardDev.CoreUtils.Logging;
using Serilog;

namespace AISManager
{
    public static class MainApp
    {
        private static readonly ILogger s_logger = WinFormSink.Build(nameof(MainApp));

        [STAThread]
        private static void Main()
        {
            s_logger.Information("-------------- Запуск приложения --------------");

            AppLogger.RegisterGlobalEventHandlers();

            MainConfig appConfig = AppConfig.GetOrLoad<MainConfig>(out bool loaded);
            if (!loaded)
            {
                appConfig.Save();
            }

            PrintInfo();
            // InitTools();

            Mutex mutex = new(true, Application.ProductName, out bool onlyInstance);
            if (!onlyInstance)
            {
                MessageBox.Show(@"Приложение уже запущено.", @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Создаем экземпляры сервисов
            var versionService = new VersionService();
            var hotfixService = new HotfixService();

            // Создаем форму с передачей сервисов
            Application.Run(new MainForm(versionService, hotfixService));

            GC.KeepAlive(mutex);
        }

        private static void PrintInfo()
        {
            s_logger.Information("{AppName}: {AppVersion}", Application.ProductName, Application.ProductVersion);
            s_logger.Information("ОС: {OsVersion}", Environment.OSVersion);
            s_logger.Information("Путь к данным: {DataPath}", AppPath.DataPath);
            s_logger.Information("Git: {Git}", AppInfo.GetBranchAndCommitInfo());
        }

        private static void InitTools()
        {
            try
            {
                s_logger.Information("Инициализация:");

                TrayTool.Init();
                s_logger.Information("{NameTool} - OK", nameof(TrayTool));
            }
            catch (Exception)
            {
                s_logger.Error("Ошибка инициализации инструментов");
                throw;
            }
        }
    }
}