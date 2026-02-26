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
- `trainer_for_cls_server.py`: 실제 분류 학습 루프/모델 export 구현

# 최근 변경 이력
- 2026-02-26: JWT Access Token 만료 시간을 하드코딩에서 설정 파일 기반으로 분리하고 기본값(2시간) 폴백을 추가. (`DeepLearningServer/Controllers/AuthController.cs`, `DeepLearningServer/appsettings.json`)
- 2026-02-24: Python 폴링 기반 진행률 저장 과적재 문제를 줄이기 위해 epoch 변경/학습 종료 시에만 콜백 호출하도록 브리지 로직 수정. (`DeepLearningServer/Classes/TrainingAiHttpBridge.cs`)

# 현재 진행 상황
- 현재 작업 중:
  - JWT 인증 설정의 운영 가변성 향상(토큰 만료시간 설정값 분리)
  - Python 서버 폴링 구조에서 DB ProgressEntry가 과도하게 쌓이는 문제를 Euresys 방식(1 epoch = 1 entry)에 가깝게 정렬
- 다음 작업:
  - 환경별 `Jwt:AccessTokenExpiresHours` 값(개발/운영) 점검 및 적용
  - 실제 학습 시나리오에서 epoch 단위 저장 동작 확인
  - 종료 시 최종 ProgressEntry 1건 저장 여부 점검
