using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AISManager.Models
{
    public class UpdateFile : INotifyPropertyChanged
    {
        private bool _isSelected = true;
        private int _progressValue;
        private string _statusText = "ОЖИДАНИЕ";
        private string _statusBgColor = "#E2E8F0";
        private string _statusFgColor = "#718096";
        private bool _isProcessing;
        private string _progressText = "";

        // Link to the real data
        public HotfixInfo? HotfixData { get; set; }

        public string FileName
        {
            get => HotfixData?.Name ?? _fileName;
            set
            {
                if (HotfixData != null) HotfixData.Name = value;
                _fileName = value;
                OnPropertyChanged();
            }
        }
        private string _fileName = "";

        public string Date
        {
            get => _date; // In real app, maybe parse from HotfixData name or metadata
            set { _date = value; OnPropertyChanged(); }
        }
        private string _date = "";

        public string Size
        {
            get => _size;
            set { _size = value; OnPropertyChanged(); }
        }
        private string _size = "";

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

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
