// Version: 1.0.0.673
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThmdPlayer.Core.configuration;
using ThmdPlayer.Core.logs;

namespace ThmdPlayer.Core
{
    /// <summary>
    /// Logger class for managing logging functionality.
    /// </summary>
    public static class Logger
    {
        private static List<string> _categories = new List<string> { "Console", "File" };
        private static Core.logs.AsyncLogger _log { get; set; } = new AsyncLogger();

        /// <summary>
        /// Gets or sets the configuration instance.
        /// </summary>
        public static Config Config { get; set; } = Config.Instance;
        /// <summary>
        /// Gets or sets the logger instance.
        /// </summary>
        public static Core.logs.AsyncLogger Log { get => _log; set => _log = value; }

        /// <summary>
        /// Initializes the logger with the specified configuration.
        /// </summary>
        /// <returns></returns>
        public static AsyncLogger InitLogs()
        {
            _log = new AsyncLogger
            {
                MinLogLevel = Config.LogLevel,
                CategoryFilters =
                {
                    ["Console"] = true,
                    ["File"] = true
                },
            };

            _log.AddSink(new CategoryFilterSink(
                new FileSink("Logs", "log", new TextFormatter()), new[] { "File" }));

            _log.AddSink(new CategoryFilterSink(
                new ConsoleSink(formatter: new TextFormatter()), new[] { "Console" }));

            return _log;
        }

        /// <summary>
        /// Adds a log entry with the specified level, message, and optional categories and exception.
        /// </summary>
        /// <param name="level">Log level</param>
        /// <param name="message">Log message</param>
        /// <param name="category">Array of category</param>
        /// <param name="exception">Exception</param>
        public static void AddLog(Core.logs.LogLevel level, string message, string[] category = null, Exception exception = null)
        {
            //if (category != null)
            // _categories.AddRange(category);
            if (category != null)
            {
                foreach (var cat in _categories)
                {
                    foreach (var c in category ?? Array.Empty<string>())
                    {
                        if (!string.IsNullOrEmpty(c) && !cat.Equals(c, StringComparison.OrdinalIgnoreCase))
                        {
                            if (!_categories.Contains(c))
                            {
                                _categories.Add(c);
                            }
                        }
                    }
                }
            }
            _log.Log(level, _categories.ToArray(), message, exception);
        }
    }
}
