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

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            // Скрываем все области контента
            ViewFixes.Visibility = Visibility.Collapsed;
            ViewSettings.Visibility = Visibility.Collapsed;

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
                BtnDistros.Style = (Style)FindResource("ActiveMenuButtonStyle");
                // Можно добавить сообщение, что раздел в разработке
                System.Windows.MessageBox.Show("Раздел 'Дистрибутивы ПО' находится в разработке.", "Информация");
                
                // Возвращаемся на вкладку фиксов для наглядности
                ViewFixes.Visibility = Visibility.Visible;
                BtnFixes.Style = (Style)FindResource("ActiveMenuButtonStyle");
                BtnDistros.Style = (Style)FindResource("MenuButtonStyle");
            }
        }
    }
}