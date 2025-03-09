using System.ComponentModel;

namespace DeepLearningServer.Dtos
{
    public class ModelMigrations
    {
        [DefaultValue("D:\\ModelUpgradeProject\\old")]
        public string OldModelsPath { get; set; }

        [DefaultValue("D:\\ModelUpgradeProject\\new")]
        public string NewModelsPath { get; set; }

        [DefaultValue("D:\\ModelUpgradeProject\\project")]
        public string ProjectDir { get; set; }
    }
}
