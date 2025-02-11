using DeepLearningServer.Enums;
using DeepLearningServer.Models;
using DeepLearningServer.Settings;
using Euresys.Open_eVision;
using Euresys.Open_eVision.EasyDeepLearning;
using Euresys.Open_eVision.LicenseFeatures;
using MongoDB.Bson;
using NuGet.Protocol;

namespace DeepLearningServer.Classes;

public class TrainingAi
{
    public delegate void TrainCallback(bool isTraining, float progress, int bestIteration,
        float[] learningRateParameters);

    private readonly ServerSettings serverSettings;

    // EDeepLearningTool tool;
    private EClassifier? classifier;
    private EDataAugmentation? dataAug;
    private EClassificationDataset? dataset;
    private readonly CreateAndRunModel? parameterData;

    private readonly string? processId;
    public ObjectId recordId;
    private EClassificationDataset? testDataset;
    private EClassificationDataset? trainingDataset;
    private EClassificationDataset? tvDataset;
    private EClassificationDataset? validationDataset;
    private readonly string[]? categories;

    #region Initialize
    public TrainingAi(CreateAndRunModel parameterData, ServerSettings serverSettings)
    {

        Console.WriteLine("Checking license..");
        try
        {
            bool hasLicense = Euresys.Open_eVision.Easy.CheckLicense(Features.EasyClassify);
            Console.WriteLine($"Has license: {hasLicense}");
            if (!hasLicense) throw new Exception("No license found");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error on checking license: {e.Message}");
        }

        this.serverSettings = serverSettings;
        this.parameterData = parameterData;
        classifier = new EClassifier();
        dataset = new EClassificationDataset();
        tvDataset = new EClassificationDataset();
        trainingDataset = new EClassificationDataset();
        validationDataset = new EClassificationDataset();
        testDataset = new EClassificationDataset();
        classifier.EnableGPU = true;
        categories = parameterData.Categories;
        processId = parameterData.ProcessId;

        Console.WriteLine($"Number of GPUs: {classifier.NumGPUs}");
    }

    #endregion

    #region Load images

    public int LoadImages()
    {
        if (parameterData == null) throw new Exception("Parameter data is null");
        var imagePath = parameterData.ImageSize switch
        {
            ImageSize.Medium => serverSettings.MiddleImagePath,
            ImageSize.Large => serverSettings.LargeImagePath,
            _ => throw new Exception($"Error on loading images. Invalid size. {parameterData.ImageSize}"),
        };
        if (dataset == null) throw new Exception("Dataset is null");

        Console.WriteLine($"Loading images from {imagePath}");
        if (categories != null)
        {
            foreach (var category in categories)
            {
                var upperCategory = category.ToUpper();
                Console.WriteLine("upper category: " + upperCategory);
                dataset.AddImages(imagePath + $@"\NG\BASE\{upperCategory}\*.jpg", upperCategory);
                // dataset.AddImages("D:\\Images\\DL_SERVER TEST\\20250107\\02_8AE05B-L-0\\J96972.1\\*.jpg", upperCategory);
                dataset.AddImages(imagePath + $@"\NG\NEW\{upperCategory}\*.jpg");
            }
        }

        //Load base OK images (processed images)
        dataset.AddImages(imagePath + $@"\OK\{processId}\BASE\*.jpg", "OK");
        //Load new OK images (processed images)
        dataset.AddImages(imagePath + $@"\OK\{processId}\NEW\*.jpg", "OK");
        var firstProportion = parameterData.TrainingProportion + parameterData.ValidationProportion;
        dataset.SplitDataset(tvDataset, testDataset, firstProportion);
        var secondProportion = parameterData.TrainingProportion /
                                 (parameterData.TrainingProportion + parameterData.ValidationProportion);
        tvDataset?.SplitDataset(trainingDataset, validationDataset, secondProportion);
        Console.WriteLine("Num labels: " + dataset.NumLabels);
        Console.WriteLine($"num images: {dataset.NumImages}");
        if (dataset.NumImages < 1)
        {
            throw new Exception("Error on loading images. Images not found");
        }
        else
        {
            return dataset.NumImages;
        }
    }

    #endregion

    #region Set parameters

