﻿using DeepLearningServer.Classes;
using DeepLearningServer.Enums;
using DeepLearningServer.Models;
using DeepLearningServer.Services;
using DeepLearningServer.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using AutoMapper;
using DeepLearningServer.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using DeepLearningServer.Attributes;
using Microsoft.Extensions.Configuration;

/// <summary>
/// 딥러닝 관련 API 요청을 처리하는 컨트롤러 클래스입니다.
/// </summary>
namespace DeepLearningServer.Controllers;

[Route("api/[controller]")]
[ApiController]

public class DeepLearningController(IOptions<ServerSettings> serverSettings,
    IMapper mapper, MssqlDbService mssqlDbService, IConfiguration configuration) : ControllerBase
{
    //private readonly MongoDbService _mongoDbService;
    private readonly ServerSettings _serverSettings = serverSettings.Value;
    private readonly MssqlDbService _mssqlDbService = mssqlDbService;
    private readonly IMapper _mapper = mapper;
    private readonly IConfiguration _configuration = configuration;
    private int _recordId = 0;

    /// <summary>
    /// 모델 훈련을 생성하고 실행합니다.
    /// </summary>
    /// <param name="parameterData">
    /// 훈련 매개변수 데이터:
    /// - AdmsProcessIds: 처리할 ADMS 프로세스 ID 목록 (최소 1개 이상 필요). 훈련 데이터로 사용할 프로세스들의 ID 리스트
    /// - ImageSize: 이미지 크기 (Middle(0) 또는 Large(1)만 지원). 훈련에 사용할 이미지 크기 설정
    /// - Categories: 분류할 카테고리 목록. 결함 유형 등 분류해야 할 클래스 이름들의 배열
    /// - IsDefaultModel: 기본 모델 여부. true인 경우 기본 모델명으로 저장됨
    /// - ClientModelDestination: 클라이언트 모델 저장 경로. 훈련된 모델이 클라이언트에 저장될 위치
    /// - TrainingProportion: 훈련 데이터 비율 (0~1 사이 값). 전체 데이터 중 훈련에 사용될 데이터 비율
    /// - ValidationProportion: 검증 데이터 비율 (0~1 사이 값). 전체 데이터 중 검증에 사용될 데이터 비율
    /// - TestProportion: 테스트 데이터 비율 (0~1 사이 값). 전체 데이터 중 테스트에 사용될 데이터 비율
    /// - Iterations: 훈련 반복 횟수. 기본값은 50회
    /// 
    /// - Geometry: 기하학적 데이터 증강 파라미터
    ///   - MaxRotation: 최대 회전 각도. 이미지 회전 증강에 사용됨 (0: 회전 없음)
    ///   - MaxVerticalShift: 최대 수직 이동 픽셀 수. 이미지 수직 이동 증강에 사용됨
    ///   - MaxHorizontalShift: 최대 수평 이동 픽셀 수. 이미지 수평 이동 증강에 사용됨
    ///   - MinScale: 최소 크기 비율. 이미지 크기 조정 시 최소 비율
    ///   - MaxScale: 최대 크기 비율. 이미지 크기 조정 시 최대 비율
    ///   - MaxVerticalShear: 최대 수직 전단 변형 비율. 이미지 수직 왜곡에 사용됨
    ///   - MaxHorizontalShear: 최대 수평 전단 변형 비율. 이미지 수평 왜곡에 사용됨
    ///   - VerticalFlip: 수직 뒤집기 사용 여부. true면 이미지를 상하 반전시켜 증강
    ///   - HorizontalFlip: 수평 뒤집기 사용 여부. true면 이미지를 좌우 반전시켜 증강
    /// 
    /// - Color: 색상 데이터 증강 파라미터
    ///   - MaxBrightnessOffset: 최대 밝기 오프셋. 이미지 밝기 조정에 사용됨
    ///   - MinContrastGain: 최소 대비 게인. 이미지 대비 조정 시 최소값
    ///   - MaxContrastGain: 최대 대비 게인. 이미지 대비 조정 시 최대값
    ///   - MinGamma: 최소 감마 값. 이미지 감마 조정 시 최소값
    ///   - MaxGamma: 최대 감마 값. 이미지 감마 조정 시 최대값
    ///   - HueOffset: 색조 오프셋. 이미지의 색조를 조정하는 값
    ///   - MinSaturationGain: 최소 채도 게인. 이미지 채도 조정 시 최소값
    ///   - MaxSaturationGain: 최대 채도 게인. 이미지 채도 조정 시 최대값
    /// 
    /// - Noise: 노이즈 데이터 증강 파라미터
    ///   - MinGaussianDeviation: 최소 가우시안 노이즈 표준편차. 가우시안 노이즈 추가 시 최소 강도
    ///   - MaxGaussianDeviation: 최대 가우시안 노이즈 표준편차. 가우시안 노이즈 추가 시 최대 강도
    ///   - MinSpeckleDeviation: 최소 스펙클 노이즈 표준편차. 스펙클 노이즈 추가 시 최소 강도
    ///   - MaxSpeckleDeviation: 최대 스펙클 노이즈 표준편차. 스펙클 노이즈 추가 시 최대 강도
    ///   - MinSaltPepperNoise: 최소 소금-후추 노이즈 비율. 소금-후추 노이즈 추가 시 최소 비율
    ///   - MaxSaltPepperNoise: 최대 소금-후추 노이즈 비율. 소금-후추 노이즈 추가 시 최대 비율
    /// 
    /// - Classifier: 분류기 설정 파라미터
    ///   - ClassifierCapacity: 분류기 용량 (Normal, Small, Large). 모델의 크기와 복잡도를 결정
    ///   - ImageWidth: 입력 이미지 너비 (픽셀). 모델 입력으로 사용될 이미지 너비
    ///   - ImageHeight: 입력 이미지 높이 (픽셀). 모델 입력으로 사용될 이미지 높이
    ///   - ImageCacheSize: 이미지 캐시 크기. 메모리에 캐시할 이미지 데이터의 크기
    ///   - ImageChannels: 이미지 채널 수. 일반적으로 3(RGB) 또는 1(그레이스케일)
    ///   - UsePretrainedModel: 사전 훈련된 모델 사용 여부. true면 기존 모델을 기반으로 추가 훈련
    ///   - ComputeHeatMap: 히트맵 계산 여부. 이미지에서 중요 영역을 시각화하는데 사용
    ///   - EnableHistogramEqualization: 히스토그램 평활화 사용 여부. 이미지 대비를 향상시키는데 사용
    ///   - BatchSize: 배치 크기. 한 번에 처리할 이미지 수로, 메모리 사용량과 학습 속도에 영향
    /// </param>
    /// <returns>훈련 초기화 성공 메시지와 훈련 ID</returns>
    [HttpPost("run")]
    [AuthorizeByPermission(PermissionType.RunModel)] // ✅ RunModel 권한을 가진 사용자만 접근 가능
    public async Task<IActionResult> CreateToolAndRun([FromBody] TrainingDto parameterData)
    {
        try
        {
            // 🔹 실행 중인지 확인
            if (ToolStatusManager.IsProcessRunning())
            {
                return BadRequest("The tool is already running.");
            }
            ToolStatusManager.SetProcessRunning(true);

            // 🔹 학습 중인지 확인
            //bool isRunning = await _mssqlDbService.CheckIsTraining();
            //if (isRunning)
            //{
            //    ToolStatusManager.SetProcessRunning(false);
            //    return BadRequest("The tool is already running.");
            //}

            await _mssqlDbService.InsertLogAsync("Create tool and run called", LogLevel.Information);
            Console.WriteLine($"AdmsProcessIds: {string.Join(",", parameterData.AdmsProcessIds)}");

            // ✅ `AdmsProcessIds`가 최소 하나 이상 있어야 함
            if (parameterData.AdmsProcessIds == null || parameterData.AdmsProcessIds.Count < 1)
            {
                ToolStatusManager.SetProcessRunning(false);
                return BadRequest(new NewRecord("At least one AdmsProcessId is required."));
            }
            TrainingRecord record = _mapper.Map<TrainingRecord>(parameterData);
            record.CreatedTime = DateTime.Now;
            record.Status = TrainingStatus.Running;
            record.StartTime = DateTime.Now;
            await _mssqlDbService.InsertTrainingAsync(record);
            _recordId = record.Id;
            Console.WriteLine("record inserted. Id: " + record.Id);
            // ✅ TrainingAdmsProcess와 TrainingRecord 연결 (기존 코드 수정 ✅)
            var trainingAdmsProcesses = parameterData.AdmsProcessIds
                .Select(id => new TrainingAdmsProcess
                {
                    TrainingRecordId = record.Id, // ✅ 저장된 TrainingRecordId 사용
                    AdmsProcessId = id
                }).ToList();

            _mssqlDbService.AddRangeTrainingAdmsProcess(trainingAdmsProcesses);
            // ✅ Singleton 체크
            if (SingletonAiDuo.GetInstance(parameterData.ImageSize) != null &&
                SingletonAiDuo.GetInstance(parameterData.ImageSize).IsTraining())
            {
                ToolStatusManager.SetProcessRunning(false);
                return BadRequest("The tool is already running.");
            }

            await _mssqlDbService.InsertLogAsync("Initializing instance", LogLevel.Debug);
            var instance = SingletonAiDuo.CreateInstance(parameterData, _serverSettings);
            await _mssqlDbService.InsertLogAsync("Initialized instance", LogLevel.Debug);

            // ✅ AdmsProcessIds에 해당하는 정보 가져오기
            List<Dictionary<string, object>> admsProcessInfoList = await _mssqlDbService.GetAdmsProcessInfos(parameterData.AdmsProcessIds);

            // ✅ processName 및 adms 조회
            var processNames = new List<string>();
            var admsList = new List<Adm>();

            foreach (var info in admsProcessInfoList)
            {
                string processName = "";
                if (info.TryGetValue("processId", out object value) && value is int intValue)
                {
                    processName = await _mssqlDbService.GetProcessNameById(intValue);
                }
                if (processName.Contains("Default"))
                {
                    Console.WriteLine($"Process {processName} is not valid.");
                    return BadRequest("Default process name should not be included.");
                }
                Console.WriteLine("Foud process name: " + processName);
                processNames.Add(processName);
                Adm adms;
                if (info.TryGetValue("admsId", out object admsIdValue) && admsIdValue is int admsIdIntValue)
                {
                    adms = await _mssqlDbService.GetAdmsById(admsIdIntValue);
                    admsList.Add(adms);
                }
            }

            // ✅ 모델 트레이닝 실행
            _ = Task.Run(async () =>
            {
                await RunOnStaThread(async () =>
                {
                    try
                    {
                        int numImages = 0;
                        TimeSpan elapsedTime = MeasureExecutionTime.Measure(() =>
                        {
                            numImages = instance.LoadImages(processNames.ToArray());
                        });

                        if (numImages == 0) return;

                        _mssqlDbService.InsertLogAsync($"Images loaded. Count: {numImages}", LogLevel.Debug).GetAwaiter().GetResult();
                        instance.SetParameters();
                        if (parameterData.Classifier.UsePretrainedModel)
                        {
                            record.HasPretrainedModel = instance.LoadPretrainedModel(parameterData.ImageSize);
                            if (!record.HasPretrainedModel)
                            {
                                throw new Exception("Failed to use pretrained model");
                            }
                            _mssqlDbService.UpdateTrainingAsync(record).GetAwaiter().GetResult();
                        }
                        instance.Train((isTraining, progress, bestIteration, currentAccuracy, bestAccuracy) =>
                        {
                            _mssqlDbService.InsertLogAsync($"progress: {progress}", LogLevel.Trace).GetAwaiter().GetResult();
                            var newEntry = new ProgressEntry
                            {
                                IsTraining = isTraining,
                                Progress = isTraining ? progress : 1,
                                BestIteration = bestIteration,
                                Timestamp = DateTime.Now,
                                Accuracy = currentAccuracy
                            };
                            record.Status = TrainingStatus.Running;
                            record.Progress = progress;
                            record.BestIteration = bestIteration;
                            record.Accuracy = bestAccuracy;
                            record.Loss = 1 - bestAccuracy;
                            _mssqlDbService.UpdateTrainingAsync(record).GetAwaiter().GetResult();
                            _mssqlDbService.PushProgressEntryAsync(record.Id, newEntry).GetAwaiter().GetResult();
                        }).GetAwaiter().GetResult();

                        // ✅ 여러 개의 프로세스에 대한 모델 저장
                        string timeStamp = DateTime.Now.ToString("yyyyMMdd");
                        foreach (var adms in admsList)
                        {
                            foreach (var processName in processNames)
                            {
                                //string savePath = $"{_serverSettings.ModelDirectory}\\{adms.Name}\\{processName}\\{timeStamp}\\";
                                string savePath = $"{_serverSettings.ModelDirectory}\\{adms.Name}\\{processName}\\";
                                string modelName;
                                if (parameterData.IsDefaultModel)
                                {
                                    if (parameterData.ImageSize == ImageSize.Middle)
                                    {
                                        modelName = "Default_Middle.edltool";
                                    }
                                    else if (parameterData.ImageSize == ImageSize.Large)
                                    {
                                        modelName = "Default_Large.edltool";
                                    }
                                    else
                                    {
                                        throw new Exception("Invalid image size. Only Middle or Large is supported");
                                    }
                                }
                                else
                                {
                                    modelName = $"{processName}.edltool";
                                }

                                if (!Directory.Exists(savePath))
                                {
                                    Directory.CreateDirectory(savePath);
                                }
                                string result = await instance.SaveModel(savePath + modelName, Path.Combine(parameterData.ClientModelDestination, modelName), adms.LocalIp);
                                var admsProcess = admsProcessInfoList.Find(admsProcessInfo => admsProcessInfo["admsId"].Equals(adms.Id) && admsProcessInfo["processName"].Equals(processName));
                                if (admsProcess == null)
                                {
                                    throw new Exception(modelName + " is not found in the admsProcessInfoList");
                                }
                                AdmsProcessType admsProcessType;
                                if (admsProcess.TryGetValue("admsProcessId", out object admsProcessId) && admsProcessId is int intAdmsProcessId)
                                {
                                    admsProcessType = await _mssqlDbService.GetAdmsProcessType(intAdmsProcessId);
                                    var modelRecord = new ModelRecord
                                    {
                                        ModelName = modelName,
                                        AdmsProcessTypeId = admsProcessType.Id,
                                        TrainingRecordId = record.Id,
                                        Status = result,
                                        ServerPath = savePath + modelName,
                                        ClientPath = Path.Combine(parameterData.ClientModelDestination, modelName),
                                        CreatedAt = DateTime.Now
                                    };
                                    await _mssqlDbService.InsertModelRecordAsync(modelRecord);
                                }
                            }
                        }

                        // Save confusion matrix data after training completes
                        if (parameterData.Categories != null && parameterData.Categories.Length > 0)
                        {
                            _mssqlDbService.InsertLogAsync("Saving confusion matrix data", LogLevel.Information).GetAwaiter().GetResult();

                            // Include OK label in the categories
                            var allCategories = new List<string>(parameterData.Categories) { "OK" };

                            // Generate and save confusion matrix data
                            foreach (string trueLabel in allCategories)
                            {
                                string trueLabelUpper = trueLabel.ToUpper();

                                foreach (string predictedLabel in allCategories)
                                {
                                    string predictedLabelUpper = predictedLabel.ToUpper();

                                    try
                                    {
                                        // Use the safe version of GetConfusion that won't throw exceptions
                                        uint count = instance.GetConfusionSafe(trueLabelUpper, predictedLabelUpper);

                                        // Only save if count is greater than 0 (optional)
                                        if (count > 0)
                                        {
                                            _mssqlDbService.SaveConfusionMatrixAsync(record.Id, trueLabelUpper, predictedLabelUpper, count)
                                                .GetAwaiter().GetResult();

                                            _mssqlDbService.InsertLogAsync(
                                                $"Saved confusion matrix: true={trueLabelUpper}, predicted={predictedLabelUpper}, count={count}",
                                                LogLevel.Debug).GetAwaiter().GetResult();
                                        }
                                        else
                                        {
                                            _mssqlDbService.InsertLogAsync(
                                                $"Skipped confusion matrix with zero count: true={trueLabelUpper}, predicted={predictedLabelUpper}",
                                                LogLevel.Debug).GetAwaiter().GetResult();
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        // Log the error but continue processing other categories
                                        _mssqlDbService.InsertLogAsync(
                                            $"Error getting confusion data for true={trueLabelUpper}, predicted={predictedLabelUpper}: {ex.Message}",
                                            LogLevel.Warning).GetAwaiter().GetResult();
                                    }
                                }
                            }

                            _mssqlDbService.InsertLogAsync("Confusion matrix data saved successfully", LogLevel.Information)
                                .GetAwaiter().GetResult();
                        }

                        record.Status = TrainingStatus.Completed;
                        record.EndTime = DateTime.Now;
                        record.Progress = 1;
                        _mssqlDbService.UpdateTrainingAsync(record).GetAwaiter().GetResult();

                        _mssqlDbService.InsertLogAsync("Model training finished", LogLevel.Information).GetAwaiter().GetResult();

                        Dictionary<string, float> trainingResults = instance.GetTrainingResult();

                        var labelList = trainingResults.Select(kvp => new Label
                        {
                            Name = kvp.Key,
                            Accuracy = kvp.Value,
                            TrainingRecordId = record.Id
                        }).ToArray();

                        _mssqlDbService.UpdateLabelsByIdAsync(record.Id, labelList).GetAwaiter().GetResult();
                        instance.StopTraining();
                        SingletonAiDuo.Reset(parameterData.ImageSize);
                        ToolStatusManager.SetProcessRunning(false);
                    }
                    catch (Exception e)
                    {
                        ToolStatusManager.SetProcessRunning(false);
                        Console.WriteLine("Error: " + e);
                        _mssqlDbService.InsertLogAsync($"Error occurred: {e.Message}", LogLevel.Error).GetAwaiter().GetResult();
                        _mssqlDbService.PartialUpdateTrainingAsync(record.Id, new Dictionary<string, object> { { "Status", TrainingStatus.Failed } }).GetAwaiter().GetResult();
                        instance.StopTraining();
                        SingletonAiDuo.Reset(parameterData.ImageSize);
                        throw;
                    }
                });
            });



            return Ok(new
            {
                Message = "Training initialized successfully.",
                TrainingId = record.Id.ToString()
            });
        }
        catch (Exception error)
        {
            ToolStatusManager.SetProcessRunning(false);
            Console.WriteLine("Error: ", error);
            Console.WriteLine("Error Message: ", error.Message);
            if (_recordId == 0)
            {
                throw;
            }
            else
            {
                _mssqlDbService.PartialUpdateTrainingAsync(_recordId, new Dictionary<string, object> { { "Status", TrainingStatus.Failed } }).GetAwaiter().GetResult();
                var instance = SingletonAiDuo.GetInstance(parameterData.ImageSize);
                if (instance != null)
                {
                    instance.StopTraining();
                }
                SingletonAiDuo.Reset(parameterData.ImageSize);
                Console.WriteLine(error.Message);
                _mssqlDbService.InsertLogAsync(error.Message, LogLevel.Error);
                throw;
            }

        }
    }

    /// <summary>
    /// 특정 이미지 크기의 훈련을 중지합니다.
    /// </summary>
    /// <param name="imageSize">
    /// 중지할 훈련의 이미지 크기:
    /// - Middle(0): 중간 크기 이미지
    /// - Large(1): 큰 크기 이미지
    /// </param>
    /// <returns>처리 완료 메시지</returns>
    [HttpDelete("stop/{imageSize}")]
    [AuthorizeByRole(UserRoleType.Operator, UserRoleType.Manager, UserRoleType.PROCEngineer, UserRoleType.ServiceEngineer)]
    public async Task<IActionResult> StopTraining([FromRoute] ImageSize imageSize)
    {
        try
        {
            await _mssqlDbService.InsertLogAsync("Stop training called", LogLevel.Information);
            var instance = SingletonAiDuo.GetInstance(imageSize);

            if (instance == null)
            {
                await _mssqlDbService.InsertLogAsync("Stop training error: Instance is null.", LogLevel.Error);
                return BadRequest(new NewRecord("Instance is null."));
            }

            instance.StopTraining();
            SingletonAiDuo.Reset(imageSize);
            ToolStatusManager.SetProcessRunning(false);
            await _mssqlDbService.InsertLogAsync("Training stopped", LogLevel.Debug);
            return Ok("Processing completed successfully.");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    /// <summary>
    /// 특정 이미지 크기의 훈련 결과를 가져옵니다.
    /// </summary>
    /// <param name="imageSize">
    /// 훈련 결과를 가져올 이미지 크기:
    /// - Middle(0): 중간 크기 이미지
    /// - Large(1): 큰 크기 이미지
    /// </param>
    /// <returns>레이블별 정확도를 포함한 훈련 결과 딕셔너리</returns>
    [HttpGet("result/{imageSize}")]
    public IActionResult GetTrainingResult([FromRoute] ImageSize imageSize)
    {
        var instance = SingletonAiDuo.GetInstance(imageSize);
        Dictionary<string, float> trainingResult = instance.GetTrainingResult();

        return Ok(trainingResult);
    }

    /// <summary>
    /// 특정 이미지 크기의 AI 인스턴스를 해제합니다.
    /// </summary>
    /// <param name="imageSize">
    /// 해제할 인스턴스의 이미지 크기:
    /// - Middle(0): 중간 크기 이미지
    /// - Large(1): 큰 크기 이미지
    /// </param>
    /// <returns>인스턴스 해제 성공 메시지</returns>
    [HttpDelete("dispose/{imageSize}")]
    public IActionResult DisposeInstance([FromRoute] ImageSize imageSize)
    {
        var instance = SingletonAiDuo.GetInstance(imageSize);
        if (instance != null)
        {
            instance.DisposeTool();
            SingletonAiDuo.Reset(imageSize);
        }

        return Ok("The tool disposed successfully");
    }

    /// <summary>
    /// 특정 이미지 크기, 실제 레이블, 예측 레이블에 대한 혼동 행렬 값을 가져옵니다.
    /// </summary>
    /// <param name="imageSize">
    /// 혼동 행렬을 가져올 이미지 크기:
    /// - Middle(0): 중간 크기 이미지
    /// - Large(1): 큰 크기 이미지
    /// </param>
    /// <param name="trueLabel">실제 레이블 (예: "OK", "NG" 등)</param>
    /// <param name="predictedLabel">예측 레이블 (예: "OK", "NG" 등)</param>
    /// <returns>해당 레이블 쌍에 대한 혼동 행렬 값 (개수)</returns>
    [HttpGet("confusion/{imageSize}/{trueLabel}/{predictedLabel}")]
    public async Task<IActionResult> GetConfusionMatrix([FromRoute] ImageSize imageSize, [FromRoute] string trueLabel, [FromRoute] string predictedLabel)
    {
        try
        {
            var instance = SingletonAiDuo.GetInstance(imageSize);
            if (instance == null)
            {
                return BadRequest("The tool is null");
            }

            // Use the safe version that won't throw exceptions
            var confusionMatrix = instance.GetConfusionSafe(trueLabel, predictedLabel);
            return Ok(confusionMatrix);
        }
        catch (Exception ex)
        {
            await _mssqlDbService.InsertLogAsync($"Error getting confusion data for true={trueLabel}, predicted={predictedLabel}: {ex.Message}", LogLevel.Warning);
            return StatusCode(500, $"Error getting confusion data: {ex.Message}");
        }
    }

    /// <summary>
    /// 특정 훈련 기록에 대한 모든 혼동 행렬 데이터를 저장합니다.
    /// </summary>
    /// <param name="trainingRecordId">혼동 행렬을 저장할 훈련 기록 ID</param>
    /// <param name="categories">분류 카테고리 배열 (OK는 자동으로 추가됨)</param>
    /// <returns>성공 및 오류 횟수를 포함한 처리 결과</returns>
    [HttpPost("saveConfusionMatrix/{trainingRecordId}")]
    [AuthorizeByRole(UserRoleType.Operator, UserRoleType.Manager, UserRoleType.PROCEngineer, UserRoleType.ServiceEngineer)]
    public async Task<IActionResult> SaveConfusionMatrix([FromRoute] int trainingRecordId, [FromBody] string[] categories)
    {
        try
        {
            await _mssqlDbService.InsertLogAsync("Saving confusion matrix for training record " + trainingRecordId, LogLevel.Information);

            var instance = SingletonAiDuo.GetInstance(_recordId == trainingRecordId ?
                await GetImageSizeFromTrainingRecord(trainingRecordId) :
                ImageSize.Middle); // Default to Middle if not current training

            if (instance == null)
            {
                return BadRequest("Training instance not available");
            }

            // Include OK label in the categories
            var allCategories = new List<string>(categories) { "OK" };
            int successCount = 0;
            int errorCount = 0;

            // Generate and save confusion matrix data
            foreach (string trueLabel in allCategories)
            {
                string trueLabelUpper = trueLabel.ToUpper();

                foreach (string predictedLabel in allCategories)
                {
                    string predictedLabelUpper = predictedLabel.ToUpper();

                    try
                    {
                        // Use the safe version of GetConfusion that won't throw exceptions
                        uint count = instance.GetConfusionSafe(trueLabelUpper, predictedLabelUpper);

                        // Only save if count is greater than 0 (optional)
                        if (count > 0)
                        {
                            await _mssqlDbService.SaveConfusionMatrixAsync(trainingRecordId, trueLabelUpper, predictedLabelUpper, count);

                            await _mssqlDbService.InsertLogAsync(
                                $"Saved confusion matrix: true={trueLabelUpper}, predicted={predictedLabelUpper}, count={count}",
                                LogLevel.Debug);
                            successCount++;
                        }
                        else
                        {
                            await _mssqlDbService.InsertLogAsync(
                                $"Skipped confusion matrix with zero count: true={trueLabelUpper}, predicted={predictedLabelUpper}",
                                LogLevel.Debug);
                            errorCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log the error but continue processing other categories
                        await _mssqlDbService.InsertLogAsync(
                            $"Error getting confusion data for true={trueLabelUpper}, predicted={predictedLabelUpper}: {ex.Message}",
                            LogLevel.Warning);
                        errorCount++;
                    }
                }
            }

            return Ok(new
            {
                Message = "Confusion matrix data processing completed",
                SuccessCount = successCount,
                ErrorCount = errorCount
            });
        }
        catch (Exception ex)
        {
            await _mssqlDbService.InsertLogAsync("Error saving confusion matrix: " + ex.Message, LogLevel.Error);
            return StatusCode(500, "Error saving confusion matrix: " + ex.Message);
        }
    }

    /// <summary>
    /// 특정 훈련 기록에 대한 모든 혼동 행렬 데이터를 가져옵니다.
    /// </summary>
    /// <param name="trainingRecordId">혼동 행렬을 가져올 훈련 기록 ID</param>
    /// <returns>해당 훈련 기록의 모든 혼동 행렬 데이터</returns>
    [HttpGet("getConfusionMatrix/{trainingRecordId}")]
    public async Task<IActionResult> GetConfusionMatrix([FromRoute] int trainingRecordId)
    {
        try
        {
            var matrices = await _mssqlDbService.GetConfusionMatricesAsync(trainingRecordId);
            return Ok(matrices);
        }
        catch (Exception ex)
        {
            await _mssqlDbService.InsertLogAsync("Error retrieving confusion matrix: " + ex.Message, LogLevel.Error);
            return StatusCode(500, "Error retrieving confusion matrix: " + ex.Message);
        }
    }

    [NonAction]
    private async Task<ImageSize> GetImageSizeFromTrainingRecord(int trainingRecordId)
    {
        try
        {
            // Use the existing mssqlDbService to fetch the training record
            var context = new DlServerContext(_mssqlDbService.GetDbContextOptions(), _configuration);
            var record = await context.TrainingRecords.FindAsync(trainingRecordId);
            return record != null ? (ImageSize)record.ImageSize : ImageSize.Middle;
        }
        catch (Exception ex)
        {
            await _mssqlDbService.InsertLogAsync($"Error getting image size: {ex.Message}", LogLevel.Error);
            return ImageSize.Middle; // Default to middle on error
        }
    }

    /// <summary>
    /// 특정 이미지 크기에 대한 모델을 로드합니다.
    /// </summary>
    /// <param name="imageSize">
    /// 모델을 로드할 이미지 크기:
    /// - Middle(0): 중간 크기 이미지
    /// - Large(1): 큰 크기 이미지
    /// </param>
    /// <param name="modelFilePath">모델 파일 경로</param>
    /// <param name="settingsFilePath">설정 파일 경로 (현재 사용되지 않음)</param>
    /// <returns>모델 로드 성공 메시지</returns>
    [HttpGet("load/{imageSize}")]
    public IActionResult LoadModel([FromRoute] ImageSize imageSize, [FromQuery] string modelFilePath, [FromQuery] string settingsFilePath)
    {
        try
        {
            var instance = SingletonAiDuo.GetInstance(imageSize);
            if (instance != null)
            {
                instance.LoadModel(modelFilePath);
                //instance.LoadSettings(settingsFilePath);
                return Ok("Ok");
            }
            else
            {
                return BadRequest("The tool is null");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return BadRequest(e.Message);
        }
    }
    [NonAction]
    public Task RunOnStaThread(Action action)
    {
        var tcs = new TaskCompletionSource<bool>();

        Thread staThread = new(() =>
        {
            try
            {
                action();
                tcs.SetResult(true);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        staThread.SetApartmentState(ApartmentState.STA);
        staThread.Start();

        return tcs.Task;
    }

}

internal record NewRecord(string Error);