using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using AISManager.Infrastructure;
using AISManager.Models;
using AISManager.Services;
using Serilog;

namespace AISManager.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly IHotfixService _hotfixService;
        private readonly IVersionService _versionService;
        private readonly ILogger _logger;

        private string _currentVersion = "Загрузка...";
        private string _logOutput = "";
        private bool _isBusy;
        private bool _autoSfx = true;
        private bool _isAutoCheckEnabled;
        private System.Threading.Timer? _autoCheckTimer;
        private string _winRarPath = @"C:\Program Files\WinRAR\WinRAR.exe";
        private string _downloadPath = @"C:\AIS_Updates\Output";

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
            set => SetProperty(ref _isBusy, value);
        }

        public bool AutoSfx
        {
            get => _autoSfx;
            set => SetProperty(ref _autoSfx, value);
        }

        public bool IsAutoCheckEnabled
        {
            get => _isAutoCheckEnabled;
            set
            {
                if (SetProperty(ref _isAutoCheckEnabled, value))
                {
                    if (value) StartAutoCheck();
                    else StopAutoCheck();
                    
                    OnPropertyChanged(nameof(AutoCheckStatusText));
                    OnPropertyChanged(nameof(AutoCheckStatusColor));
                }
            }
        }

        public string AutoCheckStatusText => IsAutoCheckEnabled ? "Автопроверка активна" : "Автопроверка выключена";
        public string AutoCheckStatusColor => IsAutoCheckEnabled ? "#48BB78" : "#718096";

        public string WinRarPath
        {
            get => _winRarPath;
            set => SetProperty(ref _winRarPath, value);
        }

        public string DownloadPath
        {
            get => _downloadPath;
            set => SetProperty(ref _downloadPath, value);
        }

        public ICommand CheckUpdatesCommand { get; }
        public ICommand DownloadCommand { get; }
        public ICommand ManualDownloadCommand { get; }

        public MainViewModel()
        {
            // In a real app, use DI. Here we manually compose for simplicity.
            _hotfixService = new HotfixService();
            _versionService = new VersionService();
            
            _logger = Log.ForContext<MainViewModel>();

            CheckUpdatesCommand = new RelayCommand(async _ => await CheckUpdatesAsync());
            DownloadCommand = new RelayCommand(async _ => await DownloadSelectedAsync(), _ => !IsBusy);
            ManualDownloadCommand = new RelayCommand(_ => MessageBox.Show("Функция ручного скачивания пока не реализована."));

            // Initial check
            Task.Run(CheckUpdatesAsync);
        }

        private void StartAutoCheck()
        {
            AddLog("Автопроверка обновлений включена (интервал 10 мин).");
            // 10 minutes interval
            _autoCheckTimer = new System.Threading.Timer(async _ => 
            {
                if (!IsBusy) 
                {
                    await Application.Current.Dispatcher.InvokeAsync(async () => await CheckUpdatesAsync());
                }
            }, null, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
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
            // AddLog("Проверка версии АИС..."); // Commented out to reduce spam in auto-check

            try
            {
                CurrentVersion = await _versionService.GetCurrentAISVersionAsync();
                // AddLog($"Текущая версия: {CurrentVersion}");

                // AddLog("Поиск обновлений...");
                var hotfixes = await _hotfixService.GetHotfixesAsync(CurrentVersion);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Merge logic: don't clear if same, but for simplicity we clear and re-add or just update
                    // For now, let's just clear to be safe, but in a real auto-check we might want to preserve selection
                    // Simple approach:
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
                                StatusFgColor = "#2B6CB0"
                            });
                            AddLog($"Найдено новое обновление: {hf.Name}");
                        }
                    }
                });

                if (hotfixes.Count == 0)
                {
                    // AddLog("Обновлений не найдено.");
                }
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
                foreach (var file in selected)
                {
                    if (file.HotfixData == null) continue;

                    file.IsProcessing = true;
                    file.StatusText = "ЗАГРУЗКА";
                    file.StatusBgColor = "#FEFCBF";
                    file.StatusFgColor = "#D69E2E";

                    AddLog($"Скачивание {file.FileName}...");

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
                    AddLog($"Скачано: {file.FileName}");

                    if (AutoSfx && !string.IsNullOrEmpty(file.HotfixData?.LocalPath))
                    {
                        await Task.Run(() => CreateSfx(file.HotfixData.LocalPath));
                    }
                }
                AddLog("Все загрузки завершены.");
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

        private void CreateSfx(string sourceFile)
        {
            if (!File.Exists(WinRarPath))
            {
                AddLog($"Ошибка: WinRAR не найден по пути {WinRarPath}");
                return;
            }

            try
            {
                var fileInfo = new FileInfo(sourceFile);
                var sfxName = Path.ChangeExtension(fileInfo.Name, ".exe");
                var sfxPath = Path.Combine(fileInfo.DirectoryName!, sfxName);

                AddLog($"Создание SFX архива: {sfxName}...");

                // WinRAR command: a -sfx -ep1 "Output.exe" "InputFile"
                var args = $"a -sfx -ep1 \"{sfxPath}\" \"{sourceFile}\"";
                
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = WinRarPath,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var process = System.Diagnostics.Process.Start(psi);
                process?.WaitForExit();

                if (process?.ExitCode == 0)
                {
                    AddLog($"SFX архив создан успешно: {sfxName}");
                }
                else
                {
                    var error = process?.StandardError.ReadToEnd();
                    AddLog($"Ошибка создания SFX. Код: {process?.ExitCode}. {error}");
                }
            }
            catch (Exception ex)
            {
                AddLog($"Ошибка при создании SFX: {ex.Message}");
            }
        }

        private void AddLog(string message)
        {
            var msg = $"[{DateTime.Now:HH:mm:ss}] {message}";
            LogOutput += msg + "\n";
        }
    }
}
