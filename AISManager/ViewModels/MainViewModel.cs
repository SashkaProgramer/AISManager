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

        public ICommand CheckUpdatesCommand { get; }
        public ICommand DownloadCommand { get; }
        public ICommand ManualDownloadCommand { get; }

        public MainViewModel()
        {
            // In a real app, use DI. Here we manually compose for simplicity.
            _hotfixService = new HotfixService();
            _versionService = new VersionService();
            
            // Configure Serilog to write to a file and also to a sink that updates LogOutput?
            // For simplicity, we'll just append to LogOutput manually in this VM or use a custom sink.
            // Let's just use a simple approach: The services log to Serilog, and we might want to capture that.
            // But for now, let's just log from VM.
            _logger = Log.ForContext<MainViewModel>();

            CheckUpdatesCommand = new RelayCommand(async _ => await CheckUpdatesAsync());
            DownloadCommand = new RelayCommand(async _ => await DownloadSelectedAsync(), _ => !IsBusy);
            ManualDownloadCommand = new RelayCommand(_ => MessageBox.Show("Функция ручного скачивания пока не реализована."));

            // Initial check
            Task.Run(CheckUpdatesAsync);
        }

        private async Task CheckUpdatesAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            AddLog("Проверка версии АИС...");

            try
            {
                CurrentVersion = await _versionService.GetCurrentAISVersionAsync();
                AddLog($"Текущая версия: {CurrentVersion}");

                AddLog("Поиск обновлений...");
                var hotfixes = await _hotfixService.GetHotfixesAsync(CurrentVersion);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Updates.Clear();
                    foreach (var hf in hotfixes)
                    {
                        Updates.Add(new UpdateFile
                        {
                            HotfixData = hf,
                            StatusText = "НОВОЕ",
                            StatusBgColor = "#BEE3F8",
                            StatusFgColor = "#2B6CB0"
                        });
                    }
                });

                if (hotfixes.Count == 0)
                {
                    AddLog("Обновлений не найдено.");
                }
                else
                {
                    AddLog($"Найдено обновлений: {hotfixes.Count}");
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
            string downloadPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Downloads");

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

        private void AddLog(string message)
        {
            var msg = $"[{DateTime.Now:HH:mm:ss}] {message}";
            LogOutput += msg + "\n";
        }
    }
}
