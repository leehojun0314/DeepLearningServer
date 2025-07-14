# Confusion Matrix Images API

이 기능은 훈련 완료 후 Confusion Matrix의 각 셀에 해당하는 실제 이미지들을 조회할 수 있게 해줍니다.

## 데이터베이스 구조

### 새로 추가된 테이블

#### `ConfusionMatrixImages`

- `Id`: Primary Key
- `ConfusionMatrixId`: ConfusionMatrix 테이블 참조
- `ImageFileId`: ImageFile 테이블 참조
- `ActualPredictedLabel`: 모델이 실제로 예측한 레이블
- `Confidence`: 예측 확신도 (0.0 ~ 1.0)
- `CreatedAt`: 생성 시간

### 관계 설정

- `ConfusionMatrix` 1:N `ConfusionMatrixImage`
- `ImageFile` 1:N `ConfusionMatrixImage`

## API 엔드포인트

### 1. Confusion Matrix 이미지 조회 (상세 정보 포함)

```
GET /api/deeplearning/getConfusionMatrixImages/{trainingRecordId}/{trueLabel}/{predictedLabel}
```

**Parameters:**

- `trainingRecordId`: 훈련 기록 ID
- `trueLabel`: 실제 레이블 (예: "OK", "SCRATCH" 등)
- `predictedLabel`: 예측 레이블 (예: "OK", "SCRATCH" 등)

**Response:**

```json
{
  "trainingRecordId": 123,
  "trueLabel": "OK",
  "predictedLabel": "SCRATCH",
  "imageCount": 5,
  "images": [
    {
      "imageFile": {
        "id": 456,
        "name": "image001.jpg",
        "directory": "D:\\Images\\OK\\Process1\\BASE",
        "size": "Middle",
        "status": "Training",
        "capturedTime": "2024-01-15T10:30:00Z"
      },
      "actualPredictedLabel": "SCRATCH",
      "confidence": 0.85,
      "createdAt": "2024-01-15T11:00:00Z"
    }
  ]
}
```

### 2. Confusion Matrix 이미지 파일 목록 조회 (간단한 버전)

```
GET /api/deeplearning/getConfusionMatrixImageFiles/{trainingRecordId}/{trueLabel}/{predictedLabel}
```

**Parameters:** 동일

**Response:**

```json
{
  "trainingRecordId": 123,
  "trueLabel": "OK",
  "predictedLabel": "SCRATCH",
  "imageCount": 5,
  "imageFiles": [
    {
      "id": 456,
      "name": "image001.jpg",
      "directory": "D:\\Images\\OK\\Process1\\BASE",
      "size": "Middle",
      "status": "Training",
      "admsProcessId": 10,
      "capturedTime": "2024-01-15T10:30:00Z"
    }
  ]
}
```

## 사용 예시

### 1. 특정 훈련의 False Positive 이미지들 조회

```http
GET /api/deeplearning/getConfusionMatrixImages/123/OK/SCRATCH
```

실제로는 OK인데 SCRATCH로 잘못 분류된 이미지들을 조회

### 2. 특정 훈련의 False Negative 이미지들 조회

```http
GET /api/deeplearning/getConfusionMatrixImages/123/SCRATCH/OK
```

실제로는 SCRATCH인데 OK로 잘못 분류된 이미지들을 조회

### 3. 정확히 분류된 이미지들 조회

```http
GET /api/deeplearning/getConfusionMatrixImages/123/OK/OK
```

OK를 OK로 정확히 분류한 이미지들을 조회

## 데이터 흐름

1. **훈련 시작**: `LoadImages()` 메서드에서 훈련에 사용되는 이미지들을 내부적으로 기록
2. **훈련 완료**:
   - Confusion Matrix 통계 저장
   - 훈련에 사용된 이미지들을 `ImageFile` 테이블에 **중복 없이** 저장
   - 기존 레코드가 있으면 상태만 업데이트 (새로 추가하지 않음)
3. **추론 단계** (향후 구현):
   - 개별 이미지에 대한 모델 예측 결과를 `ConfusionMatrixImage` 테이블에 저장
   - 각 이미지의 실제 레이블과 예측 레이블을 연결

## 중복 방지 메커니즘

### 1. 응용 프로그램 레벨 중복 체크

- 훈련 이미지 저장 시 기존 레코드를 배치로 조회하여 중복 확인
- 기존 레코드가 있으면 필요시 상태만 업데이트
- 새로운 레코드만 데이터베이스에 추가

### 2. 데이터베이스 레벨 고유 제약조건

```sql
-- ImageFile 테이블에 복합 고유 인덱스 추가
CREATE UNIQUE INDEX IX_ImageFiles_Name_Directory_AdmsProcessId
ON ImageFiles (Name, Directory, AdmsProcessId)
```

### 3. 트랜잭션 처리

- 모든 이미지 저장 작업을 단일 트랜잭션으로 처리
- 오류 발생 시 전체 롤백하여 데이터 일관성 보장
- 중복 제약조건 위반 시 명확한 오류 메시지 제공

### 4. 배치 처리 최적화

- 개별 DB 호출 대신 배치 처리로 성능 향상
- 중복 체크를 미리 수행하여 불필요한 DB 작업 최소화
- 결과 로깅: 신규/업데이트/스킵된 레코드 수 추적

## 현재 제한사항

1. **이미지-예측 연결**: 현재는 훈련에 사용된 이미지만 기록하며, 실제 예측 결과와의 연결은 eVision 라이브러리의 제약으로 인해 구현되지 않음
2. **NG 이미지 매핑**: NG 이미지들은 특정 AdmsProcess와 매핑되지 않아 현재 제외됨
3. **수동 연결 필요**: 실제 confusion matrix 결과와 이미지 연결은 별도의 추론 과정이 필요

## 향후 개선 방안

1. **개별 이미지 추론**: 훈련 완료 후 각 이미지에 대해 모델을 실행하여 예측 결과 기록
2. **NG 이미지 매핑**: NG 이미지와 AdmsProcess 간의 매핑 로직 개선
3. **실시간 업데이트**: 새로운 이미지에 대한 추론 결과를 자동으로 confusion matrix에 연결

## 마이그레이션

새로운 기능을 사용하려면 다음 마이그레이션을 실행해야 합니다:

```bash
dotnet ef database update
```

이 명령은 다음을 생성합니다:

1. `ConfusionMatrixImages` 테이블과 관련 인덱스
2. `ImageFiles` 테이블에 고유 제약조건 (`Name` + `Directory` + `AdmsProcessId`)

### 마이그레이션 순서

1. `AddConfusionMatrixImageTable`: 새로운 조인 테이블 생성
2. `AddUniqueConstraintToImageFiles`: 중복 방지를 위한 고유 제약조건 추가

### 기존 데이터 처리

기존에 중복된 `ImageFile` 레코드가 있다면 마이그레이션 전에 정리가 필요할 수 있습니다:

```sql
-- 중복 레코드 확인
SELECT Name, Directory, AdmsProcessId, COUNT(*) as Count
FROM ImageFiles
GROUP BY Name, Directory, AdmsProcessId
HAVING COUNT(*) > 1;

-- 중복 레코드 정리 (필요시)
-- 최신 레코드만 남기고 나머지 삭제
```
