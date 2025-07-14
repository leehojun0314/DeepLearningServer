using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeepLearningServer.Models;

/// <summary>
/// ⚠️ DEPRECATED: This table is no longer used. 
/// Use TrainingImageResult table instead for a simplified structure.
/// </summary>
[Obsolete("This class is deprecated. Use TrainingImageResult instead.")]
public class ConfusionMatrix
{
    [Key]
    public int Id { get; set; }

    public int TrainingRecordId { get; set; }

    [ForeignKey("TrainingRecordId")]
    public virtual TrainingRecord TrainingRecord { get; set; }

    [Required]
    [MaxLength(100)]
    public string TrueLabel { get; set; }

    [Required]
    [MaxLength(100)]
    public string PredictedLabel { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public virtual ICollection<ConfusionMatrixImage> ConfusionMatrixImages { get; set; } = new List<ConfusionMatrixImage>();
}