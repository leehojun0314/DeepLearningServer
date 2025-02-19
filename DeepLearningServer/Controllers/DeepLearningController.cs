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
using Euresys.Open_eVision;

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


    [HttpPost("run")]
    public async Task<IActionResult> CreateToolAndRun([FromBody] CreateAndRunModel parameterData)
    {
        try
        {

            // 🔹 상태 파일 확인
            if (ToolStatusManager.IsProcessRunning())
            {
                return BadRequest("The tool is already running.");
            }

            // 🔹 실행 상태를 파일에 기록
            ToolStatusManager.SetProcessRunning(true);

            // 🔹 Check license (오래 걸릴 수 있음)
            bool isRunning = await _mssqlDbService.CheckIsTraining();
            if (isRunning)
            {
                ToolStatusManager.SetProcessRunning(false); // 실패 시 상태 해제
                return BadRequest("The tool is already running.");
            }
            //Console.WriteLine("Checking license..");
            //bool hasLicense = Easy.CheckLicense(Euresys.Open_eVision.LicenseFeatures.Features.EasyClassify);
            //Console.WriteLine($"Has license: {hasLicense}");
            //if (!hasLicense) throw new Exception("No license found");
            //_mssqlDbService.InsertLogAsync("create tool and run called", LogLevel.Information);
            await _mssqlDbService.InsertLogAsync("Create tool and run called", LogLevel.Information);
        // Validate required fields
        if (parameterData.AdmsProcessId == -1)
            return BadRequest(new NewRecord("AdmsProcessId is required."));

        TrainingRecord record = _mapper.Map<TrainingRecord>(parameterData);
        //check if instance is already exist
        if (SingletonAiDuo.GetInstance(parameterData.ImageSize) != null)
        {
            //_mssqlDbService.InsertLogAsync("Instance already exists", LogLevel.Debug);
            if (SingletonAiDuo.GetInstance(parameterData.ImageSize).IsTraining())
            {
                return BadRequest("The tool is already running.");
            }
            return BadRequest(new NewRecord("Instance already exists."));
        }
        //await _mssqlDbService.InsertLogAsync("Initializing instance", LogLevel.Debug);
        await _mssqlDbService.InsertLogAsync("Initializing instance", LogLevel.Debug);
        var instance = SingletonAiDuo.CreateInstance(parameterData, _serverSettings);
        await _mssqlDbService.InsertLogAsync("Initialized instance", LogLevel.Debug);


        await _mssqlDbService.InsertLogAsync("Setting parameters", LogLevel.Debug);
        instance.SetParameters();
        await _mssqlDbService.InsertLogAsync("Parameters set", LogLevel.Debug);
        await _mssqlDbService.InsertLogAsync("Start model traning", LogLevel.Information);
        instance.recordId = record.Id;
        Dictionary<string, int> dictionary = await _mssqlDbService.GetAdmsProcessInfo(parameterData.AdmsProcessId);
        string processName = await _mssqlDbService.GetProcessNameById(dictionary["processId"]);
        Adm adms = await _mssqlDbService.GetAdmsById(dictionary["admsId"]);
            Console.WriteLine("Record id: " + record.Id);
        _ = Task.Run(async () =>
        {
            await RunOnStaThread(() =>
            {
                // 이 부분에서 Euresys 관련 작업 실행
                try
                {
                    _mssqlDbService.InsertLogAsync("Loading Images", LogLevel.Debug).GetAwaiter().GetResult();
                    Console.WriteLine("Loading images...");
                    int numImages = 0;
                    TimeSpan elapsedTime = MeasureExecutionTime.Measure(() =>
                    {
                        numImages = instance.LoadImages(processName);
                    });
                    if (numImages == 0)
                    {
                        return;
                    }
                    
                    Console.WriteLine($"Loaded {numImages} images");
                    Console.WriteLine($"Elapsed time: {elapsedTime.TotalSeconds} seconds");
                    _mssqlDbService.InsertLogAsync($"Images loaded. Count: {numImages}", LogLevel.Debug).GetAwaiter().GetResult();

                    // Train 메서드도 STA 환경에서 실행됨
                    instance.Train((isTraining, progress, bestIteration) =>
                    {
                        Console.WriteLine($"is training: {isTraining}");
                        Console.WriteLine($"progress: {progress}");
                        Console.WriteLine($"best iteration: {bestIteration}");
                        _mssqlDbService.InsertLogAsync("training ", LogLevel.Trace).GetAwaiter().GetResult();
                        _mssqlDbService.InsertLogAsync($"progress: {progress}", LogLevel.Trace).GetAwaiter().GetResult();

                        var updates = new Dictionary<string, object>
                    {
                        { "Status", TrainingStatus.Running },
                        { "Progress", progress },
                        { "BestIteration", bestIteration }
                    };

                        var newEntry = new ProgressEntry
                        {
                            IsTraining = isTraining,
                            Progress = isTraining ? progress : 1,
                            BestIteration = bestIteration,
                            Timestamp = DateTime.UtcNow
                        };

                        //_mongoDbService.PartialUpdateTraining(updates, record.Id).GetAwaiter().GetResult();
                        _mssqlDbService.PartialUpdateTrainingAsync(record.Id, updates).GetAwaiter().GetResult();

                        //_mssqlDbService.PushProgressEntry(record.Id, newEntry).GetAwaiter().GetResult();
                        _mssqlDbService.PushProgressEntryAsync(record.Id, newEntry).GetAwaiter().GetResult();
                    }).GetAwaiter().GetResult();
                    string timeStamp = DateTime.UtcNow.ToString("yyyyMMdd");
                    string savePath = $@"D:\Models\{adms.Name}\{processName}\{timeStamp}\";
                    if (!Directory.Exists(savePath))
                    {
                        Directory.CreateDirectory(savePath);
                    }
                    instance.SaveModel(savePath + "trainingModel.edltool", adms.LocalIp);

                    //instance.SaveSettings(savePath + "trainingsettings.settings");
                   
                    _mssqlDbService.InsertLogAsync("Model training finished", LogLevel.Information).GetAwaiter().GetResult();
                    //_mongoDbService.PartialUpdateTraining(new Dictionary<string, object> { { "Status", TrainingStatus.Completed } }, record.Id).GetAwaiter().GetResult();
                    _mssqlDbService.PartialUpdateTrainingAsync(record.Id, new Dictionary<string, object> { { "Status", TrainingStatus.Completed }, { "EndTime" , DateTime.UtcNow } }).GetAwaiter().GetResult();
                    Dictionary<string, float> trainingResults = instance.GetTrainingResult();
                    //_mongoDbService.UpdateLablesById(record.Id, trainingResult).GetAwaiter().GetResult();
                    var labelList = trainingResults.Select(kvp => new Label
                    {
                        Name = kvp.Key,        // 레이블 이름 (예: "okAccuracy", "weightedError" 등)
                        Accuracy = kvp.Value,     // 해당 레이블의 값
                        TrainingRecordId = record.Id  // 연관된 TrainingRecord의 Id
                    }).ToArray();
                    Console.WriteLine($"label list: {labelList.ToString()}");
                    _mssqlDbService.UpdateLabelsByIdAsync(record.Id, labelList).GetAwaiter().GetResult();
                    instance.StopTraining();
                    SingletonAiDuo.Reset(parameterData.ImageSize);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    ToolStatusManager.SetProcessRunning(false);
                    _mssqlDbService.InsertLogAsync($"Error occurred: {e.Message}", LogLevel.Error).GetAwaiter().GetResult();
                    _mssqlDbService.PartialUpdateTrainingAsync(record.Id, new Dictionary<string, object> { { "Status", TrainingStatus.Failed } }).GetAwaiter().GetResult();
                    instance.StopTraining();
                    SingletonAiDuo.Reset(parameterData.ImageSize);
                    throw;
                }
            });
        });
            //_mongoDbService.InsertTraining(record).GetAwaiter();
            record.Status = TrainingStatus.Running;
            record.StartTime = DateTime.UtcNow;
            await _mssqlDbService.InsertTrainingAsync(record);
        Console.WriteLine("Training record inserted");
        Console.WriteLine($"Record id: {record.Id}");
            ToolStatusManager.SetProcessRunning(false);
        return Ok(new
        {
            Message = "Training initialized successfully.",
            TrainingId = record.Id.ToString()
        });
        }
            catch(Exception error)
        {
            ToolStatusManager.SetProcessRunning(false);
            Console.WriteLine(error.Message);
            _mssqlDbService.InsertLogAsync(error.Message, LogLevel.Error);
            throw;
        }
    }

    [HttpDelete("stop/{imageSize}")]
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
    
    [HttpGet("status/{imageSize}")]
    public async Task<IActionResult> GetStatus([FromRoute] ImageSize imageSize)
    {
        await _mssqlDbService.InsertLogAsync("Get status called", LogLevel.Information);
        var instance = SingletonAiDuo.GetInstance(imageSize);
        if (instance == null)
        {
            await _mssqlDbService.InsertLogAsync("GetStatus error: Instance is null.", LogLevel.Error);
            return BadRequest(new NewRecord("Instance is null."));
        }

        Dictionary<string, float> status = instance.GetStatus();
        await _mssqlDbService.InsertLogAsync("status retrieved", LogLevel.Debug);
        Console.WriteLine(status);

        return Ok(status);
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