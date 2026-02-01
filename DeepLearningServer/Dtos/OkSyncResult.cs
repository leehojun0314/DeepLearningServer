using System.Collections.Generic;

namespace DeepLearningServer.Dtos;

public class OkSyncResult
{
  public string ImageSize { get; set; } = string.Empty; // "Middle" | "Large"
  public string ImageRoot { get; set; } = string.Empty; // e.g., Z:\\AI_CUT_MIDDLE

  public int AdmsProcessId { get; set; }
  public string ProcessName { get; set; } = string.Empty;

  public int TotalFilesScanned { get; set; }
  public int Inserted { get; set; }
  public int Skipped { get; set; }

  public int InsertedBase { get; set; }
  public int InsertedNew { get; set; }

  public List<string> Errors { get; set; } = new();
}


