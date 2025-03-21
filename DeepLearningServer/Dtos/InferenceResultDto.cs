namespace DeepLearningServer.Dtos
{
    public class ClassificationResultDto
    {
        public string BestLabel { get; set; }
        public float BestProbability { get; set; }
        public LabelProbabilityDto[] LabelProbabilities { get; set; }

    }
    public class LabelProbabilityDto
    {
        public string Label { get; set; }
        public float Probability { get; set; }
    }


}
