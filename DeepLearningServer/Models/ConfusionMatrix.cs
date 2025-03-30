using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeepLearningServer.Models;

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
    
    public uint Count { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
} 