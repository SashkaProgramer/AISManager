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

        public string AutoCheckStatusText => IsFullAuto ? "Автопроверка активна" : "Автопроверка выключена";
        public string AutoCheckStatusColor => IsFullAuto ? "#48BB78" : "#718096";


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

        public ICommand CheckDistrosCommand { get; }
        public ICommand DownloadDistroCommand { get; }
        public ICommand SelectDistroDownloadPathCommand { get; }

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

            CheckDistrosCommand = new RelayCommand(async _ => await CheckDistrosAsync());
            DownloadDistroCommand = new RelayCommand(async _ => await DownloadLatestDistroAsync(), _ => !IsBusy && LatestDistro != null);
            SelectDistroDownloadPathCommand = new RelayCommand(_ => BrowseFolder("Выберите папку для загрузки дистрибутивов", path => DistroDownloadPath = path));

            Updates.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (UpdateFile item in e.NewItems)
                        item.PropertyChanged += (s2, e2) => { if (e2.PropertyName == nameof(UpdateFile.IsSelected)) OnPropertyChanged(nameof(IsAllSelected)); };
                }
                OnPropertyChanged(nameof(IsAllSelected));
            };

            if (IsFullAuto) StartAutoCheck();

            // Initial check
            Task.Run(CheckUpdatesAsync);
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
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => await CheckUpdatesAsync());
                }
            }, null, TimeSpan.FromMinutes(_config.AutoCheckIntervalMinutes), TimeSpan.FromMinutes(_config.AutoCheckIntervalMinutes));
        }

        private void StopAutoCheck()
        {
            _autoCheckTimer?.Dispose();
            _autoCheckTimer = null;
            AddLog("Автопроверка обновлений выключена.");
        }

        private async Task CheckUpdatesAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                CurrentVersion = await _versionService.GetCurrentAISVersionAsync();
                var hotfixes = await _hotfixService.GetHotfixesAsync(CurrentVersion);

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    var existingFiles = Updates.Select(u => u.FileName).ToHashSet();
                    int newCount = 0;
                    foreach (var hf in hotfixes)
                    {
                        if (!existingFiles.Contains(hf.Name))
                        {
                            var newFile = new UpdateFile
                            {
                                HotfixData = hf,
                                StatusText = "НОВОЕ",
                                StatusBgColor = "#BEE3F8",
                                StatusFgColor = "#2B6CB0",
                                IsSelected = AutoDownload // Авто-выбор если включена автозагрузка
                            };
                            Updates.Add(newFile);
                            newCount++;
                        }
                    }

                    if (newCount > 0)
                    {
                        AddLog($"Найдено новых обновлений: {newCount}");

                        if (AutoDownload)
                        {
                            await DownloadSelectedAsync();
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                AddLog($"Ошибка: {ex.Message}");
                _logger.Error(ex, "Error checking updates");
                CurrentVersion = "Ошибка";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task DownloadSelectedAsync()
        {
            var selected = Updates.Where(u => u.IsSelected).ToList();
            if (!selected.Any())
            {
                AddLog("Ничего не выбрано.");
                return;
            }

            IsBusy = true;
            string downloadPath = DownloadPath;
            if (!Directory.Exists(downloadPath))
            {
                try
                {
                    Directory.CreateDirectory(downloadPath);
                }
                catch (Exception ex)
                {
                    AddLog($"Ошибка создания директории {downloadPath}: {ex.Message}");
                    IsBusy = false;
                    return;
                }
            }

            try
            {
                AddLog($"Запуск загрузки ({selected.Count})...");
                foreach (var file in selected)
                {
                    if (file.HotfixData == null) continue;

                    file.IsProcessing = true;
                    file.StatusText = "ЗАГРУЗКА";
                    file.StatusBgColor = "#FEFCBF";
                    file.StatusFgColor = "#D69E2E";

                    var progress = new Progress<int>(p =>
                    {
                        file.ProgressValue = p;
                        file.ProgressText = $"{p}%";
                    });

                    await _hotfixService.DownloadHotfixAsync(file.HotfixData, downloadPath, progress);

                    file.StatusText = "ГОТОВО";
                    file.StatusBgColor = "#48BB78";
                    file.StatusFgColor = "White";
                    file.IsProcessing = false;
                }

                if (_config.AutoSfx)
                {
                    await _archiveProcessorService.ProcessDownloadedHotfixesAsync(downloadPath, _config);
                }

                AddLog("Готово.");
            }
            catch (Exception ex)
            {
                AddLog($"Ошибка при скачивании: {ex.Message}");
                _logger.Error(ex, "Error downloading");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CheckDistrosAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                AddLog("Проверка обновлений дистрибутивов на FTP...");
                LatestDistro = await _distroService.GetLatestDistroAsync();

                if (LatestDistro != null)
                {
                    AddLog($"Найдена последняя версия дистрибутива: {LatestDistro.Version} ({LatestDistro.FileName})");
                }
                else
                {
                    AddLog("Не удалось найти дистрибутивы на FTP.");
                }
            }
            catch (Exception ex)
            {
                AddLog($"Ошибка при проверке дистрибутивов: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task DownloadLatestDistroAsync()
        {
            if (LatestDistro == null || IsBusy) return;

            string targetPath = DistroDownloadPath;
            if (string.IsNullOrWhiteSpace(targetPath))
            {
                AddLog("Ошибка: Не указан путь для загрузки дистрибутивов в настройках.");
                return;
            }

            IsBusy = true;
            try
            {
                LatestDistro.IsDownloading = true;
                LatestDistro.DownloadStatus = "ЗАГРУЗКА";
                AddLog($"Начало загрузки дистрибутива {LatestDistro.FileName}...");

                var progress = new Progress<int>(p =>
                {
                    if (LatestDistro != null)
                        LatestDistro.Progress = p;
                });

                await _distroService.DownloadDistroAsync(LatestDistro, targetPath, progress);

                LatestDistro.DownloadStatus = "ГОТОВО";
                AddLog($"Дистрибутив успешно скачан в: {targetPath}");
            }
            catch (Exception ex)
            {
                AddLog($"Ошибка при скачивании дистрибутива: {ex.Message}");
                if (LatestDistro != null)
                    LatestDistro.DownloadStatus = "ОШИБКА";
            }
            finally
            {
                if (LatestDistro != null)
                    LatestDistro.IsDownloading = false;
                IsBusy = false;
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
