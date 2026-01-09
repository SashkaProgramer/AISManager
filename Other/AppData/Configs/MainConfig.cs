using HardDev.CoreUtils.Config;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace AISFixer.AppData.Configs
{
    public sealed class MainConfig : BaseConfiguration<MainConfig>
    {
        private static readonly JsonSerializerOptions _options = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public MainConfig() : base(Path.Combine(AppPath.ConfigsPath, $"{nameof(MainConfig)}.json"))
        {
            Options = _options;
        }

        public string sourcePath { get; set; }
    }
}