    public void SetParameters()
    {
        dataAug = new EDataAugmentation();
        if (parameterData == null) return;
        //Geometry
        dataAug.MaxRotationAngle = parameterData.Geometry.MaxRotation;
        dataAug.MaxVerticalShift = parameterData.Geometry.MaxVerticalShift;
        dataAug.MaxHorizontalShift = parameterData.Geometry.MaxVerticalShift;
        dataAug.MinScale = parameterData.Geometry.MinScale;
        dataAug.MaxScale = parameterData.Geometry.MaxScale;
        dataAug.MaxVerticalShear = parameterData.Geometry.MaxVerticalShear;
        dataAug.MaxHorizontalShear = parameterData.Geometry.MaxHorizontalShear;
        dataAug.EnableVerticalFlip = parameterData.Geometry.VerticalFlip;
        dataAug.EnableHorizontalFlip = parameterData.Geometry.HorizontalFlip;

        //Color/Luminosity
        dataAug.MaxBrightnessOffset = parameterData.Color.MaxBrightnessOffset;
        dataAug.MaxContrastGain = parameterData.Color.MaxContrastGain;
        dataAug.MinContrastGain = parameterData.Color.MinContrastGain;
        dataAug.MaxGamma = parameterData.Color.MaxGamma;
        dataAug.MinGamma = parameterData.Color.MinGamma;
        dataAug.MaxHueOffset = parameterData.Color.HueOffset;
        dataAug.MaxSaturationGain = parameterData.Color.MaxSaturationGain;
        dataAug.MinSaturationGain = parameterData.Color.MinSaturationGain;

        //Noise
        dataAug.GaussianNoiseMaximumStandardDeviation = parameterData.Noise.MaxGaussianDeviation;
        dataAug.GaussianNoiseMinimumStandardDeviation = parameterData.Noise.MinGaussianDeviation;
        dataAug.SpeckleNoiseMaximumStandardDeviation = parameterData.Noise.MaxSpeckleDeviation;
        dataAug.SpeckleNoiseMinimumStandardDeviation = parameterData.Noise.MinSpeckleDeviation;
        dataAug.SaltAndPepperNoiseMaximumDensity = parameterData.Noise.MaxSaltPepperNoise;
        dataAug.SaltAndPepperNoiseMinimumDensity = parameterData.Noise.MinSaltPepperNoise;

        //Classifier properties
        if (classifier == null) return;
        classifier.Capacity = parameterData.Classifier.ClassifierCapacity;
        classifier.ImageCacheSize = parameterData.Classifier.ImageCacheSize;
        classifier.Width = parameterData.Classifier.ImageWidth;
        classifier.Height = parameterData.Classifier.ImageHeight;
        classifier.ImageCacheSize = parameterData.Classifier.ImageCacheSize;
        classifier.Channels = parameterData.Classifier.ImageChannels;
        classifier.UsePretrainedModel = parameterData.Classifier.UsePretrainedModel;
        classifier.ComputeHeatmapWithResult = parameterData.Classifier.ComputeHeatMap;
        classifier.EnableHistogramEqualization = parameterData.Classifier.EnableHistogramEqualization;
        classifier.BatchSize = parameterData.Classifier.BatchSize;
    }

    #endregion

    #region Train

    public Task Train(TrainCallback cb)
    {
        if (classifier == null) throw new Exception("Classifier is null");
        Console.WriteLine("Started training");
        var activeDevice = classifier.GetActiveDevice();
        Console.WriteLine($"active device name: {activeDevice.Name} /n type: {activeDevice.DeviceType}");
        classifier.Train(trainingDataset, validationDataset, dataAug, parameterData?.Iterations ?? 3);
        while (true)
        {
            classifier.WaitForIterationCompletion();
            cb(classifier.IsTraining(), classifier.CurrentTrainingProgression, classifier.BestIteration,
                classifier.LearningRateParameters);
            if (classifier.IsTraining() == false) break;
        }
        // classifier.WaitForTrainingCompletion();

        return Task.CompletedTask;
    }

