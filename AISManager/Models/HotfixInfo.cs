namespace AISManager.Models
{
    public class HotfixInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string? LocalPath { get; set; }
        public long FileSize { get; set; }
        public DateTime? DownloadDate { get; set; }
    }
}
