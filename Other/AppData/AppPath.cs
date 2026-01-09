using System.IO;

namespace AISFixer.AppData
{
    public static class AppPath
    {
        public static readonly string ProductName = "AISFixer";

        public static readonly string DataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            ProductName);

        public static readonly string LogsPath = Path.Combine(DataPath, "Logs");
        public static readonly string ConfigsPath = Path.Combine(DataPath, "Configs");
    }
}
