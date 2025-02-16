using System;
using System.Collections.Generic;

namespace DeepLearningServer.Models;

public partial class RecipeFile
{
    public int Id { get; set; }

    public string FileName { get; set; } = null!;

    public string Content { get; set; } = null!;

    public int ProcessId { get; set; }

    public int AdmsId { get; set; }

    public DateTime LastModified { get; set; }

    public virtual Adms Adms { get; set; } = null!;

    public virtual Process Process { get; set; } = null!;
}
