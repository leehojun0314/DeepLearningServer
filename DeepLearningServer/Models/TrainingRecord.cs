﻿using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using DeepLearningServer.Enums;

namespace DeepLearningServer.Models
{
    public class TrainingRecord
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public int Id { get; set; }
        public required ImageSize ImageSize { get; set; } = ImageSize.Medium;
        public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
        public Guid SettingID { get; set; } = Guid.NewGuid();

        // TrainingRecord 필드
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public DateTime? EndTime { get; set; }
        public string RecipeId { get; set; } = string.Empty;
        public string ProcessId { get; set; } = string.Empty;

        public string ModelName { get; set; } = string.Empty;
        public string ModelPath { get; set; } = string.Empty;
        public double Accuracy { get; set; }
        public double Loss { get; set; }
        public float Progress { get; set; }
        public int BestIteration { get; set; }
        public ICollection<ProgressEntry> ProgressHistory { get; set; } = new List<ProgressEntry>();
        public ICollection<Category> Categories { get; set; } = new List<Category>();

        [Column(TypeName = "nvarchar(50)")]
        public TrainingStatus Status { get; set; } = TrainingStatus.Stanby;

        // Geometry Params
        public float MaxRotation { get; set; }
        public float MaxVerticalShift { get; set; }
        public float MaxHorizontalShift { get; set; }
        public float MinScale { get; set; }
        public float MaxScale { get; set; }
        public float MaxVerticalShear { get; set; }
        public float MaxHorizontalShear { get; set; }
        public bool VerticalFlip { get; set; }
        public bool HorizontalFlip { get; set; }

        // Color Params
        public float MaxBrightnessOffset { get; set; }
        public float MaxContrastGain { get; set; }
        public float MinContrastGain { get; set; }
        public float MaxGamma { get; set; }
        public float MinGamma { get; set; }
        public float HueOffset { get; set; }
        public float MaxSaturationGain { get; set; }
        public float MinSaturationGain { get; set; }

        // Noise Params
        public float MaxGaussianDeviation { get; set; }
        public float MinGaussianDeviation { get; set; }
        public float MaxSpeckleDeviation { get; set; }
        public float MinSpeckleDeviation { get; set; }
        public float MaxSaltPepperNoise { get; set; }
        public float MinSaltPepperNoise { get; set; }

        // Classifier Params
        public int ClassifierCapacity { get; set; }
        public int ImageCacheSize { get; set; }
        public int ImageWidth { get; set; }
        public int ImageHeight { get; set; }
        public int ImageChannels { get; set; }
        public bool UsePretrainedModel { get; set; }
        public bool ComputeHeatMap { get; set; }
        public bool EnableHistogramEqualization { get; set; }
        public int BatchSize { get; set; }
    }
    
}
