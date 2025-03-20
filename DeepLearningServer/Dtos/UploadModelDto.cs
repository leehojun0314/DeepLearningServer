using System.ComponentModel;

namespace DeepLearningServer.Dtos
{
    public class UploadModelDto
    {
        [DefaultValue("D:\\Models\\modelname.edltool")]
        public required string ModelPath { get; set; }
        public required IFormFile File { get; set; }
        [DefaultValue(false)]
        public required bool IsRelativePath { get; set; }
    }
}
