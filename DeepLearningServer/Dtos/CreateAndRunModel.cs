using System.ComponentModel;
using DeepLearningServer.Enums;
using Euresys.Open_eVision.EasyDeepLearning;

namespace DeepLearningServer.Dtos;

public class CreateAndRunModel
{
    public required List<int> AdmsProcessIds { get; set; }
    public required ImageSize ImageSize { get; set; }
    public string[] Categories { get; set; }
    public bool IsDefaultModel { get; set; }
    [DefaultValue("Middle/  ")]
    public string ClientModelDestination { get; set; } = "Middle/";
    [DefaultValue(0.7f)]
    public float TrainingProportion { get; set; } = 0.7f;

    [DefaultValue(0.2f)]
    public float ValidationProportion { get; set; } = 0.2f;

    [DefaultValue(0.1f)]
    public float TestProportion { get; set; } = 0.1f;

    [DefaultValue(50)]
    public int Iterations { get; set; } = 50;

    // 이하부터는 기존에 하나씩 있던 파라미터들을 하위 객체로 정리
    public GeometryParamsDto Geometry { get; set; } = new();
    public ColorParamsDto Color { get; set; } = new();
    public NoiseParamsDto Noise { get; set; } = new();
    public ClassifierParamsDto Classifier { get; set; } = new();
}
public class GeometryParamsDto
{
    [DefaultValue(0.0f)]
    public float MaxRotation { get; set; } = 0.0f;

    [DefaultValue(0)]
    public int MaxVerticalShift { get; set; } = 0;

    [DefaultValue(0)]
    public int MaxHorizontalShift { get; set; } = 0;

    [DefaultValue(1.0f)]
    public float MinScale { get; set; } = 1.0f;

    [DefaultValue(1.0f)]
    public float MaxScale { get; set; } = 1.0f;

    [DefaultValue(0.0f)]
    public float MaxVerticalShear { get; set; } = 0.0f;

    [DefaultValue(0.0f)]
    public float MaxHorizontalShear { get; set; } = 0.0f;

    [DefaultValue(false)]
    public bool VerticalFlip { get; set; } = false;

    [DefaultValue(false)]
    public bool HorizontalFlip { get; set; } = false;
}
public class ColorParamsDto
{
    [DefaultValue(0.0f)]
    public float MaxBrightnessOffset { get; set; } = 0.0f;

    [DefaultValue(1.0f)]
    public float MinContrastGain { get; set; } = 1.0f;

    [DefaultValue(1.0f)]
    public float MaxContrastGain { get; set; } = 1.0f;

    [DefaultValue(1.0f)]
    public float MinGamma { get; set; } = 1.0f;

    [DefaultValue(1.0f)]
    public float MaxGamma { get; set; } = 1.0f;

    [DefaultValue(0.0f)]
    public float HueOffset { get; set; } = 0.0f;

    [DefaultValue(1.0f)]
    public float MinSaturationGain { get; set; } = 1.0f;

    [DefaultValue(1.0f)]
    public float MaxSaturationGain { get; set; } = 1.0f;
}
public class NoiseParamsDto
{
    [DefaultValue(0.0f)]
    public float MinGaussianDeviation { get; set; } = 0.0f;

    [DefaultValue(0.0f)]
    public float MaxGaussianDeviation { get; set; } = 0.0f;

    [DefaultValue(0.0f)]
    public float MinSpeckleDeviation { get; set; } = 0.0f;

    [DefaultValue(0.0f)]
    public float MaxSpeckleDeviation { get; set; } = 0.0f;

    [DefaultValue(0.0f)]
    public float MinSaltPepperNoise { get; set; } = 0.0f;

    [DefaultValue(0.0f)]
    public float MaxSaltPepperNoise { get; set; } = 0.0f;
}

public class ClassifierParamsDto
{
    [DefaultValue(EClassifierCapacity.Normal)]
    public EClassifierCapacity ClassifierCapacity { get; set; } = EClassifierCapacity.Normal;

    [DefaultValue(512)]
    public uint ImageWidth { get; set; } = 512;

    [DefaultValue(512)]
    public uint ImageHeight { get; set; } = 512;

    [DefaultValue(0UL)]
    public ulong ImageCacheSize { get; set; } = 0;

    [DefaultValue(3)]
    public uint ImageChannels { get; set; } = 3;

    [DefaultValue(true)]
    public bool UsePretrainedModel { get; set; } = true;

    [DefaultValue(true)]
    public bool ComputeHeatMap { get; set; } = true;

    [DefaultValue(false)]
    public bool EnableHistogramEqualization { get; set; } = false;

    [DefaultValue(32)]
    public int BatchSize { get; set; } = 32;
}
