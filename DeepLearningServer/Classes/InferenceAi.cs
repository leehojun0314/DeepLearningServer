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
            Console.WriteLine("Loading model...");
            classifier.Load(modelPath);
            
            Console.WriteLine("Model Loaded");
            //classifier.LoadInferenceModel(modelPath);
        }
        public EClassificationResult ClassifySingleImage(string imagePath)
        {
            try
            {


                if (classifier == null)
                {
                    throw new Exception("Classifier is not granted");
                }
                //if (classifier.HasTrainingModel())
                //{
                //    throw new Exception("Model is not granted");
                //}
                Console.WriteLine("Has training model");
                
                        EImageBW8 eImage = new EImageBW8();
                        eImage.Load(imagePath); // 로드
                    var classifyResult = classifier.Classify(eImage);
                    Console.WriteLine("classify result: " + classifyResult);
                 
                    return classifyResult;
                
            }
            catch (Exception e)
            {
                Console.WriteLine("Error on classify: " + e);
                throw new Exception(e.Message);
            }
        }
        public EClassificationResult[] ClassifyMultipleImages(string[] imagePaths)
        {
            try
            {


                if (classifier == null)
                {
                    throw new Exception("Classifier is not granted");
                }
                //if (classifier.HasTrainingModel())
                //{
                //    throw new Exception("Model is not granted");
                //}
                Console.WriteLine("Has training model");
                if (imagePaths != null && imagePaths.Length > 0 && classifier != null)
                {
                    EImageBW8[] images = new EImageBW8[imagePaths.Length];

                    for (int i = 0; i < imagePaths.Length; i++)
                    {
                        EImageBW8 eImage = new EImageBW8();
                        Console.WriteLine("Try to load image : " + imagePaths[i]);
                        eImage.Load(imagePaths[i]); // 로드
                        images[i] = eImage;         // EBaseROI 배열에 저장
                    }
                    var classifyResults = classifier.Classify(ref images);
                    
                    Console.WriteLine("classify result: " + classifyResults);
                    foreach(var classifyResult in classifyResults)
                    {
                        Console.WriteLine($"Classify result: best label {classifyResult.BestLabel}, best prob {classifyResult.BestProbability} ");
                    }
                    return classifyResults;
                }
                else
                {
                    throw new Exception("Invalid image list");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error on classify: " + e);
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
      
     
    }
}
