using System;
using System.Collections.Generic;

namespace DeepLearningServer.Models;

public partial class RecipeFile
{
    public int Id { get; set; }

    public string FileName { get; set; } = null!;

    public string Content { get; set; } = null!;

    //public int ProcessId { get; set; }

    //public int AdmsId { get; set; }
    public int AdmsProcessId { get; set; }

    public DateTime LastModified { get; set; }

    public virtual AdmsProcess AdmsProcess { get; set; } = null!;
    //public virtual Adm Adms { get; set; } = null!;

    //public virtual Process Process { get; set; } = null!;
}
