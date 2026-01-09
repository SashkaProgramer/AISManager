using System.Text.RegularExpressions;

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
                UseDefaultCredentials = true // Оставляем, если сайт требует аутентификации текущего пользователя Windows
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

                // 1. Находим первый ul с классом 'footer__text-ul' внутри div с классом 'footer__text-container',
                //    который в свою очередь находится внутри footer с классом 'footer'.
                //    Можно упростить, если структура уникальна.
                //    Начнем с более специфичного XPath:
                var versionListNodes = doc.DocumentNode.SelectNodes("//footer[@class='footer']//div[@class='footer__text-container']/ul[@class='footer__text-ul'][1]/li[@class='footer__text']");
                // Альтернативный, чуть менее строгий XPath, если предыдущий не сработает (например, если порядок ul может меняться, но класс li уникален для нужных элементов):
                // var versionListNodes = doc.DocumentNode.SelectNodes("//li[@class='footer__text']");


                if (versionListNodes != null)
                {
                    foreach (var listItemNode in versionListNodes)
                    {
                        var text = listItemNode.InnerText.Trim();
                        // Ищем строку "КПЭ АИС «Налог-3»" (с кавычками-ёлочками)
                        // или "КПЭ АИС" для большей гибкости, если кавычки могут меняться
                        if (text.Contains("КПЭ АИС «Налог-3»") || text.Contains("КПЭ АИС"))
                        {
                            // Регулярное выражение для извлечения версии формата X.X.X.X
                            // Оно должно найти версию после тире или в конце строки
                            var versionMatch = Regex.Match(text, @"(\d+\.\d+\.\d+\.\d+)");
                            if (versionMatch.Success)
                            {
                                return versionMatch.Groups[1].Value;
                            }
                        }
                    }
                }

                // Если версия не найдена через XPath, сохраняем HTML для отладки
                var debugPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug_version_page.html");
                File.WriteAllText(debugPath, response); // Сохраняем HTML, который вы предоставили
                throw new Exception($"Не удалось найти информацию о версии АИС (КПЭ АИС) на странице. XPath мог не сработать. HTML сохранен в {debugPath}");
            }
            catch (Exception ex)
            {
                // Логирование ошибки здесь было бы полезно, если у вас настроен s_logger в этом классе
                throw new Exception($"Ошибка при получении версии АИС: {ex.Message}", ex);
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