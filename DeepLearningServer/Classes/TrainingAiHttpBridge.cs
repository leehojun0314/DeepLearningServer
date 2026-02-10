using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeepLearningServer.Dtos;
using Newtonsoft.Json;

namespace DeepLearningServer.Classes
{
  /// <summary>
  /// Training callback delegate (matches Euresys TrainingAI.TrainCallback signature)
  /// </summary>
  /// <param name="isTraining">Whether training is still running</param>
  /// <param name="progress">Progress ratio (0.0 to 1.0)</param>
  /// <param name="bestIteration">Best epoch/iteration number</param>
  /// <param name="currentAccuracy">Current epoch accuracy</param>
  /// <param name="bestAccuracy">Best accuracy achieved</param>
  /// <param name="bestValidationAccuracy">Best validation accuracy</param>
  /// <param name="bestValidationError">Best validation error</param>
  public delegate Task TrainCallback(
      bool isTraining,
      float progress,
      int bestIteration,
      float currentAccuracy,
      float bestAccuracy,
      float bestValidationAccuracy,
      float bestValidationError
  );

  /// <summary>
  /// HTTP-based training bridge that replaces Euresys EClassifier for training.
  /// Communicates with Python FastAPI server (train.py) for PyTorch-based training.
  /// 
  /// Usage:
  /// <code>
  /// using var bridge = new TrainingAiHttpBridge("http://localhost:8000");
  /// bridge.SetParameters(param);
  /// await bridge.TrainAsync(callback, cts.Token);
  /// var result = bridge.GetTrainingResult();
  /// await bridge.SaveModelAsync(localPath);
  /// </code>
  /// </summary>
  public class TrainingAiHttpBridge : IDisposable
  {
    private static TrainingAiHttpBridge? _currentInstance;
    private static readonly object _instanceLock = new();

    private readonly HttpClient _client;
    private readonly string _baseUrl;
    private readonly TimeSpan _pollInterval;

    private PyTrainingParameters? _parameters;
    private PyClsTrainRequest? _lastRequest;
    private PyTrainStatusResponse? _lastStatus;
    private string? _dataPath;
    private string? _outPath;
    private string? _bestModelPath;
    private string[]? _categories;
    private List<PyCategorySource>? _categorySources;
    private string? _tempImageSessionDir;
    private bool _isTraining;
    private CancellationTokenSource? _cts;

    /// <summary>
    /// Currently active bridge instance for external stop requests.
    /// </summary>
    public static TrainingAiHttpBridge? CurrentInstance
    {
      get
      {
        lock (_instanceLock)
        {
          return _currentInstance;
        }
      }
    }

    /// <summary>
    /// Set or clear currently active bridge instance.
    /// </summary>
    public static void SetCurrentInstance(TrainingAiHttpBridge? instance)
    {
      lock (_instanceLock)
      {
        _currentInstance = instance;
      }
    }

    /// <summary>
    /// Create a new HTTP training bridge
    /// </summary>
    /// <param name="baseUrl">Python server base URL (e.g., "http://localhost:8000")</param>
    /// <param name="timeout">HTTP request timeout</param>
    /// <param name="pollInterval">Status polling interval during training</param>
    public TrainingAiHttpBridge(
        string baseUrl = "http://localhost:8000",
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null)
    {
      _baseUrl = baseUrl.TrimEnd('/');
      _pollInterval = pollInterval ?? TimeSpan.FromSeconds(1);
      _client = new HttpClient
      {
        Timeout = timeout ?? TimeSpan.FromMinutes(5)
      };
    }

    #region Configuration

    /// <summary>
    /// Set training parameters (Euresys-compatible interface)
    /// </summary>
    public void SetParameters(PyTrainingParameters param)
    {
      _parameters = param;
    }

    /// <summary>
    /// Set data and output paths
    /// </summary>
    public void SetPaths(string dataPath, string outPath)
    {
      _dataPath = dataPath;
      _outPath = outPath;
      _bestModelPath = null;
    }

    /// <summary>
    /// Set data/output paths and explicit best model path.
    /// </summary>
    public void SetPaths(string dataPath, string outPath, string bestModelPath)
    {
      _dataPath = dataPath;
      _outPath = outPath;
      _bestModelPath = bestModelPath;
    }

