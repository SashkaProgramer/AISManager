using Serilog;
using System.IO;
using System.Net.Http;
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

            s_logger.Information("Запрос списка хотфиксов для версии {Version}. URL: {SearchUrl}", version, searchUrl);

            try
            {
                var responseHtml = await _httpClient.GetStringAsync(searchUrl);
                s_logger.Debug("HTML-ответ получен, длина: {Length} символов", responseHtml.Length);

                var regexPattern = @"/upload[^""'\s]+\.(zip|rar)";
                var matches = Regex.Matches(responseHtml, regexPattern, RegexOptions.IgnoreCase);

                if (matches.Count > 0)
                {
                    s_logger.Information("Найдено {Count} потенциальных ссылок на хотфиксы (до фильтрации)", matches.Count);
                    foreach (Match match in matches)
                    {
                        var relativeUrlFromHtml = match.Value;
                        s_logger.Debug("Найдено совпадение: '{MatchValue}'", relativeUrlFromHtml);

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
                            s_logger.Warning(ex_unescape, "Ошибка при обработке Unicode-последовательностей в URL: '{OriginalUrl}'", relativeUrlFromHtml);
                            unescapedForUnicodeSequencesUrl = relativeUrlFromHtml;
                        }

                        var fullyDecodedUrl = Uri.UnescapeDataString(unescapedForUnicodeSequencesUrl);
                        var fileName = Path.GetFileName(fullyDecodedUrl);

                        hotfixesTemp.Add(new HotfixInfo
                        {
                            Name = fileName,
                            Url = fullyDecodedUrl,
                            Version = version
                        });
                    }

                    var uniqueHotfixes = hotfixesTemp.GroupBy(h => h.Url)
                                               .Select(g => g.First())
                                               .ToList();
                    s_logger.Information("После удаления дубликатов найдено {Count} уникальных хотфиксов", uniqueHotfixes.Count);

                    return uniqueHotfixes;
                }
                else
                {
                    s_logger.Warning("Хотфиксы для версии {Version} не найдены на странице поддержки", version);
                    return new List<HotfixInfo>();
                }
            }
            catch (Exception ex)
            {
                s_logger.Error(ex, "Ошибка при получении списка обновлений для версии {Version}", version);
                throw;
            }
        }

        public async Task DownloadHotfixAsync(HotfixInfo hotfix, string downloadPath, IProgress<int> progress, System.Threading.CancellationToken ct)
        {
            if (hotfix == null || string.IsNullOrEmpty(hotfix.Url) || string.IsNullOrEmpty(hotfix.Name))
            {
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

                var uriBuilder = new UriBuilder
                {
                    Scheme = new Uri(BaseDownloadUrl).Scheme,
                    Host = new Uri(BaseDownloadUrl).Host,
                    Path = pathForUriBuilder
                };
                string finalUrlToRequest = uriBuilder.Uri.AbsoluteUri;

                s_logger.Information("Начало скачивания хотфикса {HotfixName}. URL: {FinalUrl}", hotfix.Name, finalUrlToRequest);

                var response = await _httpClient.GetAsync(finalUrlToRequest, HttpCompletionOption.ResponseHeadersRead, ct);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                var filePath = Path.Combine(downloadPath, hotfix.Name);

                // Ensure directory exists
                Directory.CreateDirectory(downloadPath);

                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var contentStream = await response.Content.ReadAsStreamAsync(ct))
                {
                    var buffer = new byte[8192];
                    var totalBytesRead = 0L;
                    int bytesRead;
                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead, ct);
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
                s_logger.Information("Хотфикс {HotfixName} успешно скачан", hotfix.Name);
            }
            catch (Exception ex)
            {
                s_logger.Error(ex, "Ошибка при загрузке хотфикса {HotfixName}", hotfix.Name);
                throw;
            }
        }

        public async Task<bool> ValidateHotfixAsync(HotfixInfo hotfix)
        {
            if (hotfix == null || string.IsNullOrEmpty(hotfix.Url)) return false;

            try
            {
                string pathForUriBuilder = hotfix.Url;
                pathForUriBuilder = Regex.Replace(pathForUriBuilder, @"/+", "/");

                if (!pathForUriBuilder.StartsWith("http") && !pathForUriBuilder.StartsWith("/"))
                {
                    pathForUriBuilder = "/" + pathForUriBuilder;
                }

                var uriBuilder = new UriBuilder
                {
                    Scheme = new Uri(BaseDownloadUrl).Scheme,
                    Host = new Uri(BaseDownloadUrl).Host,
                    Path = pathForUriBuilder
                };

                var request = new HttpRequestMessage(HttpMethod.Head, uriBuilder.Uri.AbsoluteUri);
                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                s_logger.Error(ex, "Ошибка при валидации хотфикса {HotfixName}", hotfix.Name);
                return false;
            }
        }
    }
}
