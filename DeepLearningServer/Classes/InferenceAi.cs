using Euresys.Open_eVision;
using Euresys.Open_eVision.EasyDeepLearning;

namespace DeepLearningServer.Classes
{
    public class InferenceAi : IDisposable
    {
        private EClassifier classifier;
        private bool disposed = false;

        public InferenceAi(string modelPath)
        {
            try
            {
                classifier = new EClassifier();
                classifier.EnableGPU = true;
                Console.WriteLine("Loading model...");
                classifier.Load(modelPath);
                Console.WriteLine("Model Loaded");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading model: {ex.Message}");
                classifier?.Dispose();
                throw;
            }
        }

        public EClassificationResult ClassifySingleImage(string imagePath)
        {
            try
            {
                if (disposed)
                    throw new ObjectDisposedException(nameof(InferenceAi));

                if (classifier == null)
                    throw new Exception("Classifier is not initialized");

                if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
                    throw new Exception($"Image file not found: {imagePath}");

                Console.WriteLine("Has training model");
                
                using (var eImage = new EImageBW8())
                {
                    eImage.Load(imagePath);
                    var classifyResult = classifier.Classify(eImage);
                    Console.WriteLine("classify result: " + classifyResult);
                    return classifyResult;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error on classify: " + e);
                throw new Exception($"Classification failed for {imagePath}: {e.Message}");
            }
        }

        public EClassificationResult[] ClassifyMultipleImages(string[] imagePaths)
        {
            try
            {
                if (disposed)
                    throw new ObjectDisposedException(nameof(InferenceAi));

                if (classifier == null)
                    throw new Exception("Classifier is not initialized");

                if (imagePaths == null || imagePaths.Length == 0)
                    throw new Exception("Image paths array is null or empty");

                Console.WriteLine($"Processing {imagePaths.Length} images");

                // 이미지 경로 유효성 검사
                for (int i = 0; i < imagePaths.Length; i++)
                {
                    if (string.IsNullOrEmpty(imagePaths[i]) || !File.Exists(imagePaths[i]))
                        throw new Exception($"Image file not found at index {i}: {imagePaths[i]}");
                }

                var results = new List<EClassificationResult>();

                try
                {
                    // 이미지 하나씩 처리 (단일 이미지 방식으로)
                    for (int i = 0; i < imagePaths.Length; i++)
                    {
                        Console.WriteLine($"Processing image {i + 1}/{imagePaths.Length}: {imagePaths[i]}");
                        
                        try
                        {
                            using (var eImage = new EImageBW8())
                            {
                                eImage.Load(imagePaths[i]);
                                var classifyResult = classifier.Classify(eImage);
                                results.Add(classifyResult);
                                
                                Console.WriteLine($"Result {i}: best label '{classifyResult.BestLabel}', best prob {classifyResult.BestProbability}");
                               
                            }
                        }
                        catch (Exception imageEx)
                        {
                            Console.WriteLine($"Error processing image {i} ({imagePaths[i]}): {imageEx.Message}");
                            // 개별 이미지 에러 시 null 추가하거나 건너뛰기
                            // results.Add(null); // null 추가하려면 이 라인 활성화
                            throw new Exception($"Failed to process image {i}: {imageEx.Message}");
                        }
                    }
                    
                    Console.WriteLine($"Classification completed. Results count: {results.Count}");
                    return results.ToArray();
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Error during classification: {ex.Message}");
                    return null;
                }
                finally
                {
                    // 모델 언로드 (중요!)
                    try
                    {
                        Console.WriteLine("Unloading models...");
                        classifier.UnloadModels();
                        Console.WriteLine("Models unloaded successfully");
                    }
                    catch (Exception unloadEx)
                    {
                        Console.WriteLine($"Error unloading models: {unloadEx.Message}");
                    }
                    
                    // 가비지 컬렉션 강제 실행
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error on classify multiple images: {e}");
                Console.WriteLine($"Stack trace: {e.StackTrace}");
                throw new Exception($"Multiple image classification failed: {e.Message}");
            }
        }

        public void Inference(string[] imagePaths)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(InferenceAi));

            if (classifier == null)
                throw new Exception("Classifier is not initialized");
                
            if (!classifier.HasInferenceModel())
                throw new Exception("Model is not granted");
                
            if (imagePaths == null || imagePaths.Length == 0)
                throw new Exception("Invalid image list");

            var images = new List<EBaseROI>();
            
            try
            {
                for (int i = 0; i < imagePaths.Length; i++)
                {
                    var eImage = new EImageBW8();
                    eImage.Load(imagePaths[i]);
                    images.Add(eImage);
                }

                var imageArray = images.ToArray();
                classifier.InitializeInference(ref imageArray);
            }
            finally
            {
                // 이미지 메모리 정리
                foreach (var image in images)
                {
                    try
                    {
                        (image as IDisposable)?.Dispose();
                    }
                    catch (Exception disposeEx)
                    {
                        Console.WriteLine($"Error disposing image: {disposeEx.Message}");
                    }
                }
                
                // 모델 언로드
                try
                {
                    Console.WriteLine("Unloading models after inference...");
                    classifier.UnloadModels();
                    Console.WriteLine("Models unloaded successfully");
                }
                catch (Exception unloadEx)
                {
                    Console.WriteLine($"Error unloading models: {unloadEx.Message}");
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    try
                    {
                        if (classifier != null)
                        {
                            // 모델 언로드 먼저 실행
                            try
                            {
                                Console.WriteLine("Unloading models before disposal...");
                                classifier.UnloadModels();
                                Console.WriteLine("Models unloaded successfully");
                            }
                            catch (Exception unloadEx)
                            {
                                Console.WriteLine($"Error unloading models during disposal: {unloadEx.Message}");
                            }
                            
                            // 그 다음 classifier 해제
                            classifier.Dispose();
                            Console.WriteLine("Classifier disposed successfully");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error disposing classifier: {ex.Message}");
                    }
                }
                classifier = null;
                disposed = true;
            }
        }

        ~InferenceAi()
        {
            Dispose(false);
        }
    }
} 