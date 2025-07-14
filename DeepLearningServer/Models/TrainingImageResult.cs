using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeepLearningServer.Models;

/// <summary>
/// 훈련 과정에서 각 이미지별 예측 결과를 저장하는 테이블
/// NG 이미지와 OK 이미지를 분리하여 처리
/// </summary>
public class TrainingImageResult
{
  [Key]
  public int Id { get; set; }

  /// <summary>
  /// 훈련 기록 ID
  /// </summary>
  public int TrainingRecordId { get; set; }

  [ForeignKey("TrainingRecordId")]
  public virtual TrainingRecord TrainingRecord { get; set; }

  /// <summary>
  /// 이미지 파일 ID (ImageFile 테이블 참조)
  /// </summary>
  public int ImageFileId { get; set; }

  [ForeignKey("ImageFileId")]
  public virtual ImageFile ImageFile { get; set; }

  /// <summary>
  /// 실제 라벨 (Ground Truth)
  /// </summary>
  [Required]
  [MaxLength(100)]
  public string TrueLabel { get; set; }

  /// <summary>
  /// 모델이 예측한 라벨
  /// </summary>
  [Required]
  [MaxLength(100)]
  public string PredictedLabel { get; set; }

  /// <summary>
  /// 예측 신뢰도 (0.0 ~ 1.0)
  /// </summary>
  public float? Confidence { get; set; }

  /// <summary>
  /// 이미지 상태 (Base, New, Predicted)
  /// - Base: 기본 이미지
  /// - New: 새로운 이미지
  /// - Predicted: 모델 추론 결과
  /// </summary>
  [Required]
  [MaxLength(20)]
  public string Status { get; set; }

  /// <summary>
  /// NG 이미지의 카테고리 (OK 이미지의 경우 null)
  /// NG 이미지: 카테고리 있음, AdmsProcessId 없음
  /// OK 이미지: 카테고리 없음, AdmsProcessId 있음
  /// </summary>
  [MaxLength(100)]
  public string? Category { get; set; }

  /// <summary>
  /// OK 이미지의 AdmsProcessId (NG 이미지의 경우 null)
  /// NG 이미지: 카테고리 있음, AdmsProcessId 없음
  /// OK 이미지: 카테고리 없음, AdmsProcessId 있음
  /// </summary>
  public int? AdmsProcessId { get; set; }

  [ForeignKey("AdmsProcessId")]
  public virtual AdmsProcess? AdmsProcess { get; set; }

  /// <summary>
  /// 생성 일시
  /// </summary>
  public DateTime CreatedAt { get; set; } = DateTime.Now;
}