using AISManager.App.Configs;
using HardDev.CoreUtils.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace AISManager.App.WinFormSink
{
    public class WinFormSink : ILogEventSink
    {
        private readonly MessageTemplateTextFormatter _formatter;
        public static event Action<string> LogEmitted;

        public WinFormSink(string outputTemplate, IFormatProvider formatProvider = null)
        {
            _formatter = new MessageTemplateTextFormatter(outputTemplate, formatProvider);
        }

        public void Emit(LogEvent logEvent)
        {
            if (LogEmitted == null) return;

            using var writer = new StringWriter();
            _formatter.Format(logEvent, writer);
            LogEmitted.Invoke(writer.ToString());
        }

        public static ILogger Build(string name)
        {
            var cfg = new LoggerConfig
            {
                ContextName = name,
                EnableDebug = true,
                EnableConsole = true,
                LogPath = AppPath.LogsPath
            };

            cfg.Sinks.Add(new WinFormSink("{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"));

            return AppLogger.Build(cfg);
        }
    }
}
