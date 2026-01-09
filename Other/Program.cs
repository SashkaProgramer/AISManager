using System.Text.RegularExpressions;
using AISFixer.App.Configs;
using AISFixer.App.Logger;
using ICSharpCode.SharpZipLib.Zip; // NuGet: SharpZipLib
using Serilog;
using SharpCompress.Common;        // NuGet: SharpCompress
using SharpCompress.Readers;       // NuGet: SharpCompress
using AISFixer.App.Services;

namespace AISFixer
{
    // Класс для хранения информации о файле перед обработкой
    internal class ArchiveInfo
    {
        public string OriginalFilePath { get; set; }
        public string BaseName { get; set; } // Часть "kpe_22.12.26.1"
        public int FixNumber { get; set; }   // Номер фикса (1, 4, 6)
        public string Extension { get; set; }

        // Генерируем красивое имя: kpe_22.12.26.1_1.zip
        public string GetCleanFileName()
        {
            return $"{BaseName}_{FixNumber}{Extension}";
        }
    }

    internal class Program
    {
        // Регулярка для разбора имени:
        // Группа 1 (Base): Всё до версии включительно (ищет паттерн цифра.цифра.цифра.цифра)
        // Группа 2 (Garbage): Всё остальное после версии
        private static readonly Regex NameParserRegex = new Regex(@"^(.*_\d+\.\d+\.\d+\.\d+)(.*)$", RegexOptions.Compiled);

        // Регулярка для поиска числа внутри мусора (находит 4 в строке "_№_№4")
        private static readonly Regex NumberFinderRegex = new Regex(@"\d+", RegexOptions.Compiled);

        private static void Main(string[] args)
        {
            try
            {
                // 1. Инициализация логгера (сразу в нужную папку AppData)
                Log.Logger = WinFormSink.Build("AISFixer");

                // 2. Инициализация конфига
                MainConfig.Instance.Load();

                // Если путь пустой (первый запуск), сохраняем конфиг, чтобы файл физически создался
                if (string.IsNullOrWhiteSpace(MainConfig.Instance.SourcePath))
                {
                    MainConfig.Instance.Save();
                }

                Log.Information("=== Запуск AISFixer ===");

                var sourcePath = MainConfig.Instance.SourcePath;

                if (string.IsNullOrWhiteSpace(sourcePath) || !Directory.Exists(sourcePath))
                {
                    Log.Warning("Конфигурация не завершена!");
                    Log.Information("1. Откройте файл конфига: {ConfigPath}", MainConfig.Instance.FilePath);
                    Log.Information("2. Укажите путь к папке с архивами в поле \"SourcePath\".");
                    Log.Information("   Пример: \"SourcePath\": \"C:\\\\Archives\\\\MyFiles\"");
                    Log.Information("3. Перезапустите программу.");

                    Console.WriteLine("\nНажмите любую клавишу для выхода...");
                    Console.ReadKey();
                    return;
                }

                ProcessFolder(sourcePath);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Критическая ошибка при работе приложения");
                Console.ReadKey();
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static void ProcessFolder(string sourcePath)
        {
            var files = Directory.GetFiles(sourcePath);
            var archivesToProcess = new List<ArchiveInfo>();

            Log.Information("Анализ файлов в папке: {SourcePath}", sourcePath);

            // 1. АНАЛИЗ ФАЙЛОВ
            foreach (var filePath in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var extension = Path.GetExtension(filePath).ToLower();

                // Пропускаем не архивы
                if (extension != ".zip" && extension != ".rar") continue;

                // Разбираем имя на "Версию" и "Мусор"
                var match = NameParserRegex.Match(fileName);

                if (match.Success)
                {
                    var basePart = match.Groups[1].Value; // kpe_22.12.26.1
                    var garbagePart = match.Groups[2].Value; // _##1__ или _№_№4

                    // Ищем число в мусорной части
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

            // 2. СОРТИРОВКА
            var sortedList = archivesToProcess
                .OrderBy(x => x.BaseName)
                .ThenBy(x => x.FixNumber)
                .ToList();

            Log.Information("Найдено подходящих архивов: {Count}", sortedList.Count);

            // 3. ОЧИСТКА ИМЕН И РАСПАКОВКА

            if (sortedList.Count == 0)
            {
                Log.Warning("Файлы для обработки не найдены.");
                Console.WriteLine("\nНажмите любую клавишу для выхода...");
                Console.ReadKey();
                return;
            }

            // Создаем временную папку для сборки (Staging)
            var stagingPath = Path.Combine(Path.GetTempPath(), "AISFixer_Staging_" + Guid.NewGuid().ToString("N"));
            if (!Directory.Exists(stagingPath)) Directory.CreateDirectory(stagingPath);
            Log.Information("Временная папка для сборки: {StagingPath}", stagingPath);

            // 3. ОЧИСТКА ИМЕН И РАСПАКОВКА
            // Теперь распаковываем всё в ОДНУ папку stagingPath
            foreach (var archive in sortedList)
            {
                try
                {
                    string currentFilePath = archive.OriginalFilePath;

                    // (Опционально) Переименование исходных файлов оставляем, чтобы был порядок в папке Source
                    string cleanFileName = archive.GetCleanFileName();
                    string cleanFilePath = Path.Combine(Path.GetDirectoryName(currentFilePath), cleanFileName);

                    // а) ОЧИСТКА МУСОРА (ПЕРЕИМЕНОВАНИЕ)
                    if (!string.Equals(Path.GetFileName(currentFilePath), cleanFileName, StringComparison.OrdinalIgnoreCase))
                    {
                        if (File.Exists(cleanFilePath))
                        {
                            Log.Warning("Файл {CleanFileName} уже существует. Используем его.", cleanFileName);
                        }
                        else
                        {
                            Log.Information("Переименование: {OldName} -> {NewName}", Path.GetFileName(currentFilePath), cleanFileName);
                            File.Move(currentFilePath, cleanFilePath);
                        }
                        currentFilePath = cleanFilePath;
                    }

                    // б) РАСПАКОВКА в STAGING
                    Log.Information("Распаковка [{FixNumber}]: {FileName} -> Staging...", archive.FixNumber, Path.GetFileName(currentFilePath));

                    if (archive.Extension == ".zip")
                    {
                        UnzipFile(currentFilePath, stagingPath);
                    }
                    else if (archive.Extension == ".rar")
                    {
                        UnrarFile(currentFilePath, stagingPath);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Ошибка обработки архива {BaseName}", archive.BaseName);
                }
            }

            // 4. СБОРКА SFX
            try
            {
                var sfxBuilder = new SfxBuilder();

                // Берем номер последнего архива
                var lastFixNumber = sortedList.Last().FixNumber;
                var outputExeName = $"FIX_{lastFixNumber}.exe";
                var outputExe = Path.Combine(sourcePath, outputExeName);

                sfxBuilder.Build(stagingPath, outputExe);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Ошибка при сборке SFX архива");
            }
            finally
            {
                // Удаляем временную папку Staging
                try
                {
                    if (Directory.Exists(stagingPath)) Directory.Delete(stagingPath, true);
                }
                catch (Exception ex)
                {
                    Log.Warning("Не удалось удалить Staging папку: {Message}", ex.Message);
                }
            }

            Log.Information("Обработка завершена.");
            Console.WriteLine("\nНажмите любую клавишу для выхода...");
            Console.ReadKey();
        }

        private static void UnzipFile(string filePath, string targetDir)
        {
            var fastZip = new FastZip();
            fastZip.ExtractZip(filePath, targetDir, null);
        }

        private static void UnrarFile(string filePath, string targetDir)
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
    }
}