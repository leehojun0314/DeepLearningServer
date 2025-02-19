using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DeepLearningServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        // GET: api/<TestController>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<TestController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<TestController>
        [HttpPost("sendupload")]
        public async Task<IActionResult> SendUpload()
        {
            // 업로드할 파일 경로 설정
            string filePath = @"D:\Models\ADMS_01\TEST\20250219\trainingModel.edltool";
            //string filePath = @"C:\Users\dlghw\OneDrive\Pictures\1009.jpg";
            // 파일 존재 여부 확인
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("파일을 찾을 수 없습니다: " + filePath);
            }

            try
            {
                using (var client = new HttpClient())
                {
                    using (var formData = new MultipartFormDataContent())
                    {
                        // 파일을 바이트 배열로 읽기
                        byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                        var fileContent = new ByteArrayContent(fileBytes);

                        // MIME 타입 설정 (필요시 실제 파일 타입으로 변경)
                        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");

                        // "File" 필드에 파일 추가 (파일명도 함께 전송)
                        formData.Add(fileContent, "File", Path.GetFileName(filePath));

                        // "ModelPath" 필드에 파일 경로 추가
                        // 백슬래시(\) 대신 슬래시(/)를 사용하여 curl 형식과 유사하게 처리
                        string modelPathValue = filePath.Replace("\\", "/");
                        formData.Add(new StringContent("D:/tra2.edltool"), "ModelPath");

                        // 요청을 보낼 엔드포인트 URL
                        string apiUrl = "http://192.168.1.39:8080/api/model/upload";

                        // POST 요청 전송
                        HttpResponseMessage response = await client.PostAsync(apiUrl, formData);
                        string resultContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine("Reuslt content: " + resultContent);
                        if (response.IsSuccessStatusCode)
                        {
                            return Ok("업로드 성공: " + resultContent);
                        }
                        else
                        {
                            return StatusCode((int)response.StatusCode, "업로드 실패: " + resultContent);
                        }
                    }
                }
            }
            catch (Exception ex)
            {

                string errorDetails = ex.ToString();
                Console.WriteLine("요청 전송 중 오류 발생: " + errorDetails);
                return StatusCode(500, "요청 전송 중 오류 발생: " + errorDetails);
            }
        }

        // PUT api/<TestController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<TestController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
