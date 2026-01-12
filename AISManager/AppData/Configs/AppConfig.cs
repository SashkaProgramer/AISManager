using HardDev.CoreUtils.Config;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AISManager.AppData.Configs
{
    public sealed class AppConfig : BaseConfiguration<AppConfig>
    {
        public static AppConfig Instance { get; } = new();

        private static readonly JsonSerializerOptions _options = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public AppConfig() : base(Path.Combine(AppPath.ConfigsPath, $"{nameof(AppConfig)}.json"))
        {
            Options = _options;
        }

        public string DownloadPath { get; set; } = "";
        public string SfxOutputPath { get; set; } = ""; // Empty means same as DownloadPath
        public bool AutoSfx { get; set; } = true;
        public bool AutoDownload { get; set; } = false;
        public bool IsAutoCheckEnabled { get; set; } = false;
        public int AutoCheckIntervalMinutes { get; set; } = 10;
        public double LogHeight { get; set; } = 150;
    }
}
