﻿using DeepLearningServer.Dtos;
using DeepLearningServer.Settings;
using Euresys.Open_eVision.EasyDeepLearning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using SharpCompress.Common;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DeepLearningServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ModelController(IOptions<ServerSettings> serverSettings) : ControllerBase
    {
        private readonly ServerSettings _serverSettings = serverSettings.Value;

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]

        public IActionResult Post([FromForm] UploadModelDto uploadModelDto)
        {
            try
            {
                string modelPath = $"{_serverSettings.ModelDirectory}\\{uploadModelDto.ModelPath}";
                if (modelPath == null)
                {
                    return BadRequest("Model path not given");
                }
                var file = uploadModelDto.File;
                string directoryPath = Path.GetDirectoryName(modelPath);
                // 디렉토리가 존재하지 않으면 생성
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                if (file.Length > 0)
                {
                    string filePath = Path.Combine(modelPath);
                    Console.WriteLine("Model upload file path: " + filePath);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        file.CopyTo(stream);
                    }
                    Console.WriteLine("Model upload compelete.");
                    return Ok(new { Message = "Model upload compelete.", FilePath = filePath });
                }
                return BadRequest("File does not exist.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error on uploading model", Error = ex.Message });
            }
        }
        [HttpPost("migrate")]
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
                        Console.WriteLine($"Error on upgrading model. {modelFile}, Error: {ex.Message}");
                        throw new Exception(ex.Message);
                    }
                }

                return Ok(new { Message = "All models upgrade complete.", UpdatedFiles = modelFiles.Length });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error on upgrading model.", Error = ex.Message });
            }
        }

    }
}
