namespace DeepLearningServer.Dtos
{
    /// <summary>
    /// DTO that describes a saved model file.
    /// </summary>
    public class ModelInfoDto
    {
        /// <summary>
        /// Model file name (e.g. "123.onelmodel")
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Full file path.
        /// </summary>
        public string FullPath { get; set; } = string.Empty;

        /// <summary>
        /// Relative path from ModelDirectory.
        /// </summary>
        public string RelativePath { get; set; } = string.Empty;

        /// <summary>
        /// Image size (LARGE, MIDDLE).
        /// </summary>
        public string Size { get; set; } = string.Empty;

        /// <summary>
        /// Model type (BASE, Release, EVALUATION).
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// ADMS name.
        /// </summary>
        public string AdmsName { get; set; } = string.Empty;

        /// <summary>
        /// Process ID parsed from file name.
        /// </summary>
        public string ProcessId { get; set; } = string.Empty;

        /// <summary>
        /// File size in bytes.
        /// </summary>
        public long FileSizeBytes { get; set; }

        /// <summary>
        /// Human-readable file size.
        /// </summary>
        public string FileSizeFormatted { get; set; } = string.Empty;

        /// <summary>
        /// File creation timestamp.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// File modification timestamp.
        /// </summary>
        public DateTime ModifiedDate { get; set; }
    }

    /// <summary>
    /// Request DTO for model list query.
    /// </summary>
    public class ModelListRequestDto
    {
        /// <summary>
        /// Image size filter (LARGE, MIDDLE, or all).
        /// </summary>
        public string? Size { get; set; }

        /// <summary>
        /// Model type filter (BASE, Release, EVALUATION, or all).
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// ADMS name filter.
        /// </summary>
        public string? AdmsName { get; set; }

        /// <summary>
        /// Process ID filter.
        /// </summary>
        public string? ProcessId { get; set; }
    }
}
