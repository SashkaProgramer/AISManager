using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace AISDownloader
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<UpdateFile> Updates { get; set; }
        private int _fixCounter = 1; // Счетчик для нейминга Fix_1, Fix_2

        public MainWindow()
        {
            InitializeComponent();
            LoadMockData();
            DataContext = this;
        }

        private void LoadMockData()
        {
            Updates = new ObservableCollection<UpdateFile>
            {
                new UpdateFile { FileName = "patch_registry_v2.dll", Date = "Сегодня", Size = "2.4 MB", StatusText = "НОВОЕ", StatusBgColor = "#BEE3F8", StatusFgColor = "#2B6CB0" },
                new UpdateFile { FileName = "ais_security_module.exe", Date = "Вчера", Size = "14.5 MB", StatusText = "НОВОЕ", StatusBgColor = "#BEE3F8", StatusFgColor = "#2B6CB0" },
                new UpdateFile { FileName = "config_update_2023.xml", Date = "20.10.23", Size = "12 KB", StatusText = "ОЖИДАНИЕ", StatusBgColor = "#E2E8F0", StatusFgColor = "#718096", IsSelected = false }
            };
            UpdatesList.ItemsSource = Updates;
        }

        // --- ЛОГИКА АВТОМАТИЗАЦИИ ---

        private async void BtnProcess_Click(object sender, RoutedEventArgs e)
        {
            var selectedFiles = Updates.Where(u => u.IsSelected).ToList();
            if (!selectedFiles.Any())
            {
                AddLog("! Ничего не выбрано для скачивания.");
                return;
            }

            BtnProcess.IsEnabled = false; // Блокируем кнопку
            bool useSfx = ChkAutoSfx.IsChecked == true;

            AddLog($"=== Начало процесса (SFX: {useSfx}) ===");

            foreach (var file in selectedFiles)
            {
                // 1. СКАЧИВАНИЕ
                file.IsProcessing = true;
                file.StatusText = "ЗАГРУЗКА";
                file.StatusBgColor = "#FEFCBF"; // Желтый
                file.StatusFgColor = "#D69E2E";

                AddLog($"Скачивание файла: {file.FileName}...");

                // Симуляция загрузки (тут будет реальный WebClient)
                for (int i = 0; i <= 100; i += 10)
                {
                    file.ProgressValue = i;
                    file.ProgressText = $"{i}%";
                    await Task.Delay(100);
                }

                // 2. УПАКОВКА В SFX (Если включено)
                if (useSfx)
                {
                    file.StatusText = "УПАКОВКА";
                    file.StatusBgColor = "#C6F6D5"; // Светло-зеленый
                    file.StatusFgColor = "#276749";
                    file.ProgressValue = 0; // Сброс для упаковки

                    string newName = $"Fix_{_fixCounter}";
                    AddLog($"Создание SFX архива: {newName}.exe из {file.FileName}");

                    // Симуляция работы WinRAR
                    await CreateSfxArchiveFake(file.FileName, newName);

                    _fixCounter++; // Увеличиваем счетчик Fix_1 -> Fix_2
                }

                // 3. ЗАВЕРШЕНИЕ
                file.IsProcessing = false;
                file.StatusText = "ГОТОВО";
                file.StatusBgColor = "#48BB78"; // Зеленый
                file.StatusFgColor = "White";
                file.ProgressValue = 100;

                AddLog($"Файл {file.FileName} успешно обработан.");
            }

            AddLog("=== Все задачи выполнены ===");
            BtnProcess.IsEnabled = true;
        }

        private void BtnManualDownload_Click(object sender, RoutedEventArgs e)
        {
            // Логика простого скачивания без переименования
            MessageBox.Show("Здесь будет обычное скачивание файлов в папку Загрузки.", "Ручной режим");
        }

        // --- ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ---

        // Заглушка для реального вызова WinRAR
        private async Task CreateSfxArchiveFake(string inputFile, string outputName)
        {
            // В РЕАЛЬНОСТИ ЗДЕСЬ БУДЕТ:
            // Process.Start("WinRAR.exe", $"a -sfx -ep1 \"{outputName}.exe\" \"{inputFile}\"");

            // Эмуляция задержки упаковки
            await Task.Delay(1500);
        }

        private void AddLog(string message)
        {
            LogBlock.Text += $"\n[{DateTime.Now:HH:mm:ss}] {message}";
            LogScroll.ScrollToEnd();
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            // Простая логика переключения (можно улучшить через MVVM)
            ViewFixes.Visibility = Visibility.Collapsed;
            ViewSettings.Visibility = Visibility.Collapsed;

            if (sender == BtnFixes) ViewFixes.Visibility = Visibility.Visible;
            if (sender == BtnSettings) ViewSettings.Visibility = Visibility.Visible;
        }
    }

    // Класс модели данных с поддержкой уведомлений (INotifyPropertyChanged)
    public class UpdateFile : INotifyPropertyChanged
    {
        private bool _isSelected = true;
        private int _progressValue;
        private string _statusText;
        private string _statusBgColor;
        private string _statusFgColor;
        private bool _isProcessing;
        private string _progressText;

        public string FileName { get; set; }
        public string Date { get; set; }
        public string Size { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set { _isProcessing = value; OnPropertyChanged(); }
        }

        public int ProgressValue
        {
            get => _progressValue;
            set { _progressValue = value; OnPropertyChanged(); }
        }

        public string ProgressText
        {
            get => _progressText;
            set { _progressText = value; OnPropertyChanged(); }
        }

        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        public string StatusBgColor
        {
            get => _statusBgColor;
            set { _statusBgColor = value; OnPropertyChanged(); }
        }

        public string StatusFgColor
        {
            get => _statusFgColor;
            set { _statusFgColor = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}