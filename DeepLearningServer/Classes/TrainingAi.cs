using DeepLearningServer.Dtos;
using DeepLearningServer.Enums;
using DeepLearningServer.Settings;
using Euresys.Open_eVision;
using Euresys.Open_eVision.EasyDeepLearning;
using Euresys.Open_eVision.LicenseFeatures;
using NuGet.Protocol;
using System.Net.Http.Headers;

namespace DeepLearningServer.Classes;

public class TrainingAi
{
    public delegate void TrainCallback(bool isTraining, float progress, int bestIteration
        );

    private readonly ServerSettings serverSettings;

    // EDeepLearningTool tool;
    private EClassifier? classifier;
    private EDataAugmentation? dataAug;
    private EClassificationDataset? dataset;
    private readonly CreateAndRunModel? parameterData;

    //private readonly string? processId;
    public int recordId;
    //private EClassificationDataset? testDataset;
    //private EClassificationDataset? trainingDataset;
    //private EClassificationDataset? tvDataset;
    //private EClassificationDataset? validationDataset;
    private readonly string[]? categories;

    #region Initialize
    public TrainingAi(CreateAndRunModel parameterData, ServerSettings serverSettings)
    {

       

        this.serverSettings = serverSettings;
        this.parameterData = parameterData;
        classifier = new EClassifier();
        dataset = new EClassificationDataset();
        //tvDataset = new EClassificationDataset();
        //trainingDataset = new EClassificationDataset();
        //validationDataset = new EClassificationDataset();
        //testDataset = new EClassificationDataset();
        classifier.EnableGPU = true;
        categories = parameterData.Categories;

        //processId = parameterData.ProcessId;
        Console.WriteLine($"Number of GPUs: {classifier.NumGPUs}");
    }

    #endregion


    #region Load images

