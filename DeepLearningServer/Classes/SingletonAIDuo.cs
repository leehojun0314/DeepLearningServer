using DeepLearningServer.Enums;
using DeepLearningServer.Models;
using DeepLearningServer.Settings;

namespace DeepLearningServer.Classes;

public class SingletonAiDuo
{
    private static TrainingAi _instanceMedium;
    private static TrainingAi _instanceLarge;

    private SingletonAiDuo()
    {
    }

    public static TrainingAi Instance(CreateAndRunModel parameterData, ServerSettings serverSettings)
    {
        if (parameterData.ImageSize == ImageSize.Medium)
        {
            if (_instanceMedium == null) _instanceMedium = new TrainingAi(parameterData, serverSettings);
            return _instanceMedium;
        }

        if (parameterData.ImageSize == ImageSize.Large)
        {
            if (_instanceLarge == null) _instanceLarge = new TrainingAi(parameterData, serverSettings);
            return _instanceLarge;
        }

        throw new Exception("Invalid image size.");
        //return null;
    }

    public static TrainingAi CreateInstance(CreateAndRunModel parameterData, ServerSettings serverSettings)
    {
        if (parameterData.ImageSize == ImageSize.Medium)
        {
            _instanceMedium = new TrainingAi(parameterData, serverSettings);
            
            return _instanceMedium;
        }
        if (parameterData.ImageSize == ImageSize.Large)
        {
            _instanceLarge = new TrainingAi(parameterData, serverSettings);
            return _instanceLarge;
        }
        return null;
    }
    public static TrainingAi GetInstance(ImageSize imageSize)
    {
        if (imageSize == ImageSize.Large) return _instanceLarge;

        if (imageSize == ImageSize.Medium) return _instanceMedium;

        return null;
    }

    public static void Reset(ImageSize imageSize)
    {
        if (imageSize == ImageSize.Large) _instanceLarge = null;
        if (imageSize == ImageSize.Medium) _instanceMedium = null;
    }
}