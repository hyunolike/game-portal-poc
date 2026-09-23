# 개발 프로세스

## 브랜치 전략 (GitHub Flow + 릴리즈 태그)

```
main ──●────●────●────●──── (항상 배포 가능, 머지 시 staging 자동 배포)
        \  /      \  /  \
  feat/coupon-..   fix/..  v1.2.0 태그 → production (승인 후)
```

- `feat/*`, `fix/*`, `chore/*`, `refactor/*` 브랜치 → PR → `main`
- 긴급 수정도 동일한 PR 절차 (리뷰 1명, CI 필수). 파이프라인이 충분히 빨라야 이 규칙이 지켜진다.

## 커밋 메시지

[Conventional Commits](https://www.conventionalcommits.org/ko/):

```
feat(coupon): 고유 쿠폰 CSV 추출 API 추가
fix(outbox): 임대 만료 메시지가 재처리되지 않는 문제 수정
refactor(notice): 캐시 키 생성 로직 분리
test(coupon): 동일 계정 동시 요청 통합 테스트 추가
ci: 마이그레이션 누락 검사 단계 추가
```

## 로컬 개발

```bash
# 의존성(SQL Server, Redis)만 띄우고 IDE 에서 디버깅
docker compose up -d sqlserver redis

dotnet run --project src/GamePortal.Admin.Api   # 기동 시 마이그레이션 적용 (Development)
dotnet run --project src/GamePortal.Web.Api
dotnet run --project src/GamePortal.Worker
dotnet run --project tools/GameServer.Mock
```

### DB 마이그레이션

```bash
dotnet tool restore
# 엔티티/Configuration 수정 후
dotnet ef migrations add AddXxx -p src/GamePortal.Infrastructure -s src/GamePortal.Infrastructure -o Persistence/Migrations
# 생성된 SQL 확인 (리뷰 시 PR 에 첨부)
dotnet ef migrations script <이전마이그레이션> -p src/GamePortal.Infrastructure -s src/GamePortal.Infrastructure
```

CI 가 `has-pending-model-changes` 로 마이그레이션 누락을 검사한다.

### 테스트

```bash
dotnet test tests/GamePortal.UnitTests           # 빠름, Docker 불필요
dotnet test tests/GamePortal.IntegrationTests    # Docker 필요 (Testcontainers 가 SQL Server 기동)
```

## AI 도구 활용 가이드

AI(Claude Code 등)를 **실행 단계**에 통합해서 사용한다. 원칙: *생성은 AI가, 책임은 작성자가.*

| 단계 | 활용 방식 | 사람이 반드시 할 일 |
|---|---|---|
| 코드 생성 | `CLAUDE.md` 컨벤션을 컨텍스트로 제공 → 보일러플레이트(Controller, DTO, Validator, Configuration) 생성 | 트랜잭션 경계·동시성 로직은 직접 설계하고 검토 |
| 테스트 | 도메인 규칙 경계값 테스트, 통합 테스트 시나리오 초안 생성 | "무엇을 보장해야 하는가"는 사람이 정의 |
| 리팩토링 | 반복 패턴 추출, 네이밍 개선, 레거시 .NET Framework 코드 이관 초안 | 동작 동일성은 테스트로 증명 |
| 리뷰 | PR 마다 Claude 1차 리뷰 (`.github/workflows/claude-code-review.yml`) | 최종 승인은 사람 |
| 장애 분석 | 로그/스택트레이스 요약, 가설 제시 | 재현과 원인 확정 |

- PR 템플릿의 "AI 도구 사용" 항목에 사용 범위를 적는다 (리뷰어가 더 주의 깊게 볼 부분을 알 수 있도록).
- 비밀정보(연결 문자열, 키, 유저 개인정보)는 프롬프트에 넣지 않는다.
