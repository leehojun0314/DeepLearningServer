# 프로젝트 개요
- 프로젝트명: DeepLearningServer
- 목적: ADMS/공정 데이터 기반 딥러닝 학습 및 추론 API 제공
- 핵심 기능: 학습 실행/중지, 진행률 저장, 모델 저장/배포, 추론 및 혼동행렬 조회

# 기술 스택
- 백엔드: C# / ASP.NET Core Web API (.NET 8)
- 데이터베이스: MSSQL (EF Core), 일부 MongoDB 참조
- Python 서버: FastAPI 기반 분류 학습 서버 (`new ai/train_cls_server.py`)
- 기타: AutoMapper, JWT 인증, Euresys(Open eVision) 연동

# 프로젝트 구조
- `DeepLearningServer/`: 메인 ASP.NET API 프로젝트
  - `Controllers/`: 학습/추론 API 엔드포인트
  - `Classes/`: 브리지/상태관리 등 핵심 서비스 클래스
  - `Dtos/`: C# ↔ Python 서버 통신 DTO
  - `Services/`: DB/도메인 서비스
  - `Settings/`: 서버 설정 모델
- `new ai/`: Python 학습 서버 및 트레이너 코드
- `deeplearningserver-python/`: 별도 Python 서비스 코드 및 환경설정
- `Scripts/`: 운영/배포 보조 스크립트

# 주요 모듈/컴포넌트
- `DeepLearningController`: 학습 실행(`run`), 중지(`stop`), 결과 저장 흐름 오케스트레이션
- `TrainingAiHttpBridge`: Python 학습 서버와 HTTP 통신, 상태 폴링/콜백 브리지
- `ToolStatusManager`: `tool_status.txt` 기반 단일 실행 상태 관리
- `train_cls_server.py`: `/train/cls/start|status|stop` 및 추론/결과 API 제공
- `trainer_for_cls_server.py`: 실제 분류 학습 루프, 원자적 모델 저장, 재시도/중단 친화적 export 구현

