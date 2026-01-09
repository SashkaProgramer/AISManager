using System.Diagnostics;
using System.Reflection;
using AISFixer.App.Configs;
using Serilog;

namespace AISFixer.App.Services
{
    internal class SfxBuilder
    {
        private const string Tool7z = "7z.exe";
        private const string Tool7zDll = "7z.dll";
        private const string ToolSfx = "7zS.sfx";
        private const string ConfigFile = "config.txt";
        private const string RunScript = "run.cmd";

        // Путь к временной папке для инструментов сборки
        private readonly string _toolsDir;

        public SfxBuilder()
        {
            _toolsDir = Path.Combine(Path.GetTempPath(), "AISFixer_Tools_" + Guid.NewGuid().ToString("N"));
        }

        public void Build(string contentSourceDir, string outputExePath)
        {
            try
            {
                Log.Information("Подготовка к сборке SFX архива...");
                PrepareTools();

                // 1. Создаем обычный .7z архив из контента
                var tempArchive7z = Path.Combine(_toolsDir, "payload.7z");

                // Добавляем run.cmd в корень архива (берем его из инструментов)
                // Но нам нужно, чтобы run.cmd лежал ВМЕСТЕ с файлами contentSourceDir внутри архива.
                // Самый простой способ - скопировать run.cmd в contentSourceDir перед упаковкой.
                var runCmdDest = Path.Combine(contentSourceDir, "run.cmd");
                File.Copy(Path.Combine(_toolsDir, RunScript), runCmdDest, true);

                Log.Information("Упаковка файлов в 7z...");
                // Команда: 7z.exe a "archive.7z" "source\*"
                Run7z($"a \"{tempArchive7z}\" \"{contentSourceDir}\\*\"");

                // 2. Склеиваем SFX + Config + Archive -> EXE
                Log.Information("Создание исполняемого файла {OutputExe}...", Path.GetFileName(outputExePath));
                CreateSfxExe(outputExePath, tempArchive7z);

                Log.Information("SFX архив успешно создан: {Path}", outputExePath);

                // Чистим run.cmd из исходной папки, чтобы не мусорить
                if (File.Exists(runCmdDest)) File.Delete(runCmdDest);
            }
            finally
            {
                Cleanup();
            }
        }

        private void PrepareTools()
        {
            if (!Directory.Exists(_toolsDir)) Directory.CreateDirectory(_toolsDir);

            // Ресурсы лежат по путям: SfxTools.7z.exe и т.д.
            // Примечание: в csproj LogicalName мог сформироваться специфично, 
            // но обычно это Папка.Файл. Проверим точные имена.
            // Т.к. папка SfxTools лежит в Resources, а Resources на уровень выше, 
            // структура ресурсов будет скорее всего "SfxTools.7z.exe" (т.к. мы копировали ВНУТРЬ проекта в папку Resources).

            ExtractResource("SfxTools.7z.exe", Tool7z);
            ExtractResource("SfxTools.7z.dll", Tool7zDll);
            ExtractResource("SfxTools.7zS.sfx", ToolSfx);
            ExtractResource("SfxTools.config.txt", ConfigFile);
            ExtractResource("SfxTools.run.cmd", RunScript);
        }

        private void ExtractResource(string resourceName, string fileName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            // Ищем ресурс, заканчивающийся на нужное имя (чтобы не гадать с полным путем)
            var fullResourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(r => r.EndsWith(resourceName, StringComparison.OrdinalIgnoreCase));

            if (fullResourceName == null)
            {
                throw new FileNotFoundException($"Встроенный ресурс не найден: {resourceName}");
            }

            var destPath = Path.Combine(_toolsDir, fileName);
            using (var stream = assembly.GetManifestResourceStream(fullResourceName))
            {
                if (stream == null)
                    throw new InvalidOperationException($"Не удалось загрузить поток ресурса: {fullResourceName}");

                using (var fileStream = File.Create(destPath))
                {
                    stream.CopyTo(fileStream);
                }
            }
        }

        private void Run7z(string arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(_toolsDir, Tool7z),
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = _toolsDir
            };

            using (var process = Process.Start(startInfo))
            {
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    var error = process.StandardError.ReadToEnd();
                    throw new Exception($"Ошибка архиватора 7z (код {process.ExitCode}): {error}");
                }
            }
        }

        private void CreateSfxExe(string outputExePath, string archive7zPath)
        {
            // Формула: COPY /b 7zS.sfx + config.txt + archive.7z output.exe
            var sfxModule = Path.Combine(_toolsDir, ToolSfx);
            var config = Path.Combine(_toolsDir, ConfigFile);

            using (var output = File.Create(outputExePath))
            {
                AppendFileToStream(sfxModule, output);
                AppendFileToStream(config, output);
                AppendFileToStream(archive7zPath, output);
            }
        }

        private void AppendFileToStream(string filePath, Stream outputStream)
        {
            using (var input = File.OpenRead(filePath))
            {
                input.CopyTo(outputStream);
            }
        }

        private void Cleanup()
        {
            try
            {
                if (Directory.Exists(_toolsDir))
                    Directory.Delete(_toolsDir, true);
            }
            catch (Exception ex)
            {
                Log.Warning("Не удалось удалить временную папку сборки: {Message}", ex.Message);
            }
        }
    }
}
