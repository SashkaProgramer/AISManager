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
            ViewFixes.Visibility = Visibility.Collapsed;
            ViewSettings.Visibility = Visibility.Collapsed;

            if (sender == BtnFixes) ViewFixes.Visibility = Visibility.Visible;
            if (sender == BtnSettings) ViewSettings.Visibility = Visibility.Visible;
        }
    }
}