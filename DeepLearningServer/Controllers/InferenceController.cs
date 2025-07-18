﻿using DeepLearningServer.Classes;
using DeepLearningServer.Dtos;
using Euresys.Open_eVision.EasyDeepLearning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// 딥러닝 모델 추론 기능을 제공하는 컨트롤러입니다.
/// </summary>
namespace DeepLearningServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InferenceController : ControllerBase
    {
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
        public IActionResult PostSingle([FromBody] InferenceDto inferenceDto)
        {
            if (!ToolStatusManager.IsProcessRunning())
            {
                ToolStatusManager.SetProcessRunning(true);
                try
                {
                    Console.WriteLine($"Starting single image inference with model: {inferenceDto.ModelPath}");
                    Console.WriteLine($"Image path: {inferenceDto.ImagePath}");

                    using (var inferenceAi = new InferenceAi(inferenceDto.ModelPath))
                    {
                        var result = inferenceAi.ClassifySingleImage(inferenceDto.ImagePath);
                        Console.WriteLine($"Best label: {result.BestLabel}, Best probability: {result.BestProbability}");
                        return Ok(new { BestLabel = result.BestLabel, BestProbability = result.BestProbability });
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error in single inference: {e.Message}");
                    Console.WriteLine($"Stack trace: {e.StackTrace}");
                    return BadRequest(new { Error = "Single inference failed", Message = e.Message });
                }
                finally
                {
                    ToolStatusManager.SetProcessRunning(false);
                    // 가비지 컬렉션 강제 실행
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }
            return BadRequest(new { Error = "Another process is already running" });
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
        public IActionResult PostMulti([FromBody] MultiInferenceDto inferenceDto)
        {
            if (!ToolStatusManager.IsProcessRunning())
            {
                ToolStatusManager.SetProcessRunning(true);
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

                    using (var inferenceAi = new InferenceAi(inferenceDto.ModelPath))
                    {
                        var results = inferenceAi.ClassifyMultipleImages(inferenceDto.ImagePaths);

                        if (results == null)
                        {
                            return BadRequest(new { Error = "Classification returned null results" });
                        }

                        // 명시적 DTO 사용
                        var response = results.Select((result, index) => 
                        {
                            try
                            {
                                return new ClassificationResultDto
                                {
                                    BestLabel = result.BestLabel,
                                    BestProbability = result.BestProbability,
                                    // NumLabels 만큼 순회하여 각 레이블의 이름과 확률을 담음
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
                                Console.WriteLine($"Error processing result at index {index}: {ex.Message}");
                                return new ClassificationResultDto
                                {
                                    BestLabel = "ERROR",
                                    BestProbability = 0.0f,
                                    LabelProbabilities = new LabelProbabilityDto[0]
                                };
                            }
                        }).ToList();

                        Console.WriteLine($"Successfully processed {response.Count} images");
                        return Ok(response); // JSON 응답 반환
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error in multi inference: {e.Message}");
                    Console.WriteLine($"Stack trace: {e.StackTrace}");
                    return BadRequest(new { Error = "Multi inference failed", Message = e.Message, StackTrace = e.StackTrace });
                }
                finally
                {
                    ToolStatusManager.SetProcessRunning(false);
                    // 가비지 컬렉션 강제 실행
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    Console.WriteLine("Multi inference process completed and resources cleaned up");
                }
            }
            return BadRequest(new { Error = "Another process is already running" });
        }
    }
}
