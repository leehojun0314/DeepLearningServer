using System.Collections.Generic;

namespace DeepLearningServer.Dtos;

public class NgSyncResult
{
  public string ImageSize { get; set; } = string.Empty; // "Middle" | "Large"
  public string ImageRoot { get; set; } = string.Empty; // e.g., Z:\\AI_CUT_MIDDLE

  public int TotalFilesScanned { get; set; }
  public int Inserted { get; set; }
  public int Skipped { get; set; }

  public Dictionary<string, int> InsertedByCategory { get; set; } = new();
  public List<string> Errors { get; set; } = new();
}



