using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Text.Json;
using HardDev.CoreUtils.Config;

namespace AISManager.App.Configs
{
    public sealed class MainConfig : BaseConfiguration<MainConfig>
    {
        private static readonly JsonSerializerOptions s_options = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public MainConfig() : base(Path.Combine(AppPath.ConfigsPath, $"{nameof(MainConfig)}.json"))
        {
            Options = s_options;
        }

        public bool LogPanelCollapsed { get; set; } = false;
        public string? DownloadPath { get; set; }
        public bool AutoStartScan { get; set; } = false;
        public bool EnableBackgroundCheck { get; set; } = false;
        public int CheckIntervalMinutes { get; set; } = 5;
    }
}
