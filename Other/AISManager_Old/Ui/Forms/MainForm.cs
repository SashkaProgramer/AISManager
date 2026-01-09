using AISManager.App.Configs;
using AISManager.App.WinFormSink;
using AISManager.Extensions;
using AISManager.Models;
using AISManager.Services;
using AISManager.Ui.Forms;
using AISManager.Ui.Tray;
using HardDev.CoreUtils.Config;
using HardDev.CoreUtils.Logging;
using Serilog;
using System.Diagnostics;

namespace AISManager
{
    public partial class MainForm : Form
    {
        private readonly IVersionService _versionService;
        private readonly IHotfixService _hotfixService;
        private string _currentVersion = string.Empty;
        private List<HotfixInfo> _hotfixes = new();
        private string _customDownloadPath = string.Empty;
        private static MainForm _instance;
        private CancellationTokenSource? _backgroundCts;

        private static readonly ILogger s_logger = AppLogger.ForContext(nameof(TrayTool));

        public MainForm(IVersionService versionService, IHotfixService hotfixService)
        {
            InitializeComponent();

            // Text = $@"AIS Manager - {Application.ProductVersion}";

            _versionService = versionService;
            _hotfixService = hotfixService;

            WinFormSink.LogEmitted += AppendLogToTextBoxFromSink;

            LoadConfigSettings();
            SetupHotfixesListView();
            StartAutoScan();

            InitBackgroundMonitorState();
        }

        private void SetControlsEnabled(bool enabled)
        {
            _downloadButton.Enabled = enabled;
            _hotfixesListView.Enabled = enabled;
            tableLayoutPanel1.Enabled = enabled;
            checkBox.Enabled = enabled;
            menuStrip1.Enabled = enabled;
            clearLogsButton.Enabled = enabled;
            clearLogsButton.Enabled = enabled;
        }

        private void InitBackgroundMonitorState()
        {
            var config = AppConfig.GetOrLoad<MainConfig>();

            // Устанавливаем галочку в UI (событие CheckedChanged сработает автоматически, если подписаться ПОСЛЕ)
            // Но лучше управлять явно:
            checkBoxAutoScan.Checked = config.EnableBackgroundCheck;

            // Подписываемся на событие изменения
            checkBoxAutoScan.CheckedChanged += CheckBoxAutoScan_CheckedChanged;

            // Если включено в конфиге — запускаем процесс сразу
            if (config.EnableBackgroundCheck)
            {
                StartBackgroundMonitor();
            }
        }

        // 2. Обработчик нажатия на ЧекБокс
        private void CheckBoxAutoScan_CheckedChanged(object? sender, EventArgs e)
        {
            bool isEnabled = checkBoxAutoScan.Checked;

            // Сохраняем в конфиг
            var config = AppConfig.GetOrLoad<MainConfig>();
            config.EnableBackgroundCheck = isEnabled;
            config.Save();

            if (isEnabled)
            {
                StartBackgroundMonitor();
            }
            else
            {
                StopBackgroundMonitor();
            }
        }

        // 3. Метод ЗАПУСКА
        private void StartBackgroundMonitor()
        {
            if (_backgroundCts != null) return; // Уже запущено

            _backgroundCts = new CancellationTokenSource();
            // s_logger.Information("Фоновый мониторинг обновлений ВКЛЮЧЕН.");

            // Запускаем задачу (Fire and forget)
            _ = RunBackgroundUpdateLoopAsync(_backgroundCts.Token);
        }

        // 4. Метод ОСТАНОВКИ
        private void StopBackgroundMonitor()
        {
            if (_backgroundCts == null) return;

            try
            {
                _backgroundCts.Cancel();
                _backgroundCts.Dispose();
            }
            catch (ObjectDisposedException)
            {
            }
            finally
            {
                _backgroundCts = null;
            }
        }

