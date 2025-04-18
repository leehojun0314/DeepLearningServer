﻿using DeepLearningServer.Dtos;
using DeepLearningServer.Enums;
using DeepLearningServer.Settings;
using Euresys.Open_eVision;
using Euresys.Open_eVision.EasyDeepLearning;
using Euresys.Open_eVision.LicenseFeatures;
using NuGet.Protocol;
using SharpCompress.Common;
using System.Net.Http.Headers;

namespace DeepLearningServer.Classes;

public class TrainingAi
{
    public delegate void TrainCallback(bool isTraining, float progress, int bestIteration, float currentAccuracy, float bestAccuracy
        );

    private readonly ServerSettings serverSettings;

    // EDeepLearningTool tool;
    private EClassifier? classifier;
    private EDataAugmentation? dataAug;
    private EClassificationDataset? dataset;
    private readonly TrainingDto? parameterData;

    //private readonly string? processId;
    public int recordId;
    private EClassificationDataset? testDataset;
    private EClassificationDataset? trainingDataset;
    private EClassificationDataset? tvDataset;
    private EClassificationDataset? validationDataset;
    private readonly string[]? categories;

    #region Initialize
    public TrainingAi(TrainingDto parameterData, ServerSettings serverSettings)
    {



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

    }

    #endregion


    #region Load images

