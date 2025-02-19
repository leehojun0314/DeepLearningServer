using System;
using System.Collections.Generic;

namespace DeepLearningServer.Models;

public partial class Process
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;


    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<AdmsProcess> AdmsProcesses { get; set; } = new List<AdmsProcess>();

    public virtual ICollection<ImageFile> ImageFiles { get; set; } = new List<ImageFile>();

    public virtual ICollection<RecipeFile> RecipeFiles { get; set; } = new List<RecipeFile>();
}
