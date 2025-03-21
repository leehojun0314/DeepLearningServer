using System;
using System.Collections.Generic;

namespace DeepLearningServer.Models;

public partial class RecipeFile
{
    public int Id { get; set; }

    public string FileName { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string FileType { get; set; } = null!;
    public string SyncStatus { get; set; } = null!;

    public int AdmsProcessId { get; set; }

    public DateTime LastModified { get; set; }

    public virtual AdmsProcess AdmsProcess { get; set; } = null!;
}
