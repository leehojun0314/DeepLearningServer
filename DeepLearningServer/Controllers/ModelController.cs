using Euresys.Open_eVision.EasyDeepLearning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

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
                string projectDir = "D:\\ModelUpgradeProject\\project";
                // 새 폴더가 없으면 생성
                if (!Directory.Exists(newModelsPath))
                {
                    Directory.CreateDirectory(newModelsPath);
                }

                // 기존 모델 파일들 가져오기
                string[] modelFiles = Directory.GetFiles(oldModelsPath, "*.edltool");
                if (Directory.Exists(projectDir))
                {
                    Directory.Delete(projectDir, true);
                }
                Console.WriteLine("Creating project...");
                // 새 프로젝트 생성
                EDeepLearningProject project;

               
                    project = new EDeepLearningProject();
                    project.Type = EDeepLearningToolType.EasyClassify;
                    project.Name = "modelUpgrade";
                    project.ProjectDirectory = projectDir;
                    project.SaveProject();
                    Console.WriteLine("Saved project.");
                    //EDeepLearningProject project = new EDeepLearningProject();
                    // 모델 파일 업그레이드
                    //EDeepLearningTool tool = EDeepLearningTool.Create(modelFile);
                    //project.ImportTool(fileName, tool);
                //project.Name = "modelUpgrade.edlproj";
                
                int toolIndex = 0;
                foreach (string modelFile in modelFiles)
                {
                    string fileName = Path.GetFileName(modelFile);
                    try
                    {
                        Console.WriteLine("Upgrading tool: " + modelFile);

                        //project.Save(projectDir);
                        Console.WriteLine("importing tool...");
                        project.ImportTool($"Tool{toolIndex}", modelFile);
                        Console.WriteLine("saving project...");
                        Console.WriteLine("Save 2...");
                        //project.Save(projectDir);
                        //if (project.HasFileStructureUpdates())
                        //{
                        //Console.WriteLine("The project has file structure updates");
                        Console.WriteLine("UpdateProjectFileStructure");
                        project.UpdateProjectFileStructure();
                        //project.SaveProject();
                        string newModelPath = Path.Combine(newModelsPath, Path.GetFileName(modelFile));
                        Console.WriteLine("New model path: " + newModelPath);
                        EDeepLearningTool newTool = project.GetToolCopy(toolIndex);
                        toolIndex++;
                        Console.WriteLine("Saving model...");
                        newTool.SaveTrainingModel(newModelPath);
                        Console.WriteLine("Mode saved");
                        //project.Dispose();
                        //newTool.Dispose();
                        //Directory.Delete(projectDir);
                        //}
                        //else
                        //{
                        //Console.WriteLine("The project doesn't have file structure updates");
                        //}

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"모델 업그레이드 실패: {modelFile}, 오류: {ex.Message}");
                        throw new Exception(ex.Message);
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
