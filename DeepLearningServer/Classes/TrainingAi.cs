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
    public delegate Task TrainCallback(bool isTraining, float progress, int bestIteration, float currentAccuracy, float bestAccuracy
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

    private string? tempImageRootDir; // 임시 이미지 루트 경로
    private string? tempImageSessionDir; // 세션별 임시 폴더 경로

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
    public int LoadImages(string[] processNames)
    {
        if (parameterData == null) throw new Exception("Parameter data is null");
        // 임시 폴더 경로 준비
        tempImageRootDir = serverSettings.TempImageDirectory;
        string today = DateTime.Now.ToString("yyyyMMdd");
        tempImageSessionDir = Path.Combine(tempImageRootDir, today, Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempImageSessionDir);

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
                // 임시 카테고리 폴더 생성
                string tempCategoryDir = Path.Combine(tempImageSessionDir, upperCategory);
                Directory.CreateDirectory(tempCategoryDir);
                // NG/BASE 폴더 체크 및 복사
                string ngBasePath = imagePath + $@"\NG\BASE\{upperCategory}";
                if (Directory.Exists(ngBasePath))
                {
                    var ngBaseFiles = Directory.GetFiles(ngBasePath, "*.jpg", SearchOption.AllDirectories);
                    foreach (var file in ngBaseFiles)
                    {
                        string dest = Path.Combine(tempCategoryDir, Path.GetFileName(file));
                        File.Copy(file, dest, true);
                    }
                }
                // NG/NEW 폴더 체크 및 복사
                string ngNewPath = imagePath + $@"\NG\NEW\{upperCategory}";
                if (Directory.Exists(ngNewPath))
                {
                    var ngNewFiles = Directory.GetFiles(ngNewPath, "*.jpg", SearchOption.AllDirectories);
                    foreach (var file in ngNewFiles)
                    {
                        string dest = Path.Combine(tempCategoryDir, Path.GetFileName(file));
                        File.Copy(file, dest, true);
                    }
                }
            }
        }
        // NG 이미지 데이터셋 추가
        int totalImages = 0;
        int okImageCount = 0;
        if (categories != null)
        {
            foreach (var category in categories)
            {
                var upperCategory = category.ToUpper();
                string tempCategoryDir = Path.Combine(tempImageSessionDir, upperCategory);
                if (Directory.Exists(tempCategoryDir))
                {
                    var files = Directory.GetFiles(tempCategoryDir, "*.jpg", SearchOption.AllDirectories);
                    if (files.Length > 0)
                    {
                        dataset.AddImages(Path.Combine(tempCategoryDir, "*.jpg"), upperCategory);
                        totalImages += files.Length;
                        Console.WriteLine($"NG 카테고리 '{upperCategory}' 이미지 추가: {files.Length}개");
                    }
                    else
                    {
                        Console.WriteLine($"NG 카테고리 '{upperCategory}' 이미지 없음");
                    }
                }
                else
                {
                    Console.WriteLine($"NG 카테고리 '{upperCategory}' 디렉토리 없음: {tempCategoryDir}");
                }
            }
        }

        // OK 이미지 복사 및 로드
        Console.WriteLine($"OK 이미지 로딩 시작. 프로세스 이름들: [{string.Join(", ", processNames)}]");
        foreach (var processName in processNames)
        {
            // OK 이미지 복사 먼저 수행 - 올바른 경로 순서로 수정
            string okBasePath = imagePath + $@"\OK\{processName}\BASE";
            string okNewPath = imagePath + $@"\OK\{processName}\NEW";
            string tempOkProcessDir = Path.Combine(tempImageSessionDir, "OK", processName);
            
            Console.WriteLine($"프로세스 '{processName}' OK 이미지 처리 중...");
            Console.WriteLine($"  - BASE 경로: {okBasePath}");
            Console.WriteLine($"  - NEW 경로: {okNewPath}");
            Console.WriteLine($"  - 임시 디렉토리: {tempOkProcessDir}");
            
            Directory.CreateDirectory(tempOkProcessDir);
            int processOkCount = 0;

            // OK/BASE 폴더 체크 및 복사
            if (Directory.Exists(okBasePath))
            {
                var okBaseFiles = Directory.GetFiles(okBasePath, "*.jpg", SearchOption.AllDirectories);
                Console.WriteLine($"  - BASE에서 찾은 OK 이미지: {okBaseFiles.Length}개");
                foreach (var file in okBaseFiles)
                {
                    string dest = Path.Combine(tempOkProcessDir, Path.GetFileName(file));
                    File.Copy(file, dest, true);
                    processOkCount++;
                }
            }
            else
            {
                Console.WriteLine($"  - BASE 디렉토리 없음: {okBasePath}");
            }

            // OK/NEW 폴더 체크 및 복사
            if (Directory.Exists(okNewPath))
            {
                var okNewFiles = Directory.GetFiles(okNewPath, "*.jpg", SearchOption.AllDirectories);
                Console.WriteLine($"  - NEW에서 찾은 OK 이미지: {okNewFiles.Length}개");
                foreach (var file in okNewFiles)
                {
                    string dest = Path.Combine(tempOkProcessDir, Path.GetFileName(file));
                    File.Copy(file, dest, true);
                    processOkCount++;
                }
            }
            else
            {
                Console.WriteLine($"  - NEW 디렉토리 없음: {okNewPath}");
            }

            // 데이터셋에 추가
            if (Directory.Exists(tempOkProcessDir))
            {
                var files = Directory.GetFiles(tempOkProcessDir, "*.jpg", SearchOption.AllDirectories);
                if (files.Length > 0)
                {
                    dataset.AddImages(Path.Combine(tempOkProcessDir, "*.jpg"), "OK");
                    totalImages += files.Length;
                    okImageCount += files.Length;
                    Console.WriteLine($"  - 프로세스 '{processName}' OK 이미지 데이터셋 추가: {files.Length}개");
                }
                else
                {
                    Console.WriteLine($"  - 프로세스 '{processName}' OK 이미지 없음 (복사 후에도)");
                }
            }
        }
        Console.WriteLine($"총 OK 이미지 수: {okImageCount}개");
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
        return totalImages;
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
    public bool LoadPretrainedModel(ImageSize size)
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
        float bestAccuracySeenSoFar = -1; // 지금까지 본 최고 정확도
        int noImprovementCount = 0; // 개선되지 않은 연속 횟수
        float improvementThreshold = serverSettings.EarlyStoppingThreshold; // 개선으로 간주할 최소 임계값 (설정에서 가져옴)
        int patienceLimit = serverSettings.EarlyStoppingPatience; // 얼리 스타핑 patience (설정에서 가져옴)
        
        Console.WriteLine($"Early Stopping 설정 - Patience: {patienceLimit}, Threshold: {improvementThreshold}");
        
        while (true)
        {
            int completion = classifier.WaitForIterationCompletion();
            Console.WriteLine("completion: " + completion);

            float bestAccuracy = classifier.GetTrainingMetrics(classifier.BestIteration).Accuracy;
            Console.WriteLine("Best Accuracy: " + bestAccuracy);
            
            // 현재 iteration의 정확도 가져오기 (iteration 0부터 시작)
            float currentAccuracy = classifier.GetTrainingMetrics(iteration).Accuracy;
            Console.WriteLine("Current Accuracy: " + currentAccuracy);
            Console.WriteLine("Best Accuracy So Far: " + bestAccuracySeenSoFar);
            
            cb(classifier.IsTraining(), classifier.CurrentTrainingProgression, classifier.BestIteration, currentAccuracy, bestAccuracy
                ).GetAwaiter().GetResult();

            // 얼리 스타핑 로직 - 최고 정확도 기준으로 개선 여부 판단
            if (currentAccuracy > bestAccuracySeenSoFar + improvementThreshold)
            {
                // 유의미한 개선이 있었음
                bestAccuracySeenSoFar = currentAccuracy;
                noImprovementCount = 0;
                Console.WriteLine($"Accuracy improved! New best: {bestAccuracySeenSoFar:F4}");
            }
            else
            {
                // 개선되지 않음
                noImprovementCount++;
                Console.WriteLine($"No improvement for {noImprovementCount} iterations (current: {currentAccuracy:F4}, best: {bestAccuracySeenSoFar:F4})");
                
                if (noImprovementCount >= patienceLimit)
                {
                    Console.WriteLine($"Early stopping: No improvement for {patienceLimit} consecutive iterations. Stopping training...");
                    classifier.StopTraining(true);
                    break;
                }
            }
            
            iteration++;
            
            if (classifier.IsTraining() == false)
            {
                Console.WriteLine("Training completed normally.");
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
        catch (Exception error)
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

            try
            {
                float labelAccuracy = metrics.GetLabelAccuracy(upperCategory);
                float labelError = metrics.GetLabelError(upperCategory);

                Console.WriteLine($"label currentAccuracy: {labelAccuracy}");
                dictionary.Add(category.ToLower() + "Accuracy", labelAccuracy);
                dictionary.Add(category.ToLower() + "Error", labelError);

                foreach (string predictedCategory in categories)
                {
                    string upperPredictedCategory = predictedCategory.ToUpper();
                    try
                    {
                        uint confusionResult = metrics.GetConfusion(upperCategory, upperCategory);
                        Console.WriteLine($"Get confusion result: {category}, {confusionResult}");
                    }
                    catch (Exception confusionError)
                    {
                        Console.WriteLine($"Could not get confusion data for {category}: {confusionError.Message}");
                    }
                }
            }
            catch (Exception error)
            {
                // 해당 레이블이 존재하지 않는 경우 (이미지가 없어서 훈련에 사용되지 않음)
                Console.WriteLine($"Label '{upperCategory}' not found in training results (no images for this category): {error.Message}");
                dictionary.Add(category.ToLower() + "Accuracy", 0.0f);
                dictionary.Add(category.ToLower() + "Error", 1.0f);
            }
        }

        return dictionary;
    }
    public uint GetConfusion(string trueClass, string predictedClass)
    {
        if (classifier == null) throw new Exception("The classifier is null");
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
                foreach (string part in parts)
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

    /// <summary>
    /// 임시 이미지 폴더를 삭제합니다.
    /// </summary>
    public void CleanupTempImages()
    {
        if (!string.IsNullOrEmpty(tempImageSessionDir) && Directory.Exists(tempImageSessionDir))
        {
            try
            {
                Directory.Delete(tempImageSessionDir, true);
                Console.WriteLine($"임시 이미지 폴더 삭제 완료: {tempImageSessionDir}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"임시 이미지 폴더 삭제 실패: {ex.Message}");
            }
        }
    }

    ~TrainingAi()
    {
        DisposeTool();
    }
}