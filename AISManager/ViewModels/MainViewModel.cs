using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using AISManager.Infrastructure;
using AISManager.AppData.Configs;
using AISManager.Models;
using AISManager.Services;
using Serilog;
using AISManager.AppData;
using System.Diagnostics;

namespace AISManager.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly IHotfixService _hotfixService;
        private readonly IVersionService _versionService;
        private readonly IDistroService _distroService;
        private readonly ArchiveProcessorService _archiveProcessorService;
        private readonly ILogger _logger;
        private readonly AppConfig _config;

        private string _currentVersion = "Загрузка...";
        private string _logOutput = "";
        private bool _isBusy;
        private System.Threading.Timer? _autoCheckTimer;
        private System.Threading.Timer? _statusUpdateTimer;
        private DateTime? _lastCheckTime;
        private string _autoCheckStatusText = "Система готова";
        private string _autoCheckStatusColor = "#48BB78";
        private string _lastCheckTimeText = "Не проверялось";
        private System.Threading.CancellationTokenSource? _oeCts;
        private System.Threading.CancellationTokenSource? _promCts;
        private const string DistroOeUrl = "ftp://fap.regions.tax.nalog.ru/AisNalog3/OE/";
        private const string DistroPromUrl = "ftp://fap.regions.tax.nalog.ru/AisNalog3/AisNalog3_PROM/";

        private DistroInfo? _latestDistro;
        public DistroInfo? LatestDistro
        {
            get => _latestDistro;
            set
            {
                if (SetProperty(ref _latestDistro, value))
                    CommandManager.InvalidateRequerySuggested();
            }
        }

        private DistroInfo? _latestAisProm;
        public DistroInfo? LatestAisProm
        {
            get => _latestAisProm;
            set
            {
                if (SetProperty(ref _latestAisProm, value))
                    CommandManager.InvalidateRequerySuggested();
            }
        }

        public ObservableCollection<UpdateFile> Updates { get; } = new();

        public string CurrentVersion
        {
            get => _currentVersion;
            set => SetProperty(ref _currentVersion, value);
        }

        public string LogOutput
        {
            get => _logOutput;
            set => SetProperty(ref _logOutput, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetProperty(ref _isBusy, value))
                    CommandManager.InvalidateRequerySuggested();
            }
        }

        private string _busyMessage = "";
        public string BusyMessage
        {
            get => _busyMessage;
            set => SetProperty(ref _busyMessage, value);
        }

        public string AutoCheckStatusText
        {
            get => _autoCheckStatusText;
            set => SetProperty(ref _autoCheckStatusText, value);
        }

        public string AutoCheckStatusColor
        {
            get => _autoCheckStatusColor;
            set => SetProperty(ref _autoCheckStatusColor, value);
        }

        public string LastCheckTimeText
        {
            get => _lastCheckTimeText;
            set => SetProperty(ref _lastCheckTimeText, value);
        }

        public bool IsAutoCheckEnabled
        {
            get => _config.IsAutoCheckEnabled;
            set
            {
                if (_config.IsAutoCheckEnabled != value)
                {
                    _config.IsAutoCheckEnabled = value;
                    _config.Save();
                    if (value) StartAutoCheck();
                    else StopAutoCheck();
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsFullAuto));
                    OnPropertyChanged(nameof(AutoCheckStatusText));
                    OnPropertyChanged(nameof(AutoCheckStatusColor));
                }
            }
        }

        public bool IsFullAuto
        {
            get => _config.IsAutoCheckEnabled && _config.AutoSfx && _config.AutoDownload;
            set
            {
                _config.IsAutoCheckEnabled = value;
                _config.AutoSfx = value;
                _config.AutoDownload = value;
                _config.Save();

                if (value) StartAutoCheck();
                else StopAutoCheck();

                OnPropertyChanged();
                OnPropertyChanged(nameof(AutoDownload));
                OnPropertyChanged(nameof(AutoCheckStatusText));
                OnPropertyChanged(nameof(AutoCheckStatusColor));
            }
        }

        public bool AutoDownload
        {
            get => _config.AutoDownload;
            set
            {
                if (_config.AutoDownload != value)
                {
                    _config.AutoDownload = value;
                    _config.Save();
                    OnPropertyChanged();
                }
            }
        }

        public bool AutoDownloadDistro
        {
            get => _config.AutoDownloadDistro;
            set
            {
                if (_config.AutoDownloadDistro != value)
                {
                    _config.AutoDownloadDistro = value;
                    _config.Save();
                    OnPropertyChanged();
                }
            }
        }

        public bool AutoDownloadAisNalog3
        {
            get => _config.AutoDownloadAisNalog3;
            set
            {
                if (_config.AutoDownloadAisNalog3 != value)
                {
                    _config.AutoDownloadAisNalog3 = value;
                    _config.Save();
                    OnPropertyChanged();
                }
            }
        }

        // The original AutoCheckStatusText and AutoCheckStatusColor properties are now replaced by the new settable properties above.
        // public string AutoCheckStatusText => IsFullAuto ? "Автопроверка активна" : "Автопроверка выключена";
        // public string AutoCheckStatusColor => IsFullAuto ? "#48BB78" : "#718096";


        public string DownloadPath
        {
            get => _config.DownloadPath;
            set
            {
                if (_config.DownloadPath != value)
                {
                    _config.DownloadPath = value;
                    _config.Save();
                    OnPropertyChanged();
                }
            }
        }

        public string SfxOutputPath
        {
            get => _config.SfxOutputPath;
            set
            {
                if (_config.SfxOutputPath != value)
                {
                    _config.SfxOutputPath = value;
                    _config.Save();
                    OnPropertyChanged();
                }
            }
        }

        public string DistroDownloadPath
        {
            get => _config.DistroDownloadPath;
            set
            {
                if (_config.DistroDownloadPath != value)
                {
                    _config.DistroDownloadPath = value;
                    _config.Save();
                    OnPropertyChanged();
                }
            }
        }

        public string AisNalog3DownloadPath
        {
            get => _config.AisNalog3DownloadPath;
            set
            {
                if (_config.AisNalog3DownloadPath != value)
                {
                    _config.AisNalog3DownloadPath = value;
                    _config.Save();
                    OnPropertyChanged();
                }
            }
        }


        public bool IsAllSelected
        {
            get => Updates.Any() && Updates.All(x => x.IsSelected);
            set
            {
                foreach (var update in Updates)
                {
                    update.IsSelected = value;
                }
                OnPropertyChanged();
            }
        }

        private bool _isLogVisible = true;
        public bool IsLogVisible
        {
            get => _isLogVisible;
            set
            {
                if (SetProperty(ref _isLogVisible, value))
                {
                    OnPropertyChanged(nameof(LogVisibility));
                }
            }
        }

        public Visibility LogVisibility => IsLogVisible ? Visibility.Visible : Visibility.Collapsed;

        public GridLength LogHeight
        {
            get => new GridLength(_config.LogHeight > 0 ? _config.LogHeight : 150, GridUnitType.Pixel);
            set
            {
                if (_config.LogHeight != value.Value)
                {
                    _config.LogHeight = value.Value;
                    _config.Save();
                    OnPropertyChanged();
                }
            }
        }

        public ICommand CheckUpdatesCommand { get; }
        public ICommand DownloadCommand { get; }
        public ICommand ManualDownloadCommand { get; }
        public ICommand SelectDownloadPathCommand { get; }
        public ICommand SelectSfxOutputPathCommand { get; }
        public ICommand OpenAppDataCommand { get; }
        public ICommand ToggleLogCommand { get; }
        public ICommand ClearLogsCommand { get; }
        public ICommand AutoSetupPathsCommand { get; }

        public ICommand CheckDistrosCommand { get; }
        public ICommand DownloadDistroCommand { get; }
        public ICommand SelectDistroDownloadPathCommand { get; }

        public ICommand CheckAisPromCommand { get; }
        public ICommand DownloadAisPromCommand { get; }
        public ICommand CancelDownloadOeCommand { get; }
        public ICommand CancelDownloadPromCommand { get; }
        public ICommand SelectAisPromDownloadPathCommand { get; }

        public MainViewModel()
        {
            _config = AppConfig.Instance;
            _config.Load();
            _hotfixService = new HotfixService();
            _versionService = new VersionService();
            _distroService = new DistroService();
            _distroService.OnLog = msg => System.Windows.Application.Current.Dispatcher.Invoke(() => AddLog(msg));
            _archiveProcessorService = new ArchiveProcessorService();
            _archiveProcessorService.OnLog = msg => System.Windows.Application.Current.Dispatcher.Invoke(() => AddLog(msg));

            _logger = Log.ForContext<MainViewModel>();

            CheckUpdatesCommand = new RelayCommand(async _ => await CheckUpdatesAsync());
            DownloadCommand = new RelayCommand(async _ => await DownloadSelectedAsync(), _ => !IsBusy);
            ManualDownloadCommand = new RelayCommand(_ => System.Windows.MessageBox.Show("Функция ручного скачивания пока не реализована."));

            SelectDownloadPathCommand = new RelayCommand(_ => BrowseFolder("Выберите папку для загрузки архивов", path => DownloadPath = path));
            SelectSfxOutputPathCommand = new RelayCommand(_ => BrowseFolder("Выберите папку для FIX_№.exe", path => SfxOutputPath = path));
            OpenAppDataCommand = new RelayCommand(_ => OpenAppDataFolder());
            ToggleLogCommand = new RelayCommand(_ => IsLogVisible = !IsLogVisible);
            ClearLogsCommand = new RelayCommand(_ => ClearLogs());
            AutoSetupPathsCommand = new RelayCommand(_ => AutoSetupPaths());

            CheckDistrosCommand = new RelayCommand(async _ => await CheckDistrosAsync());
            DownloadDistroCommand = new RelayCommand(async _ => await DownloadLatestDistroAsync(), _ => !IsBusy && LatestDistro != null);
            SelectDistroDownloadPathCommand = new RelayCommand(_ => BrowseFolder("Выберите папку для загрузки дистрибутивов", path => DistroDownloadPath = path));

            CheckAisPromCommand = new RelayCommand(async _ => await CheckAisPromAsync());
            DownloadAisPromCommand = new RelayCommand(async _ => await DownloadLatestAisPromAsync(), _ => !IsBusy && LatestAisProm != null);
            CancelDownloadOeCommand = new RelayCommand(_ => _oeCts?.Cancel(), _ => _oeCts != null);
            CancelDownloadPromCommand = new RelayCommand(_ => _promCts?.Cancel(), _ => _promCts != null);
            SelectAisPromDownloadPathCommand = new RelayCommand(_ => BrowseFolder("Выберите папку для установки АИС Налог 3 (Пром)", path => AisNalog3DownloadPath = path));

            _statusUpdateTimer = new System.Threading.Timer(_ =>
                System.Windows.Application.Current.Dispatcher.Invoke(UpdateLastCheckText),
                null, TimeSpan.Zero, TimeSpan.FromSeconds(30));

            Updates.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (UpdateFile item in e.NewItems)
                        item.PropertyChanged += (s2, e2) => { if (e2.PropertyName == nameof(UpdateFile.IsSelected)) OnPropertyChanged(nameof(IsAllSelected)); };
                }
                OnPropertyChanged(nameof(IsAllSelected));
            };

            if (IsAutoCheckEnabled) StartAutoCheck(); // Changed from IsFullAuto to IsAutoCheckEnabled

            // Initial check
            Task.Run(async () => await CheckAllAsync());
        }

        private void UpdateLastCheckText()
        {
            if (!_lastCheckTime.HasValue)
            {
                LastCheckTimeText = "Не проверялось";
                return;
            }

            var diff = DateTime.Now - _lastCheckTime.Value;
            if (diff.TotalSeconds < 60)
                LastCheckTimeText = "Обновлено: только что";
            else if (diff.TotalMinutes < 60)
                LastCheckTimeText = $"Обновлено: {(int)diff.TotalMinutes} мин. назад";
            else
                LastCheckTimeText = $"Обновлено: {_lastCheckTime.Value:HH:mm}";
        }

        private void OpenAppDataFolder()
        {
            try
            {
                var path = AppPath.DataPath;
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch (Exception ex)
            {
                AddLog($"Ошибка при открытии папки: {ex.Message}");
            }
        }

        private void BrowseFolder(string description, Action<string> onSelected)
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = description,
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                onSelected(dialog.SelectedPath);
            }
        }

        private void AutoSetupPaths()
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Выберите место (диск или папка), где будет создана папка AIS_Files со всей структурой",
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var targetRoot = dialog.SelectedPath;
                try
                {
                    // Создаем главную объединяющую папку
                    string mainDir = Path.Combine(targetRoot, "AIS_Files");

                    // Сами подпапки
                    string hf = Path.Combine(mainDir, "Hotfixes");
                    string oe = Path.Combine(mainDir, "Distros_OE");
                    string prom = Path.Combine(mainDir, "Distros_PROM");
                    string sfx = Path.Combine(mainDir, "Ready_FIXES");

                    // Создаем всю структуру (Directory.CreateDirectory создаст и родителю тоже)
                    if (!Directory.Exists(hf)) Directory.CreateDirectory(hf);
                    if (!Directory.Exists(oe)) Directory.CreateDirectory(oe);
                    if (!Directory.Exists(prom)) Directory.CreateDirectory(prom);
                    if (!Directory.Exists(sfx)) Directory.CreateDirectory(sfx);

                    DownloadPath = hf;
                    DistroDownloadPath = oe;
                    AisNalog3DownloadPath = prom;
                    SfxOutputPath = sfx;

                    AddLog($"Авто-настройка успешно завершена! Создана структура в: {mainDir}");
                    System.Windows.MessageBox.Show($"Все пути настроены!\n\nСоздана папка: {mainDir}\n\nТеперь всё готово к работе.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    AddLog($"Ошибка при авто-настройке: {ex.Message}");
                    System.Windows.MessageBox.Show($"Не удалось создать структуру папок: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private bool EnsurePath(string path, string description, Action<string> setter)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                var result = System.Windows.MessageBox.Show(
                    $"Не указан или не существует путь: {description}.\nХотите выбрать папку сейчас?",
                    "Требуется настройка",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    using var dialog = new System.Windows.Forms.FolderBrowserDialog
                    {
                        Description = $"Выберите {description}",
                        UseDescriptionForTitle = true
                    };

                    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        setter(dialog.SelectedPath);
                        return true;
                    }
                }
                return false;
            }
            return true;
        }

        private void BrowseFile(string filter, Action<string> onSelected)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = filter
            };

            if (dialog.ShowDialog() == true)
            {
                onSelected(dialog.FileName);
            }
        }

        private void StartAutoCheck()
        {
            AddLog($"Автопроверка обновлений включена (интервал {_config.AutoCheckIntervalMinutes} мин).");
            _autoCheckTimer = new System.Threading.Timer(async _ =>
            {
                if (!IsBusy)
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => await CheckAllAsync());
                }
            }, null, TimeSpan.FromMinutes(_config.AutoCheckIntervalMinutes), TimeSpan.FromMinutes(_config.AutoCheckIntervalMinutes));
        }

        private void StopAutoCheck()
        {
            _autoCheckTimer?.Dispose();
            _autoCheckTimer = null;
            AddLog("Автопроверка обновлений выключена.");
        }

        private async Task CheckAllAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                // 1. Сначала только проверяем наличие версий (это быстро)
                BusyMessage = "Проверка версий...";

                // Проверка AIS и Hotfixes (только получение списка)
                CurrentVersion = await _versionService.GetCurrentAISVersionAsync();
                var hotfixes = await _hotfixService.GetHotfixesAsync(CurrentVersion);

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var existingFiles = Updates.Select(u => u.FileName).ToHashSet();
                    foreach (var hf in hotfixes)
                    {
                        if (!existingFiles.Contains(hf.Name))
                        {
                            Updates.Add(new UpdateFile
                            {
                                HotfixData = hf,
                                StatusText = "НОВОЕ",
                                StatusBgColor = "#BEE3F8",
                                StatusFgColor = "#2B6CB0",
                                IsSelected = AutoDownload
                            });
                        }
                    }
                });

                // Проверка OE по FTP
                BusyMessage = "Проверка дистрибутивов OE...";
                LatestDistro = await _distroService.GetLatestDistroAsync(DistroOeUrl);

                // Проверка Пром по FTP
                BusyMessage = "Проверка дистрибутивов Пром...";
                LatestAisProm = await _distroService.GetLatestDistroAsync(DistroPromUrl);

                // 2. А теперь запускаем закачки, если включена автозагрузка
                // Фиксы
                if (AutoDownload)
                {
                    var selected = Updates.Where(u => u.IsSelected && u.StatusText == "НОВОЕ").ToList();
                    if (selected.Any())
                    {
                        await DownloadSelectedAsync(internalCall: true);
                    }
                }

                // OE
                if (AutoDownloadDistro && LatestDistro != null)
                {
                    string localPath = Path.Combine(DistroDownloadPath, LatestDistro.FileName);
                    if (!File.Exists(localPath))
                    {
                        await DownloadLatestDistroAsync(internalCall: true);
                    }
                }

                // Пром
                if (AutoDownloadAisNalog3 && LatestAisProm != null)
                {
                    string localPath = Path.Combine(AisNalog3DownloadPath, LatestAisProm.FileName);
                    if (!File.Exists(localPath))
                    {
                        await DownloadLatestAisPromAsync(internalCall: true);
                    }
                }

                _lastCheckTime = DateTime.Now;
                AutoCheckStatusText = "Система обновлена";
                AutoCheckStatusColor = "#48BB78";
                UpdateLastCheckText();
            }
            catch (Exception ex)
            {
                AddLog($"Ошибка при общей проверке: {ex.Message}");
                _logger.Error(ex, "Error during CheckAllAsync");
                AutoCheckStatusText = "Ошибка проверки";
                AutoCheckStatusColor = "#E53E3E";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CheckUpdatesAsync()
        {
            if (IsBusy) return;
            await CheckAllAsync(); // Перенаправляем на общий метод для консистентности
        }

        private async Task DownloadSelectedAsync(bool internalCall = false)
        {
            var selected = Updates.Where(u => u.IsSelected).ToList();
            if (!selected.Any())
            {
                if (!internalCall) AddLog("Ничего не выбрано.");
                return;
            }

            if (!internalCall)
            {
                if (!EnsurePath(DownloadPath, "папка для загрузки фиксов", p => DownloadPath = p)) return;
            }

            if (!internalCall) IsBusy = true;
            try
            {
                BusyMessage = "Загрузка и обработка пакетов...";
                string downloadPath = DownloadPath;
                if (!Directory.Exists(downloadPath)) Directory.CreateDirectory(downloadPath);

                AddLog($"Запуск загрузки фиксов ({selected.Count})...");
                foreach (var file in selected)
                {
                    if (file.HotfixData == null) continue;
                    file.IsProcessing = true;
                    file.StatusText = "ЗАГРУЗКА";
                    file.StatusBgColor = "#FEFCBF";
                    file.StatusFgColor = "#D69E2E";

                    var progress = new Progress<int>(p => { file.ProgressValue = p; file.ProgressText = $"{p}%"; });
                    await _hotfixService.DownloadHotfixAsync(file.HotfixData, downloadPath, progress);

                    file.StatusText = "ГОТОВО";
                    file.StatusBgColor = "#48BB78";
                    file.StatusFgColor = "White";
                    file.IsProcessing = false;
                }

                if (_config.AutoSfx)
                {
                    BusyMessage = "Создание SFX...";
                    await _archiveProcessorService.ProcessDownloadedHotfixesAsync(downloadPath, _config);
                }
                AddLog("Загрузка фиксов завершена.");
            }
            finally
            {
                if (!internalCall) IsBusy = false;
            }
        }

        private async Task CheckDistrosAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                BusyMessage = "Проверка дистрибутивов OE...";
                LatestDistro = await _distroService.GetLatestDistroAsync(DistroOeUrl);
            }
            finally { IsBusy = false; }
        }

        private async Task CheckAisPromAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                BusyMessage = "Проверка дистрибутивов Пром...";
                LatestAisProm = await _distroService.GetLatestDistroAsync(DistroPromUrl);
            }
            finally { IsBusy = false; }
        }

        private async Task DownloadLatestDistroAsync(bool internalCall = false)
        {
            if (LatestDistro == null) return;
            if (!internalCall && IsBusy) return;

            if (!internalCall)
            {
                if (!EnsurePath(DistroDownloadPath, "папка для загрузки OE", p => DistroDownloadPath = p)) return;
            }

            if (!internalCall) IsBusy = true;
            _oeCts = new System.Threading.CancellationTokenSource();
            try
            {
                BusyMessage = $"Загрузка OE: {LatestDistro.Version}";
                LatestDistro.IsDownloading = true;
                LatestDistro.DownloadStatus = "ЗАГРУЗКА";
                LatestDistro.Progress = 0;

                var progress = new Progress<int>(p => { if (LatestDistro != null) LatestDistro.Progress = p; });
                await _distroService.DownloadDistroAsync(LatestDistro, DistroDownloadPath, progress, _oeCts.Token);

                LatestDistro.DownloadStatus = "ГОТОВО";
                AddLog($"OE скачан: {LatestDistro.FileName}");
            }
            catch (OperationCanceledException)
            {
                AddLog("Загрузка OE отменена пользователем.");
                if (LatestDistro != null) LatestDistro.DownloadStatus = "ОТМЕНЕНО";
            }
            catch (Exception ex)
            {
                AddLog($"Ошибка OE: {ex.Message}");
                if (LatestDistro != null) LatestDistro.DownloadStatus = "ОШИБКА";
            }
            finally
            {
                if (LatestDistro != null) LatestDistro.IsDownloading = false;
                if (!internalCall) IsBusy = false;
                _oeCts.Dispose();
                _oeCts = null;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private async Task DownloadLatestAisPromAsync(bool internalCall = false)
        {
            if (LatestAisProm == null) return;
            if (!internalCall && IsBusy) return;

            if (!internalCall)
            {
                if (!EnsurePath(AisNalog3DownloadPath, "папка для загрузки Пром", p => AisNalog3DownloadPath = p)) return;
            }

            if (!internalCall) IsBusy = true;
            _promCts = new System.Threading.CancellationTokenSource();
            try
            {
                BusyMessage = $"Загрузка Пром: {LatestAisProm.Version}";
                LatestAisProm.IsDownloading = true;
                LatestAisProm.DownloadStatus = "ЗАГРУЗКА";
                LatestAisProm.Progress = 0;

                var progress = new Progress<int>(p => { if (LatestAisProm != null) LatestAisProm.Progress = p; });
                await _distroService.DownloadDistroAsync(LatestAisProm, AisNalog3DownloadPath, progress, _promCts.Token);

                LatestAisProm.DownloadStatus = "ГОТОВО";
                AddLog($"АИС Налог 3 (Пром) скачан: {LatestAisProm.FileName}");
            }
            catch (OperationCanceledException)
            {
                AddLog("Загрузка Пром отменена пользователем.");
                if (LatestAisProm != null) LatestAisProm.DownloadStatus = "ОТМЕНЕНО";
            }
            catch (Exception ex)
            {
                AddLog($"Ошибка Пром: {ex.Message}");
                if (LatestAisProm != null) LatestAisProm.DownloadStatus = "ОШИБКА";
            }
            finally
            {
                if (LatestAisProm != null) LatestAisProm.IsDownloading = false;
                if (!internalCall) IsBusy = false;
                _promCts.Dispose();
                _promCts = null;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private void AddLog(string message)
        {
            var msg = $"[{DateTime.Now:HH:mm:ss}] {message}";
            LogOutput += msg + "\n";
        }

        private void ClearLogs()
        {
            LogOutput = "";
            try
            {
                var directory = new DirectoryInfo(AppPath.LogsPath);
                if (directory.Exists)
                {
                    foreach (var file in directory.GetFiles())
                    {
                        try
                        {
                            file.Delete();
                        }
                        catch
                        {
                            // Skip files that are in use (like the current log file)
                        }
                    }
                    AddLog("Логи на диске очищены (кроме используемых файлов).");
                }
                else
                {
                    AddLog("Папка с логами не найдена.");
                }
            }
            catch (Exception ex)
            {
                AddLog($"Ошибка при очистке логов: {ex.Message}");
            }
        }
    }
}
