using DeepLearningServer.Enums;
using System;
using System.Collections.Generic;

namespace DeepLearningServer.Models;

public partial class TrainingRecord
{
    public int Id { get; set; }

    public int ImageSize { get; set; }

    public DateTime CreatedTime { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public int AdmsProcessId { get; set; }

    public string? ModelName { get; set; } = null!;

    public string? ModelPath { get; set; } = null!;

    public double? Accuracy { get; set; }

    public double? Loss { get; set; }

    public float? Progress { get; set; }

    public int? BestIteration { get; set; }

    public TrainingStatus? Status { get; set; } = null!;

    public float MaxRotation { get; set; }

    public float MaxVerticalShift { get; set; }

    public float MaxHorizontalShift { get; set; }

    public float MinScale { get; set; }

    public float MaxScale { get; set; }

    public float MaxVerticalShear { get; set; }

    public float MaxHorizontalShear { get; set; }

    public bool VerticalFlip { get; set; }

    public bool HorizontalFlip { get; set; }

    public float MaxBrightnessOffset { get; set; }

    public float MaxContrastGain { get; set; }

    public float MinContrastGain { get; set; }

    public float MaxGamma { get; set; }

    public float MinGamma { get; set; }

    public float HueOffset { get; set; }

    public float MaxSaturationGain { get; set; }

    public float MinSaturationGain { get; set; }

    public float MaxGaussianDeviation { get; set; }

    public float MinGaussianDeviation { get; set; }

    public float MaxSpeckleDeviation { get; set; }

    public float MinSpeckleDeviation { get; set; }

    public float MaxSaltPepperNoise { get; set; }

    public float MinSaltPepperNoise { get; set; }

    public int ClassifierCapacity { get; set; }

    public int ImageCacheSize { get; set; }

    public int ImageWidth { get; set; }

    public int ImageHeight { get; set; }

    public int ImageChannels { get; set; }

    public bool UsePretrainedModel { get; set; }

    public bool ComputeHeatMap { get; set; }

    public bool EnableHistogramEqualization { get; set; }

    public int BatchSize { get; set; }

    public virtual AdmsProcess AdmsProcess { get; set; } = null!;

    public virtual ICollection<Label> Labels { get; set; } = new List<Label>();

    public virtual ICollection<ProgressEntry> ProgressEntries { get; set; } = new List<ProgressEntry>();
}