    public int LoadImages(string processId)
    {
        if (parameterData == null) throw new Exception("Parameter data is null");
        var imagePath = parameterData.ImageSize switch
        {
            ImageSize.Middle => serverSettings.MiddleImagePath,
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
        //var firstProportion = parameterData.TrainingProportion + parameterData.ValidationProportion;
        //dataset.SplitDataset(tvDataset, testDataset, firstProportion);
        //var secondProportion = parameterData.TrainingProportion /
                                 //(parameterData.TrainingProportion + parameterData.ValidationProportion);
        //tvDataset?.SplitDataset(trainingDataset, validationDataset, secondProportion);
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
    public int LoadImages(string[] processNames)
    {
        if (parameterData == null) throw new Exception("Parameter data is null");
        var imagePath = parameterData.ImageSize switch
        {
            ImageSize.Middle => serverSettings.MiddleImagePath,
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
        foreach (var processName in processNames)
        {
            dataset.AddImages(imagePath + $@"\OK\{processName}\BASE\*.jpg", "OK");
            //Load new OK images (processed images)
            dataset.AddImages(imagePath + $@"\OK\{processName}\NEW\*.jpg", "OK");
        }
        //var firstProportion = parameterData.TrainingProportion + parameterData.ValidationProportion;
        //dataset.SplitDataset(tvDataset, testDataset, firstProportion);
        //var secondProportion = parameterData.TrainingProportion /
        //(parameterData.TrainingProportion + parameterData.ValidationProportion);
        //tvDataset?.SplitDataset(trainingDataset, validationDataset, secondProportion);
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
        classifier.Train(dataset, dataAug, parameterData?.Iterations ?? 3);
        while (true)
        {
            classifier.WaitForIterationCompletion();
            cb(classifier.IsTraining(), classifier.CurrentTrainingProgression, classifier.BestIteration
                );

            if (classifier.IsTraining() == false){
                break; }
        }
        // classifier.WaitForTrainingCompletion();

        return Task.CompletedTask;
    }

    #endregion
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

            Dictionary<string, float> dictionary = new Dictionary<string, float>();
            dictionary.Add("isTraining", classifier.IsTraining() ? 1 : 0);
            dictionary.Add("bestIteration", classifier.BestIteration);
            dictionary.Add("currentTrainingProgression", classifier.CurrentTrainingProgression);
            dictionary.Add("currentTrainingNumIterations", classifier.CurrentTrainingNumIterations);
            Dictionary<string, float> resultDictionary = GetTrainingResult();

            // 배열 길이 확인
            for (var i = 0; i < learningRateParameters.Length; i++)
            {
                Console.WriteLine($"Learning rate parameter {i}: {learningRateParameters[i]}");
                dictionary.Add($"learningRateParameters{i}", learningRateParameters[i]);
            }
            foreach (var kvp in resultDictionary)
            {
                if (dictionary.ContainsKey(kvp.Key))
                    dictionary[kvp.Key] += kvp.Value; // 기존 값에 더하기
                else
                    dictionary[kvp.Key] = kvp.Value;  // 새로운 키 추가
            }

            return dictionary;
        }
        else
        {
            throw new Exception("No classifier was trained");
        }
    }
    public bool IsTraining()
    {
        return classifier?.IsTraining() ?? false;
    }
    public Dictionary<string, float> GetTrainingResult()
    {
        Console.WriteLine("Get training result called");
        if (classifier == null) throw new Exception("The classifier is null");
        Dictionary<string, float> dictionary = new();
        var metrics = classifier.GetTrainingMetrics(classifier.BestIteration);
        var weightedAccuracy = metrics.GetWeightedAccuracy(dataset);
        var weightedError = metrics.GetWeightedError(dataset);
        Console.WriteLine($"weighted accuracy: {weightedAccuracy}");
        Console.WriteLine($"weighted error: {weightedError}");
        metrics.GetLabelAccuracy("BURST");
        // var okAccuracy = metrics.GetLabelAccuracy("OK");
        // var okError = metrics.GetLabelError("OK");
        var metricsJson = JsonExtensions.ToJson(metrics);

        Console.WriteLine($"Metrics json: {metricsJson}");

        Console.WriteLine("labeled accuracies finished");
        dictionary.Add("weightedAccuracy", metrics.GetWeightedAccuracy(dataset));
        Console.WriteLine("flag 1");
        dictionary.Add("weightedError", metrics.GetWeightedError(dataset));
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
        //tvDataset?.Dispose();
        //trainingDataset?.Dispose();
        //validationDataset?.Dispose();
        //testDataset?.Dispose();
        dataAug?.Dispose();

        classifier = null;
        dataset = null;
        //tvDataset = null;
        //trainingDataset = null;
        //validationDataset = null;
        //validationDataset = null;
        //testDataset = null;
        dataAug = null;
    }

    public async Task SaveModel(string filePath, string clientIpAddress, ImageSize imageSize)
    {
        try
        {
            // 디렉토리 경로 추출
            string? directoryPath = Path.GetDirectoryName(filePath);

            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                // 디렉토리 생성
                Directory.CreateDirectory(directoryPath);

            }

            // 모델 저장
            classifier?.SaveTrainingModel(filePath);
            Console.WriteLine("file path: " + filePath);
            using (var client = new HttpClient())
            {
                using (var form = new MultipartFormDataContent())
                {
                    byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
                    var fileContent = new ByteArrayContent(fileBytes);
                    fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");

                    // 🔹 파일 추가
                    form.Add(fileContent, "File", Path.GetFileName(filePath));

                    // 🔹 ModelPath 추가
                    //form.Add(new StringContent("D:/"+ Path.GetFileName(filePath)), "ModelPath");
                    form.Add(new StringContent(Path.GetFileName(filePath)), "ModelPath");
                    form.Add(new StringContent(imageSize.ToString()), "ImageSize");
                    Console.WriteLine($"form.ToString(): {form.ToString()}");
                    Console.WriteLine("client ip address: " + clientIpAddress);
                    // 🔹 API 엔드포인트
                    string apiUrl = $"http://{clientIpAddress}/api/model/upload";

                    // 🔹 요청 전송
                    HttpResponseMessage response = await client.PostAsync(apiUrl, form);

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("모델 업로드 성공: " + response.Content.ReadAsStringAsync().Result);
                    }
                    else
                    {
                        Console.WriteLine("모델 업로드 실패: " + response.StatusCode);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"모델 저장 중 오류 발생: {ex.Message} {ex.ToString()}");
            throw new Exception($"모델 저장 중 오류 발생: {ex.ToString()}");
        }
    }

    //public void SaveSettings(string filePath)
    //{
    //    classifier?.SaveSettings(filePath);
    //}

    public void LoadModel(string filePath)
    {
        classifier?.LoadTrainingModel(filePath);
    }

    //public void LoadSettings(string filePath)
    //{
    //    classifier?.LoadSettings(filePath);
    //}

    ~TrainingAi()
    {
        DisposeTool();
    }
}