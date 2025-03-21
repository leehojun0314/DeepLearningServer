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
                    EClassificationResult[] result = inferenceAi.ClassifyMultipleImages(imagePaths);
                    var dic = new Dictionary<string, object>();
                    foreach (var res in result)
                    {
                        Console.WriteLine($"Best label: {res.BestLabel}, Best probability: {res.BestProbability}");
                        dic.Add("BestLabel", res.BestLabel);
                        dic.Add("BestProbability", res.BestProbability);

                    }
                    return Ok(dic);
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
    }
}
