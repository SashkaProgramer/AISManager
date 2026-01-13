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

        public async Task ProcessDownloadedHotfixesAsync(string sourcePath, AppConfig config)
        {
            await Task.Run(() =>
            {
                var files = Directory.GetFiles(sourcePath);
                var archivesToProcess = new List<ArchiveInfo>();

                LogInfo("Обработка архивов...");

                foreach (var filePath in files)
                {
                    var fileName = Path.GetFileNameWithoutExtension(filePath);
                    var extension = Path.GetExtension(filePath).ToLower();

                    if (extension != ".zip" && extension != ".rar") continue;

                    var match = NameParserRegex.Match(fileName);
                    if (match.Success)
                    {
                        var basePart = match.Groups[1].Value;
                        var garbagePart = match.Groups[2].Value;
                        var numberMatch = NumberFinderRegex.Matches(garbagePart);

                        if (numberMatch.Count > 0)
                        {
                            var fixNumberStr = numberMatch[numberMatch.Count - 1].Value;
                            archivesToProcess.Add(new ArchiveInfo
                            {
                                OriginalFilePath = filePath,
                                BaseName = basePart,
                                FixNumber = int.Parse(fixNumberStr),
                                Extension = extension
                            });
                        }
                    }
                }

                if (archivesToProcess.Count == 0)
                {
                    LogWarning("Файлы для обработки не найдены.");
                    return;
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
                        string currentFilePath = archive.OriginalFilePath;
                        string cleanFileName = $"{archive.BaseName}_{archive.FixNumber}{archive.Extension}";
                        string cleanFilePath = Path.Combine(Path.GetDirectoryName(currentFilePath)!, cleanFileName);

                        if (!string.Equals(Path.GetFileName(currentFilePath), cleanFileName, StringComparison.OrdinalIgnoreCase))
                        {
                            if (!File.Exists(cleanFilePath))
                            {
                                File.Move(currentFilePath, cleanFilePath);
                            }
                            currentFilePath = cleanFilePath;
                        }

                        if (archive.Extension == ".zip") UnzipFile(currentFilePath, stagingPath);
                        else if (archive.Extension == ".rar") UnrarFile(currentFilePath, stagingPath);
                    }

                    var lastFixNumber = sortedList.Last().FixNumber;

                    // SFX Creation
                    var sfxOutputFolder = string.IsNullOrWhiteSpace(config.SfxOutputPath) ? sourcePath : config.SfxOutputPath;
                    Directory.CreateDirectory(sfxOutputFolder);
                    var sfxPath = Path.Combine(sfxOutputFolder, $"FIX_{lastFixNumber}.exe");
                    _sfxBuilder.Build(stagingPath, sfxPath);
                }
                catch (Exception ex)
                {
                    s_logger.Error(ex, "Ошибка при обработке архивов");
                    OnLog?.Invoke("Ошибка при обработке архивов: " + ex.Message);
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
