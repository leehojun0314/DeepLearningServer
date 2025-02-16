using System;
using System.Collections.Generic;

namespace DeepLearningServer.Models;

public partial class AdmsProcess
{
    public int Id { get; set; }

    public int AdmsId { get; set; }

    public int ProcessId { get; set; }

    public virtual Adms Adms { get; set; } = null!;

    public virtual Process Process { get; set; } = null!;
}