# 최근 변경 이력
- 2026-08-23: **2.2.8** — 훈련 완료 후 학습된 모델을 ADMS 의 `LocalIp` 로 자동 전송하던 기능을 기본 OFF 로 전환(고객사 요청). `ServerSettings.AutoUploadModelToClient`(기본 `false`, `appsettings.json` 에도 명시) 로 제어하며, 켜면 기존과 동일하게 동작한다. 두 경로 모두 적용: 파이썬 경로는 `UploadModelToClientAsync` 호출을, Euresys 경로는 `TrainingAi.SaveModel` 의 업로드 부분을 각각 게이트했다. **모델의 서버 저장(EvaluationModelDirectory)은 그대로 유지**되며(`SaveModel` 은 저장과 업로드를 한 메서드에서 하므로 `uploadToClient` 인자로 분리), 전송이 필요하면 기존 `POST /api/model/send-remote` 를 쓴다. `ModelRecord.Status` 는 새 값을 만들지 않고 기존 `pending`(=서버에 있으나 클라이언트 미전달)을 사용한다. 참고: `TrainingAi.SaveModel2` 는 호출부가 없는 미사용 메서드라 손대지 않았다. (`DeepLearningServer/Settings/ServerSettings.cs`, `DeepLearningServer/appsettings.json`, `DeepLearningServer/Controllers/DeepLearningController.cs`, `DeepLearningServer/Classes/TrainingAi.cs`, `DeepLearningServer/version.txt`, `DeepLearningServer.Tests/AutoUploadSettingTests.cs`, `context.md`)
- 2026-08-23: **2.2.7** — 중지 요청 후 다음 학습이 "The tool is already running."으로 거절되던 문제 수정. (1) `IsTerminalStatus`가 `completed`/`stopped`/`failed`만 종료로 인정해, train_cls_server 1.2.7~1.2.8이 워커 강제 종료 시 돌려주는 `terminated`/`killed`을 만나면 상태 폴링 루프가 끝나지 않고 `ToolStatusManager` 실행 플래그가 남았다 → `terminated`/`killed`/`cancelled` 추가(1.3.0은 `stopped`로 정규화하므로 양쪽 어휘 모두 수용). (2) 이미지 복사 단계에 중지 요청이 오면 `_cts`가 아직 없어 무시되고, 성공 응답 뒤 학습이 시작됐다 → 브리지 생성 시점부터 존재하는 `_stopRequestedCts` 도입(`RequestStop`/`IsStopRequested`/`ThrowIfStopRequested`), 복사 루프와 학습 시작 직전에 검사. (3) `DELETE /stop`이 파이썬에만 알리고 로컬 취소를 하지 않던 문제 → `RequestStop()` 호출. (4) `Default` 프로세스명 early return 시 실행 플래그 미해제로 서비스 재시작 전까지 학습 불가하던 누수 수정. 회귀 테스트 프로젝트 `DeepLearningServer.Tests` 신설(26개). (`DeepLearningServer/Classes/TrainingAiHttpBridge.cs`, `DeepLearningServer/Controllers/DeepLearningController.cs`, `DeepLearningServer/DeepLearningServer.csproj`, `DeepLearningServer/version.txt`, `DeepLearningServer.Tests/*`, `DeepLearningServer.sln`, `context.md`)
- 2026-03-17: Python 학습 재시도/중단 안정화를 위해 run 상태 추적, stop/finalizing 구분, 원자적 `.onnlmodel` 저장, 반복 실행용 캐시/정리 보강, C# 브리지/컨트롤러의 상태 판정 및 상세 오류 로깅 강화. (`new ai/train_cls_server.py`, `new ai/trainer_for_cls_server.py`, `DeepLearningServer/Classes/TrainingAiHttpBridge.cs`, `DeepLearningServer/Controllers/DeepLearningController.cs`, `DeepLearningServer/Dtos/PyTrainingDtos.cs`, `context.md`)
- 2026-03-05: 모델 확장자 전환 대응으로 `.edltool` 참조를 `.onnlmodel`로 변경하고 Python 모델 목록 조회에 `.onnlmodel` 검색을 추가(기존 `.onnlmodel` 호환 유지). (`DeepLearningServer/Controllers/DeepLearningController.cs`, `DeepLearningServer/Controllers/ModelController.cs`, `DeepLearningServer/Dtos/UploadModelDto.cs`, `DeepLearningServer/Dtos/InferenceDto.cs`, `DeepLearningServer/Dtos/ModelInfoDto.cs`, `deeplearningserver-python/controllers/model_controller.py`)
- 2026-03-05: Python 훈련 시 DB에 저장되는 이미지 경로가 임시 폴더 경로라 훈련 후 삭제되어 파일이 없어지는 문제 수정. 원본 경로(OriginalPaths)를 보존해 DB 저장 및 추론 시 원본 경로 사용. (`DeepLearningServer/Dtos/PyTrainingDtos.cs`, `DeepLearningServer/Classes/TrainingAiHttpBridge.cs`)
- 2026-02-26: JWT Access Token 만료 시간을 하드코딩에서 설정 파일 기반으로 분리하고 기본값(2시간) 폴백을 추가. (`DeepLearningServer/Controllers/AuthController.cs`, `DeepLearningServer/appsettings.json`)
- 2026-02-24: Python 폴링 기반 진행률 저장 과적재 문제를 줄이기 위해 epoch 변경/학습 종료 시에만 콜백 호출하도록 브리지 로직 수정. (`DeepLearningServer/Classes/TrainingAiHttpBridge.cs`)

# 현재 진행 상황
- 현재 작업 중: Python 학습 재시도/중단 안정화 반영 후 실환경 연속 학습/중단/재시도 검증
- 다음 작업:
  - 실제 학습 데이터로 `시작 -> 중단 -> 즉시 재시도`, `정상 완료 -> 즉시 재시도`, `동일 출력 경로 반복 실행` 시나리오 검증
  - 작은 데이터셋에서 `val_split/val_min` 보정 동작과 최종 모델 export 여부 확인
  - Python stop 응답이 `finalizing` 또는 `timeout`일 때 UI/운영 절차 정리
