using DeepLearningServer.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DeepLearningServer.Models;

public class TrainingRecord
{
    [BsonId] public required ObjectId Id { get; set; }

    public required ImageSize ImageSize { get; set; }

    // 언제 훈련을 시작했는지
    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    // 언제 훈련을 완료했는지(훈련이 끝나면 값 채움)
    public DateTime? EndTime { get; set; }

    // 특정 레시피 / 프로세스(사용 시 식별용)
    public string RecipeId { get; set; } = string.Empty;
    public string ProcessId { get; set; } = string.Empty;

    // ---- Data Augmentation 파라미터 (필요한 것들만)
    public GeometryParams Geometry { get; set; } = new();
    public ColorParams Color { get; set; } = new();
    public NoiseParams Noise { get; set; } = new();
    public ClassifierParams Classifier { get; set; } = new();

    // ---- 결과(모델 이름, 경로, 정확도 등)
    public string ModelName { get; set; } = string.Empty;
    public string ModelPath { get; set; } = string.Empty;
    public double Accuracy { get; set; }
    public double Loss { get; set; }
    public float Progress { get; set; }
    public int BestIteration { get; set; }
    public float[] LearningRateParameters { get; set; }

    // 기타 필요한 결과나 상태
    [BsonRepresentation(BsonType.String)]
    public TrainingStatus Status { get; set; } = TrainingStatus.Stanby; // "Running", "Completed", "Failed" 등
    public List<ProgressEntry> ProgressHistory { get; set; } = new();
    
    public Dictionary<string, float> Lables { get; set; } = new();

}

public class GeometryParams
{
    public float MaxRotation { get; set; }
    public float MaxVerticalShift { get; set; }
    public float MaxHorizontalShift { get; set; }
    public float MinScale { get; set; }
    public float MaxScale { get; set; }
    public float MaxVerticalShear { get; set; }
    public float MaxHorizontalShear { get; set; }
    public bool VerticalFlip { get; set; }
    public bool HorizontalFlip { get; set; }
}

public class ColorParams
{
    public float MaxBrightnessOffset { get; set; }
    public float MaxContrastGain { get; set; }
    public float MinContrastGain { get; set; }
    public float MaxGamma { get; set; }
    public float MinGamma { get; set; }
    public float HueOffset { get; set; }
    public float MaxSaturationGain { get; set; }
    public float MinSaturationGain { get; set; }
}

public class NoiseParams
{
    public float MaxGaussianDeviation { get; set; }
    public float MinGaussianDeviation { get; set; }
    public float MaxSpeckleDeviation { get; set; }
    public float MinSpeckleDeviation { get; set; }
    public float MaxSaltPepperNoise { get; set; }
    public float MinSaltPepperNoise { get; set; }
}

public class ClassifierParams
{
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
public class ProgressEntry
{
    public bool IsTraining { get; set; }
    public double Progress { get; set; }
    public double BestIteration { get; set; }
    public object LearningRateParameters { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}  