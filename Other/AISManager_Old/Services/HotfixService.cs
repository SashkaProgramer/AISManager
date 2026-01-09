using Serilog;
using System.Text.RegularExpressions;
using AISManager.Models;

namespace AISManager.Services
{
    public class HotfixService : IHotfixService
    {
        private readonly HttpClient _httpClient;
        private const string HotfixSearchBaseURL = "https://support.tax.nalog.ru/sections/knowledge_base/search.php";
        private const string HotfixPrefix = "kpe_";
        private const string BaseDownloadUrl = "https://support.tax.nalog.ru";


        private static readonly ILogger s_logger = Log.ForContext<HotfixService>();

        public HotfixService()
        {
            var handler = new HttpClientHandler { UseDefaultCredentials = true };
            _httpClient = new HttpClient(handler);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/100.0.0.0 Safari/537.36");
        }

        public async Task<List<HotfixInfo>> GetHotfixesAsync(string version)
        {
            var hotfixesTemp = new List<HotfixInfo>();
            string searchTerm = $"{HotfixPrefix}{version}";
            string searchUrl = $"{HotfixSearchBaseURL}?kb=fns&term={Uri.EscapeDataString(searchTerm)}&from=&to=";

            s_logger.Information("[GetHotfixesAsync] Запрос списка хотфиксов для версии {Version} по URL: {SearchUrl}", version, searchUrl);

            try
            {
                var responseHtml = await _httpClient.GetStringAsync(searchUrl);
                s_logger.Debug("[GetHotfixesAsync] HTML-ответ получен, длина: {Length}", responseHtml.Length);

                var regexPattern = @"/upload[^""'\s]+\.(zip|rar)";
                var matches = Regex.Matches(responseHtml, regexPattern, RegexOptions.IgnoreCase);

                if (matches.Count > 0)
                {
                    s_logger.Information("[GetHotfixesAsync] Найдено {Count} потенциальных ссылок на хотфиксы на странице (до удаления дубликатов).", matches.Count);
                    foreach (Match match in matches)
                    {
                        var relativeUrlFromHtml = match.Value;
                        s_logger.Debug("[GetHotfixesAsync] Regex Match.Value: '{MatchValue}'", relativeUrlFromHtml);

                        string unescapedForUnicodeSequencesUrl;
                        try
                        {
                            unescapedForUnicodeSequencesUrl = Regex.Replace(
                                relativeUrlFromHtml,
                                @"\\u([0-9A-Fa-f]{4})", // Ищет литеральный \uXXXX
                                m => ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString()
                            );
                        }
                        catch (Exception ex_unescape)
                        {
                            s_logger.Warning(ex_unescape, "[GetHotfixesAsync] Ошибка при Regex.Replace для \\uXXXX в '{OriginalUrl}'. Используем как есть.", relativeUrlFromHtml);
                            unescapedForUnicodeSequencesUrl = relativeUrlFromHtml;
                        }
                        s_logger.Debug("[GetHotfixesAsync] URL после Regex.Replace для \\uXXXX: '{ProcessedUrl}'", unescapedForUnicodeSequencesUrl);

                        var fullyDecodedUrl = Uri.UnescapeDataString(unescapedForUnicodeSequencesUrl);
                        s_logger.Debug("[GetHotfixesAsync] URL после Uri.UnescapeDataString: '{DecodedUrl}'", fullyDecodedUrl);

                        var fileName = Path.GetFileName(fullyDecodedUrl);
                        s_logger.Debug("[GetHotfixesAsync] Extracted FileName (Path.GetFileName): '{FileName}'", fileName);

                        hotfixesTemp.Add(new HotfixInfo
                        {
                            Name = fileName,       
                            Url = fullyDecodedUrl, 
                            Version = version
                        });
                    }

                    var initialCount = hotfixesTemp.Count;
                    var uniqueHotfixes = hotfixesTemp.GroupBy(h => h.Url)
                                               .Select(g => g.First())
                                               .ToList();
                    s_logger.Information("[GetHotfixesAsync] После удаления дубликатов по URL найдено {Count} уникальных хотфиксов (было {InitialCount}).", uniqueHotfixes.Count, initialCount);

                    s_logger.Debug("[GetHotfixesAsync] Финальное содержимое возвращаемого списка uniqueHotfixes:");
                    if (uniqueHotfixes.Any())
                    {
                        foreach (var hf in uniqueHotfixes)
                        {
                            s_logger.Debug("[GetHotfixesAsync] HotfixInfo to return: Name='{HotfixName}', Url='{HotfixUrl}', Version='{Version}'", hf.Name, hf.Url, hf.Version);
                        }
                    }
                    else
                    {
                        s_logger.Debug("[GetHotfixesAsync] Список uniqueHotfixes пуст после обработки.");
                    }
                    return uniqueHotfixes;
                }
                else
                {
                    s_logger.Warning("[GetHotfixesAsync] Хотфиксы для версии {Version} (поисковый термин: '{SearchTerm}') не найдены на странице {SearchUrl}", version, searchTerm, searchUrl);
                    var debugPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"debug_hotfix_search_{version}_{DateTime.Now:yyyyMMddHHmmss}.html");
                    File.WriteAllText(debugPath, responseHtml);
                    s_logger.Information("[GetHotfixesAsync] HTML-ответ поиска хотфиксов сохранен в: {DebugPath}", debugPath);
                    return new List<HotfixInfo>();
                }
            }
            catch (Exception ex)
            {
                s_logger.Error(ex, "[GetHotfixesAsync] Ошибка при получении или обработке списка обновлений для версии {Version} с URL {SearchUrl}", version, searchUrl);
                throw new Exception($"[GetHotfixesAsync] Ошибка при получении списка обновлений для версии {version}: {ex.Message}", ex);
            }
        }