    /*public int LoadImages(string processId)
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
    }*/
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
            string basePath = imagePath + $@"\OK\{processName}\BASE\*.jpg";
            string newPath = imagePath + $@"\OK\{processName}\NEW\*.jpg";
            Console.WriteLine($"Base path: {basePath}");
            Console.WriteLine($"New path: {newPath}");
            dataset.AddImages(basePath, "OK");
            //Load new OK images (processed images)
            dataset.AddImages(newPath, "OK");
        }
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
        return dataset.NumImages;
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
    public bool LoadPretrainedModel( ImageSize size)
    {
        if (classifier == null)
        {
            throw new Exception("Classifier is null");
        }
        classifier.UsePretrainedModel = true;
        Console.WriteLine("Pretrained model loaded");
        return classifier.HasPretrainedModel();

    }
    #region Train

    public Task Train(TrainCallback cb)
    {
        if (classifier == null) throw new Exception("Classifier is null");
        Console.WriteLine("Started training");
        var activeDevice = classifier.GetActiveDevice();

        Console.WriteLine($"active device name: {activeDevice.Name} /n type: {activeDevice.DeviceType}");
        if (parameterData.TrainingProportion == 1)
        {
            classifier.Train(dataset, dataAug, parameterData?.Iterations ?? 3);
        }
        else
        {
            classifier.Train(trainingDataset, validationDataset, dataAug, parameterData?.Iterations ?? 3);
        }
        int iteration = 0;
        float prevAccuracy = -1; // 이전 Accuracy 저장 변수
        int sameAccuracyCount = 0; // 이전 Accuracy와 같은 Accuracy가 나온 횟수
        while (true)
        {
            //iteration++;
            int completion = classifier.WaitForIterationCompletion();
            Console.WriteLine("completion: " + completion);

            float bestAccuracy = classifier.GetTrainingMetrics(classifier.BestIteration).Accuracy;
            Console.WriteLine("Best Accuracy: " + bestAccuracy);
            float currentAccuracy = classifier.GetTrainingMetrics(iteration).Accuracy;
            Console.WriteLine("Current Accuracy: " + currentAccuracy);

            cb(classifier.IsTraining(), classifier.CurrentTrainingProgression, classifier.BestIteration, currentAccuracy, bestAccuracy
                );
            // 동일한 Accuracy가 10번 반복되었으면 트레이닝 강제 종료
            if (currentAccuracy == prevAccuracy)
            {
                sameAccuracyCount++;
                if (sameAccuracyCount >= 10)
                {
                    Console.WriteLine("Accuracy hasn't improved for 10 iterations. Stopping training...");
                    classifier.StopTraining(false);
                    break;
                }
            }
            else
            {
                sameAccuracyCount = 0; // Accuracy가 바뀌었으면 카운트 초기화
            }
            iteration++;
            if (classifier.IsTraining() == false)
            {
                break;
            }
            
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

    public bool IsTraining()
    {
        return classifier?.IsTraining() ?? false;
    }
    //public float GetAccuracy()
    //{
    //    if (classifier == null) throw new Exception("The classifier is null");
    //    var metrics = classifier.GetTrainingMetrics(classifier.BestIteration);
        
    //    return metrics.Accuracy;
    //}
    public Dictionary<string, float> GetTrainingResult()
    {
        Console.WriteLine("Get training result called");
        if (classifier == null) throw new Exception("The classifier is null");
        Dictionary<string, float> dictionary = new();
        var metrics = classifier.GetTrainingMetrics(classifier.BestIteration);
        var weightedAccuracy = metrics.GetWeightedAccuracy(dataset);
        var weightedError = metrics.GetWeightedError(dataset);
        Console.WriteLine($"weighted currentAccuracy: {weightedAccuracy}");
        Console.WriteLine($"weighted error: {weightedError}");
        // var okAccuracy = metrics.GetLabelAccuracy("OK");
        // var okError = metrics.GetLabelError("OK");
        var metricsJson = JsonExtensions.ToJson(metrics);

        Console.WriteLine($"Metrics json: {metricsJson}");

        Console.WriteLine("labeled accuracies finished");
        dictionary.Add("weightedAccuracy", metrics.GetWeightedAccuracy(dataset));
        Console.WriteLine("flag 1");
        dictionary.Add("weightedError", metrics.GetWeightedError(dataset));
        Console.WriteLine("flag 2");
        try
        {
            dictionary.Add("okAccuracy", metrics.GetLabelAccuracy("OK"));
            dictionary.Add("okError", metrics.GetLabelError("OK"));

        }
        catch(Exception error)
        {
            Console.WriteLine($"Error getting ok accuracy: {error.Message}");
        }

        Console.WriteLine("flag 3");
        
        Console.WriteLine("flag 4");
        if (categories == null) return dictionary;
        foreach (string category in categories)
        {
            string upperCategory = category.ToUpper();
            Console.WriteLine($"balanced currentAccuracy: {metrics.BalancedAccuracy}");
            Console.WriteLine($"label currentAccuracy: {metrics.GetLabelAccuracy(upperCategory)}");
            dictionary.Add(category.ToLower() + "Accuracy", metrics.GetLabelAccuracy(upperCategory));
            dictionary.Add(category.ToLower() + "Error", metrics.GetLabelError(upperCategory));
            foreach(string predictedCategory in categories)
            {
                string upperPredictedCategory = predictedCategory.ToUpper();
                uint confusionResult = metrics.GetConfusion(upperCategory, upperCategory);
                Console.WriteLine($"Get confusion result: {category}, {confusionResult}");

            }
        }

        return dictionary;
    }
    public uint GetConfusion(string trueClass, string predictedClass)
    {
        if(classifier == null) throw new Exception("The classifier is null");
        var metrics = classifier.GetTrainingMetrics(classifier.BestIteration);

        return metrics.GetConfusion(trueClass, predictedClass);
    }
    
    // Add a safe version of GetConfusion that returns 0 instead of throwing an exception
    public uint GetConfusionSafe(string trueClass, string predictedClass)
    {
        try
        {
            if (classifier == null) return 0;
            var metrics = classifier.GetTrainingMetrics(classifier.BestIteration);
            
            return metrics.GetConfusion(trueClass, predictedClass);
        }
        catch (Exception)
        {
            // Return 0 counts for any missing categories instead of throwing an exception
            return 0;
        }
    }
    public void GetImageProbability(string imagePath)
    {
        if (classifier == null) throw new Exception("The classifier is null");
        var metrics = classifier.GetTrainingMetrics(classifier.BestIteration);
        
        //metrics.
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
    public async Task<string> SaveModel(string localPath, string remotePath, string clientIpAddress)
    {
        try
        {
            string? directoryPath = Path.GetDirectoryName(localPath);
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                // 디렉토리 생성
                Directory.CreateDirectory(directoryPath);

            }

            // 모델 저장
            classifier?.Save(localPath, true);

            using (var client = new HttpClient())
            {
                using (var form = new MultipartFormDataContent())
                {
                    byte[] fileBytes = await File.ReadAllBytesAsync(localPath);
                    var fileContent = new ByteArrayContent(fileBytes);
                    fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
                    string fileName = Path.GetFileName(localPath);
                    // 🔹 파일 추가
                    form.Add(fileContent, "File", fileName);

                    // 🔹 ModelPath 추가
                    //form.Add(new StringContent("D:/"+ Path.GetFileName(filePath)), "ModelPath");
                    Console.WriteLine($"Remote path: {remotePath}");
                    form.Add(new StringContent(remotePath), "ModelPath");
                    //Console.WriteLine($"form.ToString(): {form.ToString()}");
                    Console.WriteLine("client ip address: " + clientIpAddress);
                    // 🔹 API 엔드포인트
                    string apiUrl = $"http://{clientIpAddress}/api/model/upload";

                    // 🔹 요청 전송
                    HttpResponseMessage response = await client.PostAsync(apiUrl, form);

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("모델 업로드 성공: " + response.Content.ReadAsStringAsync().Result);
                        return "saved";
                    }
                    else
                    {
                        Console.WriteLine("모델 업로드 실패: " + response.StatusCode);
                        return "pending";
                    }
                }
            }
        }
        catch (Exception error)
        {
            Console.WriteLine($"모델 저장 중 오류 발생: {error.Message} {error.ToString()}");
            return "error";
            //throw new Exception($"모델 저장 중 오류 발생: {error.ToString()}");
        }
    }

    public async Task<string> SaveModel2(string localPath, string remotePath, string clientIpAddress)
    {
        try
        {
            Console.WriteLine("Received local path: " + localPath);
            string directoryPath = Path.GetDirectoryName(localPath);
            Console.WriteLine("Directory Path: " + directoryPath);
            string tempPath = "D:\\ModelUpgradeProject\\project";
            if (!string.IsNullOrEmpty(tempPath) || !Directory.Exists(tempPath))
            {
                Directory.CreateDirectory(tempPath);
            }
            Console.WriteLine($"temp path: {tempPath}");
            Console.WriteLine("temp path2: " + tempPath);
            if (!string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
           

            if (Directory.Exists(tempPath))
            {
                // 기존 프로젝트 폴더 삭제
                Directory.Delete(tempPath, true);
            }
            Console.WriteLine("Creating project...");
            EDeepLearningProject project = new EDeepLearningProject();
            project.Type = EDeepLearningToolType.EasyClassify;
            project.Name = "modelSave";
            project.ProjectDirectory = tempPath;
            Console.WriteLine("Saving project...");
            project.SaveProject();
            Console.WriteLine("Saved project.");

            try
            {
                Console.WriteLine("Importing tool...");
                project.ImportTool("Tool0", localPath);
                Console.WriteLine("Updating project file structure...");
                project.UpdateProjectFileStructure();

                string modifiedPath = "";
                string[] parts = localPath.Split("\\");
                foreach(string part in parts)
                {
                    Console.WriteLine("part: " + part);
                    modifiedPath += part + "\\";
                }

                EDeepLearningTool newTool = project.GetToolCopy(0);
                Console.WriteLine("Saving model...");
                newTool.SaveTrainingModel(localPath);
                Console.WriteLine("Model saved");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Model save failed: {localPath}, Error: {ex.Message}");
                return "error";
            }

            using (var client = new HttpClient())
            {
                using (var form = new MultipartFormDataContent())
                {
                    byte[] fileBytes = await File.ReadAllBytesAsync(localPath);
                    var fileContent = new ByteArrayContent(fileBytes);
                    fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
                    string fileName = Path.GetFileName(localPath);
                    form.Add(fileContent, "File", fileName);
                    form.Add(new StringContent(remotePath), "ModelPath");
                    Console.WriteLine($"Remote path: {remotePath}");
                    Console.WriteLine("Client IP address: " + clientIpAddress);
                    string apiUrl = $"http://{clientIpAddress}/api/model/upload";
                    HttpResponseMessage response = await client.PostAsync(apiUrl, form);

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Model upload success: " + response.Content.ReadAsStringAsync().Result);
                        return "saved";
                    }
                    else
                    {
                        Console.WriteLine("Model upload failed: " + response.StatusCode);
                        return "pending";
                    }
                }
            }
        }
        catch (Exception error)
        {
            Console.WriteLine($"Error saving model: {error.Message} {error.ToString()}");
            return "error";
        }
    }



    public void LoadModel(string filePath)
    {
        classifier?.LoadTrainingModel(filePath);
    }

    ~TrainingAi()
    {
        DisposeTool();
    }
}