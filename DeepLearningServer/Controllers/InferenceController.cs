using DeepLearningServer.Classes;
using DeepLearningServer.Dtos;
using DeepLearningServer.Settings;
using Euresys.Open_eVision.EasyDeepLearning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Diagnostics;

/// <summary>
/// 딥러닝 모델 추론 기능을 제공하는 컨트롤러입니다.
/// </summary>
namespace DeepLearningServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InferenceController(IOptions<ServerSettings> serverSettings) : ControllerBase
    {
        private readonly ServerSettings _serverSettings = serverSettings.Value;

        //[HttpPost]
        //public IActionResult Post([FromBody] InferenceDto inferenceDto)
        //{
        //    if (!ToolStatusManager.IsProcessRunning())
        //    {
        //        ToolStatusManager.SetProcessRunning(true);
        //        try
        //        {
        //            InferenceAi inferenceAi = new InferenceAi(inferenceDto.ModelPath);
        //            string[] imagePaths = inferenceDto.ImagePaths;
        //            EClassificationResult[] results = inferenceAi.ClassifyMultipleImages(imagePaths);
        //            foreach(var result in results)
        //            {
        //                Console.WriteLine($"Class : {result.BestLabel}, Score : {result.BestProbability}");
        //            }
        //            return Ok(results);
        //        }
        //        catch (Exception e)
        //        {
        //            return BadRequest(e.Message);
        //        }
        //        finally
        //        {
        //            ToolStatusManager.SetProcessRunning(false);
        //        }
        //    }
        //    return Ok("Inference Controller");
        //}

        /// <summary>
        /// 단일 이미지를 모델을 사용하여 분류합니다.
        /// </summary>
        /// <param name="inferenceDto">
        /// 추론을 위한 데이터:
        /// - ModelPath: 추론에 사용할 모델 파일 경로
        /// - ImagePath: 분류할 단일 이미지 파일 경로
        /// </param>
        /// <returns>최적 레이블과 확률 값 반환</returns>
        /// <response code="200">추론 성공</response>
        /// <response code="400">모델 로드 또는 추론 과정에서 오류 발생</response>
        [HttpPost("single")]
        public async Task<IActionResult> PostSingle([FromBody] InferenceDto inferenceDto)
        {
            try
            {
                Console.WriteLine($"Starting single image inference with model: {inferenceDto.ModelPath}");
                Console.WriteLine($"Image path: {inferenceDto.ImagePath}");
                if (!System.IO.File.Exists(inferenceDto.ImagePath))
                {
                    return BadRequest(new { Error = $"Image file not found: {inferenceDto.ImagePath}" });
                }

                var stopwatch = Stopwatch.StartNew();
                if (_serverSettings.UsePythonServer)
                {
                    using var bridge = new TrainingAiHttpBridge(_serverSettings.PyTrainingServerUrl);
                    var result = await bridge.ClassifyAsync(inferenceDto.ImagePath, inferenceDto.ModelPath);
                    stopwatch.Stop();

                    var elapsedMs = stopwatch.ElapsedMilliseconds;
                    Console.WriteLine($"Best label: {result.BestLabel}, Best score: {result.BestScore}");
                    Console.WriteLine($"Single inference elapsed: {elapsedMs} ms");

                    Response.Headers["X-Inference-Duration-ms"] = elapsedMs.ToString();
                    return Ok(new
                    {
                        BestLabel = result.BestLabel,
                        BestProbability = result.BestScore,
                        ElapsedMilliseconds = elapsedMs,
                        LabelProbabilities = result.AllScores ?? new Dictionary<string, float>()
                    });
                }
                else
                {
                    using var inferenceAi = new InferenceAi(inferenceDto.ModelPath);
                    var result = inferenceAi.ClassifySingleImage(inferenceDto.ImagePath);
                    stopwatch.Stop();

                    var elapsedMs = stopwatch.ElapsedMilliseconds;
                    Console.WriteLine($"Best label: {result.BestLabel}, Best probability: {result.BestProbability}");
                    Console.WriteLine($"Single inference elapsed: {elapsedMs} ms");

                    Response.Headers["X-Inference-Duration-ms"] = elapsedMs.ToString();
                    Dictionary<string, float> labelProbabilities = new Dictionary<string, float>();
                    for (int i = 0; i < result.NumLabels; i++)
                    {
                        string label = result.GetLabel(i);
                        float probability = result.GetProbability(label);
                        labelProbabilities[label] = probability;
                    }
                    return Ok(new { BestLabel = result.BestLabel, BestProbability = result.BestProbability, ElapsedMilliseconds = elapsedMs, LabelProbabilities = labelProbabilities });
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"Error in single inference: {e.Message}");
                return BadRequest(new { Error = "Single inference failed" });
            }
            finally
            {
                // 가비지 컬렉션 강제 실행
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        /// <summary>
        /// 여러 이미지를 모델을 사용하여 일괄 분류합니다.
        /// </summary>
        /// <param name="inferenceDto">
        /// 추론을 위한 데이터:
        /// - ModelPath: 추론에 사용할 모델 파일 경로
        /// - ImagePaths: 분류할 다중 이미지 파일 경로 배열
        /// </param>
        /// <returns>각 이미지별 분류 결과와 레이블별 확률값 목록 반환</returns>
        /// <response code="200">일괄 추론 성공</response>
        /// <response code="400">모델 로드 또는 추론 과정에서 오류 발생</response>
        [HttpPost("multi")]
        public async Task<IActionResult> PostMulti([FromBody] MultiInferenceDto inferenceDto)
        {
            try
            {
                Console.WriteLine($"Starting multi image inference with model: {inferenceDto.ModelPath}");
                Console.WriteLine($"Number of images: {inferenceDto.ImagePaths?.Length ?? 0}");

                if (inferenceDto.ImagePaths == null || inferenceDto.ImagePaths.Length == 0)
                {
                    return BadRequest(new { Error = "Image paths array is null or empty" });
                }

                // 이미지 경로 유효성 사전 검사
                for (int i = 0; i < inferenceDto.ImagePaths.Length; i++)
                {
                    if (string.IsNullOrEmpty(inferenceDto.ImagePaths[i]))
                    {
                        return BadRequest(new { Error = $"Image path at index {i} is null or empty" });
                    }
                    if (!System.IO.File.Exists(inferenceDto.ImagePaths[i]))
                    {
                        return BadRequest(new { Error = $"Image file not found at index {i}: {inferenceDto.ImagePaths[i]}" });
                    }
                }

                var stopwatch = Stopwatch.StartNew();
                if (_serverSettings.UsePythonServer)
                {
                    using var bridge = new TrainingAiHttpBridge(_serverSettings.PyTrainingServerUrl);
                    var tasks = inferenceDto.ImagePaths.Select(path => bridge.ClassifyAsync(path, inferenceDto.ModelPath));
                    var results = await Task.WhenAll(tasks);
                    stopwatch.Stop();

                    var response = results.Select((result, index) =>
                    {
                        try
                        {
                            return new ClassificationResultDto
                            {
                                BestLabel = result.BestLabel,
                                BestProbability = result.BestScore,
                                LabelProbabilities = (result.AllScores ?? new Dictionary<string, float>())
                                  .Select(kvp =>
                                  {
                                      return new LabelProbabilityDto
                                      {
                                          Label = kvp.Key,
                                          Probability = kvp.Value
                                      };
                                  }).ToArray()
                            };
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error processing result at index {index}: {ex.Message}");
                            return new ClassificationResultDto
                            {
                                BestLabel = "ERROR",
                                BestProbability = 0.0f,
                                LabelProbabilities = new LabelProbabilityDto[0]
                            };
                        }
                    }).ToList();

                    var elapsedMs = stopwatch.ElapsedMilliseconds;
                    Console.WriteLine($"Successfully processed {response.Count} images");
                    Console.WriteLine($"Multi inference elapsed: {elapsedMs} ms");

                    Response.Headers["X-Inference-Duration-ms"] = elapsedMs.ToString();
                    return Ok(response); // JSON 응답 반환
                }
                else
                {
                    using var inferenceAi = new InferenceAi(inferenceDto.ModelPath);
                    var results = inferenceAi.ClassifyMultipleImages(inferenceDto.ImagePaths);
                    stopwatch.Stop();

                    if (results == null)
                    {
                        return BadRequest(new { Error = "Classification returned null results" });
                    }

                    var response = results.Select((result, index) =>
                    {
                        try
                        {
                            return new ClassificationResultDto
                            {
                                BestLabel = result.BestLabel,
                                BestProbability = result.BestProbability,
                                LabelProbabilities = Enumerable.Range(0, result.NumLabels)
                                  .Select(i =>
                                  {
                                      string label = result.GetLabel(i);
                                      return new LabelProbabilityDto
                                      {
                                          Label = label,
                                          Probability = result.GetProbability(label)
                                      };
                                  }).ToArray()
                            };
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error processing result at index {index}: {ex.Message}");
                            return new ClassificationResultDto
                            {
                                BestLabel = "ERROR",
                                BestProbability = 0.0f,
                                LabelProbabilities = new LabelProbabilityDto[0]
                            };
                        }
                    }).ToList();

                    var elapsedMs = stopwatch.ElapsedMilliseconds;
                    Console.WriteLine($"Successfully processed {response.Count} images");
                    Console.WriteLine($"Multi inference elapsed: {elapsedMs} ms");

                    Response.Headers["X-Inference-Duration-ms"] = elapsedMs.ToString();
                    return Ok(response);
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"Error in multi inference: {e.Message}");
                return BadRequest(new { Error = "Multi inference failed" });
            }
            finally
            {
                // 가비지 컬렉션 강제 실행
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Console.WriteLine("Multi inference process completed and resources cleaned up");
            }
        }
    }
}
