using HardDev.CoreUtils.Logging;
using Serilog;
using AISManager.App;

namespace AISManager.Ui.Tray
{
    public static class TrayTool
    {
        private static readonly ILogger s_logger = AppLogger.ForContext(nameof(TrayTool));

        private static NotifyIcon _trayIcon;
        private static ContextMenuStrip _trayMenu;
        private static MainForm s_currentForm;
        private static bool s_isInitialized;

        private static readonly Icon _iconAlert = AppResource.GetIcon("computer_alert.ico");
        private static readonly Icon _iconNoAlert = AppResource.GetIcon("computer_no_alert.ico");

        public static bool HasAlert
        {
            get => _trayIcon.Icon == _iconAlert;
            set => _trayIcon.Icon = value ? _iconAlert : _iconNoAlert;
        }

        public static void Init()
        {
            if (s_isInitialized)
            {
                return;
            }

            _trayMenu = new ContextMenuStrip();
            _trayIcon = new NotifyIcon
            {
                Icon = IconResources.AISDownloaderIcon,
                ContextMenuStrip = _trayMenu,
                Visible = true,
            };

            _trayIcon.MouseClick += TrayIconClickedHandler;

            s_isInitialized = true;
        }

        private static void TrayIconClickedHandler(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // MainForm.ShowNonModal();
            }
        }

        private static void BalloonTipClickedHandler(object sender, EventArgs e)
        {
            if (Control.MouseButtons == MouseButtons.Left || Control.MouseButtons == MouseButtons.None)
            {
                // MainForm.ShowNonModal();
            }
        }
        private static void OnTrayExitClick(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}