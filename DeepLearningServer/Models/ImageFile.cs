using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DeepLearningServer.Models;

public partial class ImageFile
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Directory { get; set; } = null!;

    public string Size { get; set; } = null!;

    public string Status { get; set; } = null!;

    //public int ProcessId { get; set; }

    //public int AdmsId { get; set; }

    /// <summary>
    /// OK 이미지의 AdmsProcessId (NG 이미지의 경우 null)
    /// NG 이미지: 카테고리 있음, AdmsProcessId 없음
    /// OK 이미지: 카테고리 없음, AdmsProcessId 있음
    /// </summary>
    public int? AdmsProcessId { get; set; }

    /// <summary>
    /// NG 이미지의 카테고리 (OK 이미지의 경우 null)
    /// NG 이미지: 카테고리 있음, AdmsProcessId 없음
    /// OK 이미지: 카테고리 없음, AdmsProcessId 있음
    /// </summary>
    [MaxLength(100)]
    public string? Category { get; set; }

    public DateTime CapturedTime { get; set; }

    //public virtual Adm Adms { get; set; } = null!;

    //public virtual Process Process { get; set; } = null!;
    public virtual AdmsProcess? AdmsProcess { get; set; }

    /// <summary>
    /// ⚠️ DEPRECATED: Use TrainingImageResults instead
    /// </summary>
    [Obsolete("This property is deprecated. Use TrainingImageResults instead.")]
    public virtual ICollection<ConfusionMatrixImage> ConfusionMatrixImages { get; set; } = new List<ConfusionMatrixImage>();

    /// <summary>
    /// 훈련 이미지 결과 목록 (새로운 단순화된 구조)
    /// </summary>
    public virtual ICollection<TrainingImageResult> TrainingImageResults { get; set; } = new List<TrainingImageResult>();
}
