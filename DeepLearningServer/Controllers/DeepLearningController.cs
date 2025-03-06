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

/// <summary>
/// Represents a controller for handling deep learning-related API requests.
/// </summary>
namespace DeepLearningServer.Controllers;

[Route("api/[controller]")]
[ApiController]

public class DeepLearningController(IOptions<ServerSettings> serverSettings,
    IMapper mapper, MssqlDbService mssqlDbService) : ControllerBase
{
    //private readonly MongoDbService _mongoDbService;
    private readonly ServerSettings _serverSettings = serverSettings.Value;
    private readonly MssqlDbService _mssqlDbService = mssqlDbService;
    private readonly IMapper _mapper = mapper;
    private int _recordId = 0;

    [HttpPost("run")]
    [AuthorizeByPermission(PermissionType.RunModel)] // ✅ RunModel 권한을 가진 사용자만 접근 가능
    public async Task<IActionResult> CreateToolAndRun([FromBody] CreateAndRunModel parameterData)
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
            Console.WriteLine($"AdmsProcessIds: {string.Join("," , parameterData.AdmsProcessIds)}");

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
                if(info.TryGetValue("admsId", out object admsIdValue) && admsIdValue is int admsIdIntValue)
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
                            //foreach (var processName in processNames)
                            //{
                            //    numImages += instance.LoadImages(processName);
                            //}
                            numImages = instance.LoadImages(processNames.ToArray());
                        });

                        if (numImages == 0) return;

                        _mssqlDbService.InsertLogAsync($"Images loaded. Count: {numImages}", LogLevel.Debug).GetAwaiter().GetResult();
                        instance.SetParameters();
                        if (parameterData.Classifier.UsePretrainedModel)
                        {
                            record.HasPretrainedModel = instance.LoadPretrainedModel(_serverSettings.PretrainedModelPath, parameterData.ImageSize);
                            if (!record.HasPretrainedModel)
                            {
                                throw new Exception("Failed to use pretrained model");
                            }
                            _mssqlDbService.UpdateTrainingAsync(record).GetAwaiter().GetResult();
                        }
                        instance.Train((isTraining, progress, bestIteration, currentAccuracy, bestAccuracy) =>
                        {
                            _mssqlDbService.InsertLogAsync($"progress: {progress}", LogLevel.Trace).GetAwaiter().GetResult();
                        //    var updates = new Dictionary<string, object>
                        //{
                        //    { "Status", TrainingStatus.Running },
                        //    { "Progress", progress },
                        //    { "BestIteration", bestIteration },
                        //    { "Accuracy", bestAccuracy }

                        //};

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

                            //_mssqlDbService.PartialUpdateTrainingAsync(record.Id, updates).GetAwaiter().GetResult();

                            _mssqlDbService.PushProgressEntryAsync(record.Id, newEntry).GetAwaiter().GetResult();
                        }).GetAwaiter().GetResult();

                        // ✅ 여러 개의 프로세스에 대한 모델 저장
                        string timeStamp = DateTime.Now.ToString("yyyyMMdd");
                        foreach (var adms in admsList)
                        {
                            foreach (var processName in processNames)
                            {
                                string savePath = $@"D:\Models\{adms.Name}\{processName}\{timeStamp}\";
                                string modelName;
                                if (parameterData.IsDefaultModel)
                                {
                                    if(parameterData.ImageSize == ImageSize.Middle)
                                    {
                                        modelName = "Default_Middle.edltool";
                                    }
                                    else if(parameterData.ImageSize == ImageSize.Large)
                                    {
                                        modelName = "Default_Large.edltool";
                                    }
                                    else
                                    {
                                        //modelName = "Default_Large.edltool";
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
                                //instance.SaveModel(savePath + $"{processName}.edltool", adms.LocalIp, parameterData.ImageSize );
                                string result = await instance.SaveModel(savePath + modelName, Path.Combine(parameterData.ClientModelDestination, modelName), adms.LocalIp);
                                //var admsProcess = await _mssqlDbService.GetAdmsProcess(adms.Id, processName);
                                //var admsProcessTypeId = await _mssqlDbService.GetAdmsProcessType(admsProcessId);
                                var admsProcess = admsProcessInfoList.Find(admsProcessInfo => admsProcessInfo["admsId"].Equals(adms.Id) && admsProcessInfo["processName"].Equals(processName));
                                if (admsProcess == null) { 
                                    throw new Exception(modelName + " is not found in the admsProcessInfoList");
                                }
                                AdmsProcessType admsProcessType;
                                if(admsProcess.TryGetValue("admsProcessId", out object admsProcessId) && admsProcessId is int intAdmsProcessId)
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
                                
                                //record.ModelName = modelName;
                                //record.ModelPath = savePath;
                            }
                        }
                        record.Status = TrainingStatus.Completed;
                        record.EndTime = DateTime.Now;
                        record.Progress = 1;
                        _mssqlDbService.UpdateTrainingAsync(record).GetAwaiter().GetResult();

                        _mssqlDbService.InsertLogAsync("Model training finished", LogLevel.Information).GetAwaiter().GetResult();
                    //    _mssqlDbService.PartialUpdateTrainingAsync(record.Id, new Dictionary<string, object>
                    //{
                    //    { "Status", TrainingStatus.Completed },
                    //    { "EndTime" , DateTime.Now }
                    //}).GetAwaiter().GetResult();

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
            if(_recordId == 0) {
                throw;
            }
            else
            {
                _mssqlDbService.PartialUpdateTrainingAsync(_recordId, new Dictionary<string, object> { { "Status", TrainingStatus.Failed } }).GetAwaiter().GetResult();
                var instance = SingletonAiDuo.GetInstance(parameterData.ImageSize);
                if(instance != null)
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
              await  _mssqlDbService.InsertLogAsync("Stop training error: Instance is null.", LogLevel.Error);
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
    

    [HttpGet("result/{imageSize}")]
    public IActionResult GetTrainingResult([FromRoute] ImageSize imageSize)
    {
        var instance = SingletonAiDuo.GetInstance(imageSize);
        Dictionary<string, float> trainingResult = instance.GetTrainingResult();

        return Ok(trainingResult);
    }

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

    //[HttpGet("classify/{imageSize}")]
    //public IActionResult Classify([FromBody] string[] imagePaths, [FromRoute] ImageSize imageSize) =>
    //    //Console.WriteLine($"ImagePaths: {imagePaths}");
    //    //var instance = SingletonAiDuo.GetInstance(imageSize);
    //    //if(instance == null)
    //    //{
    //    //    return BadRequest("Invalid image size.");
    //    //}
    //    //instance.Classify(imagePaths);
    //    Ok("OK");

    //[HttpGet("save/{imageSize}")]
    //public async Task<IActionResult> SaveModel([FromRoute] ImageSize imageSize, [FromQuery] string modelFilePath, [FromQuery] string settingsFilePath)
    //{
    //    try
    //    {
    //        var instance = SingletonAiDuo.GetInstance(imageSize);
    //        await instance.SaveModel(modelFilePath, );
    //        //instance.SaveSettings(settingsFilePath);
    //        return Ok("OK");
    //    }
    //    catch (Exception e)
    //    {
    //        Console.WriteLine(e);
    //        return BadRequest(e.Message);
    //    }
    //}

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