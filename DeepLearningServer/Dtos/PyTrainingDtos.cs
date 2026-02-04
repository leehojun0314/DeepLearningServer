using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeepLearningServer.Dtos
{
  #region Enums

  /// <summary>
  /// Label mode for classification training
  /// </summary>
  public enum PyLabelMode
  {
    /// <summary>Binary OK/DEFECT classification based on mask pixels</summary>
    Binary,
    /// <summary>Category-based classification from meta JSON</summary>
    Category
  }

  /// <summary>
  /// ROI masking mode
  /// </summary>
  public enum PyRoiMode
  {
    /// <summary>No ROI masking</summary>
    None,
    /// <summary>Use segmentation mask for ROI</summary>
    Mask,
    /// <summary>Automatic wafer extraction heuristic</summary>
    Auto
  }

  /// <summary>
  /// Dataset sourcing mode
  /// </summary>
  public enum PyDatasetMode
  {
    /// <summary>Auto-detect dataset structure</summary>
    Auto,
    /// <summary>Force folder dataset (data/CATEGORY/*)</summary>
    Folders,
    /// <summary>Use pre-split train/val folders</summary>
    SplitFolders
  }

  #endregion

  #region Augmentation Parameters (Euresys-compatible)

  /// <summary>
  /// Geometry augmentation parameters (matches Euresys EDataAugmentation)
  /// </summary>
  public class PyGeometryParams
  {
    [JsonProperty("max_rotation")]
    public float MaxRotation { get; set; } = 180f;

    [JsonProperty("max_vertical_shift")]
    public float MaxVerticalShift { get; set; } = 0.1f;

    [JsonProperty("max_horizontal_shift")]
    public float MaxHorizontalShift { get; set; } = 0.1f;

    [JsonProperty("min_scale")]
    public float MinScale { get; set; } = 0.9f;

    [JsonProperty("max_scale")]
    public float MaxScale { get; set; } = 1.1f;

    [JsonProperty("max_vertical_shear")]
    public float MaxVerticalShear { get; set; } = 0f;

    [JsonProperty("max_horizontal_shear")]
    public float MaxHorizontalShear { get; set; } = 0f;

    [JsonProperty("vertical_flip")]
    public bool VerticalFlip { get; set; } = true;

    [JsonProperty("horizontal_flip")]
    public bool HorizontalFlip { get; set; } = true;
  }

  /// <summary>
  /// Color/luminosity augmentation parameters
  /// </summary>
  public class PyColorParams
  {
    [JsonProperty("max_brightness_offset")]
    public float MaxBrightnessOffset { get; set; } = 0.2f;

    [JsonProperty("max_contrast_gain")]
    public float MaxContrastGain { get; set; } = 1.2f;

    [JsonProperty("min_contrast_gain")]
    public float MinContrastGain { get; set; } = 0.8f;

    [JsonProperty("max_gamma")]
    public float MaxGamma { get; set; } = 1.2f;

    [JsonProperty("min_gamma")]
    public float MinGamma { get; set; } = 0.8f;

    [JsonProperty("hue_offset")]
    public float HueOffset { get; set; } = 0f;

    [JsonProperty("max_saturation_gain")]
    public float MaxSaturationGain { get; set; } = 1.0f;

    [JsonProperty("min_saturation_gain")]
    public float MinSaturationGain { get; set; } = 1.0f;
  }

  /// <summary>
  /// Noise augmentation parameters
  /// </summary>
  public class PyNoiseParams
  {
    [JsonProperty("max_gaussian_deviation")]
    public float MaxGaussianDeviation { get; set; } = 0.05f;

    [JsonProperty("min_gaussian_deviation")]
    public float MinGaussianDeviation { get; set; } = 0f;

    [JsonProperty("max_speckle_deviation")]
    public float MaxSpeckleDeviation { get; set; } = 0f;

    [JsonProperty("min_speckle_deviation")]
    public float MinSpeckleDeviation { get; set; } = 0f;

    [JsonProperty("max_salt_pepper_noise")]
    public float MaxSaltPepperNoise { get; set; } = 0f;

    [JsonProperty("min_salt_pepper_noise")]
    public float MinSaltPepperNoise { get; set; } = 0f;
  }

  #endregion

  #region Classifier Parameters

  /// <summary>
  /// Classifier-specific parameters (matches Euresys EClassifier properties)
  /// </summary>
  public class PyClassifierParams
  {
    [JsonProperty("image_width")]
    public int ImageWidth { get; set; } = 512;

    [JsonProperty("image_height")]
    public int ImageHeight { get; set; } = 512;

    [JsonProperty("image_channels")]
    public int ImageChannels { get; set; } = 3;

    [JsonProperty("batch_size")]
    public int BatchSize { get; set; } = 16;

    [JsonProperty("use_pretrained_model")]
    public bool UsePretrainedModel { get; set; } = true;

    [JsonProperty("compute_heat_map")]
    public bool ComputeHeatMap { get; set; } = false;

    [JsonProperty("enable_histogram_equalization")]
    public bool EnableHistogramEqualization { get; set; } = false;

    [JsonProperty("enable_deterministic_training")]
    public bool EnableDeterministicTraining { get; set; } = false;

    /// <summary>Classifier capacity (Euresys-specific, mapped to backbone choice)</summary>
    [JsonProperty("classifier_capacity")]
    public int ClassifierCapacity { get; set; } = 1;

    [JsonProperty("image_cache_size")]
    public int ImageCacheSize { get; set; } = 1024;
  }

  #endregion

  #region Training Parameters (Euresys-compatible wrapper)

  /// <summary>
  /// Training parameters compatible with Euresys TrainingDto structure
  /// </summary>
  public class PyTrainingParameters
  {
    public string[] Categories { get; set; } = Array.Empty<string>();
    public int Iterations { get; set; } = 20;
    public float TrainingProportion { get; set; } = 0.8f;
    public float ValidationProportion { get; set; } = 0.2f;
    public float TestProportion { get; set; } = 0f;
    public int EarlyStoppingPatience { get; set; } = 10;

    public PyGeometryParams Geometry { get; set; } = new PyGeometryParams();
    public PyColorParams Color { get; set; } = new PyColorParams();
    public PyNoiseParams Noise { get; set; } = new PyNoiseParams();
    public PyClassifierParams Classifier { get; set; } = new PyClassifierParams();

    /// <summary>
    /// Convert from TrainingDto to PyTrainingParameters
    /// </summary>
    public static PyTrainingParameters FromTrainingDto(TrainingDto dto)
    {
      return new PyTrainingParameters
      {
        Categories = dto.Categories,
        Iterations = dto.Iterations,
        TrainingProportion = dto.TrainingProportion,
        ValidationProportion = dto.ValidationProportion,
        TestProportion = dto.TestProportion,
        EarlyStoppingPatience = dto.EarlyStoppingPatience,
        Geometry = new PyGeometryParams
        {
          MaxRotation = dto.Geometry.MaxRotation,
          MaxVerticalShift = dto.Geometry.MaxVerticalShift,
          MaxHorizontalShift = dto.Geometry.MaxHorizontalShift,
          MinScale = dto.Geometry.MinScale,
          MaxScale = dto.Geometry.MaxScale,
          MaxVerticalShear = dto.Geometry.MaxVerticalShear,
          MaxHorizontalShear = dto.Geometry.MaxHorizontalShear,
          VerticalFlip = dto.Geometry.VerticalFlip,
          HorizontalFlip = dto.Geometry.HorizontalFlip
        },
        Color = new PyColorParams
        {
          MaxBrightnessOffset = dto.Color.MaxBrightnessOffset,
          MaxContrastGain = dto.Color.MaxContrastGain,
          MinContrastGain = dto.Color.MinContrastGain,
          MaxGamma = dto.Color.MaxGamma,
          MinGamma = dto.Color.MinGamma,
          HueOffset = dto.Color.HueOffset,
          MaxSaturationGain = dto.Color.MaxSaturationGain,
          MinSaturationGain = dto.Color.MinSaturationGain
        },
        Noise = new PyNoiseParams
        {
          MaxGaussianDeviation = dto.Noise.MaxGaussianDeviation,
          MinGaussianDeviation = dto.Noise.MinGaussianDeviation,
          MaxSpeckleDeviation = dto.Noise.MaxSpeckleDeviation,
          MinSpeckleDeviation = dto.Noise.MinSpeckleDeviation,
          MaxSaltPepperNoise = dto.Noise.MaxSaltPepperNoise,
          MinSaltPepperNoise = dto.Noise.MinSaltPepperNoise
        },
        Classifier = new PyClassifierParams
        {
          ImageWidth = (int)dto.Classifier.ImageWidth,
          ImageHeight = (int)dto.Classifier.ImageHeight,
          ImageChannels = (int)dto.Classifier.ImageChannels,
          BatchSize = dto.Classifier.BatchSize,
          UsePretrainedModel = dto.Classifier.UsePretrainedModel,
          ComputeHeatMap = dto.Classifier.ComputeHeatMap,
          EnableHistogramEqualization = dto.Classifier.EnableHistogramEqualization,
          EnableDeterministicTraining = dto.Classifier.EnableDeterministicTraining,
          ClassifierCapacity = (int)dto.Classifier.ClassifierCapacity,
          ImageCacheSize = (int)dto.Classifier.ImageCacheSize
        }
      };
    }
  }

  #endregion

  #region HTTP API Request/Response DTOs

  /// <summary>
  /// Category-to-path mapping for explicit dataset sources
  /// </summary>
  public class PyCategorySource
  {
    /// <summary>Classification label (e.g., "OK", "SCRATCH")</summary>
    [JsonProperty("label")]
    public string Label { get; set; } = string.Empty;

    /// <summary>Image directory paths for this label</summary>
    [JsonProperty("paths")]
    public List<string> Paths { get; set; } = new();
  }

  /// <summary>
  /// Classification training request (maps to Python CLSTrainRequest)
  /// </summary>
  public class PyClsTrainRequest
  {
    /// <summary>Dataset root folder</summary>
    [JsonProperty("data")]
    public string Data { get; set; } = string.Empty;

    /// <summary>Output directory for checkpoints</summary>
    [JsonProperty("out")]
    public string Out { get; set; } = string.Empty;

    /// <summary>Explicit category-to-path mapping for training</summary>
    [JsonProperty("category_sources")]
    public List<PyCategorySource>? CategorySources { get; set; }

    /// <summary>Input image size (square)</summary>
    [JsonProperty("img_size")]
    public int ImgSize { get; set; } = 512;

    /// <summary>Number of training epochs</summary>
    [JsonProperty("epochs")]
    public int Epochs { get; set; } = 20;

    /// <summary>Batch size</summary>
    [JsonProperty("batch_size")]
    public int BatchSize { get; set; } = 16;

    /// <summary>DataLoader workers</summary>
    [JsonProperty("workers")]
    public int Workers { get; set; } = 4;

    /// <summary>timm backbone name</summary>
    [JsonProperty("backbone")]
    public string Backbone { get; set; } = "tf_efficientnet_b0";

    /// <summary>Learning rate</summary>
    [JsonProperty("lr")]
    public float Lr { get; set; } = 3e-4f;

    /// <summary>Wafer class ID in segmentation mask</summary>
    [JsonProperty("wafer_id")]
    public int WaferId { get; set; } = -1;

    /// <summary>Minimum defect pixels to classify as DEFECT</summary>
    [JsonProperty("min_defect_px")]
    public int MinDefectPx { get; set; } = 32;

    /// <summary>Validation split ratio</summary>
    [JsonProperty("val_split")]
    public float ValSplit { get; set; } = 0.2f;

    /// <summary>Minimum validation samples</summary>
    [JsonProperty("val_min")]
    public int ValMin { get; set; } = 32;

    /// <summary>Apply ROI masking</summary>
    [JsonProperty("use_roi")]
    public bool UseRoi { get; set; } = false;

    /// <summary>ROI mode: none, mask, auto</summary>
    [JsonProperty("roi_mode")]
    public string RoiModeStr { get; set; } = "none";

    /// <summary>Label mode: binary, category</summary>
    [JsonProperty("label_mode")]
    public string LabelModeStr { get; set; } = "category";

    /// <summary>Dataset mode: auto, folders, split_folders</summary>
    [JsonProperty("dataset_mode")]
    public string DatasetModeStr { get; set; } = "auto";

    /// <summary>Enable class-balanced sampling</summary>
    [JsonProperty("balance_sampler")]
    public bool BalanceSampler { get; set; } = true;

    /// <summary>Enable stronger augmentation for minority classes</summary>
    [JsonProperty("balance_aug")]
    public bool BalanceAug { get; set; } = true;

    /// <summary>Checkpoint path to resume from</summary>
    [JsonProperty("resume_from")]
    public string? ResumeFrom { get; set; }

    /// <summary>Total epochs target (for resume)</summary>
    [JsonProperty("total_epochs")]
    public int? TotalEpochs { get; set; }

    /// <summary>Early stopping patience (0 = disabled)</summary>
    [JsonProperty("patience")]
    public int Patience { get; set; } = 0;

    /// <summary>Minimum accuracy delta for improvement</summary>
    [JsonProperty("min_delta")]
    public float MinDelta { get; set; } = 0f;

    /// <summary>Add FFT magnitude channel</summary>
    [JsonProperty("add_fft")]
    public bool AddFft { get; set; } = false;

    /// <summary>Use K-gray input instead of RGB</summary>
    [JsonProperty("gray_input")]
    public bool GrayInput { get; set; } = false;

    /// <summary>Geometry augmentation parameters</summary>
    [JsonProperty("geom_aug")]
    public PyGeometryParams? GeomAug { get; set; }

    /// <summary>Color augmentation parameters</summary>
    [JsonProperty("color_aug")]
    public PyColorParams? ColorAug { get; set; }

    /// <summary>Noise augmentation parameters</summary>
    [JsonProperty("noise_aug")]
    public PyNoiseParams? NoiseAug { get; set; }

    /// <summary>Use stronger augmentation variant</summary>
    [JsonProperty("strong_aug")]
    public bool StrongAug { get; set; } = false;

    /// <summary>
    /// Create from Euresys-compatible PyTrainingParameters
    /// </summary>
    public static PyClsTrainRequest FromTrainingParameters(
        PyTrainingParameters param,
        string dataPath,
        string outPath)
    {
      return new PyClsTrainRequest
      {
        Data = dataPath,
        Out = outPath,
        ImgSize = param.Classifier.ImageWidth,
        Epochs = param.Iterations,
        BatchSize = param.Classifier.BatchSize,
        ValSplit = param.ValidationProportion,
        Patience = param.EarlyStoppingPatience,
        GeomAug = param.Geometry,
        ColorAug = param.Color,
        NoiseAug = param.Noise,
      };
    }
  }

  /// <summary>
  /// Training status response from /train/cls/status
  /// </summary>
  public class PyTrainStatusResponse
  {
    /// <summary>Whether training is currently running</summary>
    [JsonProperty("running")]
    public bool Running { get; set; }

    /// <summary>Progress ratio (0.0 to 1.0)</summary>
    [JsonProperty("progress")]
    public float Progress { get; set; }

    /// <summary>Current epoch number</summary>
    [JsonProperty("current_epoch")]
    public int CurrentEpoch { get; set; }

    /// <summary>Total epochs to train</summary>
    [JsonProperty("total_epochs")]
    public int TotalEpochs { get; set; }

    /// <summary>Current epoch accuracy</summary>
    [JsonProperty("current_accuracy")]
    public float CurrentAccuracy { get; set; }

    /// <summary>Best accuracy achieved so far</summary>
    [JsonProperty("best_accuracy")]
    public float BestAccuracy { get; set; }

    /// <summary>Current training loss</summary>
    [JsonProperty("train_loss")]
    public float TrainLoss { get; set; }

    /// <summary>Current validation loss</summary>
    [JsonProperty("val_loss")]
    public float ValLoss { get; set; }

    /// <summary>Path to best model checkpoint</summary>
    [JsonProperty("best_model")]
    public string? BestModel { get; set; }

    /// <summary>Last update timestamp</summary>
    [JsonProperty("last_update")]
    public string? LastUpdate { get; set; }
  }

  /// <summary>
  /// Training start response
  /// </summary>
  public class PyTrainStartResponse
  {
    [JsonProperty("result")]
    public string Result { get; set; } = string.Empty;
  }

  /// <summary>
  /// Training stop response
  /// </summary>
  public class PyTrainStopResponse
  {
    [JsonProperty("result")]
    public string Result { get; set; } = string.Empty;

    [JsonProperty("status")]
    public string? Status { get; set; }

    [JsonProperty("best_model")]
    public string? BestModel { get; set; }

    [JsonProperty("out_dir")]
    public string? OutDir { get; set; }

    [JsonProperty("last_lines")]
    public string? LastLines { get; set; }
  }

  /// <summary>
  /// ONNL pack export request
  /// </summary>
  public class PyOnnlPackRequest
  {
    [JsonProperty("weights")]
    public string Weights { get; set; } = string.Empty;

    [JsonProperty("out_path")]
    public string? OutPath { get; set; }

    [JsonProperty("opset")]
    public int Opset { get; set; } = 13;

    [JsonProperty("seal")]
    public bool Seal { get; set; } = false;

    [JsonProperty("aud")]
    public string? Aud { get; set; }
  }

  /// <summary>
  /// ONNL pack export response
  /// </summary>
  public class PyOnnlPackResponse
  {
    [JsonProperty("path")]
    public string? Path { get; set; }

    [JsonProperty("status")]
    public string? Status { get; set; }

    [JsonProperty("error")]
    public string? Error { get; set; }
  }

  /// <summary>
  /// Training log response
  /// </summary>
  public class PyTrainLogResponse
  {
    [JsonProperty("log")]
    public string Log { get; set; } = string.Empty;
  }

  #endregion

  #region Classification Result (for inference)

  /// <summary>
  /// Classification result (matches existing ClassificationResult in TrainingAI.cs)
  /// </summary>
  public class PyClassificationResult
  {
    public string? BestLabel { get; set; }
    public float BestScore { get; set; }
    public Dictionary<string, float>? AllScores { get; set; }
  }

  #endregion

  #region Confusion Matrix Response

  /// <summary>
  /// Single confusion count response
  /// </summary>
  public class PyConfusionResponse
  {
    [JsonProperty("count")]
    public int Count { get; set; }

    [JsonProperty("error")]
    public string? Error { get; set; }
  }

  /// <summary>
  /// Full confusion matrix response
  /// </summary>
  public class PyConfusionMatrixResponse
  {
    [JsonProperty("confusion")]
    public Dictionary<string, Dictionary<string, int>>? Confusion { get; set; }

    [JsonProperty("matrix")]
    public List<List<int>>? Matrix { get; set; }

    [JsonProperty("classes")]
    public List<string>? Classes { get; set; }

    [JsonProperty("error")]
    public string? Error { get; set; }
  }

  /// <summary>
  /// Confusion matrix result for client usage
  /// </summary>
  public class PyConfusionMatrixResult
  {
    public Dictionary<string, Dictionary<string, int>> Confusion { get; set; } = new();
    public List<string> Classes { get; set; } = new();
  }

  #endregion

  #region Classify Response

  /// <summary>
  /// Single image classification response from /infer/cls/single
  /// </summary>
  public class PyClassifyResponse
  {
    [JsonProperty("bestLabel")]
    public string? BestLabel { get; set; }

    [JsonProperty("bestScore")]
    public float BestScore { get; set; }

    [JsonProperty("allScores")]
    public Dictionary<string, float>? AllScores { get; set; }

    [JsonProperty("error")]
    public string? Error { get; set; }
  }

  #endregion
}
