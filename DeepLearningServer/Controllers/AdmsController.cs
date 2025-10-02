using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using DeepLearningServer.Settings;
using DeepLearningServer.Enums;
using DeepLearningServer.Services;
using DeepLearningServer.Dtos;
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
        /// 🚀 데이터베이스 기반으로 지정된 이미지 크기에 따른 NG 카테고리 목록과 이미지 개수를 가져옵니다.
        /// </summary>
        /// <param name="imageSize">
        /// 이미지 크기:
        /// - 0 (Middle): 중간 크기 이미지
        /// - 1 (Large): 큰 크기 이미지
        /// </param>
        /// <returns>NG/BASE와 NG/NEW 폴더에서 발견된 카테고리 목록과 이미지 개수 (데이터베이스 기반)</returns>
        [HttpGet("categories/{imageSize}")]
        public async Task<IActionResult> GetCategoriesByImageSize([FromRoute] ImageSize imageSize)
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

                // 🚀 데이터베이스에서 NG 카테고리별 이미지 개수 가져오기
                string sizeString = imageSize == ImageSize.Middle ? "Middle" : "Large";
                var categoryImageCounts = await _mssqlDbService.GetNgCategoriesImageCountAsync(sizeString);

                Console.WriteLine($"🚀 DB-based category count retrieved: {categoryImageCounts.Count} categories found");
                foreach (var kvp in categoryImageCounts)
                {
                    Console.WriteLine($"Category {kvp.Key}: {kvp.Value} images");
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
                    TotalImages = totalImages,
                    Source = "Database" // 🚀 데이터베이스 기반임을 명시
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting categories from database: {ex.Message}");
                return StatusCode(500, new
                {
                    Error = "Failed to retrieve categories from database",
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// 파일 시스템과 DB를 동기화하여 NG 이미지들을 ImageFiles 테이블에 등록합니다.
        /// NG 경로 형식: {AI_CUT_MIDDLE|AI_CUT_LARGE}/NG/{NEW|BASE}/{category}/{*.jpg}
        /// </summary>
        [HttpPost("sync-ng/{imageSize}")]
        public async Task<IActionResult> SyncNgImages([FromRoute] ImageSize imageSize)
        {
            try
            {
                var imageRoot = imageSize switch
                {
                    ImageSize.Middle => _serverSettings.MiddleImagePath,
                    ImageSize.Large => _serverSettings.LargeImagePath,
                    _ => throw new Exception($"Invalid image size: {imageSize}")
                };

                if (string.IsNullOrWhiteSpace(imageRoot))
                {
                    return BadRequest($"Image path not configured for size: {imageSize}");
                }

                var syncResult = await _mssqlDbService.SyncNgImagesAsync(imageSize, imageRoot);

                // 동기화 후 최신 카테고리 카운트 재조회
                string sizeString = imageSize == ImageSize.Middle ? "Middle" : "Large";
                var categoryCounts = await _mssqlDbService.GetNgCategoriesImageCountAsync(sizeString);
                int totalImages = categoryCounts.Values.Sum();

                return Ok(new
                {
                    Sync = syncResult,
                    Categories = categoryCounts.Keys.OrderBy(x => x).ToArray(),
                    CategoryCounts = categoryCounts,
                    TotalImages = totalImages,
                    Source = "Database"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error syncing NG images: {ex.Message}");
                return StatusCode(500, new { Error = "Failed to sync NG images", Message = ex.Message });
            }
        }

        /// <summary>
        /// 🚀 데이터베이스 기반으로 특정 AdmsProcessId와 이미지 크기에 해당하는 프로세스의 OK 이미지 개수를 가져옵니다.
        /// </summary>
        /// <param name="admsProcessId">ADMS 프로세스 ID</param>
        /// <param name="imageSize">
        /// 이미지 크기:
        /// - 0 (Middle): 중간 크기 이미지
        /// - 1 (Large): 큰 크기 이미지
        /// </param>
        /// <returns>해당 프로세스의 BASE와 NEW 폴더에 있는 OK 이미지 개수 (데이터베이스 기반)</returns>
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

                // 🚀 데이터베이스에서 OK 이미지 개수 가져오기
                string sizeString = imageSize == ImageSize.Middle ? "Middle" : "Large";
                var imageDetails = await _mssqlDbService.GetOkImageCountByProcessAsync(admsProcessId, sizeString);

                int totalImages = imageDetails.Values.Sum();

                Console.WriteLine($"🚀 DB-based process image count retrieved for {processName}: BASE={imageDetails["BASE"]}, NEW={imageDetails["NEW"]}, Total={totalImages}");

                return Ok(new
                {
                    AdmsProcessId = admsProcessId,
                    ProcessId = processId,
                    ProcessName = processName,
                    ImageSize = imageSize.ToString(),
                    ImagePath = imagePath,
                    ImageDetails = imageDetails,
                    TotalImages = totalImages,
                    Source = "Database" // 🚀 데이터베이스 기반임을 명시
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting process image count from database: {ex.Message}");
                return StatusCode(500, new
                {
                    Error = "Failed to retrieve process image count from database",
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// 🚀 데이터베이스 기반으로 여러 AdmsProcessId와 이미지 크기에 해당하는 프로세스들의 OK 이미지 개수를 일괄로 가져옵니다.
        /// </summary>
        /// <param name="request">
        /// 요청 객체:
        /// - AdmsProcessIds: ADMS 프로세스 ID 배열
        /// - ImageSize: 이미지 크기 (0: Middle, 1: Large)
        /// </param>
        /// <returns>각 프로세스별 BASE와 NEW 폴더에 있는 OK 이미지 개수 배열 (데이터베이스 기반)</returns>
        [HttpPost("process-images-bulk")]
        public async Task<IActionResult> GetProcessImageCountBulk([FromBody] ProcessImageCountRequest request)
        {
            try
            {
                if (request.AdmsProcessIds == null || !request.AdmsProcessIds.Any())
                {
                    return BadRequest("AdmsProcessIds is required and cannot be empty");
                }

                // AdmsProcessIds로부터 프로세스 정보들 가져오기
                var admsProcessInfoList = await _mssqlDbService.GetAdmsProcessInfos(request.AdmsProcessIds);

                if (admsProcessInfoList == null || !admsProcessInfoList.Any())
                {
                    return NotFound("No AdmsProcess found for the provided IDs");
                }

                // 이미지 경로 결정
                var imagePath = request.ImageSize switch
                {
                    ImageSize.Middle => _serverSettings.MiddleImagePath,
                    ImageSize.Large => _serverSettings.LargeImagePath,
                    _ => throw new Exception($"Invalid image size: {request.ImageSize}"),
                };

                if (string.IsNullOrEmpty(imagePath))
                {
                    return BadRequest($"Image path not configured for size: {request.ImageSize}");
                }

                // 🚀 데이터베이스에서 일괄로 OK 이미지 개수 가져오기
                string sizeString = request.ImageSize == ImageSize.Middle ? "Middle" : "Large";
                var bulkImageCounts = await _mssqlDbService.GetOkImageCountBulkAsync(request.AdmsProcessIds, sizeString);

                var results = new List<ProcessImageCountResult>();
                int totalAllImages = 0;

                // 각 프로세스별로 결과 구성
                foreach (var processInfo in admsProcessInfoList)
                {
                    try
                    {
                        // Process 정보 추출
                        if (!processInfo.TryGetValue("admsProcessId", out object admsProcessIdValue) || !(admsProcessIdValue is int admsProcessId))
                        {
                            Console.WriteLine("Invalid admsProcessId in process info");
                            continue;
                        }

                        if (!processInfo.TryGetValue("processId", out object processIdValue) || !(processIdValue is int processId))
                        {
                            Console.WriteLine($"Invalid processId for AdmsProcessId {admsProcessId}");
                            continue;
                        }

                        string processName = await _mssqlDbService.GetProcessNameById(processId);
                        if (string.IsNullOrEmpty(processName))
                        {
                            Console.WriteLine($"Process name not found for process ID {processId}");
                            continue;
                        }

                        // 🚀 데이터베이스에서 가져온 이미지 개수 사용
                        var imageDetails = bulkImageCounts.ContainsKey(admsProcessId)
                            ? bulkImageCounts[admsProcessId]
                            : new Dictionary<string, int> { { "BASE", 0 }, { "NEW", 0 } };

                        int totalImages = imageDetails.Values.Sum();

                        // 결과 추가
                        results.Add(new ProcessImageCountResult
                        {
                            AdmsProcessId = admsProcessId,
                            ProcessId = processId,
                            ProcessName = processName,
                            ImageDetails = imageDetails,
                            TotalImages = totalImages
                        });

                        totalAllImages += totalImages;

                        Console.WriteLine($"🚀 DB-based process {processName}: BASE={imageDetails["BASE"]}, NEW={imageDetails["NEW"]}, Total={totalImages}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing AdmsProcessId: {ex.Message}");
                        // 개별 프로세스 오류는 무시하고 계속 진행
                    }
                }

                Console.WriteLine($"🚀 DB-based bulk process image count completed: {results.Count} processes, {totalAllImages} total images");

                return Ok(new
                {
                    ImageSize = request.ImageSize.ToString(),
                    ImagePath = imagePath,
                    ProcessedCount = results.Count,
                    TotalProcesses = request.AdmsProcessIds.Count,
                    TotalAllImages = totalAllImages,
                    Results = results,
                    Source = "Database" // 🚀 데이터베이스 기반임을 명시
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting bulk process image count from database: {ex.Message}");
                return StatusCode(500, new
                {
                    Error = "Failed to retrieve bulk process image count from database",
                    Message = ex.Message
                });
            }
        }
    }

    // Request/Response 모델들
    public class ProcessImageCountRequest
    {
        public List<int> AdmsProcessIds { get; set; } = new List<int>();
        public ImageSize ImageSize { get; set; }
    }

    public class ProcessImageCountResult
    {
        public int AdmsProcessId { get; set; }
        public int ProcessId { get; set; }
        public string ProcessName { get; set; } = string.Empty;
        public Dictionary<string, int> ImageDetails { get; set; } = new Dictionary<string, int>();
        public int TotalImages { get; set; }
    }
}
