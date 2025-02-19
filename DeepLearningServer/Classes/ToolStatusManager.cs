namespace DeepLearningServer.Classes
{
    public class ToolStatusManager
    {
        private static readonly string statusFilePath = "tool_status.txt";

        public static bool IsProcessRunning()
        {
            if (File.Exists(statusFilePath))
            {
                string content = File.ReadAllText(statusFilePath);
                return content.Trim() == "Running";
            }
            return false;
        }

        public static void SetProcessRunning(bool isRunning)
        {
            File.WriteAllText(statusFilePath, isRunning ? "Running" : "Idle");
        }

    }
}
