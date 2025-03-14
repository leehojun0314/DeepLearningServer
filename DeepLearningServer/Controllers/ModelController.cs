﻿using DeepLearningServer.Dtos;
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
        [HttpPost]
        public IActionResult Post([FromBody] MigrationDto modelMigrations)
        {
            try
            {
                //string oldModelsPath = "D:\\ModelUpgradeProject\\old";  // 기존 모델 폴더
                //string newModelsPath = "D:\\ModelUpgradeProject\\new"; // 새로운 모델 저장 폴더
                //string projectDir = "D:\\ModelUpgradeProject\\project";
                string oldModelsPath = modelMigrations.OldModelsPath;  // 기존 모델 폴더
                string newModelsPath = modelMigrations.NewModelsPath; // 새로운 모델 저장 폴더
                string projectDir = modelMigrations.ProjectDir;
                // 새 폴더가 없으면 생성
                if (!Directory.Exists(newModelsPath))
                {
                    Directory.CreateDirectory(newModelsPath);
                }

                // 기존 모델 파일들 가져오기
                string[] modelFiles = Directory.GetFiles(oldModelsPath, "*.edltool");
                if (Directory.Exists(projectDir))
                {
                    // 기존 프로젝트 폴더 삭제
                    Directory.Delete(projectDir, true);
                }
                Console.WriteLine("Creating project...");
                // 새 프로젝트 생성
                EDeepLearningProject project;
                project = new EDeepLearningProject();
                project.Type = EDeepLearningToolType.EasyClassify;
                project.Name = "modelUpgrade";
                project.ProjectDirectory = projectDir;
                Console.WriteLine("Saving project...");
                project.SaveProject();
                Console.WriteLine("Saved project.");
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

                        Console.WriteLine("Updating project file structure...");
                        project.UpdateProjectFileStructure();

                        string newModelPath = Path.Combine(newModelsPath, Path.GetFileName(modelFile));
                        Console.WriteLine("New model path: " + newModelPath);
                        EDeepLearningTool newTool = project.GetToolCopy(toolIndex);

                        toolIndex++;
                        Console.WriteLine("Saving model...");
                        //newTool.SaveTrainingModel(newModelPath);

                        newTool.Save(newModelPath, true);
                        Console.WriteLine("Mode saved");
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
