using DeepLearningServer.Classes;
using DeepLearningServer.Enums;
using DeepLearningServer.Models;
using DeepLearningServer.Services;
using DeepLearningServer.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using AutoMapper;
using MongoDB.Bson;

/// <summary>
/// Represents a controller for handling deep learning-related API requests.
/// </summary>
namespace DeepLearningServer.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DeepLearningController : ControllerBase
{
    private readonly MongoDbService _mongoDbService;
    private readonly ServerSettings _serverSettings;
    private readonly IMapper _mapper;

    private ObjectId _recordId;

    // Constructor injection for ServerSettings
    public DeepLearningController(IOptions<ServerSettings> serverSettings, MongoDbService mongoDbService,
        IMapper mapper)
    {
        _serverSettings = serverSettings.Value;
        _mongoDbService = mongoDbService;
        _mapper = mapper;
    }

    [HttpPost("run")]
    public async Task<IActionResult> CreateToolAndRun([FromBody] CreateAndRunModel parameterData)
    {
        _mongoDbService.InsertLog("create tool and run called", LogLevel.Information);
        // Validate required fields
        if (string.IsNullOrWhiteSpace(parameterData.RecipeId))
            return BadRequest(new NewRecord("RecipeId is required."));

        if (string.IsNullOrWhiteSpace(parameterData.ProcessId))
            return BadRequest(new NewRecord("ProcessId is required."));

        TrainingRecord record = _mapper.Map<TrainingRecord>(parameterData);
        //check if instance is already exist
        if (SingletonAiDuo.GetInstance(parameterData.ImageSize) != null)
        {
            _mongoDbService.InsertLog("Instance already exists", LogLevel.Debug);
            return BadRequest(new NewRecord("Instance already exists."));
        }
        await _mongoDbService.InsertLog("Initializing instance", LogLevel.Debug);
        var instance = SingletonAiDuo.CreateInstance(parameterData, _serverSettings);
        await _mongoDbService.InsertLog("Initialized instance", LogLevel.Debug);


        await _mongoDbService.InsertLog("Setting parameters", LogLevel.Debug);
        instance.SetParameters();
        await _mongoDbService.InsertLog("Parameters set", LogLevel.Debug);
        await _mongoDbService.InsertLog("Start model traning", LogLevel.Information);
        instance.recordId = record.Id;
        _recordId = record.Id;
        Console.WriteLine("Record id: " + record.Id);
        _ = Task.Run(async () =>
        {
            await RunOnStaThread(() =>
            {
                // 이 부분에서 Euresys 관련 작업 실행
                try
                {
                    _mongoDbService.InsertLog("Loading Images", LogLevel.Debug).GetAwaiter().GetResult();
                    Console.WriteLine("Loading images...");
                    int numImages = instance.LoadImages();
                    Console.WriteLine($"Loaded {numImages} images");
                    _mongoDbService.InsertLog($"Images loaded. Count: {numImages}", LogLevel.Debug).GetAwaiter().GetResult();

                    // Train 메서드도 STA 환경에서 실행됨
                    instance.Train((isTraining, progress, bestIteration, learningRateParameters) =>
                    {
                        Console.WriteLine($"is training: {isTraining}");
                        Console.WriteLine($"progress: {progress}");
                        Console.WriteLine($"best iteration: {bestIteration}");
                        Console.WriteLine($"learning rate: {learningRateParameters}");
                        _mongoDbService.InsertLog("training ", LogLevel.Trace).GetAwaiter().GetResult();
                        _mongoDbService.InsertLog($"progress: {progress}", LogLevel.Trace).GetAwaiter().GetResult();

                        var updates = new Dictionary<string, object>
                    {
                        { "Status", TrainingStatus.Running },
                        { "Progress", progress },
                        { "BestIteration", bestIteration },
                        { "LearningRate", learningRateParameters }
                    };

                        var newEntry = new ProgressEntry
                        {
                            IsTraining = isTraining,
                            Progress = progress,
                            BestIteration = bestIteration,
                            LearningRateParameters = learningRateParameters,
                            Timestamp = DateTime.UtcNow
                        };

                        _mongoDbService.PartialUpdateTraining(updates, record.Id).GetAwaiter().GetResult();
                        _mongoDbService.PushProgressEntry(record.Id, newEntry).GetAwaiter().GetResult();
                    }).GetAwaiter().GetResult();

                    _mongoDbService.InsertLog("Model training finished", LogLevel.Information).GetAwaiter().GetResult();
                    _mongoDbService.PartialUpdateTraining(new Dictionary<string, object> { { "Status", TrainingStatus.Completed } }, record.Id).GetAwaiter().GetResult();
                    Dictionary<string, float> trainingResult = instance.GetTrainingResult();
                    _mongoDbService.UpdateLablesById(record.Id, trainingResult).GetAwaiter().GetResult();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    _mongoDbService.InsertLog($"Error occurred: {e.Message}", LogLevel.Error).GetAwaiter().GetResult();
                    _mongoDbService.PartialUpdateTraining(new Dictionary<string, object> { { "Status", TrainingStatus.Failed } }, _recordId).GetAwaiter().GetResult();
                    SingletonAiDuo.Reset(parameterData.ImageSize);
                    throw;
                }
            });
        });
        _mongoDbService.InsertTraining(record).GetAwaiter();
        Console.WriteLine("Training record inserted");
        Console.WriteLine($"Record id: {record.Id}");
        return Ok(new
        {
            Message = "Training initialized successfully.",
            TrainingId = record.Id.ToString()
        });
    }

