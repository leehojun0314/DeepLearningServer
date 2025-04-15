using DeepLearningServer.Dtos;
using DeepLearningServer.Settings;
using Euresys.Open_eVision.EasyDeepLearning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using SharpCompress.Common;

/// <summary>
/// 딥러닝 모델 관리 기능을 제공하는 컨트롤러입니다.
/// </summary>
namespace DeepLearningServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ModelController(IOptions<ServerSettings> serverSettings) : ControllerBase
    {
        private readonly ServerSettings _serverSettings = serverSettings.Value;

        /// <summary>
        /// 딥러닝 모델 파일을 서버에 업로드합니다.
        /// </summary>
        /// <param name="uploadModelDto">
        /// 모델 업로드 데이터:
        /// - ModelPath: 서버에 저장될 모델 경로 (상대 경로)
        /// - File: 업로드할 모델 파일
        /// </param>
        /// <returns>업로드 성공 메시지와 저장된 파일 경로</returns>
        /// <response code="200">모델 업로드 성공</response>
        /// <response code="400">파일이 비어있거나 경로가 지정되지 않음</response>
        /// <response code="500">서버 내부 오류</response>
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public IActionResult Post([FromForm] UploadModelDto uploadModelDto)
        {
            try
            {
                string modelPath = $"{_serverSettings.ModelDirectory}\\{uploadModelDto.ModelPath}";
                Console.WriteLine("Model path: " + modelPath);
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

        /// <summary>
        /// 기존 모델을 최신 버전으로 마이그레이션합니다.
        /// </summary>
        /// <param name="modelMigrations">
        /// 모델 마이그레이션 데이터:
        /// - OldModelsPath: 기존 모델 파일이 저장된 폴더 경로
        /// - NewModelsPath: 업그레이드된 모델을 저장할 폴더 경로
        /// - ProjectDir: 임시 프로젝트 폴더 경로 (마이그레이션 중 사용됨)
        /// </param>
        /// <returns>마이그레이션 완료 메시지와 업데이트된 파일 수</returns>
        /// <response code="200">모델 마이그레이션 성공</response>
        /// <response code="500">마이그레이션 중 오류 발생</response>
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

                foreach (string modelFile in modelFiles)
                {
                    string fileName = Path.GetFileName(modelFile);
                    EDeepLearningProject project;
                    project = new EDeepLearningProject();
                    try
                    {
                        project.Type = EDeepLearningToolType.EasyClassify;
                        project.Name = "modelUpgrade";
                        project.ProjectDirectory = projectDir;
                        Console.WriteLine("Saving project...");
                        project.SaveProject();
                        Console.WriteLine("Saved project.");
                        int toolIndex = 0;
                        Console.WriteLine("Upgrading tool: " + modelFile);

                        //project.Save(projectDir);
                        Console.WriteLine("importing tool...");
                        project.ImportTool($"Tool{toolIndex}", modelFile);

                        Console.WriteLine("Updating project file structure...");
                        project.UpdateProjectFileStructure();

                        string newModelPath = Path.Combine(newModelsPath, Path.GetFileName(modelFile));
                        Console.WriteLine("New model path: " + newModelPath);
                        EDeepLearningTool newTool = project.GetToolCopy(toolIndex);

                        //toolIndex++;
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
                    finally
                    {
                        project.Dispose();
                        Directory.Delete(projectDir, true);
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
