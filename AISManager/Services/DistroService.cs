using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AISManager.Models;
using Serilog;

namespace AISManager.Services
{
    public struct DownloadProgress
    {
        public long Received { get; set; }
        public long Total { get; set; }
        public int Percentage { get; set; }
    }

    public interface IDistroService
    {
        Task<DistroInfo?> GetLatestDistroAsync(string ftpUrl);
        Task DownloadDistroAsync(DistroInfo distro, string localPath, IProgress<DownloadProgress>? progress = null, System.Threading.CancellationToken ct = default);
        Action<string>? OnLog { get; set; }
    }

    public class DistroService : IDistroService
    {
        private readonly ILogger _logger = Log.ForContext<DistroService>();

        public Action<string>? OnLog { get; set; }
        private void AddUiLog(string message) => OnLog?.Invoke(message);

        public async Task<DistroInfo?> GetLatestDistroAsync(string ftpUrl)
        {
            try
            {
                _logger.Debug("Checking latest distro at {Url}", ftpUrl);
                string target = ftpUrl.Contains("/OE/") ? "OE" : (ftpUrl.Contains("/AisNalog3_PROM/") ? "Пром" : "FTP");
                AddUiLog($"FTP: Поиск обновлений {target}...");
                // 1. Get version folders
                var versions = await ListFtpDirectoryAsync(ftpUrl);
                if (!versions.Any())
                {
                    _logger.Warning("ListFtpDirectoryAsync returned empty list for {Url}", ftpUrl);
                    return null;
                }

                // Регулярка для поиска версии: 4 числа через точку ИЛИ через подчеркивание
                var versionRegex = new System.Text.RegularExpressions.Regex(@"(\d+[\._]\d+[\._]\d+[\._]\d+)");

                // Ищем версию в каждой строке и берем саму строку как имя папки
                var versionEntries = versions
                    .Select(v => new { Raw = v, Match = versionRegex.Match(v) })
                    .Where(x => x.Match.Success)
                    .Select(x =>
                    {
                        var rawVersion = x.Match.Value;
                        // Для сравнения приводим всё к точкам (25.9.30.1)
                        var normalizedVersion = rawVersion.Replace('_', '.');
                        return new { x.Raw, Version = normalizedVersion };
                    })
                    .OrderByDescending(x => x.Version, new VersionComparer())
                    .ToList();

                if (!versionEntries.Any())
                {
                    _logger.Warning("No version folders found in {Url}", ftpUrl);
                    return null;
                }

                var latest = versionEntries.First();
                string latestVersionFolder = latest.Raw;
                string versionStr = latest.Version;

                AddUiLog($"FTP: Найдена версия {versionStr}");

                // 2. Go to EKP folder - пробуем разные варианты регистра
                string ekpPath = $"{ftpUrl}{latestVersionFolder}/EKP/";
                var files = await ListFtpDirectoryAsync(ekpPath);

                if (!files.Any())
                {
                    AddUiLog("Папка /EKP/ не найдена или пуста, пробуем /ekp/...");
                    ekpPath = $"{ftpUrl}{latestVersionFolder}/ekp/";
                    files = await ListFtpDirectoryAsync(ekpPath);
                }

                if (!files.Any())
                {
                    _logger.Warning("EKP folder is empty: {Url}", ekpPath);
                    return null;
                }

                // 3. Find .rar file (ищем файл, в котором есть расширение .rar)
                var rarFile = files.FirstOrDefault(f => f.EndsWith(".rar", StringComparison.OrdinalIgnoreCase));

                if (string.IsNullOrEmpty(rarFile))
                {
                    _logger.Warning("No .rar file found in {Url}", ekpPath);
                    return null;
                }

                return new DistroInfo
                {
                    Version = versionStr,
                    FileName = rarFile,
                    FullUrl = $"{ekpPath}{rarFile}"
                };
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error getting latest distro from FTP: {Url}", ftpUrl);
                return null;
            }
        }