        // 5. Сам цикл (Task.Delay)
        private async Task RunBackgroundUpdateLoopAsync(CancellationToken token)
        {
            var config = AppConfig.GetOrLoad<MainConfig>();

            // Берем значение напрямую из конфига
            var interval = TimeSpan.FromMinutes(config.CheckIntervalMinutes);

            // s_logger.Information($"Автопроверка запущена. Интервал: {config.CheckIntervalMinutes} мин.");

            try
            {
                while (!token.IsCancellationRequested)
                {
                    // 1. СНАЧАЛА проверяем (сразу при старте)
                    await CheckAndDownloadUpdatesAutomaticAsync();

                    // 2. ПОТОМ ждем указанное время
                    await Task.Delay(interval, token);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                s_logger.Error(ex, "Критическая ошибка в фоновом цикле.");
                _backgroundCts = null;
                this.RunOnUiThread(() => checkBoxAutoScan.Checked = false);
            }
        }

        // Метод, который выполняется внутри фонового цикла
        private async Task CheckAndDownloadUpdatesAutomaticAsync()
        {
            try
            {
                // 1. Получаем текущую версию (тихо, без блокировки UI)
                string version = await _versionService.GetCurrentAISVersionAsync();

                // Обновляем Label в UI потоке
                this.RunOnUiThread(() => versionLabel.Text = version);

                // 2. Получаем список фиксов для этой версии
                var foundHotfixes = await _hotfixService.GetHotfixesAsync(version);

                if (foundHotfixes.Count == 0)
                {
                    // s_logger.Information("[AutoCheck] Новых обновлений на сайте не найдено.");
                    return;
                }

                // Обновляем список в UI (чтобы пользователь видел, что что-то нашлось)
                this.RunOnUiThread(() => UpdateHotfixesList(foundHotfixes));

                // 3. Определяем путь для скачивания
                string downloadBase = GetDownloadPath(); // Используем существующий метод
                string fixesPath = Path.Combine(downloadBase, "Fixes");

                if (!Directory.Exists(fixesPath))
                {
                    Directory.CreateDirectory(fixesPath);
                }

                int newDownloadedCount = 0;

                // 4. Проверяем каждый фикс: скачан ли он уже?
                foreach (var hotfix in foundHotfixes)
                {
                    string localFilePath = Path.Combine(fixesPath, hotfix.Name);

                    // Если файл уже есть — пропускаем
                    if (File.Exists(localFilePath))
                    {
                        continue;
                    }

                    s_logger.Information($"[AutoCheck] Найден новый файл: {hotfix.Name}. Скачиваю...");

                    // Создаем пустой прогресс, так как в фоне нам не нужно дергать ProgressBar
                    var progress = new Progress<int>();

                    await _hotfixService.DownloadHotfixAsync(hotfix, fixesPath, progress);
                    newDownloadedCount++;
                }

                // 5. Итог
                if (newDownloadedCount > 0)
                {
                    s_logger.Information($"[AutoCheck] Успешно загружено новых обновлений: {newDownloadedCount}.");

                    // Можно показать всплывающее уведомление в трее, если нужно:
                    // notifyIcon.ShowBalloonTip(3000, "AIS Manager", $"Загружено обновлений: {newDownloadedCount}", ToolTipIcon.Info);
                }
            }
            catch (Exception ex)
            {
                // Логируем ошибку, но не показываем MessageBox, чтобы не мешать работе
                s_logger.Error(ex, "[AutoCheck] Ошибка при автоматической проверке обновлений.");
            }
        }

        // Не забудьте обновить закрытие формы, чтобы остановить поток корректно

        private void LoadConfigSettings()
        {
            MainConfig config = AppConfig.GetOrLoad<MainConfig>();
            // splitContainer1.Panel2Collapsed = config.LogPanelCollapsed;

            if (!string.IsNullOrEmpty(config.DownloadPath) && Directory.Exists(config.DownloadPath))
            {
                _customDownloadPath = config.DownloadPath;
            }
            else if (!string.IsNullOrEmpty(config.DownloadPath))
            {
                config.DownloadPath = null;
                config.Save();
            }
        }

        private void SetupHotfixesListView()
        {
            _hotfixesListView.CheckBoxes = true;
            if (_hotfixesListView.View == View.List && _hotfixesListView.Columns.Count > 0)
            {
                _hotfixesListView.Columns.Clear();
            }
            else if (_hotfixesListView.View == View.Details)
            {
                _hotfixesListView.FullRowSelect = true;
                if (_hotfixesListView.Columns.Count == 0)
                {
                    _hotfixesListView.Columns.Add("Имя обновления", -2);
                }
            }
            _hotfixesListView.MultiSelect = false;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            StopBackgroundMonitor(); // Используем наш метод остановки
            WinFormSink.LogEmitted -= AppendLogToTextBoxFromSink;
            base.OnFormClosed(e);
        }

        private void AppendLogToTextBoxFromSink(string message)
        {
            if (_logTextBox.InvokeRequired)
            {
                _logTextBox.Invoke(new Action(() =>
                {
                    _logTextBox.AppendText(message);
                    _logTextBox.ScrollToCaret();
                }));
            }
            else
            {
                _logTextBox.AppendText(message);
                _logTextBox.ScrollToCaret();
            }
        }

        private void StartAutoScan()
        {
            checkBox.Enabled = false;
            checkBox.CheckedChanged += CheckBox_SelectAll_CheckedChanged;

            checkVersionButton.Click += OnCheckVersionClick;
            _downloadButton.Click += OnDownloadClick;
        }

        private async void OnCheckVersionClick(object sender, EventArgs e)
        {
            try
            {
                SetControlsEnabled(false);
                checkVersionButton.Enabled = false;

                _currentVersion = await _versionService.GetCurrentAISVersionAsync();

                versionLabel.Text = _currentVersion;
                // s_logger.Information("Актуальная версия АИС: {Version}", _currentVersion);

                _hotfixes = await _hotfixService.GetHotfixesAsync(_currentVersion);

                UpdateHotfixesList(_hotfixes);
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при проверке версии: {ex.Message}", ex);
            }
            finally
            {
                checkVersionButton.Enabled = true;
                SetControlsEnabled(true);
            }
        }

        private async void OnDownloadClick(object sender, EventArgs e)
        {
            if (_hotfixesListView.CheckedItems.Count == 0)
            {
                ShowError("Выберите обновления для загрузки");
                return;
            }

            try
            {
                SetControlsEnabled(false);
                _downloadButton.Enabled = false;

                string downloadBase = GetDownloadPath();
                string fixesPath = Path.Combine(downloadBase, "Fixes");

                if (!Directory.Exists(fixesPath))
                {
                    Directory.CreateDirectory(fixesPath);
                    s_logger.Information("Создана директория для загрузки: {Path}", fixesPath);
                }
                else
                {
                    s_logger.Information("Фиксы будут сохранены в папку: {Path}", fixesPath);
                }

                var progress = new Progress<int>(value =>
                {
                    this.RunOnUiThread(() => _progressBar.Value = value);
                });

                int successCount = 0;

                foreach (ListViewItem selectedItem in _hotfixesListView.CheckedItems)
                {
                    if (selectedItem.Tag is not HotfixInfo hotfix)
                    {
                        s_logger.Error("Критическая ошибка: не удалось получить данные для элемента '{ItemText}'. Пропуск.", selectedItem.Text);
                        continue;
                    }

                    await _hotfixService.DownloadHotfixAsync(hotfix, fixesPath, progress);
                    successCount++;
                }

                s_logger.Information("Загрузка завершена. Скачано обновлений: {Count}.", successCount);
                MessageBox.Show(this, $"Загрузка {successCount} обновлений завершена.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при загрузке обновлений: {ex.Message}", ex);
            }
            finally
            {
                _downloadButton.Enabled = true;
                _progressBar.Value = 0;
                SetControlsEnabled(true);
            }
        }

        private void ShowError(string message, Exception ex = null)
        {
            if (ex != null)
            {
                s_logger.Error(ex, "{Message}", message);
            }
            else
            {
                s_logger.Error("{Message}", message);
            }

            this.RunOnUiThread(() =>
            {
                MessageBox.Show(this, message, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            });
        }

        private void UpdateHotfixesList(List<HotfixInfo> hotfixes)
        {
            _hotfixesListView.Items.Clear();
            checkBox.Checked = false;

            if (hotfixes.Any())
            {
                var items = hotfixes.Select(h =>
                {
                    var item = new ListViewItem(h.Name);
                    item.Tag = h;
                    return item;
                }).ToArray();

                _hotfixesListView.Items.AddRange(items);
                checkBox.Enabled = true;
                s_logger.Information("Найдено обновлений: {Count}.", hotfixes.Count);
            }
            else
            {
                checkBox.Enabled = false;
                s_logger.Information("Обновления не найдены.");
            }

            _downloadButton.Enabled = hotfixes.Count > 0;
        }

        private string GetDownloadPath()
        {
            if (!string.IsNullOrEmpty(_customDownloadPath) && Directory.Exists(_customDownloadPath))
            {
                return _customDownloadPath;
            }
            return Application.StartupPath;
        }

        private void CheckBox_SelectAll_CheckedChanged(object sender, EventArgs e)
        {
            bool selectAllState = checkBox.Checked;
            foreach (ListViewItem item in _hotfixesListView.Items)
            {
                item.Checked = selectAllState;
            }
        }

        private void ButtonClearLogs_Click(object sender, EventArgs e)
        {
            if (_logTextBox != null && !_logTextBox.IsDisposed)
            {
                if (_logTextBox.InvokeRequired)
                {
                    _logTextBox.Invoke(new Action(() =>
                    {
                        _logTextBox.Clear();
                    }));
                }
                else
                {
                    _logTextBox.Clear();
                }
            }
        }

        private void OpenApplicationFolder_Click(object sender, EventArgs e)
        {
            Process.Start(
                new ProcessStartInfo
                {
                    FileName = AppPath.DataPath,
                    UseShellExecute = true,
                    Verb = "open"
                }
            );
        }

        private void OpenDownloadFolder_Click(object sender, EventArgs e)
        {
            string folderPath;

            if (!string.IsNullOrEmpty(_customDownloadPath) && Directory.Exists(_customDownloadPath))
            {
                folderPath = _customDownloadPath;
            }
            else
            {
                folderPath = Application.StartupPath;
            }

            var finalPath = Path.Combine(folderPath, "Fixes");

            if (Directory.Exists(finalPath))
            {
                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = finalPath,
                        UseShellExecute = true,
                        Verb = "open"
                    }
                );
            }
            else
            {
                s_logger.Information($"Папка не существует: {finalPath}", true);
            }
        }

