using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace DeepLearningServer.Models
{
    


    public class LogRecord
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string Message { get; set; } = string.Empty;

        [Required]
        [Column("Level")]
        public string LevelString { get; set; } = "Info";

        [NotMapped]
        public LogLevel Level
        {
            get => StringToEnum(LevelString);
            set => LevelString = EnumToString(value);
        }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        private static string EnumToString(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Trace => "Trace",
                LogLevel.Debug => "Debug",
                LogLevel.Information => "Info",
                LogLevel.Warning => "Warning",
                LogLevel.Error => "Error",
                LogLevel.Critical => "Critical",
                LogLevel.None => "None",
                _ => "Info"
            };
        }

        private static LogLevel StringToEnum(string levelString)
        {
            return levelString switch
            {
                "Trace" => LogLevel.Trace,
                "Debug" => LogLevel.Debug,
                "Info" => LogLevel.Information,
                "Warning" => LogLevel.Warning,
                "Error" => LogLevel.Error,
                "Critical" => LogLevel.Critical,
                "None" => LogLevel.None,
                _ => LogLevel.Information
            };
        }
    }
}

