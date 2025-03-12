using Euresys.Open_eVision;
using Euresys.Open_eVision.EasyDeepLearning;

namespace DeepLearningServer.Classes
{
    public class InferenceAi
    {
        private EClassifier classifier;
        public InferenceAi(string modelPath)
        {
            classifier = new EClassifier();
            classifier.EnableGPU = true;
            classifier.Load(modelPath);
            classifier.LoadInferenceModel(modelPath);
        }

        public EClassificationResult[] Classify(string[] imagePaths)
        {
            try
            {


                if (classifier == null)
                {
                    throw new Exception("Classifier is not granted");
                }
                if (classifier.HasTrainingModel())
                {
                    throw new Exception("Model is not granted");
                }
                if (imagePaths != null && imagePaths.Length > 0 && classifier != null && classifier.HasTrainingModel())
                {
                    EBaseROI[] images = new EBaseROI[imagePaths.Length];

                    for (int i = 0; i < imagePaths.Length; i++)
                    {
                        EImageBW8 eImage = new EImageBW8();
                        eImage.Load(imagePaths[i]); // 로드
                        images[i] = eImage;         // EBaseROI 배열에 저장
                    }
                    classifier.EnableGPU = true;
                    return classifier.Classify(ref images);
                }
                else
                {
                    throw new Exception("Invalid image list");
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public void Inference(string[] imagePaths)
        {
            if (classifier == null)
            {
                throw new Exception("Classifier is not granted");
            }
            if (!classifier.HasInferenceModel())
            {
                throw new Exception("Model is not granted");
            }
            if (imagePaths != null && imagePaths.Length > 0 && classifier != null && classifier.HasTrainingModel())
            {
                EBaseROI[] images = new EBaseROI[imagePaths.Length];

                for (int i = 0; i < imagePaths.Length; i++)
                {
                    EImageBW8 eImage = new EImageBW8();
                    eImage.Load(imagePaths[i]); // 로드
                    images[i] = eImage;         // EBaseROI 배열에 저장
                }

                classifier.InitializeInference(ref images);
                
            }
            else
            {
                throw new Exception("Invalid image list");
            }
        }
        public void LoadInferenceModel(string modelPath)
        {
            if (classifier == null)
            {
                throw new Exception("Classifier is not granted");
            }
            classifier.LoadInferenceModel(modelPath);
        }
     
    }
}
