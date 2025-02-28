using System;
using System.Collections.Generic;

namespace DeepLearningServer.Models;

public partial class ProgressEntry
{
    public int Id { get; set; }

    public bool IsTraining { get; set; }

    public double Progress { get; set; }

    public double BestIteration { get; set; }

    public float? Accuracy { get; set; }
    public DateTime Timestamp { get; set; }

    public int TrainingRecordId { get; set; }

    public virtual TrainingRecord TrainingRecord { get; set; } = null!;
}
