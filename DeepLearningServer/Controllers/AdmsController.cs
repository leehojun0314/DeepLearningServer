using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using DeepLearningServer.Settings;
using DeepLearningServer.Enums;
using DeepLearningServer.Services;
using System.Linq;

namespace DeepLearningServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdmsController : ControllerBase
    {
        private readonly ServerSettings _serverSettings;
        private readonly MssqlDbService _mssqlDbService;

        public AdmsController(IOptions<ServerSettings> serverSettings, MssqlDbService mssqlDbService)
        {
            _serverSettings = serverSettings.Value;
            _mssqlDbService = mssqlDbService;
        }

        /// <summary>
        /// 지정된 이미지 크기에 따른 NG 폴더 구조에서 카테고리 목록을 가져옵니다.
        /// </summary>
        /// <param name="imageSize">
        /// 이미지 크기:
        /// - 0 (Middle): 중간 크기 이미지
        /// - 1 (Large): 큰 크기 이미지
        /// </param>
        /// <returns>NG/BASE와 NG/NEW 폴더에서 발견된 카테고리 목록 (중복 제거됨)</returns>
        [HttpGet("categories/{imageSize}")]
        public IActionResult GetCategoriesByImageSize([FromRoute] ImageSize imageSize)
        {
            try
            {
                var imagePath = imageSize switch
                {
                    ImageSize.Middle => _serverSettings.MiddleImagePath,
                    ImageSize.Large => _serverSettings.LargeImagePath,
                    _ => throw new Exception($"Invalid image size: {imageSize}"),
                };

                if (string.IsNullOrEmpty(imagePath))
                {
                    return BadRequest($"Image path not configured for size: {imageSize}");
                }

                var categoryImageCounts = new Dictionary<string, int>(); // 카테고리별 이미지 개수 저장

                // NG/BASE 폴더에서 카테고리 가져오기
                string basePath = Path.Combine(imagePath, "NG", "BASE");
                Console.WriteLine("Base path: " + basePath);
                if (Directory.Exists(basePath))
                {
                    Console.WriteLine("base path exist");
                    var baseCategories = Directory.GetDirectories(basePath);
                    
                    foreach (var categoryPath in baseCategories)
                    {
                        string categoryName = Path.GetFileName(categoryPath).ToUpper();
                        Console.WriteLine("category: " + categoryName);
                        
                        // jpg 파일 개수 계산
                        var imageFiles = Directory.GetFiles(categoryPath, "*.jpg", SearchOption.AllDirectories);
                        int imageCount = imageFiles.Length;
                        
                        if (categoryImageCounts.ContainsKey(categoryName))
                        {
                            categoryImageCounts[categoryName] += imageCount;
                        }
                        else
                        {
                            categoryImageCounts[categoryName] = imageCount;
                        }
                        
                        Console.WriteLine($"Category {categoryName}: {imageCount} images in BASE");
                    }
                }

                // NG/NEW 폴더에서 카테고리 가져오기
                string newPath = Path.Combine(imagePath, "NG", "NEW");
                Console.WriteLine("New path: " + newPath);
                if (Directory.Exists(newPath))
                {
                    Console.WriteLine("new path exist");
                    var newCategories = Directory.GetDirectories(newPath);
                    
                    foreach (var categoryPath in newCategories)
                    {
                        string categoryName = Path.GetFileName(categoryPath).ToUpper();
                        Console.WriteLine("category: " + categoryName);
                        
                        // jpg 파일 개수 계산
                        var imageFiles = Directory.GetFiles(categoryPath, "*.jpg", SearchOption.AllDirectories);
                        int imageCount = imageFiles.Length;
                        
                        if (categoryImageCounts.ContainsKey(categoryName))
                        {
                            categoryImageCounts[categoryName] += imageCount;
                        }
                        else
                        {
                            categoryImageCounts[categoryName] = imageCount;
                        }
                        
                        Console.WriteLine($"Category {categoryName}: {imageCount} images in NEW");
                    }
                }

                // 카테고리 이름만 추출하여 정렬
                var categories = categoryImageCounts.Keys.OrderBy(c => c).ToArray();
                
                // 전체 이미지 개수 계산
                int totalImages = categoryImageCounts.Values.Sum();

                return Ok(new
                {
                    ImageSize = imageSize.ToString(),
                    ImagePath = imagePath,
                    Categories = categories,
                    CategoryCounts = categoryImageCounts,
                    Count = categories.Length,
                    TotalImages = totalImages
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Error = "Failed to retrieve categories",
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// 특정 AdmsProcessId와 이미지 크기에 해당하는 프로세스의 OK 이미지 개수를 가져옵니다.
        /// </summary>
        /// <param name="admsProcessId">ADMS 프로세스 ID</param>
        /// <param name="imageSize">
        /// 이미지 크기:
        /// - 0 (Middle): 중간 크기 이미지
        /// - 1 (Large): 큰 크기 이미지
        /// </param>
        /// <returns>해당 프로세스의 BASE와 NEW 폴더에 있는 OK 이미지 개수</returns>
        [HttpGet("process-images/{admsProcessId}/{imageSize}")]
        public async Task<IActionResult> GetProcessImageCount([FromRoute] int admsProcessId, [FromRoute] ImageSize imageSize)
        {
            try
            {
                // AdmsProcessId로부터 프로세스 정보 가져오기
                var admsProcessInfo = await _mssqlDbService.GetAdmsProcessInfos(new List<int> { admsProcessId });
                
                if (admsProcessInfo == null || !admsProcessInfo.Any())
                {
                    return NotFound($"AdmsProcess with ID {admsProcessId} not found");
                }

                var processInfo = admsProcessInfo.First();
                
                // Process Name 가져오기
                if (!processInfo.TryGetValue("processId", out object processIdValue) || !(processIdValue is int processId))
                {
                    return BadRequest("Invalid process information");
                }

                string processName = await _mssqlDbService.GetProcessNameById(processId);
                if (string.IsNullOrEmpty(processName))
                {
                    return NotFound($"Process name not found for process ID {processId}");
                }

                // 이미지 경로 결정
                var imagePath = imageSize switch
                {
                    ImageSize.Middle => _serverSettings.MiddleImagePath,
                    ImageSize.Large => _serverSettings.LargeImagePath,
                    _ => throw new Exception($"Invalid image size: {imageSize}"),
                };

                if (string.IsNullOrEmpty(imagePath))
                {
                    return BadRequest($"Image path not configured for size: {imageSize}");
                }

                int totalImages = 0;
                var imageDetails = new Dictionary<string, int>();

                // OK/{processName}/BASE 폴더의 이미지 개수 계산
                string basePath = Path.Combine(imagePath, "OK", processName, "BASE");
                Console.WriteLine($"Base path: {basePath}");
                
                int baseImageCount = 0;
                if (Directory.Exists(basePath))
                {
                    var baseImages = Directory.GetFiles(basePath, "*.jpg", SearchOption.AllDirectories);
                    baseImageCount = baseImages.Length;
                    Console.WriteLine($"Base images count: {baseImageCount}");
                }
                imageDetails.Add("BASE", baseImageCount);
                totalImages += baseImageCount;

                // OK/{processName}/NEW 폴더의 이미지 개수 계산
                string newPath = Path.Combine(imagePath, "OK", processName, "NEW");
                Console.WriteLine($"New path: {newPath}");
                
                int newImageCount = 0;
                if (Directory.Exists(newPath))
                {
                    var newImages = Directory.GetFiles(newPath, "*.jpg", SearchOption.AllDirectories);
                    newImageCount = newImages.Length;
                    Console.WriteLine($"New images count: {newImageCount}");
                }
                imageDetails.Add("NEW", newImageCount);
                totalImages += newImageCount;

                return Ok(new
                {
                    AdmsProcessId = admsProcessId,
                    ProcessId = processId,
                    ProcessName = processName,
                    ImageSize = imageSize.ToString(),
                    ImagePath = imagePath,
                    ImageDetails = imageDetails,
                    TotalImages = totalImages
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting process image count: {ex.Message}");
                return StatusCode(500, new
                {
                    Error = "Failed to retrieve process image count",
                    Message = ex.Message
                });
            }
        }
    }
}
