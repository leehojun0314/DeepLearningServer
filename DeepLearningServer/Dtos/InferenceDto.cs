namespace DeepLearningServer.Dtos
{
    public class InferenceDto
    {
        public required string ModelPath { get; set; }
        public required string[] ImagePaths { get; set; }
    }
}
