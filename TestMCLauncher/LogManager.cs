using System;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace TestMCLauncher
{
    public class LogManager
    {
        private readonly StringBuilder _logBuilder = new();
        private readonly XNamespace _ns = "urn:ignore";
        private bool _isLogging;

        public event EventHandler<LogRecord>? LogAdded;

        public void AddLog(string log)
        {
            if (!_isLogging)
            {
                if (log.Contains("<log4j:Event"))
                {
                    _isLogging = true;
                }
                else
                {
                    LogAdded?.Invoke(this, new LogRecord
                    {
                        Logger = "LogManager",
                        Level = "INFO",
                        Message = log,
                        Thread = "LogManager",
                        TimeStamp = DateTime.Now.Ticks,
                        StackTrace = string.Empty
                    });
                    return;
                }
            }
            var data = log.Replace("<log4j:Event", $"<log4j:Event xmlns:log4j=\"{_ns}\"");
            _logBuilder.AppendLine(data);
            if (!data.Contains("</log4j:Event>")) return;
            var rawLog = _logBuilder.ToString();
            var element = XElement.Parse(rawLog);
            var logRecord = new LogRecord
            {
                Logger = element.Attribute("logger")?.Value ?? string.Empty,
                Level = element.Attribute("level")?.Value ?? string.Empty,
                Message = element.Elements(_ns + "Message").Select(s => s.Value).FirstOrDefault() ??
                          string.Empty,
                Thread = element.Attribute("thread")?.Value ?? string.Empty,
                TimeStamp = long.Parse(element.Attribute("timestamp")?.Value ?? "0"),
                StackTrace = element.Elements(_ns + "Throwable").Select(s => s.Value).FirstOrDefault() ??
                             string.Empty
            };
            LogAdded?.Invoke(this, logRecord);
            _logBuilder.Clear();
            _isLogging = false;
        }
    }

    public record LogRecord
    {
        public string Logger { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string StackTrace { get; set; } = string.Empty;
        public string Thread { get; set; } = string.Empty;
        public long TimeStamp { get; set; } = 0;
    }
}
