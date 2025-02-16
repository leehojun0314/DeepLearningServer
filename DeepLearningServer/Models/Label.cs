using System;
using System.Collections.Generic;

namespace DeepLearningServer.Models;

public partial class Label
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int TrainingRecordId { get; set; }

    public virtual TrainingRecord TrainingRecord { get; set; } = null!;
}