    /// <summary>
    /// Set categories for classification
    /// </summary>
    public void SetCategories(string[] categories)
    {
      _categories = categories;
    }

    #endregion

    #region Image Loading (Compatibility)

    /// <summary>
    /// Load images for training (compatibility method - actual loading happens on server)
    /// </summary>
    /// <param name="categories">Category names to train on</param>
    /// <param name="imagePath">Root path containing images</param>
    /// <returns>Estimated image count (actual count determined by server)</returns>
    public Task<int> LoadImagesAsync(string[] categories, string imagePath)
    {
      _categories = categories;
      _dataPath = imagePath;

      // Image loading is handled by the Python server
      // Just count files locally for estimation
      int count = 0;
      try
      {
        if (Directory.Exists(imagePath))
        {
          foreach (var cat in categories)
          {
            var catPath = Path.Combine(imagePath, cat);
            if (Directory.Exists(catPath))
            {
              count += Directory.GetFiles(catPath, "*.jpg", SearchOption.AllDirectories).Length;
              count += Directory.GetFiles(catPath, "*.png", SearchOption.AllDirectories).Length;
            }
          }
        }
      }
      catch (Exception ex)
      {
        // File system errors are non-critical for image count estimation
        Console.WriteLine($"[TrainingAiHttpBridge] Image count estimation warning: {ex.Message}");
      }

      Console.WriteLine($"[TrainingAiHttpBridge] Prepared {count} images from {imagePath}");
      return Task.FromResult(count);
    }

    /// <summary>
    /// Load images with explicit category/path mapping (Euresys-compatible layout).
    /// </summary>
    /// <param name="categories">NG category names</param>
    /// <param name="processNames">OK process names</param>
    /// <param name="imagePath">Root image directory</param>
    /// <returns>Estimated image count</returns>
    public Task<int> LoadImagesAsync(string[] categories, string[] processNames, string imagePath, string? tempImageDir = null)
    {
      _categories = categories;
      _dataPath = imagePath;
      _categorySources = new List<PyCategorySource>();

      int count = 0;
      bool useTempCopy = !string.IsNullOrWhiteSpace(tempImageDir);

      CleanupTempImages();
      if (useTempCopy)
      {
        string today = DateTime.Now.ToString("yyyyMMdd");
        _tempImageSessionDir = Path.Combine(tempImageDir!, today, Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempImageSessionDir);
        Console.WriteLine($"[TrainingAiHttpBridge] Using temp image session dir: {_tempImageSessionDir}");
      }

      // NG categories
      foreach (var cat in categories)
      {
        var paths = new List<string>();
        var basePath = Path.Combine(imagePath, "NG", "BASE", cat.ToUpper());
        var newPath = Path.Combine(imagePath, "NG", "NEW", cat.ToUpper());
        var baseExists = Directory.Exists(basePath);
        var newExists = Directory.Exists(newPath);
        Console.WriteLine($"[TrainingAiHttpBridge] Checking NG paths for '{cat}': {basePath} (exists={baseExists}), {newPath} (exists={newExists})");
        if (baseExists) paths.Add(basePath);
        if (newExists) paths.Add(newPath);

        if (paths.Count > 0)
        {
          var targetPaths = paths;
          if (useTempCopy)
          {
            targetPaths = CopyCategoryImagesToTemp(
                paths,
                Path.Combine(_tempImageSessionDir!, "NG", cat.ToUpperInvariant()));
          }

          _categorySources.Add(new PyCategorySource
          {
            Label = cat.ToUpper(),
            Paths = targetPaths
          });
          count += CountImagesInPaths(targetPaths);
        }
      }

      // OK categories (merge all processes under label "OK")
      var okPaths = new List<string>();
      foreach (var proc in processNames)
      {
        var basePath = Path.Combine(imagePath, "OK", proc, "BASE");
        var newPath = Path.Combine(imagePath, "OK", proc, "NEW");
        var baseExists = Directory.Exists(basePath);
        var newExists = Directory.Exists(newPath);
        Console.WriteLine($"[TrainingAiHttpBridge] Checking OK paths for '{proc}': {basePath} (exists={baseExists}), {newPath} (exists={newExists})");
        if (baseExists) okPaths.Add(basePath);
        if (newExists) okPaths.Add(newPath);
      }

      if (okPaths.Count > 0)
      {
        var targetOkPaths = okPaths;
        if (useTempCopy)
        {
          targetOkPaths = CopyCategoryImagesToTemp(
              okPaths,
              Path.Combine(_tempImageSessionDir!, "OK"));
        }

        _categorySources.Add(new PyCategorySource
        {
          Label = "OK",
          Paths = targetOkPaths
        });
        count += CountImagesInPaths(targetOkPaths);
      }

      Console.WriteLine($"[TrainingAiHttpBridge] Prepared {count} images from {imagePath}");
      return Task.FromResult(count);
    }

