using System.Net.Http;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Serilog;
using System;
using System.Threading.Tasks;

namespace AISManager.Services
{
    public class VersionService : IVersionService
    {
        private readonly HttpClient _httpClient;
        private const string AISVersionCheckURL = "https://support.tax.nalog.ru/sections/knowledge_base/";
        private static readonly ILogger s_logger = Log.ForContext<VersionService>();

        public VersionService()
        {
            var handler = new HttpClientHandler
            {
                UseDefaultCredentials = true
            };
            _httpClient = new HttpClient(handler);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/100.0.0.0 Safari/537.36");
        }

        public async Task<string> GetCurrentAISVersionAsync()
        {
            try
            {
                var response = await _httpClient.GetStringAsync(AISVersionCheckURL);
                var doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(response);

                var versionListNodes = doc.DocumentNode.SelectNodes("//footer[@class='footer']//div[@class='footer__text-container']/ul[@class='footer__text-ul'][1]/li[@class='footer__text']");

                string result = "Не найдено";
                if (versionListNodes != null)
                {
                    foreach (var listItemNode in versionListNodes)
                    {
                        var text = listItemNode.InnerText.Trim();
                        if (text.Contains("КПЭ АИС «Налог-3»") || text.Contains("КПЭ АИС"))
                        {
                            var versionMatch = Regex.Match(text, @"(\d+\.\d+\.\d+\.\d+)");
                            if (versionMatch.Success)
                            {
                                result = versionMatch.Groups[1].Value;
                                break;
                            }
                        }
                    }
                }

                if (result == "Не найдено")
                {
                    s_logger.Warning("Не удалось определить текущую версию АИС на странице техподдержки");
                }
                else
                {
                    s_logger.Information("Определена текущая версия АИС: {Version}", result);
                }

                return result;
            }
            catch (Exception ex)
            {
                s_logger.Error(ex, "Ошибка при попытке определить версию АИС");
                throw;
            }
        }

        public Task<bool> ValidateVersionAsync(string version)
        {
            if (string.IsNullOrWhiteSpace(version)) return Task.FromResult(false);
            var versionPattern = @"^\d+\.\d+\.\d+\.\d+$";
            return Task.FromResult(Regex.IsMatch(version, versionPattern));
        }
    }
}
