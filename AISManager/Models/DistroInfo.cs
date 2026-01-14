using AISManager.Infrastructure;

namespace AISManager.Models
{
    public class DistroInfo : ObservableObject
    {
        private string _version = string.Empty;
        private string _fileName = string.Empty;
        private string _fullUrl = string.Empty;
        private long _size;
        private string _downloadStatus = "Доступно";
        private int _progress;
        private string _progressText = string.Empty;
        private bool _isDownloading;
        private bool _isIndeterminate;
        private bool _isDownloaded;

        public string Version { get => _version; set => SetProperty(ref _version, value); }
        public string FileName { get => _fileName; set => SetProperty(ref _fileName, value); }
        public string FullUrl { get => _fullUrl; set => SetProperty(ref _fullUrl, value); }
        public long Size { get => _size; set => SetProperty(ref _size, value); }

        public string DownloadStatus { get => _downloadStatus; set => SetProperty(ref _downloadStatus, value); }
        public int Progress { get => _progress; set => SetProperty(ref _progress, value); }
        public string ProgressText { get => _progressText; set => SetProperty(ref _progressText, value); }
        public bool IsDownloading { get => _isDownloading; set => SetProperty(ref _isDownloading, value); }
        public bool IsIndeterminate { get => _isIndeterminate; set => SetProperty(ref _isIndeterminate, value); }
        public bool IsDownloaded { get => _isDownloaded; set => SetProperty(ref _isDownloaded, value); }
    }
}