    private static int CountImagesInPaths(IEnumerable<string> paths)
    {
      int count = 0;
      foreach (var p in paths)
      {
        if (!Directory.Exists(p))
          continue;
        count += Directory.GetFiles(p, "*.jpg", SearchOption.AllDirectories).Length;
        count += Directory.GetFiles(p, "*.png", SearchOption.AllDirectories).Length;
      }
      return count;
    }

    private static List<string> CopyCategoryImagesToTemp(IEnumerable<string> sourcePaths, string tempTargetRoot)
    {
      var copiedPaths = new List<string>();
      Directory.CreateDirectory(tempTargetRoot);

      foreach (var sourcePath in sourcePaths)
      {
        if (!Directory.Exists(sourcePath))
        {
          continue;
        }

        var parentName = Path.GetFileName(Path.GetDirectoryName(sourcePath) ?? string.Empty);
        var leafName = Path.GetFileName(sourcePath);
        var destinationPath = string.IsNullOrEmpty(parentName)
            ? Path.Combine(tempTargetRoot, leafName)
            : Path.Combine(tempTargetRoot, parentName, leafName);

        Directory.CreateDirectory(destinationPath);

        foreach (var file in EnumerateImageFiles(sourcePath))
        {
          var relativePath = Path.GetRelativePath(sourcePath, file);
          var destinationFile = Path.Combine(destinationPath, relativePath);
          var destinationFileDir = Path.GetDirectoryName(destinationFile);
          if (!string.IsNullOrEmpty(destinationFileDir))
          {
            Directory.CreateDirectory(destinationFileDir);
          }

          File.Copy(file, destinationFile, true);
        }

        copiedPaths.Add(destinationPath);
      }

      return copiedPaths;
    }

    /// <summary>
    /// Remove temporary image folder copied for Python training.
    /// </summary>
    public void CleanupTempImages()
    {
      if (!string.IsNullOrEmpty(_tempImageSessionDir) && Directory.Exists(_tempImageSessionDir))
      {
        try
        {
          Directory.Delete(_tempImageSessionDir, true);
          Console.WriteLine($"[TrainingAiHttpBridge] Temp image folder deleted: {_tempImageSessionDir}");
        }
        catch (Exception ex)
        {
          Console.WriteLine($"[TrainingAiHttpBridge] Temp image folder cleanup failed: {ex.Message}");
        }
      }
    }

    /// <summary>
    /// Build training image records for DB storage/metrics (Euresys-compatible shape)
    /// </summary>
    public List<(string imagePath, string trueLabel, string status, string? category, int? admsProcessId)> GetTrainingImageRecords(
        Dictionary<string, int> processAdmsMapping)
    {
      var records = new List<(string imagePath, string trueLabel, string status, string? category, int? admsProcessId)>();
      if (_categorySources == null || _categorySources.Count == 0)
      {
        return records;
      }

      foreach (var source in _categorySources)
      {
        if (source.Paths == null)
        {
          continue;
        }

        foreach (var path in source.Paths)
        {
          if (!Directory.Exists(path))
          {
            continue;
          }

          foreach (var file in EnumerateImageFiles(path))
          {
            var status = ResolveStatusFromPath(file);

            if (source.Label.Equals("OK", StringComparison.OrdinalIgnoreCase))
            {
              int? admsProcessId = null;
              foreach (var mapping in processAdmsMapping)
              {
                if (file.Contains(mapping.Key, StringComparison.OrdinalIgnoreCase))
                {
                  admsProcessId = mapping.Value;
                  break;
                }
              }

              records.Add((file, "OK", status, null, admsProcessId));
            }
            else
            {
              var label = source.Label.ToUpperInvariant();
              records.Add((file, label, status, label, null));
            }
          }
        }
      }

      return records;
    }

