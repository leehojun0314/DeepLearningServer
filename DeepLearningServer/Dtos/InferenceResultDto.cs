namespace DeepLearningServer.Dtos
{
    public class ClassificationResultDto
    {
        public string BestLabel { get; set; }
        public float BestProbability { get; set; }
    }

}
