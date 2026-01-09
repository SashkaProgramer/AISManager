using System.Net.Http;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace AISManager.Services
{
    public class VersionService : IVersionService
    {
        private readonly HttpClient _httpClient;
        private const string AISVersionCheckURL = "https://support.tax.nalog.ru/sections/knowledge_base/";

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
                var doc = new HtmlDocument();
                doc.LoadHtml(response);

                var versionListNodes = doc.DocumentNode.SelectNodes("//footer[@class='footer']//div[@class='footer__text-container']/ul[@class='footer__text-ul'][1]/li[@class='footer__text']");

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
                                return versionMatch.Groups[1].Value;
                            }
                        }
                    }
                }
                
                // Fallback or error handling could go here
                return "Не найдено";
            }
            catch (Exception)
            {
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