        public async Task DownloadHotfixAsync(HotfixInfo hotfix, string downloadPath, IProgress<int> progress)
        {
            string finalUrlToRequest = string.Empty;

            if (hotfix == null || string.IsNullOrEmpty(hotfix.Url) || string.IsNullOrEmpty(hotfix.Name))
            {
                s_logger.Error("[DownloadHotfixAsync] Попытка скачивания хотфикса с некорректными данными. Hotfix Name: {HotfixName}, URL: {HotfixUrl}", hotfix?.Name ?? "N/A", hotfix?.Url ?? "N/A");
                throw new ArgumentException("HotfixInfo, его URL или Name не могут быть null или пустыми.");
            }

            try
            {
                string pathForUriBuilder = hotfix.Url;

                pathForUriBuilder = Regex.Replace(pathForUriBuilder, @"/+", "/");


                if (!pathForUriBuilder.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                    !pathForUriBuilder.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrEmpty(pathForUriBuilder) || !pathForUriBuilder.StartsWith("/"))
                    {
                        pathForUriBuilder = "/" + (pathForUriBuilder ?? string.Empty);
                    }
                }
                s_logger.Debug("[DownloadHotfixAsync] Путь для UriBuilder ПОСЛЕ нормализации слешей: '{PathForBuilder}' для хотфикса '{HotfixName}'", pathForUriBuilder, hotfix.Name);

                var uriBuilder = new UriBuilder
                {
                    Scheme = new Uri(BaseDownloadUrl).Scheme,
                    Host = new Uri(BaseDownloadUrl).Host,
                    Path = pathForUriBuilder 
                };
                finalUrlToRequest = uriBuilder.Uri.AbsoluteUri;

                s_logger.Information("[DownloadHotfixAsync] Начало скачивания хотфикса {HotfixName} по URL: {FinalUrl}", hotfix.Name, finalUrlToRequest);

                var response = await _httpClient.GetAsync(finalUrlToRequest, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                var filePath = Path.Combine(downloadPath, hotfix.Name); 

                s_logger.Information("[DownloadHotfixAsync] Сохранение файла {HotfixName} в {FilePath}. Ожидаемый размер: {TotalBytes} байт.", hotfix.Name, filePath, totalBytes > 0 ? totalBytes.ToString() : "неизвестен");

                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var contentStream = await response.Content.ReadAsStreamAsync())
                {
                    var buffer = new byte[8192];
                    var totalBytesRead = 0L;
                    int bytesRead;
                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        totalBytesRead += bytesRead;
                        if (totalBytes > 0)
                        {
                            var percentage = (int)((totalBytesRead * 100L) / totalBytes);
                            progress.Report(Math.Min(100, percentage));
                        }
                    }
                }

