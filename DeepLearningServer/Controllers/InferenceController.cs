using DeepLearningServer.Classes;
using DeepLearningServer.Dtos;
using Euresys.Open_eVision.EasyDeepLearning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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

        [HttpPost("single")]
        public IActionResult PostSingle([FromBody] InferenceDto inferenceDto)
        {
            if (!ToolStatusManager.IsProcessRunning())
            {
                ToolStatusManager.SetProcessRunning(true);
                try
                {
                    InferenceAi inferenceAi = new InferenceAi(inferenceDto.ModelPath);
                    string imagePaths = inferenceDto.ImagePath;
                    EClassificationResult result = inferenceAi.ClassifySingleImage(imagePaths);
                    Console.WriteLine($"Best label: {result.BestLabel}, Best probability: {result.BestProbability}");
                    return Ok(new { BestLabel = result.BestLabel, BestProbability = result.BestProbability });
                }
                catch (Exception e)
                {
                    return BadRequest(e.Message);
                }
                finally
                {
                    ToolStatusManager.SetProcessRunning(false);
                }
            }
            return Ok("Inference Controller");
        }

        [HttpPost("multi")]
        public IActionResult PostMulti([FromBody] MultiInferenceDto inferenceDto)
        {
            if (!ToolStatusManager.IsProcessRunning())
            {
                ToolStatusManager.SetProcessRunning(true);
                try
                {
                    InferenceAi inferenceAi = new InferenceAi(inferenceDto.ModelPath);
                    string[] imagePaths = inferenceDto.ImagePaths;
                    EClassificationResult[] results = inferenceAi.ClassifyMultipleImages(imagePaths);

                    // 명시적 DTO 사용
                    var response = results.Select(result => new ClassificationResultDto
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
                    }).ToList();

                    return Ok(response); // JSON 응답 반환
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error: " + e.Message);
                    Console.WriteLine("StackTrace: " + e.StackTrace);
                    return BadRequest(e.Message);
                }
                finally
                {
                    ToolStatusManager.SetProcessRunning(false);
                }
            }
            return Ok("Inference Controller");
        }
    }
}
