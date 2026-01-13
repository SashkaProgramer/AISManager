using System.Windows;
using AISManager.ViewModels;

namespace AISManager
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var vm = new MainViewModel();
            DataContext = vm;

            // Если пути не настроены, сразу открываем настройки
            if (string.IsNullOrWhiteSpace(vm.DownloadPath) ||
                string.IsNullOrWhiteSpace(vm.DistroDownloadPath) ||
                string.IsNullOrWhiteSpace(vm.AisNalog3DownloadPath))
            {
                SwitchToView(BtnSettings);
            }
            else
            {
                SwitchToView(BtnFixes);
            }
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn)
            {
                SwitchToView(btn);
            }
        }

        private void SwitchToView(System.Windows.Controls.Button targetButton)
        {
            // Скрываем все области контента
            ViewFixes.Visibility = Visibility.Collapsed;
            ViewSettings.Visibility = Visibility.Collapsed;
            ViewDistros.Visibility = Visibility.Collapsed;

            // Сбрасываем стили всех кнопок меню до обычного состояния
            BtnFixes.Style = (Style)FindResource("MenuButtonStyle");
            BtnDistros.Style = (Style)FindResource("MenuButtonStyle");
            BtnSettings.Style = (Style)FindResource("MenuButtonStyle");

            // Активируем нужную область и меняем стиль кнопки
            if (targetButton == BtnFixes)
            {
                ViewFixes.Visibility = Visibility.Visible;
                BtnFixes.Style = (Style)FindResource("ActiveMenuButtonStyle");
            }
            else if (targetButton == BtnSettings)
            {
                ViewSettings.Visibility = Visibility.Visible;
                BtnSettings.Style = (Style)FindResource("ActiveMenuButtonStyle");
            }
            else if (targetButton == BtnDistros)
            {
                ViewDistros.Visibility = Visibility.Visible;
                BtnDistros.Style = (Style)FindResource("ActiveMenuButtonStyle");
            }
        }

        private void TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox textBox)
            {
                textBox.ScrollToEnd();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DataContext is MainViewModel viewModel && viewModel.IsBusy)
            {
                var result = System.Windows.MessageBox.Show(
                    $"Сейчас выполняется: {viewModel.BusyMessage}\n\nВы уверены, что хотите закрыть приложение? Это может прервать текущую операцию.",
                    "Внимание: работа не завершена",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                }
            }
        }
    }
}