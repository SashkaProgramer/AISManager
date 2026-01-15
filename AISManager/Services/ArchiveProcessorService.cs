using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using AISManager.AppData.Configs;
using ICSharpCode.SharpZipLib.Zip;
using SharpCompress.Common;
using SharpCompress.Readers;
using Serilog;

namespace AISManager.Services
{
    public class ArchiveProcessorService
    {
        private static readonly Regex NameParserRegex = new Regex(@"^(.*_\d+\.\d+\.\d+\.\d+)(.*)$", RegexOptions.Compiled);
        private static readonly Regex NumberFinderRegex = new Regex(@"\d+", RegexOptions.Compiled);
        private static readonly ILogger s_logger = Log.ForContext<ArchiveProcessorService>();

        private readonly SfxBuilder _sfxBuilder;
        public Action<string>? OnLog { get; set; }

        public ArchiveProcessorService()
        {
            _sfxBuilder = new SfxBuilder();
            _sfxBuilder.OnLog = msg => OnLog?.Invoke(msg);
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

        public static (string baseName, int num)? ParseArchiveName(string fileName)
        {
            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            var match = NameParserRegex.Match(nameWithoutExt);
            if (match.Success)
            {
                var basePart = match.Groups[1].Value;
                var garbagePart = match.Groups[2].Value;
                var numberMatches = NumberFinderRegex.Matches(garbagePart);

                if (numberMatches.Count > 0)
                {
                    if (int.TryParse(numberMatches[numberMatches.Count - 1].Value, out int num))
                        return (basePart, num);
                }
            }
            return null;
        }

        public static string GenerateFixesString(IEnumerable<int> numbers)
        {
            var sorted = numbers.Distinct().OrderBy(n => n).ToList();
            if (sorted.Count == 0) return "№";
            if (sorted.Count == 1) return sorted[0].ToString();

            var ranges = new List<string>();
            int start = sorted[0];
            int prev = start;

            for (int i = 1; i <= sorted.Count; i++)
            {
                if (i < sorted.Count && sorted[i] == prev + 1)
                {
                    prev = sorted[i];
                    continue;
                }

                if (start == prev)
                {
                    ranges.Add(start.ToString());
                }
                else
                {
                    ranges.Add($"{start}-{prev}");
                }

                if (i < sorted.Count)
                {
                    start = sorted[i];
                    prev = start;
                }
            }

            var result = string.Join(",", ranges);
            // Если строка слишком длинная, сокращаем
            if (result.Length > 30)
            {
                return $"{sorted.First()}...{sorted.Last()}({sorted.Count}_шт)";
            }
            return result;
        }

        public async Task<int> ProcessDownloadedHotfixesAsync(string sourcePath, AppConfig config, IEnumerable<string>? selectedFiles = null)
        {
            return await Task.Run(() =>
            {
                if (!Directory.Exists(sourcePath)) return 0;

                var files = Directory.GetFiles(sourcePath);
                var archivesToProcess = new List<ArchiveInfo>();

                // Если переданы выбранные файлы, создаем набор ключей для фильтрации
                HashSet<(string, int)>? selectedKeys = null;
                if (selectedFiles != null)
                {
                    selectedKeys = new HashSet<(string, int)>();
                    foreach (var f in selectedFiles)
                    {
                        var key = ParseArchiveName(f);
                        if (key.HasValue) selectedKeys.Add(key.Value);
                    }
                }

                foreach (var filePath in files)
                {
                    var fileName = Path.GetFileName(filePath);
                    var extension = Path.GetExtension(filePath).ToLower();
                    if (extension != ".zip" && extension != ".rar") continue;

                    var info = ParseArchiveName(fileName);
                    if (info.HasValue)
                    {
                        // Если есть фильтр - проверяем, входит ли этот файл в список выбранных
                        if (selectedKeys != null && !selectedKeys.Contains(info.Value))
                        {
                            continue;
                        }

                        archivesToProcess.Add(new ArchiveInfo
                        {
                            OriginalFilePath = filePath,
                            BaseName = info.Value.baseName,
                            FixNumber = info.Value.num,
                            Extension = extension
                        });
                    }
                }

                if (archivesToProcess.Count == 0)
                {
                    if (selectedFiles != null) LogWarning("Выбранные файлы не найдены в папке для обработки.");
                    else LogWarning("Архивы для обработки не найдены.");
                    return 0;
                }

                var sortedList = archivesToProcess
                    .OrderBy(x => x.BaseName)
                    .ThenBy(x => x.FixNumber)
                    .ToList();

                var stagingPath = Path.Combine(Path.GetTempPath(), "AISManager_Staging_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(stagingPath);

                try
                {
                    foreach (var archive in sortedList)
                    {
                        if (archive.Extension == ".zip")
                        {
                            s_logger.Information("Распаковка ZIP: {FileName}", archive.OriginalFilePath);
                            UnzipFile(archive.OriginalFilePath, stagingPath);
                        }
                        else if (archive.Extension == ".rar")
                        {
                            s_logger.Information("Распаковка RAR: {FileName}", archive.OriginalFilePath);
                            UnrarFile(archive.OriginalFilePath, stagingPath);
                        }
                    }

                    var fixNumbers = sortedList.Select(x => x.FixNumber).Distinct();
                    var fixLabel = GenerateFixesString(fixNumbers);

                    // SFX Creation
                    var sfxOutputFolder = string.IsNullOrWhiteSpace(config.SfxOutputPath) ? sourcePath : config.SfxOutputPath;
                    Directory.CreateDirectory(sfxOutputFolder);
                    var sfxPath = Path.Combine(sfxOutputFolder, $"FIX_{fixLabel}.exe");
                    _sfxBuilder.Build(stagingPath, sfxPath);

                    return sortedList.Count;
                }
                catch (Exception ex)
                {
                    s_logger.Error(ex, "Критическая ошибка при распаковке архивов или сборке SFX");
                    OnLog?.Invoke("Ошибка при обработке архивов: " + ex.Message);
                    return 0;
                }
                finally
                {
                    if (Directory.Exists(stagingPath)) Directory.Delete(stagingPath, true);
                }
            });
        }

        private void UnzipFile(string filePath, string targetDir)
        {
            var fastZip = new FastZip();
            fastZip.ExtractZip(filePath, targetDir, null);
        }

        private void UnrarFile(string filePath, string targetDir)
        {
            using (var stream = File.OpenRead(filePath))
            using (var reader = ReaderFactory.Open(stream))
            {
                while (reader.MoveToNextEntry())
                {
                    if (!reader.Entry.IsDirectory)
                    {
                        reader.WriteEntryToDirectory(targetDir, new ExtractionOptions()
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });
                    }
                }
            }
        }


        private class ArchiveInfo
        {
            public string OriginalFilePath { get; set; } = "";
            public string BaseName { get; set; } = "";
            public int FixNumber { get; set; }
            public string Extension { get; set; } = "";
        }
    }
}
