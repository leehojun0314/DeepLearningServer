using System.ComponentModel;

namespace DeepLearningServer.Dtos
{
    public class InferenceDto
    {
        [DefaultValue("D:\\ModelUpgradeProject\\new\\2208_8PB-EG-M09237.edltool")]
        public required string ModelPath { get; set; }
        [DefaultValue("D:\\ADMS\\AI_CUT_MIDDLE\\20250227\\ABNORMAL_COATING\\2208_8BE-EG-M09791_2436304_01_ABNORMAL_X(1024)_Y(0) - 복사본.jpg")]
        public required string ImagePath { get; set; }
    }
    public class MultiInferenceDto
    {
        [DefaultValue("D:\\ModelUpgradeProject\\new\\2208_8PB-EG-M09237.edltool")]
        public required string ModelPath { get; set; }
        public required string[] ImagePaths { get; set; }
    }
}
