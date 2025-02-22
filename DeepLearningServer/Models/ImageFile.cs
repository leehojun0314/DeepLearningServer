using System;
using System.Collections.Generic;

namespace DeepLearningServer.Models;

public partial class ImageFile
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Directory { get; set; } = null!;

    public string Size { get; set; } = null!;

    public string Status { get; set; } = null!;

    //public int ProcessId { get; set; }

    //public int AdmsId { get; set; }
    public int AdmsProcessId { get; set; }
    public DateTime CapturedTime { get; set; }

    //public virtual Adm Adms { get; set; } = null!;

    //public virtual Process Process { get; set; } = null!;
    public virtual AdmsProcess AdmsProcess { get; set; } = null!;
}