        private void ScanOnF5Key(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
                s_logger.Information("Нажата клавиша F5, запуск поиска обновлений...");
                checkVersionButton.PerformClick();
            }
        }

        private void labelLog_Click(object sender, EventArgs e)
        {
            splitContainer1.Panel2Collapsed = !splitContainer1.Panel2Collapsed;
            MainConfig config = AppConfig.GetOrLoad<MainConfig>();
            config.LogPanelCollapsed = splitContainer1.Panel2Collapsed;
            config.Save();
        }

        private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = @"Выберите папку для сохранения фиксов";
                folderDialog.ShowNewFolderButton = true;

                if (!string.IsNullOrEmpty(_customDownloadPath) && Directory.Exists(_customDownloadPath))
                {
                    folderDialog.SelectedPath = _customDownloadPath;
                }
                else
                {
                    folderDialog.SelectedPath = Application.StartupPath;
                }

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    _customDownloadPath = folderDialog.SelectedPath;
                    s_logger.Information($"Путь для сохранения фиксов изменен на: '{_customDownloadPath}'");

                    MainConfig config = AppConfig.GetOrLoad<MainConfig>();
                    config.DownloadPath = _customDownloadPath;
                    config.Save();
                }
            }
        }

        private void SettingFormClick(object sender, EventArgs e)
        {
            SettingForm.ShowModal();
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            if (AppConfig.GetOrLoad<MainConfig>().AutoStartScan)
            {
                StartAutoScan();
            }
        }

        private void checkBox_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void label_Click(object sender, EventArgs e)
        {
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
        }

        private void panel3_Paint(object sender, PaintEventArgs e)
        {
        }
    }
}