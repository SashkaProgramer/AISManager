using System.Windows;
using AISManager.ViewModels;

namespace AISManager
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
        // 
        private void MenuButton_Click(object sender, RoutedEventArgs e)
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
            if (sender == BtnFixes)
            {
                ViewFixes.Visibility = Visibility.Visible;
                BtnFixes.Style = (Style)FindResource("ActiveMenuButtonStyle");
            }
            else if (sender == BtnSettings)
            {
                ViewSettings.Visibility = Visibility.Visible;
                BtnSettings.Style = (Style)FindResource("ActiveMenuButtonStyle");
            }
            else if (sender == BtnDistros)
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