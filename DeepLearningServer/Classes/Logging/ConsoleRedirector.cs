using System;
using System.IO;
using System.Text;

namespace DeepLearningServer.Classes.Logging
{
  public static class ConsoleRedirector
  {
    public static void RedirectToLogger(FileLoggerProvider provider)
    {
      if (provider == null) return;
      var writer = new LoggerTextWriter(provider);
      Console.SetOut(writer);
      Console.SetError(writer);
    }

    private sealed class LoggerTextWriter : TextWriter
    {
      private readonly FileLoggerProvider _provider;
      private readonly StringBuilder _buffer = new StringBuilder();

      public LoggerTextWriter(FileLoggerProvider provider)
      {
        _provider = provider;
      }

      public override Encoding Encoding => new UTF8Encoding(false);

      public override void Write(char value)
      {
        if (value == '\n')
        {
          FlushBuffer();
        }
        else if (value != '\r')
        {
          _buffer.Append(value);
        }
      }

      public override void Write(string value)
      {
        if (string.IsNullOrEmpty(value)) return;
        int start = 0;
        int idx;
        while ((idx = value.IndexOf('\n', start)) >= 0)
        {
          var segment = value.Substring(start, idx - start);
          if (segment.EndsWith("\r")) segment = segment.Substring(0, segment.Length - 1);
          _buffer.Append(segment);
          FlushBuffer();
          start = idx + 1;
        }
        if (start < value.Length)
        {
          var tail = value.Substring(start);
          _buffer.Append(tail);
        }
      }

      public override void WriteLine(string value)
      {
        if (!string.IsNullOrEmpty(value))
        {
          _buffer.Append(value);
        }
        FlushBuffer();
      }

      public override void Flush()
      {
        FlushBuffer();
      }

      private void FlushBuffer()
      {
        if (_buffer.Length == 0) return;
        var line = _buffer.ToString();
        _buffer.Clear();
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        _provider.WriteLine($"{timestamp} [CONSOLE] {line}");
      }
    }
  }
}


