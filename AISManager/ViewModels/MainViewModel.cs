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
        //     
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
        private System.Threading.CancellationTokenSource? _fixesCts;
        private bool _hasNewDistroNotification;
        private const string DistroOeUrl = "ftp://fap.regions.tax.nalog.ru/AisNalog3/OE/";
        private const string DistroPromUrl = "ftp://fap.regions.tax.nalog.ru/AisNalog3/AisNalog3_PROM/";

        private string? _lastAisVersion;
        private string? _lastOeVersion;
        private string? _lastPromVersion;
        private int? _lastHotfixesCount;

        private DistroInfo? _latestDistro;
        public DistroInfo? LatestDistro
        {
            get => _latestDistro;
            set
            {
                if (SetProperty(ref _latestDistro, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private DistroInfo? _latestAisProm;
        public DistroInfo? LatestAisProm
        {
            get => _latestAisProm;
            set
            {
                if (SetProperty(ref _latestAisProm, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
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

        private bool _isFixesBusy;
        public bool IsFixesBusy
        {
            get => _isFixesBusy;
            set
            {
                if (SetProperty(ref _isFixesBusy, value))
                    CommandManager.InvalidateRequerySuggested();
            }
        }

        private string _busyMessage = "";
        public string BusyMessage
        {
            get => _busyMessage;
            set => SetProperty(ref _busyMessage, value);
        }

        private double _totalFixesProgress;
        public double TotalFixesProgress
        {
            get => _totalFixesProgress;
            set => SetProperty(ref _totalFixesProgress, value);
        }

        private bool _isFixesIndeterminate;
        public bool IsFixesIndeterminate
        {
            get => _isFixesIndeterminate;
            set => SetProperty(ref _isFixesIndeterminate, value);
        }

        private string _cancelActionText = "Отменить загрузку";
        public string CancelActionText
        {
            get => _cancelActionText;
            set => SetProperty(ref _cancelActionText, value);
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
                    if (value) HasUnreadLogs = false; // Сбрасываем при открытии
                }
            }
        }

        public Visibility LogVisibility => IsLogVisible ? Visibility.Visible : Visibility.Collapsed;

        private bool _hasUnreadLogs;
        public bool HasUnreadLogs
        {
            get => _hasUnreadLogs;
            set => SetProperty(ref _hasUnreadLogs, value);
        }

        public bool HasNewDistroNotification
        {
            get => _hasNewDistroNotification;
            set => SetProperty(ref _hasNewDistroNotification, value);
        }

        public void MarkDistrosAsRead()
        {
            HasNewDistroNotification = false;
        }

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
        public ICommand CancelDownloadFixesCommand { get; }
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
            _logger.Information("Приложение инициализировано. Запущена работа с конфигурацией и сервисами.");

            CheckUpdatesCommand = new RelayCommand(async _ => await CheckUpdatesAsync());
            DownloadCommand = new RelayCommand(async _ => await DownloadSelectedAsync(forceSfx: true), _ => !IsBusy);
            ManualDownloadCommand = new RelayCommand(_ => System.Windows.MessageBox.Show("Функция ручного скачивания пока не реализована."));
            CancelDownloadFixesCommand = new RelayCommand(_ => _fixesCts?.Cancel(), _ => _fixesCts != null);

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
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => await CheckAllAsync(true));
                }
            }, null, TimeSpan.FromMinutes(_config.AutoCheckIntervalMinutes), TimeSpan.FromMinutes(_config.AutoCheckIntervalMinutes));
        }

        private void StopAutoCheck()
        {
            _autoCheckTimer?.Dispose();
            _autoCheckTimer = null;
            AddLog("Автопроверка обновлений выключена.");
        }

        private async Task CheckAllAsync(bool isSilent = false)
        {
            if (IsBusy) return;
            IsBusy = true;
            IsFixesBusy = true;
            IsFixesIndeterminate = true;
            CancelActionText = "Отменить проверку";

            try
            {
                if (!isSilent) AddLog("Запуск полной проверки...");

                // 1. Пакеты исправлений
                BusyMessage = "Проверка версий АИС и Hotfixes...";

                string newAisVersion = await _versionService.GetCurrentAISVersionAsync();
                if (!isSilent || newAisVersion != _lastAisVersion)
                {
                    CurrentVersion = newAisVersion;
                    AddLog($"Версия АИС: {CurrentVersion}");
                    _lastAisVersion = newAisVersion;
                }
                else
                {
                    CurrentVersion = newAisVersion;
                }

                var hotfixes = await _hotfixService.GetHotfixesAsync(CurrentVersion);

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    string hfPath = DownloadPath;

                    // Собираем ключи (версия + номер) всех файлов, которые уже есть в папке
                    var localKeys = new HashSet<(string, int)>();
                    if (!string.IsNullOrWhiteSpace(hfPath) && Directory.Exists(hfPath))
                    {
                        var files = Directory.GetFiles(hfPath);
                        foreach (var f in files)
                        {
                            var key = ArchiveProcessorService.ParseArchiveName(Path.GetFileName(f));
                            if (key.HasValue) localKeys.Add(key.Value);
                        }
                    }

                    foreach (var hf in hotfixes)
                    {
                        var existing = Updates.FirstOrDefault(u => u.FileName == hf.Name);

                        // Определяем, скачан ли файл, через парсинг его имени
                        bool isDownloaded = false;
                        var hfKey = ArchiveProcessorService.ParseArchiveName(hf.Name);
                        if (hfKey.HasValue && localKeys.Contains(hfKey.Value))
                        {
                            isDownloaded = true;
                        }

                        if (existing == null)
                        {
                            Updates.Add(new UpdateFile
                            {
                                HotfixData = hf,
                                StatusText = isDownloaded ? "ГОТОВО" : "НОВОЕ",
                                StatusBgColor = isDownloaded ? "#48BB78" : "#BEE3F8",
                                StatusFgColor = isDownloaded ? "White" : "#2B6CB0",
                                IsSelected = !isDownloaded && AutoDownload,
                                ProgressValue = isDownloaded ? 100 : 0
                            });
                        }
                        else
                        {
                            // Если файл появился на диске (или пропал), обновляем статус существующего элемента
                            if (isDownloaded && existing.StatusText != "ГОТОВО")
                            {
                                existing.StatusText = "ГОТОВО";
                                existing.StatusBgColor = "#48BB78";
                                existing.StatusFgColor = "White";
                                existing.IsSelected = false;
                                existing.ProgressValue = 100;
                            }
                            else if (!isDownloaded && existing.StatusText == "ГОТОВО")
                            {
                                existing.StatusText = "НОВОЕ";
                                existing.StatusBgColor = "#BEE3F8";
                                existing.StatusFgColor = "#2B6CB0";
                                existing.IsSelected = AutoDownload;
                                existing.ProgressValue = 0;
                            }
                        }
                    }

                    int newCount = Updates.Count(u => u.StatusText == "НОВОЕ");
                    if (!isSilent || hotfixes.Count != _lastHotfixesCount)
                    {
                        if (hotfixes.Count > 0)
                        {
                            AddLog($"Пакеты исправлений: всего {hotfixes.Count} (новых: {newCount})");
                        }
                        else
                        {
                            AddLog("Для текущей версии фиксов не найдено.");
                        }
                        _lastHotfixesCount = hotfixes.Count;
                    }
                });

                // 2. OE
                BusyMessage = "Связь с FTP: Проверка OE...";
                var oeInfo = await _distroService.GetLatestDistroAsync(DistroOeUrl);
                if (oeInfo != null)
                {
                    if (!isSilent || oeInfo.Version != _lastOeVersion)
                    {
                        string prefix = (_lastOeVersion != null && oeInfo.Version != _lastOeVersion) ? "✨ НАЙДЕНА НОВАЯ ВЕРСИЯ! " : "";
                        AddLog($"{prefix}FTP OE: {oeInfo.Version}");
                        _lastOeVersion = oeInfo.Version;
                    }
                    LatestDistro = oeInfo;
                }

                // 3. Пром
                BusyMessage = "Связь с FTP: Проверка Пром...";
                var promInfo = await _distroService.GetLatestDistroAsync(DistroPromUrl);
                if (promInfo != null)
                {
                    if (!isSilent || promInfo.Version != _lastPromVersion)
                    {
                        string prefix = (_lastPromVersion != null && promInfo.Version != _lastPromVersion) ? "✨ НАЙДЕНА НОВАЯ ВЕРСИЯ! " : "";
                        AddLog($"{prefix}FTP Пром: {promInfo.Version}");
                        _lastPromVersion = promInfo.Version;
                    }
                    LatestAisProm = promInfo;
                }

                // Если ручной запуск и ничего не изменилось - добавим маленькое подтверждение
                if (!isSilent && _lastOeVersion == oeInfo?.Version && _lastPromVersion == promInfo?.Version)
                {
                    // Можно было бы что-то добавить, но лучше не мусорить
                }

                // 2. А теперь запускаем закачки, если включена автозагрузка
                // Фиксы
                bool fixesProcessed = false;
                if (AutoDownload)
                {
                    var selected = Updates.Where(u => u.IsSelected && u.StatusText == "НОВОЕ").ToList();
                    if (selected.Any())
                    {
                        await DownloadSelectedAsync(internalCall: true);
                        fixesProcessed = true;
                    }
                }

                // Если скачивание не запускалось, но включена авто-сборка SFX - проверим, нужно ли собрать/обновить SFX
                if (_config.AutoSfx && !fixesProcessed && !string.IsNullOrEmpty(DownloadPath) && Directory.Exists(DownloadPath))
                {
                    var hasArchives = Directory.GetFiles(DownloadPath).Any(f => f.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".rar", StringComparison.OrdinalIgnoreCase));
                    if (hasArchives)
                    {
                        BusyMessage = "Проверка актуальности SFX...";
                        await _archiveProcessorService.ProcessDownloadedHotfixesAsync(DownloadPath, _config);
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
                IsFixesBusy = false;
                IsFixesIndeterminate = false;
                IsBusy = false;
            }
        }

        private async Task CheckUpdatesAsync()
        {
            if (IsBusy) return;
            await CheckAllAsync(); // Перенаправляем на общий метод для консистентности
        }

        private async Task DownloadSelectedAsync(bool internalCall = false, bool forceSfx = false)
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
            IsFixesBusy = true;
            TotalFixesProgress = 0;
            IsFixesIndeterminate = false;
            CancelActionText = "Отменить загрузку";
            _fixesCts = new System.Threading.CancellationTokenSource();
            try
            {
                BusyMessage = "Загрузка и обработка пакетов...";
                string downloadPath = DownloadPath;
                if (!Directory.Exists(downloadPath)) Directory.CreateDirectory(downloadPath);

                // AddLog($"Запуск загрузки фиксов ({selected.Count})...");
                for (int i = 0; i < selected.Count; i++)
                {
                    var file = selected[i];
                    if (file.HotfixData == null) continue;
                    file.IsProcessing = true;
                    file.StatusText = "ЗАГРУЗКА";
                    file.StatusBgColor = "#FEFCBF";
                    file.StatusFgColor = "#D69E2E";

                    int currentFileIndex = i;
                    var progress = new Progress<int>(p =>
                    {
                        file.ProgressValue = p;
                        file.ProgressText = $"{p}%";
                        TotalFixesProgress = ((double)currentFileIndex + (p / 100.0)) / selected.Count * 100;
                    });

                    try
                    {
                        string localFile = Path.Combine(downloadPath, file.FileName);
                        if (File.Exists(localFile))
                        {
                            file.ProgressValue = 100;
                            file.ProgressText = "100%";
                            TotalFixesProgress = ((double)currentFileIndex + 1) / selected.Count * 100;
                            // AddLog($"Файл {file.FileName} уже существует, пропускаем загрузку.");
                        }
                        else
                        {
                            await _hotfixService.DownloadHotfixAsync(file.HotfixData, downloadPath, progress, _fixesCts.Token);
                        }

                        file.StatusText = "ГОТОВО";
                        file.StatusBgColor = "#48BB78";
                        file.StatusFgColor = "White";
                        file.IsProcessing = false;
                    }
                    catch (OperationCanceledException)
                    {
                        file.StatusText = "ОТМЕНЕНО";
                        file.StatusBgColor = "#E2E8F0";
                        file.StatusFgColor = "#4A5568";
                        file.IsProcessing = false;
                        throw; // Прерываем весь цикл загрузки
                    }
                    catch (Exception ex)
                    {
                        file.StatusText = "ОШИБКА";
                        file.StatusBgColor = "#FFF5F5";
                        file.StatusFgColor = "#C53030";
                        file.IsProcessing = false;
                        AddLog($"Ошибка при скачивании {file.FileName}: {ex.Message}");
                    }
                }

                if (_config.AutoSfx || forceSfx)
                {
                    _logger.Information("Запуск автоматической сборки SFX для выбранных фиксов...");
                    BusyMessage = "Создание SFX...";
                    IsFixesIndeterminate = true;
                    // Передаем список выбранных имен файлов, чтобы в SFX попали только они
                    int processedCount = await _archiveProcessorService.ProcessDownloadedHotfixesAsync(downloadPath, _config, selected.Select(s => s.FileName));

                    if (processedCount > 0)
                    {
                        var fixNumbers = selected
                            .Select(f => ArchiveProcessorService.ParseArchiveName(f.FileName))
                            .Where(x => x.HasValue)
                            .Select(x => x!.Value.num);

                        string fixLabel = $"FIX_{ArchiveProcessorService.GenerateFixesString(fixNumbers)}.exe";
                        AddLog($"Сформирован сборник: {fixLabel} (объединено {processedCount} фикса).");
                    }
                }
                else
                {
                    AddLog($"Загрузка фиксов завершена ({selected.Count} шт).");
                }
            }
            catch (OperationCanceledException)
            {
                AddLog("Загрузка фиксов отменена пользователем.");
            }
            finally
            {
                if (!internalCall)
                {
                    IsBusy = false;
                    IsFixesBusy = false;
                    IsFixesIndeterminate = false;
                }
                _fixesCts?.Dispose();
                _fixesCts = null;
            }
        }

        private async Task CheckDistrosAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            CancelActionText = "Отменить поиск";
            try
            {
                BusyMessage = "Связь с FTP: Проверка OE...";
                IsFixesBusy = true;
                IsFixesIndeterminate = true;
                var info = await _distroService.GetLatestDistroAsync(DistroOeUrl);
                if (info != null)
                {
                    AddLog($"Результат проверки OE: {info.Version}");
                    _lastOeVersion = info.Version;
                    LatestDistro = info;
                }
            }
            finally
            {
                IsFixesBusy = false;
                IsFixesIndeterminate = false;
                IsBusy = false;
            }
        }

        private async Task CheckAisPromAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            CancelActionText = "Отменить поиск";
            try
            {
                BusyMessage = "Связь с FTP: Проверка Пром...";
                IsFixesBusy = true;
                IsFixesIndeterminate = true;
                var info = await _distroService.GetLatestDistroAsync(DistroPromUrl);
                if (info != null)
                {
                    AddLog($"Результат проверки Пром: {info.Version}");
                    _lastPromVersion = info.Version;
                    LatestAisProm = info;
                }
            }
            finally
            {
                IsFixesBusy = false;
                IsFixesIndeterminate = false;
                IsBusy = false;
            }
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
            CancelActionText = "Отменить загрузку";
            _oeCts = new System.Threading.CancellationTokenSource();
            try
            {
                BusyMessage = $"Загрузка OE: {LatestDistro.Version}";
                LatestDistro.IsDownloading = true;
                LatestDistro.IsDownloaded = false;
                LatestDistro.DownloadStatus = "ЗАГРУЗКА";
                LatestDistro.Progress = 0;
                LatestDistro.ProgressText = "0 МБ";
                LatestDistro.IsIndeterminate = true; // Сначала indeterminate, пока не поймем размер

                var progress = new Progress<DownloadProgress>(p =>
                {
                    if (LatestDistro != null)
                    {
                        LatestDistro.Progress = p.Percentage;
                        LatestDistro.IsIndeterminate = p.Total <= 0 && p.Percentage < 100;

                        string received = (p.Received / 1024.0 / 1024.0).ToString("F1");
                        if (p.Total > 0)
                        {
                            string total = (p.Total / 1024.0 / 1024.0).ToString("F1");
                            LatestDistro.ProgressText = $"{received} / {total} МБ ({p.Percentage}%)";
                        }
                        else
                        {
                            LatestDistro.ProgressText = $"{received} МБ";
                        }
                    }
                });
                await _distroService.DownloadDistroAsync(LatestDistro, DistroDownloadPath, progress, _oeCts.Token);

                LatestDistro.DownloadStatus = "ГОТОВО";
                LatestDistro.IsDownloaded = true;
                HasNewDistroNotification = true;
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
            CancelActionText = "Отменить загрузку";
            _promCts = new System.Threading.CancellationTokenSource();
            try
            {
                BusyMessage = $"Загрузка Пром: {LatestAisProm.Version}";
                LatestAisProm.IsDownloading = true;
                LatestAisProm.IsDownloaded = false;
                LatestAisProm.DownloadStatus = "ЗАГРУЗКА";
                LatestAisProm.Progress = 0;
                LatestAisProm.ProgressText = "0 МБ";
                LatestAisProm.IsIndeterminate = true;

                var progress = new Progress<DownloadProgress>(p =>
                {
                    if (LatestAisProm != null)
                    {
                        LatestAisProm.Progress = p.Percentage;
                        LatestAisProm.IsIndeterminate = p.Total <= 0 && p.Percentage < 100;

                        string received = (p.Received / 1024.0 / 1024.0).ToString("F1");
                        if (p.Total > 0)
                        {
                            string total = (p.Total / 1024.0 / 1024.0).ToString("F1");
                            LatestAisProm.ProgressText = $"{received} / {total} МБ ({p.Percentage}%)";
                        }
                        else
                        {
                            LatestAisProm.ProgressText = $"{received} МБ";
                        }
                    }
                });
                await _distroService.DownloadDistroAsync(LatestAisProm, AisNalog3DownloadPath, progress, _promCts.Token);

                LatestAisProm.DownloadStatus = "ГОТОВО";
                LatestAisProm.IsDownloaded = true;
                HasNewDistroNotification = true;
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
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                LogOutput += msg + "\n";
                if (!IsLogVisible) HasUnreadLogs = true; // Показываем красную точку, если лог скрыт
            });
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
                    AddLog("Логи очищены.");
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
