using DeepLearningServer.Dtos;
using DeepLearningServer.Settings;
using Euresys.Open_eVision.EasyDeepLearning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using SharpCompress.Common;
using System.Text.RegularExpressions;

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
        /// 저장된 딥러닝 모델 파일들을 조회합니다.
        /// </summary>
        /// <param name="size">이미지 크기 필터 (LARGE, MIDDLE, 또는 전체 조회시 생략)</param>
        /// <param name="type">모델 타입 필터 (BASE, Release, EVALUATION, 또는 전체 조회시 생략)</param>
        /// <param name="admsName">ADMS 이름 필터 (특정 ADMS만 조회시 사용)</param>
        /// <param name="processId">프로세스 ID 필터 (특정 프로세스만 조회시 사용)</param>
        /// <returns>필터 조건에 맞는 모델 파일 목록</returns>
        /// <response code="200">모델 조회 성공</response>
        /// <response code="400">잘못된 필터 조건</response>
        /// <response code="500">서버 내부 오류</response>
        [HttpGet("list")]
        public IActionResult GetModels(
            [FromQuery] string? size = null,
            [FromQuery] string? type = null, 
            [FromQuery] string? admsName = null,
            [FromQuery] string? processId = null)
        {
            try
            {
                var validSizes = new[] { "LARGE", "MIDDLE" };
                var validTypes = new[] { "BASE", "Release", "EVALUATION" };

                // 파라미터 유효성 검사
                if (!string.IsNullOrEmpty(size) && !validSizes.Contains(size.ToUpper()))
                {
                    return BadRequest($"Invalid size parameter. Valid values: {string.Join(", ", validSizes)}");
                }

                if (!string.IsNullOrEmpty(type) && !validTypes.Contains(type))
                {
                    return BadRequest($"Invalid type parameter. Valid values: {string.Join(", ", validTypes)}");
                }

                var models = new List<ModelInfoDto>();
                string baseModelDirectory = _serverSettings.ModelDirectory;

                // 경로 구조: D:/Transfer/{SIZE}/{TYPE}/{AdmsName}/{ProcessId}.edltool
                var searchSizes = string.IsNullOrEmpty(size) ? validSizes : new[] { size.ToUpper() };
                var searchTypes = string.IsNullOrEmpty(type) ? validTypes : new[] { type };

                foreach (var searchSize in searchSizes)
                {
                    foreach (var searchType in searchTypes)
                    {
                        string searchPath = Path.Combine(baseModelDirectory, searchSize, searchType);
                        
                        if (!Directory.Exists(searchPath))
                            continue;

                        // ADMS 폴더들 탐색
                        var admsDirectories = Directory.GetDirectories(searchPath);
                        
                        foreach (var admsDir in admsDirectories)
                        {
                            var currentAdmsName = Path.GetFileName(admsDir);
                            
                            // ADMS 이름 필터링
                            if (!string.IsNullOrEmpty(admsName) && !currentAdmsName.Equals(admsName, StringComparison.OrdinalIgnoreCase))
                                continue;

                            // .edltool 파일들 찾기
                            var modelFiles = Directory.GetFiles(admsDir, "*.edltool");
                            
                            foreach (var modelFile in modelFiles)
                            {
                                var fileInfo = new FileInfo(modelFile);
                                var fileName = fileInfo.Name;
                                var currentProcessId = Path.GetFileNameWithoutExtension(fileName);
                                
                                // 프로세스 ID 필터링
                                if (!string.IsNullOrEmpty(processId) && !currentProcessId.Equals(processId, StringComparison.OrdinalIgnoreCase))
                                    continue;

                                var relativePath = Path.GetRelativePath(baseModelDirectory, modelFile);
                                
                                var modelDto = new ModelInfoDto
                                {
                                    FileName = fileName,
                                    FullPath = modelFile,
                                    RelativePath = relativePath.Replace('\\', '/'),
                                    Size = searchSize,
                                    Type = searchType,
                                    AdmsName = currentAdmsName,
                                    ProcessId = currentProcessId,
                                    FileSizeBytes = fileInfo.Length,
                                    FileSizeFormatted = FormatFileSize(fileInfo.Length),
                                    CreatedDate = fileInfo.CreationTime,
                                    ModifiedDate = fileInfo.LastWriteTime
                                };
                                
                                models.Add(modelDto);
                            }
                        }
                    }
                }

                // 결과 정렬 (Size, Type, AdmsName, ProcessId 순)
                models = models.OrderBy(m => m.Size)
                              .ThenBy(m => m.Type)
                              .ThenBy(m => m.AdmsName)
                              .ThenBy(m => m.ProcessId)
                              .ToList();

                return Ok(new
                {
                    Message = "Models retrieved successfully",
                    Count = models.Count,
                    Filters = new
                    {
                        Size = size,
                        Type = type,
                        AdmsName = admsName,
                        ProcessId = processId
                    },
                    Models = models
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error retrieving models", Error = ex.Message });
            }
        }

        /// <summary>
        /// 파일 크기를 읽기 쉬운 형태로 포맷합니다.
        /// </summary>
        /// <param name="bytes">파일 크기 (바이트)</param>
        /// <returns>포맷된 파일 크기 문자열</returns>
        private static string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;
            
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            
            return string.Format("{0:n1} {1}", number, suffixes[counter]);
        }

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