        public async Task DownloadDistroAsync(DistroInfo distro, string localPath, IProgress<DownloadProgress>? progress = null, System.Threading.CancellationToken ct = default)
        {
            try
            {
                if (!Directory.Exists(localPath)) Directory.CreateDirectory(localPath);
                string fullFilePath = Path.Combine(localPath, distro.FileName);

                _logger.Information("Starting download of {FileName} to {LocalPath}", distro.FileName, fullFilePath);

                // Get file size first to ensure we can track progress (some FTP servers return -1 for ContentLength on download request)
                long fileSize = -1;
                try
                {
#pragma warning disable SYSLIB0014
                    var sizeRequest = (FtpWebRequest)WebRequest.Create(distro.FullUrl);
#pragma warning restore SYSLIB0014
                    sizeRequest.Proxy = null;
                    sizeRequest.Method = WebRequestMethods.Ftp.GetFileSize;
                    using var sizeResponse = (FtpWebResponse)await sizeRequest.GetResponseAsync();
                    fileSize = sizeResponse.ContentLength;
                    if (fileSize > 0) AddUiLog($"FTP: Размер файла {(fileSize / 1024.0 / 1024.0):F1} МБ");
                }
                catch (Exception exSize)
                {
                    _logger.Warning(exSize, "Could not get file size for FTP progress tracking.");
                }

#pragma warning disable SYSLIB0014
                var request = (FtpWebRequest)WebRequest.Create(distro.FullUrl);
#pragma warning restore SYSLIB0014
                request.Proxy = null;
                request.Method = WebRequestMethods.Ftp.DownloadFile;
                request.UseBinary = true;
                request.EnableSsl = false;
                request.KeepAlive = false;

                AddUiLog($"FTP: Скачивание {distro.FileName}...");
                using var response = (FtpWebResponse)await request.GetResponseAsync();

                // Use fileSize from GetFileSize if ContentLength is not available
                if (fileSize <= 0) fileSize = response.ContentLength;

                using var responseStream = response.GetResponseStream();
                using var fileStream = new FileStream(fullFilePath, FileMode.Create);

                byte[] buffer = new byte[81920]; // 80KB buffer
                int bytesRead;
                long totalBytesRead = 0;

                while ((bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
                {
                    ct.ThrowIfCancellationRequested();
                    await fileStream.WriteAsync(buffer, 0, bytesRead, ct);
                    totalBytesRead += bytesRead;

                    int p = 0;
                    if (fileSize > 0)
                    {
                        p = (int)((totalBytesRead * 100L) / fileSize);
                        if (p > 100) p = 100;
                    }
                    progress?.Report(new DownloadProgress { Received = totalBytesRead, Total = fileSize, Percentage = p });
                }

                progress?.Report(new DownloadProgress { Received = totalBytesRead, Total = fileSize, Percentage = 100 });
                _logger.Information("Download completed: {FileName}", distro.FileName);
            }
            catch (OperationCanceledException)
            {
                _logger.Warning("Download cancelled for {FileName}", distro.FileName);
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error downloading distro from FTP");
                throw;
            }
        }

        private async Task<List<string>> ListFtpDirectoryAsync(string url)
        {
            var results = new List<string>();
            try
            {
#pragma warning disable SYSLIB0014
                var request = (FtpWebRequest)WebRequest.Create(url);
#pragma warning restore SYSLIB0014
                request.Proxy = null;
                request.Method = WebRequestMethods.Ftp.ListDirectory;

                using var response = (FtpWebResponse)await request.GetResponseAsync();
                using var reader = new StreamReader(response.GetResponseStream());

                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var entry = line.Trim();
                    if (entry == "." || entry == "..") continue;

                    // Если сервер возвращает полный путь, берем только имя
                    var parts = entry.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0)
                    {
                        var name = parts.Last();
                        results.Add(name);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warning("Could not list FTP directory {Url}: {Message}", url, ex.Message);
                AddUiLog($"Ошибка FTP при чтении {url}: {ex.Message}");
            }
            return results;
        }

        private class VersionComparer : IComparer<string>
        {
            public int Compare(string? x, string? y)
            {
                if (x == null || y == null) return 0;

                var xParts = x.Split('.').Select(p => int.TryParse(p, out var v) ? v : 0).ToArray();
                var yParts = y.Split('.').Select(p => int.TryParse(p, out var v) ? v : 0).ToArray();

                int length = Math.Max(xParts.Length, yParts.Length);
                for (int i = 0; i < length; i++)
                {
                    int xV = i < xParts.Length ? xParts[i] : 0;
                    int yV = i < yParts.Length ? yParts[i] : 0;
                    if (xV != yV) return xV.CompareTo(yV);
                }
                return 0;
            }
        }
    }
}
