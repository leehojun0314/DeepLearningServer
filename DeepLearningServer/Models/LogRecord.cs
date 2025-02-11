using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DeepLearningServer.Models;

public class LogRecord
{
    // 실제 DB에는 문자열이 들어갑니다
    [BsonElement("Level")] private string _levelString = "Info";

    [BsonId] public ObjectId Id { get; set; }

    public string Message { get; set; } = string.Empty;

    [BsonIgnore] // 이 속성은 DB 필드에 직접 매핑되지 않도록
    public LogLevel Level
    {
        get => StringToEnum(_levelString);
        set => _levelString = EnumToString(value);
    }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // 변환 로직
    private static string EnumToString(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Information => "Info",
            LogLevel.Error => "Error",
            LogLevel.Debug => "Debug",
            LogLevel.Trace => "Trace",
            LogLevel.Warning => "Warning",
            LogLevel.Critical => "Critical",
            LogLevel.None => "None",
            _ => "Info"
        };
    }

    private static LogLevel StringToEnum(string levelString)
    {
        return levelString switch
        {
            "Info" => LogLevel.Information,
            "Error" => LogLevel.Error,
            "Debug" => LogLevel.Debug,
            "Trace" => LogLevel.Trace,
            "Warning" => LogLevel.Warning,
            "Critical" => LogLevel.Critical,
            "None" => LogLevel.None,
            _ => LogLevel.Information
        };
    }
}