    private static IEnumerable<string> EnumerateImageFiles(string path)
    {
      foreach (var file in Directory.GetFiles(path, "*.jpg", SearchOption.AllDirectories))
      {
        yield return file;
      }

      foreach (var file in Directory.GetFiles(path, "*.png", SearchOption.AllDirectories))
      {
        yield return file;
      }
    }

    private static string ResolveStatusFromPath(string path)
    {
      if (path.Contains($"{Path.DirectorySeparatorChar}BASE{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
          path.Contains("/BASE/", StringComparison.OrdinalIgnoreCase))
      {
        return "Base";
      }

      if (path.Contains($"{Path.DirectorySeparatorChar}NEW{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
          path.Contains("/NEW/", StringComparison.OrdinalIgnoreCase))
      {
        return "New";
      }

      return "Unknown";
    }

    #endregion

    #region Training

    /// <summary>
    /// Start training asynchronously with progress callback
    /// </summary>
    public async Task TrainAsync(TrainCallback? callback, CancellationToken ct = default)
    {
      if (_parameters == null)
        throw new InvalidOperationException("Parameters not set. Call SetParameters first.");
      if (string.IsNullOrEmpty(_outPath))
        throw new InvalidOperationException("Output path not set. Call SetPaths first.");
      if (_categorySources == null || _categorySources.Count == 0)
        throw new InvalidOperationException(
            $"No image directories found. " +
            $"LoadImagesAsync was called but no matching folders exist under '{_dataPath}'. " +
            "Expected structure: NG/BASE|NEW/{CATEGORY}, OK/{processName}/BASE|NEW");

      _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

      // Build request
      bool grayInput = _parameters.GrayInput ||
                       _parameters.Classifier.GrayInput ||
                       _parameters.Classifier.ImageChannels == 1 ||
                       _parameters.Classifier.ImageChannels == 2;
      bool addFft = _parameters.AddFft ||
                    _parameters.Classifier.AddFft ||
                    _parameters.Classifier.ImageChannels == 2 ||
                    _parameters.Classifier.ImageChannels == 4;
      if (string.IsNullOrEmpty(_bestModelPath))
      {
        _bestModelPath = Path.Combine(_outPath!, "best.onnlmodel");
      }

      _lastRequest = new PyClsTrainRequest
      {
        Out = _outPath!,
        BestModelPath = _bestModelPath,
        ImgSize = _parameters.Classifier.ImageWidth,
        Epochs = _parameters.Iterations,
        BatchSize = _parameters.Classifier.BatchSize,
        ValSplit = _parameters.ValidationProportion,
        Patience = _parameters.EarlyStoppingPatience,
        AddFft = addFft,
        GrayInput = grayInput,
        GeomAug = _parameters.Geometry,
        ColorAug = _parameters.Color,
        NoiseAug = _parameters.Noise,
        CategorySources = _categorySources
      };

      // Start training
      Console.WriteLine($"[TrainingAiHttpBridge] Starting training: {_baseUrl}/train/cls/start");
      var startResponse = await PostAsync<PyTrainStartResponse>("/train/cls/start", _lastRequest);

      if (startResponse?.Result != "started")
      {
        throw new InvalidOperationException($"Failed to start training: {startResponse?.Result}");
      }

      _isTraining = true;
      Console.WriteLine("[TrainingAiHttpBridge] Training started, polling for progress...");

      // Poll for progress
      int bestIteration = 0;
      while (!_cts.Token.IsCancellationRequested)
      {
        try
        {
          _lastStatus = await GetAsync<PyTrainStatusResponse>("/train/cls/status");

          if (_lastStatus != null)
          {
            // Track best iteration
            if (_lastStatus.CurrentAccuracy >= _lastStatus.BestAccuracy && _lastStatus.CurrentEpoch > 0)
            {
              bestIteration = _lastStatus.CurrentEpoch;
            }

            // Invoke callback
            if (callback != null)
            {
              await callback(
                  _lastStatus.Running,
                  _lastStatus.Progress,
                  bestIteration,
                  _lastStatus.CurrentAccuracy,
                  _lastStatus.BestAccuracy,
                  _lastStatus.BestAccuracy, // validation accuracy
                  1f - _lastStatus.BestAccuracy // validation error
              );
            }

            // Check if training completed
            if (!_lastStatus.Running)
            {
              Console.WriteLine("[TrainingAiHttpBridge] Training completed");
              break;
            }
          }
        }
        catch (Exception ex)
        {
          Console.WriteLine($"[TrainingAiHttpBridge] Status poll error: {ex.Message}");
        }

        try
        {
          await Task.Delay(_pollInterval, _cts.Token);
        }
        catch (OperationCanceledException)
        {
          break;
        }
      }

      _isTraining = false;

      // After training completes, fetch final status to ensure BestModel is populated
      if (!_cts.Token.IsCancellationRequested)
      {
        try
        {
          var finalStatus = await GetAsync<PyTrainStatusResponse>("/train/cls/status");
          if (finalStatus != null)
          {
            _lastStatus = finalStatus;
            Console.WriteLine($"[TrainingAiHttpBridge] Final status - BestModel: {finalStatus.BestModel ?? "(null)"}, BestAccuracy: {finalStatus.BestAccuracy}");
          }
        }
        catch (Exception ex)
        {
          Console.WriteLine($"[TrainingAiHttpBridge] Final status fetch warning: {ex.Message}");
        }
      }

      // If cancelled, stop training on server
      if (_cts.Token.IsCancellationRequested)
      {
        try
        {
          await StopTrainingAsync();
        }
        catch (Exception ex)
        {
          // Stop errors during cancellation are logged but not propagated
          Console.WriteLine($"[TrainingAiHttpBridge] Stop during cancellation warning: {ex.Message}");
        }
      }
    }

    /// <summary>
    /// Stop training (synchronous, for compatibility)
    /// </summary>
    public void StopTraining()
    {
      _cts?.Cancel();
      try
      {
        StopTrainingAsync().GetAwaiter().GetResult();
      }
      catch (Exception ex)
      {
        Console.WriteLine($"[TrainingAiHttpBridge] Stop error: {ex.Message}");
      }
    }

    /// <summary>
    /// Stop training asynchronously
    /// </summary>
    public async Task<PyTrainStopResponse?> StopTrainingAsync()
    {
      Console.WriteLine("[TrainingAiHttpBridge] Stopping training...");
      var response = await PostAsync<PyTrainStopResponse>("/train/cls/stop", new { });
      _isTraining = false;
      Console.WriteLine($"[TrainingAiHttpBridge] Stop result: {response?.Result}");
      return response;
    }

    /// <summary>
    /// Check if training is currently running
    /// </summary>
    public bool IsTraining()
    {
      // Quick check from last status
      if (_lastStatus != null && !_lastStatus.Running)
        return false;

      // Otherwise check server
      try
      {
        var status = GetAsync<PyTrainStatusResponse>("/train/cls/status").GetAwaiter().GetResult();
        _lastStatus = status;
        return status?.Running ?? false;
      }
      catch (Exception ex)
      {
        // Server unreachable; fallback to cached state
        Console.WriteLine($"[TrainingAiHttpBridge] IsTraining check failed, using cached state: {ex.Message}");
        return _isTraining;
      }
    }

    #endregion

    #region Results

    /// <summary>
    /// Get training results (Euresys-compatible interface, synchronous)
    /// </summary>
    public Dictionary<string, float> GetTrainingResult()
    {
      try
      {
        return GetTrainingResultAsync().GetAwaiter().GetResult();
      }
      catch (Exception ex)
      {
        // Fallback to cached status when server is unreachable
        Console.WriteLine($"[TrainingAiHttpBridge] GetTrainingResult failed, using cached status: {ex.Message}");
        var result = new Dictionary<string, float>();
        if (_lastStatus != null)
        {
          result["weightedAccuracy"] = _lastStatus.BestAccuracy;
          result["weightedError"] = 1f - _lastStatus.BestAccuracy;
        }
        return result;
      }
    }

    /// <summary>
    /// Get training results from server (Euresys GetTrainingResult() compatible)
    /// Returns: weightedAccuracy, weightedError, okAccuracy, okError, {category}Accuracy, {category}Error
    /// </summary>
    public async Task<Dictionary<string, float>> GetTrainingResultAsync()
    {
      var response = await GetAsync<Dictionary<string, object>>("/train/cls/result");
      var result = new Dictionary<string, float>();

      if (response != null)
      {
        foreach (var kvp in response)
        {
          if (kvp.Value is double d)
            result[kvp.Key] = (float)d;
          else if (kvp.Value is float f)
            result[kvp.Key] = f;
          else if (float.TryParse(kvp.Value?.ToString(), out float parsed))
            result[kvp.Key] = parsed;
        }
      }

      return result;
    }

    /// <summary>
    /// Get the path to the best model checkpoint.
    /// Falls back to /train/cls/result endpoint if status doesn't have it.
    /// </summary>
    public string? GetBestModelPath()
    {
      if (!string.IsNullOrEmpty(_bestModelPath))
        return _bestModelPath;

      var path = _lastStatus?.BestModel;
      if (!string.IsNullOrEmpty(path))
        return path;

      // Fallback: try to get best_model from the result endpoint
      try
      {
        Console.WriteLine("[TrainingAiHttpBridge] BestModel not in status, trying result endpoint...");
        var result = GetAsync<Dictionary<string, object>>("/train/cls/result").GetAwaiter().GetResult();
        if (result != null && result.TryGetValue("best_model", out var bestModelObj) && bestModelObj is string bestModelStr && !string.IsNullOrEmpty(bestModelStr))
        {
          Console.WriteLine($"[TrainingAiHttpBridge] Got BestModel from result endpoint: {bestModelStr}");
          return bestModelStr;
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"[TrainingAiHttpBridge] Failed to get BestModel from result endpoint: {ex.Message}");
      }

      // Fallback: try to get from stop endpoint (which also reports best_model)
      try
      {
        Console.WriteLine("[TrainingAiHttpBridge] Trying status endpoint one more time...");
        var status = GetAsync<PyTrainStatusResponse>("/train/cls/status").GetAwaiter().GetResult();
        if (status != null && !string.IsNullOrEmpty(status.BestModel))
        {
          _lastStatus = status;
          Console.WriteLine($"[TrainingAiHttpBridge] Got BestModel from re-fetched status: {status.BestModel}");
          return status.BestModel;
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"[TrainingAiHttpBridge] Failed to re-fetch status: {ex.Message}");
      }

      Console.WriteLine("[TrainingAiHttpBridge] WARNING: Could not obtain BestModel path from any source");
      return null;
    }

    /// <summary>
    /// Get training log from server
    /// </summary>
    public async Task<string> GetLogAsync()
    {
      var response = await GetAsync<PyTrainLogResponse>("/train/cls/log");
      return response?.Log ?? string.Empty;
    }

    #endregion

    #region Confusion Matrix (Euresys GetConfusion compatible)

    /// <summary>
    /// Get confusion count for specific class pair (Euresys GetConfusion compatible, synchronous)
    /// </summary>
    /// <param name="trueClass">True class name</param>
    /// <param name="predictedClass">Predicted class name</param>
    /// <returns>Count of samples where trueClass was predicted as predictedClass</returns>
    public uint GetConfusion(string trueClass, string predictedClass)
    {
      try
      {
        return GetConfusionAsync(trueClass, predictedClass).GetAwaiter().GetResult();
      }
      catch
      {
        return 0;
      }
    }

    /// <summary>
    /// Get confusion count for specific class pair (async)
    /// </summary>
    public async Task<uint> GetConfusionAsync(string trueClass, string predictedClass)
    {
      var request = new { true_class = trueClass, predicted_class = predictedClass };
      var response = await PostAsync<PyConfusionResponse>("/train/cls/confusion", request);
      return (uint)(response?.Count ?? 0);
    }

    /// <summary>
    /// Get full confusion matrix
    /// </summary>
    public async Task<PyConfusionMatrixResult?> GetConfusionMatrixAsync()
    {
      var request = new { true_class = (string?)null, predicted_class = (string?)null };
      var response = await PostAsync<PyConfusionMatrixResponse>("/train/cls/confusion", request);

      if (response?.Confusion == null)
        return null;

      return new PyConfusionMatrixResult
      {
        Confusion = response.Confusion,
        Classes = response.Classes ?? new List<string>(),
      };
    }

    #endregion

    #region Inference (Euresys Classify compatible)

    /// <summary>
    /// Classify a single image (Euresys Classify compatible, synchronous)
    /// </summary>
    /// <param name="imagePath">Path to image file</param>
    /// <returns>Classification result with bestLabel, bestScore, allScores</returns>
    public ClassificationResult Classify(string imagePath)
    {
      try
      {
        return ClassifyAsync(imagePath).GetAwaiter().GetResult();
      }
      catch (Exception ex)
      {
        Console.WriteLine($"[TrainingAiHttpBridge] Classify failed for {imagePath}: {ex.Message}");
        return new ClassificationResult
        {
          BestLabel = null,
          BestScore = 0f,
          AllScores = new Dictionary<string, float>(),
        };
      }
    }

    /// <summary>
    /// Classify a single image (async)
    /// </summary>
    /// <param name="imagePath">Path to image file</param>
    /// <param name="weightsPath">Optional model weights path (uses last trained if null)</param>
    public async Task<ClassificationResult> ClassifyAsync(string imagePath, string? weightsPath = null)
    {
      var request = new { image_path = imagePath, weights = weightsPath };
      var response = await PostAsync<PyClassifyResponse>("/infer/cls/single", request);

      if (response == null || !string.IsNullOrEmpty(response.Error))
      {
        throw new InvalidOperationException(response?.Error ?? "Classification failed");
      }

      return new ClassificationResult
      {
        BestLabel = response.BestLabel,
        BestScore = response.BestScore,
        AllScores = response.AllScores ?? new Dictionary<string, float>(),
      };
    }

    #endregion

    #region Model Export

    /// <summary>
    /// Save/export model to .onnlmodel format
    /// </summary>
    /// <param name="outputPath">Destination path for .onnlmodel</param>
    /// <param name="checkpointPath">Source checkpoint path (null = use best from training)</param>
    public async Task<string> SaveModelAsync(string outputPath, string? checkpointPath = null)
    {
      var weights = checkpointPath ?? _lastStatus?.BestModel;
      if (string.IsNullOrEmpty(weights))
      {
        throw new InvalidOperationException("No model checkpoint available. Train first or specify checkpointPath.");
      }

      Console.WriteLine($"[TrainingAiHttpBridge] Exporting model: {weights} -> {outputPath}");

      var request = new PyOnnlPackRequest
      {
        Weights = weights,
        OutPath = outputPath
      };

      var response = await PostAsync<PyOnnlPackResponse>("/export/cls/onnl_pack", request);

      if (!string.IsNullOrEmpty(response?.Error))
      {
        throw new InvalidOperationException($"Export failed: {response.Error}");
      }

      Console.WriteLine($"[TrainingAiHttpBridge] Model exported to: {response?.Path}");
      return response?.Path ?? outputPath;
    }

    #endregion

    #region HTTP Helpers

    private async Task<T?> GetAsync<T>(string endpoint) where T : class
    {
      var url = _baseUrl + endpoint;
      var response = await _client.GetAsync(url);
      response.EnsureSuccessStatusCode();
      var json = await response.Content.ReadAsStringAsync();
      return JsonConvert.DeserializeObject<T>(json);
    }

    private async Task<T?> PostAsync<T>(string endpoint, object body) where T : class
    {
      var url = _baseUrl + endpoint;
      var json = JsonConvert.SerializeObject(body);
      var content = new StringContent(json, Encoding.UTF8, "application/json");
      var response = await _client.PostAsync(url, content);
      response.EnsureSuccessStatusCode();
      var responseJson = await response.Content.ReadAsStringAsync();
      return JsonConvert.DeserializeObject<T>(responseJson);
    }

    #endregion

    #region Health Check

    /// <summary>
    /// Check if the Python training server is available
    /// </summary>
    public async Task<bool> IsServerAvailableAsync()
    {
      try
      {
        var response = await _client.GetAsync(_baseUrl + "/health");
        return response.IsSuccessStatusCode;
      }
      catch
      {
        return false;
      }
    }

    /// <summary>
    /// Wait for server to become available
    /// </summary>
    public async Task WaitForServerAsync(TimeSpan timeout, CancellationToken ct = default)
    {
      var deadline = DateTime.UtcNow + timeout;
      while (DateTime.UtcNow < deadline && !ct.IsCancellationRequested)
      {
        if (await IsServerAvailableAsync())
        {
          Console.WriteLine("[TrainingAiHttpBridge] Server is available");
          return;
        }
        await Task.Delay(500, ct);
      }
      throw new TimeoutException($"Training server at {_baseUrl} did not become available within {timeout}");
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
      _cts?.Cancel();
      _cts?.Dispose();
      CleanupTempImages();
      _client.Dispose();
    }

    #endregion
  }
}
