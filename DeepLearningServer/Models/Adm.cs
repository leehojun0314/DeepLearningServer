using System;
using System.Collections.Generic;

namespace DeepLearningServer.Models;

public partial class Adm
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string LocalIp { get; set; } = null!;

    public string MacAddress { get; set; } = null!;

    public string CpuId { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<AdmsProcess> AdmsProcesses { get; set; } = new List<AdmsProcess>();

    public virtual ICollection<ImageFile> ImageFiles { get; set; } = new List<ImageFile>();

    public virtual ICollection<RecipeFile> RecipeFiles { get; set; } = new List<RecipeFile>();
}
