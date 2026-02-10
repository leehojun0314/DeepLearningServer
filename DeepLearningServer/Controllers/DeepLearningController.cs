using DeepLearningServer.Classes;
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
using System.Net.Http;
using System.Net.Http.Headers;

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
    /// - EarlyStoppingPatience: 조기 중단 파라미터. 기본값은 10회
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
    ///   - AddFft: FFT 채널 추가 여부. true이면 기본 채널에 FFT magnitude 채널을 1개 추가
    ///   - GrayInput: 흑백 입력 사용 여부. true이면 RGB 대신 K-gray 기반 채널 사용
    ///   - UsePretrainedModel: 사전 훈련된 모델 사용 여부. true면 기존 모델을 기반으로 추가 훈련
    ///   - ComputeHeatMap: 히트맵 계산 여부. 이미지에서 중요 영역을 시각화하는데 사용
    ///   - EnableHistogramEqualization: 히스토그램 평활화 사용 여부. 이미지 대비를 향상시키는데 사용
    ///   - BatchSize: 배치 크기. 한 번에 처리할 이미지 수로, 메모리 사용량과 학습 속도에 영향
    ///   - EnableDeterministicTraining: 결정적 훈련 사용 여부. true로 설정하면 동일한 결과를 보장하는 훈련을 수행
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

            bool usePythonServer = _serverSettings.UsePythonServer;
            await _mssqlDbService.InsertLogAsync(
                $"Training backend selected: {(usePythonServer ? "PythonServer" : "Euresys")}",
                LogLevel.Information);

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

            parameterData.Classifier.ImageChannels = ResolveRequestedChannels(
                parameterData.Classifier.ImageChannels,
                parameterData.Classifier.GrayInput,
                parameterData.Classifier.AddFft);

            TrainingRecord record = _mapper.Map<TrainingRecord>(parameterData);
            record.CreatedTime = DateTime.Now; // 요청을 받자마자 기록
            record.Status = TrainingStatus.Loading; // 이미지 로딩 단계로 시작
            record.StartTime = null; // 실제 훈련 시작 전까지는 null로 유지
            await _mssqlDbService.InsertTrainingAsync(record);
            _recordId = record.Id;
            Console.WriteLine("record inserted. Id: " + record.Id);
            // ✅ TrainingAdmsProcess와 TrainingRecord 연결 (중복 제거 및 await 추가)
            var trainingAdmsProcesses = parameterData.AdmsProcessIds
                .Distinct() // 중복 AdmsProcessId 제거
                .Select(id => new TrainingAdmsProcess
                {
                    TrainingRecordId = record.Id, // ✅ 저장된 TrainingRecordId 사용
                    AdmsProcessId = id
                }).ToList();

            await _mssqlDbService.AddRangeTrainingAdmsProcess(trainingAdmsProcesses); // await 추가
            // ✅ Singleton 체크
            if (SingletonAiDuo.GetInstance(parameterData.ImageSize) != null &&
                SingletonAiDuo.GetInstance(parameterData.ImageSize).IsTraining())
            {
                ToolStatusManager.SetProcessRunning(false);
                return BadRequest("The tool is already running.");
            }

            TrainingAi? instance = null;
            if (!usePythonServer)
            {
                await _mssqlDbService.InsertLogAsync("Initializing instance", LogLevel.Debug);
                instance = SingletonAiDuo.CreateInstance(parameterData, _serverSettings);
                await _mssqlDbService.InsertLogAsync("Initialized instance", LogLevel.Debug);
            }
            else
            {
                await _mssqlDbService.InsertLogAsync("Python training enabled: skipping Euresys init", LogLevel.Debug);
            }

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
                if (usePythonServer)
                {
                    TrainingAiHttpBridge? bridge = null;
                    try
                    {
                        bridge = new TrainingAiHttpBridge(_serverSettings.PyTrainingServerUrl);
                        TrainingAiHttpBridge.SetCurrentInstance(bridge);

                        string sizeFolder = parameterData.ImageSize == ImageSize.Large ? "LARGE" : "MIDDLE";
                        string imagePath = parameterData.ImageSize == ImageSize.Large
                            ? _serverSettings.LargeImagePath
                            : _serverSettings.MiddleImagePath;
                        if (admsProcessInfoList.Count == 0 || processNames.Count == 0)
                        {
                            throw new InvalidOperationException("No process mapping available for Python model output path.");
                        }

                        var firstProcessInfo = admsProcessInfoList[0];
                        if (!firstProcessInfo.TryGetValue("admsId", out object firstAdmsIdValue) || firstAdmsIdValue is not int firstAdmsId)
                        {
                            throw new InvalidOperationException("Invalid first process mapping: admsId is missing.");
                        }

                        var firstAdms = admsList.Find(a => a.Id == firstAdmsId)
                            ?? throw new InvalidOperationException($"ADMS not found for id: {firstAdmsId}");
                        string firstProcessName = processNames[0];
                        string firstAdmsName = !string.IsNullOrWhiteSpace(firstAdms.Name)
                            ? firstAdms.Name
                            : throw new InvalidOperationException($"ADMS name is empty for id: {firstAdmsId}");
                        string outputPath = Path.Combine(_serverSettings.EvaluationModelDirectory, sizeFolder, firstAdmsName);
                        string bestModelFullPath = Path.Combine(outputPath, $"{firstProcessName}.onnlmodel");

                        if (!Directory.Exists(outputPath))
                        {
                            Directory.CreateDirectory(outputPath);
                        }

                        var pyParams = PyTrainingParameters.FromTrainingDto(parameterData);
                        var categories = parameterData.Categories ?? Array.Empty<string>();
                        bridge.SetParameters(pyParams);
                        bridge.SetPaths(imagePath, outputPath, bestModelFullPath);
                        bridge.SetCategories(categories);

                        int numImages = await bridge.LoadImagesAsync(
                            categories,
                            processNames.ToArray(),
                            imagePath,
                            _serverSettings.TempImageDirectory);
                        await _mssqlDbService.InsertLogAsync($"[Py] Images prepared. Count: {numImages}", LogLevel.Debug);
                        if (numImages == 0)
                        {
                            var msg = $"[Py] No images found. imagePath={imagePath}, " +
                                      $"categories=[{string.Join(",", categories)}], " +
                                      $"processNames=[{string.Join(",", processNames)}]. " +
                                      "Expected: NG/BASE|NEW/{CATEGORY} and OK/{processName}/BASE|NEW";
                            await _mssqlDbService.InsertLogAsync(msg, LogLevel.Error);
                            throw new InvalidOperationException(msg);
                        }

                        // 이미지 로딩 완료 후 훈련 시작 준비
                        record.Status = TrainingStatus.Running;
                        record.StartTime = DateTime.Now;
                        await _mssqlDbService.UpdateTrainingAsync(record);
                        await _mssqlDbService.InsertLogAsync("[Py] Training phase started after image loading completion", LogLevel.Information);

                        ProgressEntry? previousProgressEntry = null;

                        await bridge.TrainAsync(async (isTraining, progress, bestIteration, currentAccuracy, bestAccuracy, bestValidationAccuracy, bestValidationError) =>
                        {
                            var now = DateTime.Now;

                            record.Status = TrainingStatus.Running;
                            record.Progress = progress;
                            record.BestIteration = bestIteration;
                            record.Accuracy = bestAccuracy;
                            record.Loss = 1 - bestAccuracy;

                            try
                            {
                                if (previousProgressEntry != null)
                                {
                                    previousProgressEntry.EndTime = now;
                                    previousProgressEntry.Duration = (now - previousProgressEntry.StartTime).TotalSeconds;
                                    await _mssqlDbService.UpdateProgressEntryAsync(previousProgressEntry);
                                }

                                await _mssqlDbService.UpdateTrainingAsync(record);

                                var progressEntry = new ProgressEntry
                                {
                                    IsTraining = isTraining,
                                    Progress = isTraining ? progress : 1,
                                    BestIteration = bestIteration,
                                    StartTime = now,
                                    EndTime = null,
                                    Duration = null,
                                    Accuracy = currentAccuracy,
                                    ValidationAccuracy = bestValidationAccuracy,
                                    ValidationError = bestValidationError,
                                    TrainingRecordId = record.Id
                                };

                                await _mssqlDbService.PushProgressEntryAsync(record.Id, progressEntry);
                                previousProgressEntry = progressEntry;

                                var durationText = previousProgressEntry?.Duration != null ? $", Duration: {previousProgressEntry.Duration:F2}s" : "";
                                await _mssqlDbService.InsertLogAsync($"[Py] Training progress: {progress:P1}, Best iteration: {bestIteration}, Accuracy: {bestAccuracy:P2}{durationText}", LogLevel.Debug);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[Py] DB update error: {ex.Message}");
                            }
                        });

                        if (previousProgressEntry != null)
                        {
                            try
                            {
                                var finalTime = DateTime.Now;
                                previousProgressEntry.EndTime = finalTime;
                                previousProgressEntry.Duration = (finalTime - previousProgressEntry.StartTime).TotalSeconds;
                                await _mssqlDbService.UpdateProgressEntryAsync(previousProgressEntry);
                                Console.WriteLine("[Py] 마지막 ProgressEntry 업데이트 완료");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[Py] 마지막 ProgressEntry 업데이트 오류: {ex.Message}");
                            }
                        }

                        string? bestModelPath = bridge.GetBestModelPath();
                        Console.WriteLine($"[Py] bestModelPath = {bestModelPath ?? "(null)"}");
                        if (string.IsNullOrEmpty(bestModelPath))
                        {
                            Console.WriteLine("[Py] WARNING: bestModelPath is null - TrainingImageResult inference will likely fail");
                            await _mssqlDbService.InsertLogAsync("[Py] WARNING: No best model path available after training. Check Python server logs.", LogLevel.Warning);
                        }

                        // ✅ 훈련 이미지 기록을 데이터베이스에 저장
                        try
                        {
                            var processAdmsMapping = new Dictionary<string, int>();
                            foreach (var info in admsProcessInfoList)
                            {
                                if (info.TryGetValue("processName", out object processNameValue) &&
                                    info.TryGetValue("admsProcessId", out object admsProcessIdValue) &&
                                    processNameValue is string pName &&
                                    admsProcessIdValue is int apId)
                                {
                                    processAdmsMapping[pName] = apId;
                                }
                            }

                            var trainingImageRecords = bridge.GetTrainingImageRecords(processAdmsMapping);
                            if (trainingImageRecords.Count > 0)
                            {
                                await _mssqlDbService.SaveTrainingImagesAsync(trainingImageRecords, record.Id, parameterData.ImageSize);
                                await _mssqlDbService.InsertLogAsync($"[Py] Saved {trainingImageRecords.Count} training image records to database", LogLevel.Information);
                            }

                            // TrainingImageResult 저장
                            if (categories.Length > 0 && !string.IsNullOrEmpty(bestModelPath))
                            {
                                await _mssqlDbService.InsertLogAsync("[Py] Starting TrainingImageResult processing", LogLevel.Information);
                                await SaveTrainingImageResultsFromRecords(
                                    record.Id,
                                    trainingImageRecords,
                                    imagePath => bridge.ClassifyAsync(imagePath, bestModelPath));
                                await _mssqlDbService.InsertLogAsync("[Py] TrainingImageResult processing completed", LogLevel.Information);
                            }
                            else if (categories.Length > 0 && string.IsNullOrEmpty(bestModelPath))
                            {
                                Console.WriteLine("[Py] SKIPPING TrainingImageResult processing - no model path available");
                                await _mssqlDbService.InsertLogAsync("[Py] SKIPPED TrainingImageResult processing - bestModelPath is null. Python server may not have returned best_model in status/result.", LogLevel.Warning);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Py] Error saving training images/results: {ex.Message}");
                            await _mssqlDbService.InsertLogAsync($"[Py] Error saving training images/results: {ex.Message}", LogLevel.Error);
                        }

                        // ✅ 여러 개의 프로세스에 대한 모델 저장
                        foreach (var admsProcessInfo in admsProcessInfoList)
                        {
                            if (!admsProcessInfo.TryGetValue("admsId", out object admsIdValue) || !(admsIdValue is int admsId)) continue;
                            if (!admsProcessInfo.TryGetValue("processId", out object processIdValue) || !(processIdValue is int processId)) continue;
                            if (!admsProcessInfo.TryGetValue("processName", out object processNameValue) || !(processNameValue is string processName)) continue;

                            var adms = admsList.Find(a => a.Id == admsId);
                            if (adms == null) continue;

                            string savePath = $"{_serverSettings.EvaluationModelDirectory}\\{sizeFolder}\\{adms.Name}\\";
                            string modelName = $"{processName}.onnlmodel";

                            if (!Directory.Exists(savePath))
                            {
                                Directory.CreateDirectory(savePath);
                            }

                            string result;
                            try
                            {
                                string localPath = savePath + modelName;
                                string clientPath = Path.Combine(parameterData.ClientModelDestination, modelName);

                                var sourceModelPath = !string.IsNullOrEmpty(bestModelPath) ? bestModelPath : bestModelFullPath;
                                if (!System.IO.File.Exists(sourceModelPath))
                                {
                                    throw new FileNotFoundException($"Source model not found: {sourceModelPath}");
                                }

                                if (!string.Equals(sourceModelPath, localPath, StringComparison.OrdinalIgnoreCase))
                                {
                                    System.IO.File.Copy(sourceModelPath, localPath, true);
                                }

                                result = await UploadModelToClientAsync(localPath, clientPath, adms.LocalIp);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[Py] Model save error: {ex.Message}");
                                await _mssqlDbService.InsertLogAsync($"[Py] Model save error: {ex.Message}", LogLevel.Error);
                                result = "error";
                            }

                            if (admsProcessInfo.TryGetValue("admsProcessId", out object admsProcessId) && admsProcessId is int intAdmsProcessId)
                            {
                                string admsProcessTypeString = parameterData.ImageSize switch
                                {
                                    ImageSize.Middle => "Middle",
                                    ImageSize.Large => "Large",
                                    _ => "Middle"
                                };

                                AdmsProcessType admsProcessType = await _mssqlDbService.GetOrCreateAdmsProcessType(intAdmsProcessId, admsProcessTypeString);

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

                                Console.WriteLine($"[Py] 모델 레코드 저장 완료: ModelName={modelName}, AdmsProcessTypeId={admsProcessType.Id}, Status={result}");
                            }
                        }

                        record.Status = TrainingStatus.Completed;
                        record.EndTime = DateTime.Now;
                        record.Progress = 1;
                        await _mssqlDbService.UpdateTrainingAsync(record);
                        await _mssqlDbService.InsertLogAsync("[Py] Model training finished", LogLevel.Information);

                        Dictionary<string, float> trainingResults = bridge.GetTrainingResult();
                        var labelList = trainingResults.Select(kvp => new Label
                        {
                            Name = kvp.Key,
                            Accuracy = kvp.Value,
                            TrainingRecordId = record.Id
                        }).ToArray();

                        await _mssqlDbService.UpdateLabelsByIdAsync(record.Id, labelList);
                    }
                    catch (OperationCanceledException cancelEx)
                    {
                        Console.WriteLine($"[Py] Training was cancelled: {cancelEx.Message}");
                        await _mssqlDbService.InsertLogAsync($"[Py] Training was cancelled: {cancelEx.Message}", LogLevel.Information);

                        record.Status = TrainingStatus.Cancelled;
                        record.EndTime = DateTime.Now;
                        await _mssqlDbService.UpdateTrainingAsync(record);
                        bridge?.CleanupTempImages();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"[Py] Error: {e}");
                        await _mssqlDbService.InsertLogAsync($"[Py] Error occurred: {e.Message}", LogLevel.Error);

                        record.Status = TrainingStatus.Failed;
                        record.EndTime = DateTime.Now;
                        await _mssqlDbService.UpdateTrainingAsync(record);
                        bridge?.CleanupTempImages();
                    }
                    finally
                    {
                        TrainingAiHttpBridge.SetCurrentInstance(null);
                        bridge?.CleanupTempImages();
                        bridge?.Dispose();
                        ToolStatusManager.SetProcessRunning(false);
                    }

                    return;
                }

                await RunOnStaThread(async () =>
                {
                    try
                    {
                        // 새로운 훈련 시작 시 중단 상태 리셋
                        instance.ResetStopState();

                        int numImages = 0;
                        TimeSpan elapsedTime = MeasureExecutionTime.Measure(() =>
                        {
                            numImages = instance.LoadImages(processNames.ToArray());
                        });

                        if (numImages == 0) return;

                        _mssqlDbService.InsertLogAsync($"Images loaded. Count: {numImages}, Elapsed time: {elapsedTime}", LogLevel.Debug).GetAwaiter().GetResult();

                        // 이미지 로딩 완료 후 훈련 시작 준비
                        record.Status = TrainingStatus.Running; // 실제 훈련 단계로 전환
                        record.StartTime = DateTime.Now; // 실질적인 훈련 시작 시간 기록
                        _mssqlDbService.UpdateTrainingAsync(record).GetAwaiter().GetResult();
                        _mssqlDbService.InsertLogAsync("Training phase started after image loading completion", LogLevel.Information).GetAwaiter().GetResult();

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
                        ProgressEntry? previousProgressEntry = null;

                        instance.Train(async (isTraining, progress, bestIteration, currentAccuracy, bestAccuracy, bestValidationAccuracy, bestValidationError) =>
                        {
                            var now = DateTime.Now;

                            // 메모리에 진행상황 저장 (매번)
                            record.Status = TrainingStatus.Running;
                            record.Progress = progress;
                            record.BestIteration = bestIteration;
                            record.Accuracy = bestAccuracy;
                            record.Loss = 1 - bestAccuracy;

                            // 비동기로 DB 업데이트 - 하지만 순차 처리로 race condition 방지
                            try
                            {
                                // 이전 ProgressEntry의 EndTime과 Duration 업데이트
                                if (previousProgressEntry != null)
                                {
                                    previousProgressEntry.EndTime = now;
                                    previousProgressEntry.Duration = (now - previousProgressEntry.StartTime).TotalSeconds;
                                    await _mssqlDbService.UpdateProgressEntryAsync(previousProgressEntry);
                                }

                                // TrainingRecord 업데이트
                                await _mssqlDbService.UpdateTrainingAsync(record);

                                // 새로운 ProgressEntry 생성 및 저장
                                var progressEntry = new ProgressEntry
                                {
                                    IsTraining = isTraining,
                                    Progress = isTraining ? progress : 1,
                                    BestIteration = bestIteration,
                                    StartTime = now,
                                    EndTime = null, // 다음 콜백에서 설정됨
                                    Duration = null, // 다음 콜백에서 계산됨
                                    Accuracy = currentAccuracy,
                                    ValidationAccuracy = bestValidationAccuracy,
                                    ValidationError = bestValidationError,
                                    TrainingRecordId = record.Id
                                };

                                await _mssqlDbService.PushProgressEntryAsync(record.Id, progressEntry);
                                previousProgressEntry = progressEntry; // 다음 콜백에서 사용하기 위해 저장

                                // 매 iteration마다 로깅
                                var durationText = previousProgressEntry?.Duration != null ? $", Duration: {previousProgressEntry.Duration:F2}s" : "";
                                await _mssqlDbService.InsertLogAsync($"Training progress: {progress:P1}, Best iteration: {bestIteration}, Accuracy: {bestAccuracy:P2}{durationText}", LogLevel.Debug);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"DB update error: {ex.Message}");
                            }
                        }).GetAwaiter().GetResult();

                        // 마지막 ProgressEntry의 EndTime과 Duration 설정
                        if (previousProgressEntry != null)
                        {
                            try
                            {
                                var finalTime = DateTime.Now;
                                previousProgressEntry.EndTime = finalTime;
                                previousProgressEntry.Duration = (finalTime - previousProgressEntry.StartTime).TotalSeconds;
                                await _mssqlDbService.UpdateProgressEntryAsync(previousProgressEntry);
                                Console.WriteLine("마지막 ProgressEntry 업데이트 완료");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"마지막 ProgressEntry 업데이트 오류: {ex.Message}");
                            }
                        }

                        // ✅ 훈련 이미지 기록을 데이터베이스에 저장
                        try
                        {
                            // AdmsProcessId 매핑 정보 생성 (processName -> admsProcessId)
                            var processAdmsMapping = new Dictionary<string, int>();
                            foreach (var info in admsProcessInfoList)
                            {
                                if (info.TryGetValue("processName", out object processNameValue) &&
                                    info.TryGetValue("admsProcessId", out object admsProcessIdValue) &&
                                    processNameValue is string pName &&
                                    admsProcessIdValue is int apId)
                                {
                                    processAdmsMapping[pName] = apId;
                                }
                            }

                            // TrainingAi 인스턴스에 매핑 정보 설정
                            instance.SetAdmsProcessMapping(processAdmsMapping);

                            // 훈련 이미지 기록 가져오기 및 저장
                            var trainingImageRecords = instance.GetTrainingImageRecords();
                            if (trainingImageRecords.Count > 0)
                            {
                                await _mssqlDbService.SaveTrainingImagesAsync(trainingImageRecords, record.Id, parameterData.ImageSize);
                                await _mssqlDbService.InsertLogAsync($"Saved {trainingImageRecords.Count} training image records to database", LogLevel.Information);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error saving training images to database: {ex.Message}");
                            await _mssqlDbService.InsertLogAsync($"Error saving training images: {ex.Message}", LogLevel.Error);
                        }

                        // ✅ 여러 개의 프로세스에 대한 모델 저장
                        string timeStamp = DateTime.Now.ToString("yyyyMMdd");

                        // ✅ admsProcessInfoList를 기반으로 모델 저장 (중복 제거 및 정확한 매핑)
                        foreach (var admsProcessInfo in admsProcessInfoList)
                        {
                            if (!admsProcessInfo.TryGetValue("admsId", out object admsIdValue) || !(admsIdValue is int admsId)) continue;
                            if (!admsProcessInfo.TryGetValue("processId", out object processIdValue) || !(processIdValue is int processId)) continue;
                            if (!admsProcessInfo.TryGetValue("processName", out object processNameValue) || !(processNameValue is string processName)) continue;

                            var adms = admsList.Find(a => a.Id == admsId);
                            if (adms == null) continue;

                            // ✅ 새로운 경로 구조: ImageSize에 따라 LARGE 또는 MIDDLE 폴더 사용
                            string sizeFolder = parameterData.ImageSize == ImageSize.Large ? "LARGE" : "MIDDLE";
                            string savePath = $"{_serverSettings.EvaluationModelDirectory}\\{sizeFolder}\\{adms.Name}\\";

                            // ✅ 모델명을 ProcessId.edltool로 변경
                            string modelName = $"{processName}.edltool";

                            if (!Directory.Exists(savePath))
                            {
                                Directory.CreateDirectory(savePath);
                            }

                            string result = await instance.SaveModel(savePath + modelName, Path.Combine(parameterData.ClientModelDestination, modelName), adms.LocalIp);

                            if (admsProcessInfo.TryGetValue("admsProcessId", out object admsProcessId) && admsProcessId is int intAdmsProcessId)
                            {
                                // ImageSize에 따른 Type 결정
                                string admsProcessTypeString = parameterData.ImageSize switch
                                {
                                    ImageSize.Middle => "Middle",
                                    ImageSize.Large => "Large",
                                    _ => "Middle" // 기본값
                                };

                                // AdmsProcessType이 없으면 생성하고, 있으면 가져옴
                                AdmsProcessType admsProcessType = await _mssqlDbService.GetOrCreateAdmsProcessType(intAdmsProcessId, admsProcessTypeString);

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

                                Console.WriteLine($"모델 레코드 저장 완료: ModelName={modelName}, AdmsProcessTypeId={admsProcessType.Id}, Status={result}");
                            }
                        }

                        // 🎯 새로운 단순한 구조: 개별 이미지 추론 결과만 저장
                        if (parameterData.Categories != null && parameterData.Categories.Length > 0)
                        {
                            await _mssqlDbService.InsertLogAsync("🚀 Starting simplified TrainingImageResult processing", LogLevel.Information);
                            Console.WriteLine("🔍 DEBUG: Starting simplified TrainingImageResult processing...");

                            // Include OK label in the categories  
                            var allCategories = new List<string>(parameterData.Categories) { "OK" };

                            // 🔥 단순화된 접근: 각 이미지에 대해 추론 실행하여 TrainingImageResult에 직접 저장
                            await SaveConfusionMatrixImages(record.Id, allCategories, instance, parameterData.ImageSize);

                            await _mssqlDbService.InsertLogAsync("✅ TrainingImageResult processing completed", LogLevel.Information);
                            Console.WriteLine("✅ DEBUG: Simplified TrainingImageResult processing completed");
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
                        instance.CleanupTempImages();
                        SingletonAiDuo.Reset(parameterData.ImageSize);
                        ToolStatusManager.SetProcessRunning(false);
                    }
                    catch (OperationCanceledException cancelEx)
                    {
                        ToolStatusManager.SetProcessRunning(false);
                        Console.WriteLine($"Training was cancelled: {cancelEx.Message}");
                        _mssqlDbService.InsertLogAsync($"Training was cancelled: {cancelEx.Message}", LogLevel.Information).GetAwaiter().GetResult();

                        // 취소 시 상태를 Cancelled로 업데이트
                        var cancelUpdates = new Dictionary<string, object>
                        {
                            { "Status", TrainingStatus.Cancelled }, // 또는 별도의 Cancelled 상태가 있다면 사용
                            { "EndTime", DateTime.Now }
                        };
                        _mssqlDbService.PartialUpdateTrainingAsync(record.Id, cancelUpdates).GetAwaiter().GetResult();

                        instance.StopTraining();
                        instance.CleanupTempImages();
                        SingletonAiDuo.Reset(parameterData.ImageSize);
                        Console.WriteLine("Training cancellation cleanup completed");
                        return;
                    }
                    catch (Exception e)
                    {
                        ToolStatusManager.SetProcessRunning(false);
                        Console.WriteLine("Error: " + e);
                        _mssqlDbService.InsertLogAsync($"Error occurred: {e.Message}", LogLevel.Error).GetAwaiter().GetResult();

                        // 에러 발생 시 EndTime 설정 및 Status 업데이트
                        var errorUpdates = new Dictionary<string, object>
                        {
                            { "Status", TrainingStatus.Failed },
                            { "EndTime", DateTime.Now }
                        };
                        _mssqlDbService.PartialUpdateTrainingAsync(record.Id, errorUpdates).GetAwaiter().GetResult();

                        instance.StopTraining();
                        instance.CleanupTempImages();
                        SingletonAiDuo.Reset(parameterData.ImageSize);
                        // throw; 제거 - 예외를 다시 던지지 않고 정상적으로 종료
                        return;
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
                var instance = SingletonAiDuo.GetInstance(parameterData.ImageSize);
                instance?.CleanupTempImages();
                return BadRequest(new { Error = error.Message });
            }
            else
            {
                // 메인 에러 처리에서도 EndTime 설정
                var errorUpdates = new Dictionary<string, object>
                {
                    { "Status", TrainingStatus.Failed },
                    { "EndTime", DateTime.Now }
                };
                _mssqlDbService.PartialUpdateTrainingAsync(_recordId, errorUpdates).GetAwaiter().GetResult();

                var instance = SingletonAiDuo.GetInstance(parameterData.ImageSize);
                if (instance != null)
                {
                    instance.StopTraining();
                    instance.CleanupTempImages();
                }
                SingletonAiDuo.Reset(parameterData.ImageSize);
                Console.WriteLine(error.Message);
                _mssqlDbService.InsertLogAsync(error.Message, LogLevel.Error);
                return BadRequest(new { Error = error.Message });
            }

        }
    }

    /// <summary>
    /// 현재 진행 중인 훈련을 중지하고 인스턴스를 완전히 해제합니다.
    /// </summary>
    /// <returns>처리 완료 메시지</returns>
    [HttpDelete("stop")]
    [AuthorizeByRole(UserRoleType.Operator, UserRoleType.Manager, UserRoleType.PROCEngineer, UserRoleType.ServiceEngineer)]
    public async Task<IActionResult> StopTraining()
    {
        try
        {
            await _mssqlDbService.InsertLogAsync("Stop training and dispose instance called", LogLevel.Information);

            if (_serverSettings.UsePythonServer)
            {
                var bridge = TrainingAiHttpBridge.CurrentInstance;
                if (bridge == null)
                {
                    await _mssqlDbService.InsertLogAsync("No running Python training instance found", LogLevel.Warning);
                    return BadRequest(new NewRecord("No running Python training instance found."));
                }

                await _mssqlDbService.InsertLogAsync("Stopping Python training via /train/cls/stop", LogLevel.Information);
                bridge.StopTraining();
                TrainingAiHttpBridge.SetCurrentInstance(null);
                ToolStatusManager.SetProcessRunning(false);

                await _mssqlDbService.InsertLogAsync("Python training stopped successfully", LogLevel.Information);
                return Ok(new
                {
                    Message = "Python training stopped successfully",
                    Status = "Success"
                });
            }

            // 실행 중인 인스턴스 찾기  
            var middleInstance = SingletonAiDuo.GetInstance(ImageSize.Middle);
            var largeInstance = SingletonAiDuo.GetInstance(ImageSize.Large);

            TrainingAi? runningInstance = null;
            ImageSize runningImageSize = ImageSize.Middle; // 초기화 추가  

            // Middle 인스턴스가 실행 중인지 확인  
            if (middleInstance != null && middleInstance.IsTraining())
            {
                runningInstance = middleInstance;
                runningImageSize = ImageSize.Middle;
                Console.WriteLine("Found running Middle instance");
                await _mssqlDbService.InsertLogAsync("Found running Middle instance", LogLevel.Debug);
            }
            // Large 인스턴스가 실행 중인지 확인  
            else if (largeInstance != null && largeInstance.IsTraining())
            {
                runningInstance = largeInstance;
                runningImageSize = ImageSize.Large;
                Console.WriteLine("Found running Large instance");
                await _mssqlDbService.InsertLogAsync("Found running Large instance", LogLevel.Debug);
            }
            // 둘 다 실행 중이 아니라면 첫 번째로 찾은 인스턴스 사용 (이미지 로딩 중일 수 있음)  
            else if (middleInstance != null)
            {
                runningInstance = middleInstance;
                runningImageSize = ImageSize.Middle;
                Console.WriteLine("Using Middle instance (may be in image loading phase)");
                await _mssqlDbService.InsertLogAsync("Using Middle instance (may be in image loading phase)", LogLevel.Debug);
            }
            else if (largeInstance != null)
            {
                runningInstance = largeInstance;
                runningImageSize = ImageSize.Large;
                Console.WriteLine("Using Large instance (may be in image loading phase)");
                await _mssqlDbService.InsertLogAsync("Using Large instance (may be in image loading phase)", LogLevel.Debug);
            }

            if (runningInstance == null)
            {
                await _mssqlDbService.InsertLogAsync("No running or available training instance found", LogLevel.Warning);
                return BadRequest(new NewRecord("No running or available training instance found."));
            }
            else
            {
                // 훈련 중단 및 완전한 리소스 해제  
                Console.WriteLine($"Stopping {runningImageSize} training and disposing all resources...");
                await _mssqlDbService.InsertLogAsync($"Stopping {runningImageSize} training and disposing all resources", LogLevel.Information);

                // 1. 훈련 중단 (이미지 로딩 중이라면 즉시 중단)  
                runningInstance.StopTraining();
                await _mssqlDbService.InsertLogAsync("Training stopped", LogLevel.Debug);

                // 2. 임시 이미지 파일 정리  
                runningInstance.CleanupTempImages();
                await _mssqlDbService.InsertLogAsync("Temporary images cleaned up", LogLevel.Debug);

                // 3. 모든 리소스 해제 (메모리, GPU 등)  
                runningInstance.DisposeTool();
                await _mssqlDbService.InsertLogAsync("All resources disposed", LogLevel.Debug);

                // 4. 싱글톤 인스턴스 리셋  
                SingletonAiDuo.Reset(runningImageSize);
                await _mssqlDbService.InsertLogAsync("Singleton instance reset", LogLevel.Debug);

                // 5. 프로세스 실행 상태 해제  
                ToolStatusManager.SetProcessRunning(false);
                await _mssqlDbService.InsertLogAsync("Process status set to not running", LogLevel.Debug);

                Console.WriteLine($"{runningImageSize} training stopped and all resources disposed successfully");
                await _mssqlDbService.InsertLogAsync($"{runningImageSize} training stopped and all resources disposed successfully", LogLevel.Information);

                return Ok(new
                {
                    Message = "Training stopped and all resources disposed successfully",
                    ImageSize = runningImageSize.ToString(),
                    Status = "Success"
                });
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error during stop and dispose: {e}");
            await _mssqlDbService.InsertLogAsync($"Error during stop and dispose: {e.Message}", LogLevel.Error);
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
    /// ⚠️ DEPRECATED: 이 API는 더 이상 필요하지 않습니다. 
    /// TrainingImageResult 테이블이 자동으로 생성되므로 별도의 저장이 불필요합니다.
    /// </summary>
    /// <param name="trainingRecordId">훈련 기록 ID</param>
    /// <param name="categories">분류 카테고리 배열</param>
    /// <returns>성공 메시지</returns>
    [HttpPost("saveConfusionMatrix/{trainingRecordId}")]
    [AuthorizeByRole(UserRoleType.Operator, UserRoleType.Manager, UserRoleType.PROCEngineer, UserRoleType.ServiceEngineer)]
    public async Task<IActionResult> SaveConfusionMatrix([FromRoute] int trainingRecordId, [FromBody] string[] categories)
    {
        await _mssqlDbService.InsertLogAsync($"DEPRECATED API called: saveConfusionMatrix for training record {trainingRecordId}", LogLevel.Warning);

        return Ok(new
        {
            Message = "This API is deprecated. TrainingImageResult data is automatically created during training.",
            Status = "Deprecated",
            Recommendation = "Use getConfusionMatrix/{trainingRecordId} to retrieve confusion matrix data"
        });
    }

    /// <summary>
    /// 🎯 특정 훈련 기록에 대한 혼동 행렬을 동적으로 계산하여 가져옵니다.
    /// 새로운 단순한 TrainingImageResult 기반 구조
    /// </summary>
    /// <param name="trainingRecordId">혼동 행렬을 가져올 훈련 기록 ID</param>
    /// <returns>동적으로 계산된 혼동 행렬 데이터</returns>
    [HttpGet("getConfusionMatrix/{trainingRecordId}")]
    public async Task<IActionResult> GetConfusionMatrix([FromRoute] int trainingRecordId)
    {
        try
        {
            var matrices = await _mssqlDbService.GetTrainingConfusionMatrixAsync(trainingRecordId);
            return Ok(new
            {
                TrainingRecordId = trainingRecordId,
                ConfusionMatrix = matrices,
                Message = "Dynamically calculated from TrainingImageResult table"
            });
        }
        catch (Exception ex)
        {
            await _mssqlDbService.InsertLogAsync("Error retrieving confusion matrix: " + ex.Message, LogLevel.Error);
            return StatusCode(500, "Error retrieving confusion matrix: " + ex.Message);
        }
    }

    /// <summary>
    /// 🎯 특정 혼동 행렬 항목에 해당하는 이미지들을 가져옵니다.
    /// 새로운 단순한 TrainingImageResult 기반 구조
    /// </summary>
    /// <param name="trainingRecordId">훈련 기록 ID</param>
    /// <param name="trueLabel">실제 레이블 (예: "OK", "NG" 등)</param>
    /// <param name="predictedLabel">예측 레이블 (예: "OK", "NG" 등)</param>
    /// <returns>해당 혼동 행렬 항목의 이미지 목록</returns>
    [HttpGet("getConfusionMatrixImages/{trainingRecordId}/{trueLabel}/{predictedLabel}")]
    public async Task<IActionResult> GetConfusionMatrixImages([FromRoute] int trainingRecordId, [FromRoute] string trueLabel, [FromRoute] string predictedLabel)
    {
        try
        {
            await _mssqlDbService.InsertLogAsync($"Getting training images for {trainingRecordId}, true: {trueLabel}, predicted: {predictedLabel}", LogLevel.Information);

            var images = await _mssqlDbService.GetTrainingImagesByLabelsAsync(trainingRecordId, trueLabel, predictedLabel);

            return Ok(new
            {
                TrainingRecordId = trainingRecordId,
                TrueLabel = trueLabel,
                PredictedLabel = predictedLabel,
                ImageCount = images.Count,
                Images = images,
                Message = "Retrieved from simplified TrainingImageResult table"
            });
        }
        catch (Exception ex)
        {
            await _mssqlDbService.InsertLogAsync($"Error retrieving training images: {ex.Message}", LogLevel.Error);
            return StatusCode(500, $"Error retrieving training images: {ex.Message}");
        }
    }

    /// <summary>
    /// 🎯 특정 혼동 행렬 항목에 해당하는 이미지 파일 목록을 가져옵니다.
    /// 새로운 단순한 TrainingImageResult 기반 구조 사용
    /// </summary>
    /// <param name="trainingRecordId">훈련 기록 ID</param>
    /// <param name="trueLabel">실제 레이블</param>
    /// <param name="predictedLabel">예측 레이블</param>
    /// <returns>이미지 파일 목록</returns>
    [HttpGet("getConfusionMatrixImageFiles/{trainingRecordId}/{trueLabel}/{predictedLabel}")]
    public async Task<IActionResult> GetConfusionMatrixImageFiles([FromRoute] int trainingRecordId, [FromRoute] string trueLabel, [FromRoute] string predictedLabel)
    {
        try
        {
            // 🎯 새로운 구조: TrainingImageResult에서 직접 조회
            var images = await _mssqlDbService.GetTrainingImagesByLabelsAsync(trainingRecordId, trueLabel, predictedLabel);

            // ImageFile 정보만 추출
            var imageFiles = images.Select(img =>
            {
                var imageObj = (dynamic)img;
                return imageObj.ImageFile;
            }).ToList();

            return Ok(new
            {
                TrainingRecordId = trainingRecordId,
                TrueLabel = trueLabel,
                PredictedLabel = predictedLabel,
                ImageCount = imageFiles.Count,
                ImageFiles = imageFiles,
                Message = "Retrieved from simplified TrainingImageResult table"
            });
        }
        catch (Exception ex)
        {
            await _mssqlDbService.InsertLogAsync($"Error retrieving image files: {ex.Message}", LogLevel.Error);
            return StatusCode(500, $"Error retrieving image files: {ex.Message}");
        }
    }

    /// <summary>
    /// 🎯 훈련에 사용된 이미지들에 대해 모델 추론을 실행하고 TrainingImageResult 테이블에 저장합니다.
    /// 기존의 복잡한 ConfusionMatrix + ConfusionMatrixImage 구조를 단순화
    /// </summary>
    [NonAction]
    private async Task SaveConfusionMatrixImages(int trainingRecordId, List<string> allCategories, TrainingAi instance, ImageSize imageSize)
    {
        var trainingImageRecords = instance.GetTrainingImageRecords();
        await SaveTrainingImageResultsFromRecords(
            trainingRecordId,
            trainingImageRecords,
            imagePath => Task.FromResult(instance.Classify(imagePath)));
    }

    [NonAction]
    private async Task SaveTrainingImageResultsFromRecords(
        int trainingRecordId,
        List<(string imagePath, string trueLabel, string status, string? category, int? admsProcessId)> trainingImageRecords,
        Func<string, Task<ClassificationResult>> classifyAsync)
    {
        try
        {
            await _mssqlDbService.InsertLogAsync("🚀 Starting TrainingImageResult data processing...", LogLevel.Information);
            Console.WriteLine("🔍 DEBUG: Starting TrainingImageResult data processing...");

            Console.WriteLine($"🔍 DEBUG: Retrieved {trainingImageRecords.Count} training image records");

            if (trainingImageRecords.Count == 0)
            {
                Console.WriteLine("⚠️ WARNING: No training image records found - skipping TrainingImageResult processing");
                await _mssqlDbService.InsertLogAsync("No training image records found - skipping TrainingImageResult processing", LogLevel.Warning);
                return;
            }

            // 🔍 DEBUG: 데이터베이스에 저장된 ImageFile 레코드들 확인
            try
            {
                var allImageFiles = await _mssqlDbService.GetAllImageFilesForTrainingAsync(trainingRecordId);
                Console.WriteLine($"🔍 DEBUG: Found {allImageFiles.Count} ImageFile records in database for this training");
                foreach (var dbFile in allImageFiles.Take(5)) // 처음 5개만 로깅
                {
                    Console.WriteLine($"🔍 DEBUG: DB ImageFile - Name: {dbFile.Name}, Directory: {dbFile.Directory}, AdmsProcessId: {dbFile.AdmsProcessId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔍 DEBUG: Error retrieving ImageFile records: {ex.Message}");
            }

            int processedCount = 0;
            int savedCount = 0;
            int errorCount = 0;

            // 훈련에 사용된 이미지들을 배치로 처리
            var imageRecordBatches = trainingImageRecords.Chunk(50); // 50개씩 배치 처리

            foreach (var batch in imageRecordBatches)
            {
                Console.WriteLine($"🔍 DEBUG: Processing batch of {batch.Count()} images...");

                var batchTasks = batch.Select(async imageRecord =>
                {
                    try
                    {
                        processedCount++;
                        Console.WriteLine($"🔍 DEBUG: Processing image {processedCount}: {imageRecord.imagePath}");

                        // 이미지 파일이 존재하는지 확인
                        if (!System.IO.File.Exists(imageRecord.imagePath))
                        {
                            Console.WriteLine($"⚠️ WARNING: Image file not found: {imageRecord.imagePath}");
                            return;
                        }

                        // 모델 추론 실행
                        var classifyResult = await classifyAsync(imageRecord.imagePath);
                        string predictedLabel = classifyResult.BestLabel?.ToUpper() ?? "UNKNOWN";
                        float confidence = classifyResult.BestScore;

                        Console.WriteLine($"🔍 DEBUG: Image {Path.GetFileName(imageRecord.imagePath)} - True: {imageRecord.trueLabel}, Predicted: {predictedLabel}, Confidence: {confidence:F3}");

                        // ImageFile 레코드 찾기
                        var fileName = Path.GetFileName(imageRecord.imagePath);
                        var directory = _mssqlDbService.ConvertToRelativePath(Path.GetDirectoryName(imageRecord.imagePath) ?? "");

                        Console.WriteLine($"🔍 DEBUG: Looking for ImageFile - FileName: {fileName}, Directory: {directory}");

                        // NG 이미지와 OK 이미지를 구분하여 검색
                        ImageFile? imageFile = null;
                        if (imageRecord.trueLabel == "OK")
                        {
                            // OK 이미지: AdmsProcessId로 검색
                            Console.WriteLine($"🔍 DEBUG: Searching OK image with AdmsProcessId: {imageRecord.admsProcessId}");
                            imageFile = await _mssqlDbService.FindImageFileAsync(fileName, directory, imageRecord.admsProcessId);
                        }
                        else
                        {
                            // NG 이미지: Category로 검색
                            Console.WriteLine($"🔍 DEBUG: Searching NG image with Category: {imageRecord.trueLabel}");
                            imageFile = await _mssqlDbService.FindImageFileAsync(fileName, directory, category: imageRecord.trueLabel);
                        }

                        if (imageFile != null)
                        {
                            Console.WriteLine($"✅ DEBUG: Found ImageFile ID: {imageFile.Id} for {fileName}");

                            // 🎯 새로운 단순한 구조: 직접 TrainingImageResult에 저장
                            // NG 이미지와 OK 이미지 구분하여 저장
                            string? category = imageRecord.trueLabel != "OK" ? imageRecord.trueLabel : null;
                            int? admsProcessId = imageRecord.trueLabel == "OK" ? imageRecord.admsProcessId : null;

                            await _mssqlDbService.SaveTrainingImageResultAsync(
                                trainingRecordId,
                                imageFile.Id,
                                imageRecord.trueLabel,
                                predictedLabel,
                                confidence,
                                "Predicted", // 모델 추론 결과
                                category,
                                admsProcessId);

                            savedCount++;
                            Console.WriteLine($"✅ DEBUG: Saved TrainingImageResult for {Path.GetFileName(imageRecord.imagePath)} (ID: {imageFile.Id})");
                        }
                        else
                        {
                            Console.WriteLine($"⚠️ WARNING: ImageFile not found in database - FileName: {fileName}, Directory: {directory}, AdmsProcessId: {imageRecord.admsProcessId}");
                            Console.WriteLine($"⚠️ DEBUG: Full image path: {imageRecord.imagePath}");
                            Console.WriteLine($"⚠️ DEBUG: True label: {imageRecord.trueLabel}, Predicted: {predictedLabel}");
                            errorCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        Console.WriteLine($"❌ ERROR: Error processing image {imageRecord.imagePath}: {ex.Message}");
                        await _mssqlDbService.InsertLogAsync($"Error processing image {imageRecord.imagePath}: {ex.Message}", LogLevel.Error);
                    }
                });

                // 배치 내의 모든 작업 완료 대기
                await Task.WhenAll(batchTasks);

                Console.WriteLine($"🔍 DEBUG: Batch completed. Total processed: {processedCount}, Saved: {savedCount}, Errors: {errorCount}");
            }

            await _mssqlDbService.InsertLogAsync($"TrainingImageResult processing completed - Processed: {processedCount}, Saved: {savedCount}, Errors: {errorCount}", LogLevel.Information);
            Console.WriteLine($"✅ DEBUG: TrainingImageResult processing completed - Processed: {processedCount}, Saved: {savedCount}, Errors: {errorCount}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR: Critical error in SaveTrainingImageResultsFromRecords: {ex.Message}");
            await _mssqlDbService.InsertLogAsync($"Critical error in SaveTrainingImageResultsFromRecords: {ex.Message}", LogLevel.Error);
        }
    }

    [NonAction]
    private static async Task<string> UploadModelToClientAsync(string localPath, string remotePath, string clientIpAddress)
    {
        try
        {
            string? directoryPath = Path.GetDirectoryName(localPath);
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            using var client = new HttpClient();
            using var form = new MultipartFormDataContent();

            byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(localPath);
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
            string fileName = Path.GetFileName(localPath);
            form.Add(fileContent, "File", fileName);
            form.Add(new StringContent(remotePath), "ModelPath");

            string apiUrl = $"http://{clientIpAddress}/api/model/upload";
            HttpResponseMessage response = await client.PostAsync(apiUrl, form);

            if (response.IsSuccessStatusCode)
            {
                return "saved";
            }

            return "pending";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Py] Model upload error: {ex.Message}");
            return "error";
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
    private static uint ResolveRequestedChannels(uint imageChannels, bool grayInput, bool addFft)
    {
        if (grayInput || addFft)
        {
            return (uint)((grayInput ? 1 : 3) + (addFft ? 1 : 0));
        }

        return imageChannels > 0 ? imageChannels : 3;
    }

    [NonAction]
    public Task RunOnStaThread(Func<Task> asyncAction)
    {
        var tcs = new TaskCompletionSource<bool>();

        Thread staThread = new(() =>
        {
            try
            {
                // STA 스레드에서 비동기 작업을 안전하게 실행
                asyncAction().GetAwaiter().GetResult();
                tcs.SetResult(true);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        })
        {
            Name = "DeepLearning-STA",
            IsBackground = true
        };

        staThread.SetApartmentState(ApartmentState.STA);
        staThread.Start();

        return tcs.Task;
    }

}

internal record NewRecord(string Error);