namespace AISManager.App.Configs
{
    public static class AppPath
    {
        public static readonly string DataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Application.ProductName!);

        public static readonly string LogsPath = Path.Combine(DataPath, "Logs");
        public static readonly string ConfigsPath = Path.Combine(DataPath, "Configs");
    }
}
