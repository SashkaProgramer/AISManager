using System;
using System.IO;

namespace AISManager.AppData
{
    public static class AppPath
    {
        public const string ProductName = "AISManager";

        public static string DataPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            ProductName);

        public static string LogsPath => Path.Combine(DataPath, "Logs");
        public static string ConfigsPath => Path.Combine(DataPath, "Configs");
    }
}
