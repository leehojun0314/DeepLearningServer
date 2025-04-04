﻿
namespace DeepLearningServer.Models;

public partial class LogRecord
{
    public int Id { get; set; }

    public string Message { get; set; } = null!;

    public LogLevel Level { get; set; } = LogLevel.Debug!;

    public DateTime CreatedAt { get; set; }
}
