using System;
using System.Collections.Generic;

namespace DeepLearningServer.Models;

public partial class AdmsProcess
{
    public int Id { get; set; }

    public int AdmsId { get; set; }

    public int ProcessId { get; set; }

    public DateTime? LastSyncDate { get; set; }
    public bool IsTrainned { get; set; }
    public bool IsCategorized { get; set; }
    public int L { get; set; }
    public int M { get; set; }
    public int S { get; set; }
    public virtual Adm Adms { get; set; } = null!;

    public virtual Process Process { get; set; } = null!;

    public virtual ICollection<TrainingRecord> TrainingRecords { get; set; } = new List<TrainingRecord>();
}
