using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;

namespace DeepLearningServer.Classes.Logging
{
    public sealed class FileLoggerProvider : ILoggerProvider
    {
        private readonly string _logsDirectory;
        private readonly LogLevel _minimumLevel;
        private readonly object _sync = new object();
        private bool _disposed;

        public FileLoggerProvider(string logsDirectory, LogLevel minimumLevel = LogLevel.Information)
        {
            _logsDirectory = logsDirectory ?? throw new ArgumentNullException(nameof(logsDirectory));
            _minimumLevel = minimumLevel;
            Directory.CreateDirectory(_logsDirectory);
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new FileLogger(this, categoryName);
        }

        internal bool IsEnabled(LogLevel level)
        {
            return level >= _minimumLevel;
        }

        internal void WriteLine(string line)
        {
            if (_disposed) return;

            lock (_sync)
            {
                if (_disposed) return;

                var filePath = GetLogFilePath(DateTime.Now.Date);
                try
                {
                    using (var fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                    using (var writer = new StreamWriter(fileStream, new UTF8Encoding(false)))
                    {
                        writer.WriteLine(line);
                    }
                }
                catch (IOException ex)
                {
                    System.Diagnostics.Trace.TraceError($"FileLoggerProvider: IOException while writing log: {ex}");
                }
                catch (UnauthorizedAccessException ex)
                {
                    System.Diagnostics.Trace.TraceError($"FileLoggerProvider: UnauthorizedAccessException while writing log: {ex}");
                }
                catch (ArgumentException ex)
                {
                    System.Diagnostics.Trace.TraceError($"FileLoggerProvider: ArgumentException while writing log: {ex}");
                }
            }
        }

        private string GetLogFilePath(DateTime date)
        {
            var fileName = $"app-{date:yyyy-MM-dd}.log";
            return Path.Combine(_logsDirectory, fileName);
        }

        public void Dispose()
        {
            if (_disposed) return;
            lock (_sync)
            {
                _disposed = true;
            }
        }

        private sealed class FileLogger : ILogger
        {
            private readonly FileLoggerProvider _provider;
            private readonly string _categoryName;

            public FileLogger(FileLoggerProvider provider, string categoryName)
            {
                _provider = provider;
                _categoryName = categoryName;
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                return NullScope.Instance;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return _provider.IsEnabled(logLevel);
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                if (!_provider.IsEnabled(logLevel)) return;
                if (formatter == null) return;

                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var message = formatter(state, exception);
                var level = logLevel.ToString().ToUpperInvariant();

                var line = $"{timestamp} [{level}] {_categoryName} ({eventId.Id}) {message}";
                if (exception != null)
                {
                    line += Environment.NewLine + exception;
                }

                _provider.WriteLine(line);
            }
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new NullScope();
            public void Dispose() { }
        }
    }
}
