﻿using System;
using System.Collections.Generic;

namespace DeepLearningServer.Models;

public partial class Process
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;


    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<AdmsProcess> AdmsProcesses { get; set; } = new List<AdmsProcess>();

}
