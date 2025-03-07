using Euresys.Open_eVision.EasyDeepLearning;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DeepLearningServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ModelController : ControllerBase
    {
        // GET: api/<ModelController>
        [HttpGet]
        public IActionResult Get()
        {
            //EDeepLearningProject project = new EDeepLearningProject();
            //string projectPath = "D:\\ModelUpgradeProject\\project.edlproj";
            //project.Save(projectPath);
            //project.AddTool("D:/tool.edltool");
            //if (project.HasFileStructureUpdates())
            //{
            //    project.UpdateProjectFileStructure();

            //    project.SaveProject();
            //}
            //return new string[] { "value1", "value2" };
            try
            {
                string oldModelsPath = "D:\\ModelUpgradeProject\\old";  // 기존 모델 폴더
                string newModelsPath = "D:\\ModelUpgradeProject\\new"; // 새로운 모델 저장 폴더

                // 새 폴더가 없으면 생성
                if (!Directory.Exists(newModelsPath))
                {
                    Directory.CreateDirectory(newModelsPath);
                }

                // 기존 모델 파일들 가져오기
                string[] modelFiles = Directory.GetFiles(oldModelsPath, "*.edltool");

                foreach (string modelFile in modelFiles)
                {
                    string fileName = Path.GetFileName(modelFile);
                    try
                    {
                        Console.WriteLine("Upgrading tool: " + modelFile);
                        // 새 프로젝트 생성
                        EDeepLearningProject project = new EDeepLearningProject();
                        string projectPath = Path.Combine(newModelsPath, "project.edlproj");
                        project.Save(projectPath);

                        // 모델 파일 업그레이드
                        project.ImportTool(modelFile, Path.Combine(newModelsPath, fileName));
                        
                        if (project.HasFileStructureUpdates())
                        {
                            Console.WriteLine("The project has file structure updates");
                            project.UpdateProjectFileStructure();
                            project.SaveProject();
                            string newModelPath = Path.Combine(newModelsPath, Path.GetFileName(modelFile));
                            EDeepLearningTool tool = project.GetToolCopy(0);
                            tool.SaveTrainingModel(newModelPath);
                        }
                        else
                        {
                            Console.WriteLine("The project doesn't have file structure updates");
                        }
                       
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"모델 업그레이드 실패: {modelFile}, 오류: {ex.Message}");
                    }
                }

                return Ok(new { Message = "모든 모델 업그레이드 완료", UpdatedFiles = modelFiles.Length });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "업그레이드 중 오류 발생", Error = ex.Message });
            }
        }

    }
}
