using System;
using System.Collections.Generic;

namespace DeepLearningServer.Models;

public partial class Adm
{
    public int Id { get; set; }

    public string? Name { get; set; } = null!;

    public string LocalIp { get; set; } = null!;

    public string MacAddress { get; set; } = null!;

    public string CpuId { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<AdmsProcess> AdmsProcesses { get; set; } = new List<AdmsProcess>();

}
