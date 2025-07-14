using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeepLearningServer.Models;

/// <summary>
/// ⚠️ DEPRECATED: This table is no longer used. 
/// Use TrainingImageResult table instead for a simplified structure.
/// </summary>
[Obsolete("This class is deprecated. Use TrainingImageResult instead.")]
public class ConfusionMatrixImage
{
  [Key]
  public int Id { get; set; }

  public int ConfusionMatrixId { get; set; }

  [ForeignKey("ConfusionMatrixId")]
  public virtual ConfusionMatrix ConfusionMatrix { get; set; }

  public int ImageFileId { get; set; }

  [ForeignKey("ImageFileId")]
  public virtual ImageFile ImageFile { get; set; }

  /// <summary>
  /// 실제 추론 시 모델이 예측한 레이블
  /// </summary>
  [Required]
  [MaxLength(100)]
  public string ActualPredictedLabel { get; set; }

  /// <summary>
  /// 모델의 예측 확신도 (0.0 ~ 1.0)
  /// </summary>
  public float? Confidence { get; set; }

  public DateTime CreatedAt { get; set; } = DateTime.Now;
}