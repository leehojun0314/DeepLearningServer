namespace DeepLearningServer.Services;

public class Configuration
{
    private readonly string _Settings;

    public Configuration(string settings)
    {
        _Settings = settings;
    }

    public string GetSettings()
    {
        return _Settings;
    }
}