    #endregion
    public EClassificationResult[] Classify(string[] imagePaths)
    {
        if(imagePaths != null && imagePaths.Length > 0 && classifier != null && classifier.HasTrainingModel())
        {
            EBaseROI[] images = new EBaseROI[imagePaths.Length];

            for (int i = 0; i < imagePaths.Length; i++)
            {
                EImageBW8 eImage = new EImageBW8();
                eImage.Load(imagePaths[i]); // 로드
                images[i] = eImage;         // EBaseROI 배열에 저장
            }

            return classifier.Classify(ref images);
        }
        else
        {
            throw new Exception("Invalid image list");
        }
    }
    public void StopTraining()
    {
        if (classifier != null)
        {
            var result = classifier.StopTraining(true);
            Console.WriteLine($"Stop training result: {result}");
        }
        else
        {
            throw new Exception("No classifier was trained");
        }
    }

    public Dictionary<string, float> GetStatus()
    {
        if (classifier != null)
        {
            var learningRateParameters = classifier.LearningRateParameters;
            Console.WriteLine($"Learning rate: {learningRateParameters}");
            Console.WriteLine($"LearningRateParameters Length: {learningRateParameters.Length}");

            Dictionary<string, float> dictionary = new();
            // 배열 길이 확인
            for (var i = 0; i < learningRateParameters.Length; i++)
            {
                Console.WriteLine($"Learning rate parameter {i}: {learningRateParameters[i]}");
                dictionary.Add($"learningRateParameters{i}", learningRateParameters[i]);
            }

            return dictionary;
        }
        else
        {
            throw new Exception("No classifier was trained");
        }
    }

    public Dictionary<string, float> GetTrainingResult()
    {
        Console.WriteLine("Get training result called");
        if (classifier == null) throw new Exception("The classifier is null");
        Dictionary<string, float> dictionary = new();
        var metrics = classifier.GetTrainingMetrics(classifier.BestIteration);
        var weightedAccuracy = metrics.GetWeightedAccuracy(validationDataset);
        var weightedError = metrics.GetWeightedError(validationDataset);
        Console.WriteLine($"weighted accuracy: {weightedAccuracy}");
        Console.WriteLine($"weighted error: {weightedError}");
        metrics.GetLabelAccuracy("BURST");
        // var okAccuracy = metrics.GetLabelAccuracy("OK");
        // var okError = metrics.GetLabelError("OK");
        var metricsJson = JsonExtensions.ToJson(metrics);

        Console.WriteLine($"Metrics json: {metricsJson}");

        Console.WriteLine("labeled accuracies finished");
        dictionary.Add("weightedAccuracy", metrics.GetWeightedAccuracy(validationDataset));
        Console.WriteLine("flag 1");
        dictionary.Add("weightedError", metrics.GetWeightedError(validationDataset));
        Console.WriteLine("flag 2");

        dictionary.Add("okAccuracy", metrics.GetLabelAccuracy("OK"));
        Console.WriteLine("flag 3");
        dictionary.Add("okError", metrics.GetLabelError("OK"));
        Console.WriteLine("flag 4");
        if (categories == null) return dictionary;
        foreach (string category in categories)
        {
            string upperCategory = category.ToUpper();
            Console.WriteLine($"balanced accuracy: {metrics.BalancedAccuracy}");
            Console.WriteLine($"label accuracy: {metrics.GetLabelAccuracy(upperCategory)}");
            dictionary.Add(category.ToLower() + "Accuracy", metrics.GetLabelAccuracy(upperCategory));
            dictionary.Add(category.ToLower() + "Error", metrics.GetLabelError(upperCategory));
        }

        return dictionary;
    }


    public void DisposeTool()
    {
        classifier?.Dispose();
        dataset?.Dispose();
        tvDataset?.Dispose();
        trainingDataset?.Dispose();
        validationDataset?.Dispose();
        testDataset?.Dispose();
        dataAug?.Dispose();

        classifier = null;
        dataset = null;
        tvDataset = null;
        trainingDataset = null;
        validationDataset = null;
        validationDataset = null;
        testDataset = null;
        dataAug = null;
    }

    public void SaveModel(string filePath)
    {
        classifier?.SaveTrainingModel(filePath);
    }

    public void SaveSettings(string filePath)
    {
        classifier?.SaveSettings(filePath);
    }

    public void LoadModel(string filePath)
    {
        classifier?.LoadTrainingModel(filePath);
    }

    public void LoadSettings(string filePath)
    {
        classifier?.LoadSettings(filePath);
    }

    ~TrainingAi()
    {
        DisposeTool();
    }
}