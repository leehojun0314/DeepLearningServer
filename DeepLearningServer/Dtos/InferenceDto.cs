namespace DeepLearningServer.Dtos
{
    public class InferenceDto
    {
        public string ModelPath { get; set; }
        public string[] ImagePaths { get; set; }
        public string DataAugPath { get; set; }

    }
}
