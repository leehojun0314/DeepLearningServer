using System;
using System.Collections.Generic;

namespace DeepLearningServer.Models;

public partial class AdmsProcess
{
    public int Id { get; set; }
    public int AdmsId { get; set; }
    public int ProcessId { get; set; }
    public virtual Adm Adms { get; set; } = null!;
    public virtual Process Process { get; set; } = null!;
    public virtual ICollection<RecipeFile> RecipeFiles { get; set; } = new List<RecipeFile>();
    public virtual ICollection<ImageFile> ImageFiles { get; set; } = new List<ImageFile>();
    public virtual ICollection<AdmsProcessType> AdmsProcessTypes { get; set; } = new List<AdmsProcessType>();
    public virtual ICollection<TrainingAdmsProcess> TrainingAdmsProcesses { get; set;} = new List<TrainingAdmsProcess>();

}
