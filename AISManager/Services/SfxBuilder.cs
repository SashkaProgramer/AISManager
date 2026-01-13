using System.Diagnostics;
using System.IO;
using System.Reflection;
using Serilog;

namespace AISManager.Services
{
    public class SfxBuilder
    {
        private const string Tool7z = "7z.exe";
        private const string Tool7zDll = "7z.dll";
        private const string ToolSfx = "7zS.sfx";
        private const string ConfigFile = "config.txt";
        private const string RunScript = "run.cmd";

        private readonly string _toolsDir;
        private static readonly ILogger s_logger = Log.ForContext<SfxBuilder>();
        public Action<string>? OnLog { get; set; }

        public SfxBuilder()
        {
            _toolsDir = Path.Combine(Path.GetTempPath(), "AISManager_Tools_" + Guid.NewGuid().ToString("N"));
        }

        private void LogInfo(string message, params object[] args)
        {
            string finalMsg = args.Length > 0 ? string.Format(message, args) : message;
            s_logger.Information(message, args);
            OnLog?.Invoke(finalMsg);
        }

        private void LogWarning(string message, params object[] args)
        {
            string finalMsg = args.Length > 0 ? string.Format(message, args) : message;
            s_logger.Warning(message, args);
            OnLog?.Invoke("WARN: " + finalMsg);
        }

        public void Build(string contentSourceDir, string outputExePath)
        {
            try
            {
                // PrepareTools(); // Already call it below
                PrepareTools();

                var tempArchive7z = Path.Combine(_toolsDir, "payload.7z");

                // Copy run.cmd to source dir before packing
                var runCmdDest = Path.Combine(contentSourceDir, RunScript);
                var runCmdSource = Path.Combine(_toolsDir, RunScript);

                if (File.Exists(runCmdSource))
                {
                    File.Copy(runCmdSource, runCmdDest, true);
                }
                else
                {
                    s_logger.Warning("Файл run.cmd не найден в инструментах.");
                }

                s_logger.Information("Упаковка файлов в 7z...");
                Run7z($"a \"{tempArchive7z}\" \"{contentSourceDir}\\*\"");

                // CreateSfxExe(outputExePath, tempArchive7z);
                CreateSfxExe(outputExePath, tempArchive7z);

                LogInfo("SFX архив успешно создан: {0}", outputExePath);

                if (File.Exists(runCmdDest)) File.Delete(runCmdDest);
            }
            catch (Exception ex)
            {
                s_logger.Error(ex, "Ошибка при сборке SFX");
                throw;
            }
            finally
            {
                Cleanup();
            }
        }

        private void PrepareTools()
        {
            if (!Directory.Exists(_toolsDir)) Directory.CreateDirectory(_toolsDir);

            // We will embed these as resources in the project
            ExtractResource("7z.exe", Tool7z);
            ExtractResource("7z.dll", Tool7zDll);
            ExtractResource("7zS.sfx", ToolSfx);
            ExtractResource("config.txt", ConfigFile);
            ExtractResource("run.cmd", RunScript);
        }

        private void ExtractResource(string resourceName, string fileName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            // In WPF projects, resources are often named like "AISManager.Resources.SfxTools.7z.exe"
            var fullResourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(r => r.EndsWith("." + resourceName, StringComparison.OrdinalIgnoreCase) || r.Equals(resourceName, StringComparison.OrdinalIgnoreCase));

            if (fullResourceName == null)
            {
                LogWarning("Встроенный ресурс не найден: {0}. Пытаемся найти в папке Resources.", resourceName);

                // Fallback to local file if resource not found (for development)
                var localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "SfxTools", resourceName);
                if (File.Exists(localPath))
                {
                    File.Copy(localPath, Path.Combine(_toolsDir, fileName), true);
                    return;
                }

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
                process?.WaitForExit();
                if (process?.ExitCode != 0)
                {
                    var error = process?.StandardError.ReadToEnd();
                    throw new Exception($"Ошибка архиватора 7z (код {process?.ExitCode}): {error}");
                }
            }
        }

        private void CreateSfxExe(string outputExePath, string archive7zPath)
        {
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
                LogWarning("Не удалось удалить временную папку сборки: {0}", ex.Message);
            }
        }
    }
}