                hotfix.LocalPath = filePath;
                hotfix.DownloadDate = DateTime.Now;
                hotfix.FileSize = new FileInfo(filePath).Length;
                s_logger.Information("[DownloadHotfixAsync] Хотфикс {HotfixName} успешно скачан. Размер: {FileSize} байт. Сохранен в: {FilePath}", hotfix.Name, hotfix.FileSize, hotfix.LocalPath);
            }
            catch (HttpRequestException httpEx)
            {
                s_logger.Error(httpEx, "[DownloadHotfixAsync] HTTP ошибка при загрузке обновления {HotfixName} с URL {UrlToRequest}. Код состояния: {StatusCode}", hotfix.Name, finalUrlToRequest, httpEx.StatusCode);
                throw new Exception($"Ошибка при загрузке обновления {hotfix.Name}: {httpEx.Message} (URL: {finalUrlToRequest})", httpEx);
            }
            catch (Exception ex)
            {
                s_logger.Error(ex, "[DownloadHotfixAsync] Общая ошибка при загрузке обновления {HotfixName} с URL {UrlToRequest}", hotfix.Name, finalUrlToRequest);
                throw new Exception($"Ошибка при загрузке обновления {hotfix.Name}: {ex.Message} (URL: {finalUrlToRequest})", ex);
            }
        }

        public async Task<bool> ValidateHotfixAsync(HotfixInfo hotfix)
        {
            string finalUrlToRequest = string.Empty;

            if (hotfix == null || string.IsNullOrEmpty(hotfix.Url) || string.IsNullOrEmpty(hotfix.Name))
            {
                s_logger.Warning("[ValidateHotfixAsync] Попытка валидации хотфикса с некорректными данными. Hotfix Name: {HotfixName}, URL: {HotfixUrl}", hotfix?.Name ?? "N/A", hotfix?.Url ?? "N/A");
                return false;
            }

            try
            {
                string pathForUriBuilder = hotfix.Url;
                pathForUriBuilder = Regex.Replace(pathForUriBuilder, @"/+", "/");

                if (!pathForUriBuilder.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                    !pathForUriBuilder.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrEmpty(pathForUriBuilder) || !pathForUriBuilder.StartsWith("/"))
                    {
                        pathForUriBuilder = "/" + (pathForUriBuilder ?? string.Empty);
                    }
                }
                s_logger.Debug("[ValidateHotfixAsync] Путь для UriBuilder ПОСЛЕ нормализации слешей: '{PathForBuilder}' для хотфикса '{HotfixName}'", pathForUriBuilder, hotfix.Name);


                var uriBuilder = new UriBuilder
                {
                    Scheme = new Uri(BaseDownloadUrl).Scheme,
                    Host = new Uri(BaseDownloadUrl).Host,
                    Path = pathForUriBuilder
                };
                finalUrlToRequest = uriBuilder.Uri.AbsoluteUri;

                s_logger.Debug("[ValidateHotfixAsync] Валидация хотфикса {HotfixName} по URL: {FinalUrl}", hotfix.Name, finalUrlToRequest);
                var request = new HttpRequestMessage(HttpMethod.Head, finalUrlToRequest);
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    s_logger.Debug("[ValidateHotfixAsync] Валидация хотфикса {HotfixName} успешна (URL: {FinalUrl})", hotfix.Name, finalUrlToRequest);
                    return true;
                }
                else
                {
                    s_logger.Warning("[ValidateHotfixAsync] Валидация хотфикса {HotfixName} не удалась. Код: {StatusCode} (URL: {FinalUrl})", hotfix.Name, response.StatusCode, finalUrlToRequest);
                    return false;
                }
            }
            catch (Exception ex)
            {
                s_logger.Error(ex, "[ValidateHotfixAsync] Ошибка при валидации хотфикса {HotfixName} по URL {UrlToRequest}", hotfix.Name, finalUrlToRequest);
                return false;
            }
        }
    }
}