    [HttpDelete("stop/{imageSize}")]
    public IActionResult StopTraining([FromRoute] ImageSize imageSize)
    {
        try
        {
            _mongoDbService.InsertLog("Stop training called", LogLevel.Information);
            var instance = SingletonAiDuo.GetInstance(imageSize);

            if (instance == null)
            {
                _mongoDbService.InsertLog("Stop training error: Instance is null.", LogLevel.Error);
                return BadRequest(new NewRecord("Instance is null."));
            }

            instance.StopTraining();
            SingletonAiDuo.Reset(imageSize);
            _mongoDbService.InsertLog("Training stopped", LogLevel.Debug);
            return Ok("Processing completed successfully.");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    [HttpGet("status/{imageSize}")]
    public IActionResult GetStatus([FromRoute] ImageSize imageSize)
    {
        _mongoDbService.InsertLog("Get status called", LogLevel.Information);
        var instance = SingletonAiDuo.GetInstance(imageSize);
        if (instance == null)
        {
            _mongoDbService.InsertLog("GetStatus error: Instance is null.", LogLevel.Error);
            return BadRequest(new NewRecord("Instance is null."));
        }

        Dictionary<string, float> status = instance.GetStatus();
        _mongoDbService.InsertLog("status retrieved", LogLevel.Debug);
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

    [HttpGet("dispose/{imageSize}")]
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

    [HttpGet("classify/{imageSize}")]
    public IActionResult Classify([FromBody] string[] imagePaths, [FromRoute] ImageSize imageSize)
    {
        Console.WriteLine($"ImagePaths: {imagePaths}");
        var instance = SingletonAiDuo.GetInstance(imageSize);
        if(instance == null)
        {
            return BadRequest("Invalid image size.");
        }
        instance.Classify(imagePaths);
        return Ok("OK");
    }

    [HttpGet("save/{imageSize}")]
    public IActionResult SaveModel([FromRoute] ImageSize imageSize, [FromQuery] string modelFilePath, [FromQuery] string settingsFilePath)
    {
        try
        {
            var instance = SingletonAiDuo.GetInstance(imageSize);
            instance.SaveModel(modelFilePath);
            instance.SaveSettings(settingsFilePath);
            return Ok("OK");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return BadRequest(e.Message);
        }
    }

    [HttpGet("load/{imageSize}")]
    public IActionResult LoadModel([FromRoute] ImageSize imageSize, [FromQuery] string modelFilePath, [FromQuery] string settingsFilePath)
    {
        try
        {
            var instance = SingletonAiDuo.GetInstance(imageSize);
            if (instance != null)
            {
                instance.LoadModel(modelFilePath);
                instance.LoadSettings(settingsFilePath);
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

        Thread staThread = new Thread(() =>
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