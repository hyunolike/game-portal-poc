# CLAUDE.md

AI 코딩 에이전트(Claude Code)가 이 저장소에서 작업할 때 따르는 규칙. 사람 개발자에게도 동일하게 적용된다.

## 프로젝트

게임 홈페이지(Web: Razor Pages) + 유저 API(Web.Api) + 운영툴 화면(Admin.Web: Next.js) + 운영툴 API(Admin.Api) + Outbox Worker. .NET 8, Node 22, SQL Server, Redis.
설계 배경은 `docs/ARCHITECTURE.md`, 리뷰 기준은 `docs/CODE_REVIEW.md`.

## 명령어

```bash
dotnet build                                   # 경고 = 에러
dotnet format --verify-no-changes              # CI 와 동일한 스타일 검사 (수정: dotnet format)
dotnet test tests/GamePortal.UnitTests
dotnet test tests/GamePortal.IntegrationTests  # Docker 필요
dotnet tool restore && dotnet ef migrations add <Name> -p src/GamePortal.Infrastructure -s src/GamePortal.Infrastructure -o Persistence/Migrations
```

```bash
# 운영툴 화면 (src/GamePortal.Admin.Web)
npm run lint -- --max-warnings 0 && npm run typecheck && npm test && npm run build
npm run e2e                                    # 전체 스택 실행 중일 때
```

작업 완료 전 반드시 `dotnet format --verify-no-changes`, `dotnet build`, 관련 테스트를 통과시킬 것.

## 코드 규칙

- **레이어 의존 방향**: Domain ← Application ← Infrastructure ← AspNetCore ← Web/Admin/Worker. 역방향 참조 금지.
- **홈페이지(`GamePortal.Web`)는 서버 프로젝트를 참조하지 않는다.** Web.Api HTTP 계약만 사용 (`ApiClient/`).
  - 유저 문구는 에러 코드로 분기 (`CouponMessages`). 서버에 쿠폰 에러 코드를 추가하면 문구도 추가 (계약 테스트가 검사).
  - 사용자 입력/운영 콘텐츠는 Razor 인코딩 그대로 출력. `Html.Raw` 금지.
- **운영툴 화면(`GamePortal.Admin.Web`)**
  - Admin.Api 호출은 서버에서만 (`lib/api.ts`, `server-only`). 토큰·API 주소를 클라이언트 컴포넌트나 `NEXT_PUBLIC_` 변수로 노출 금지.
  - 변경은 Server Action + `runAction()` (redirect 예외를 삼키지 않도록). 메뉴/버튼 권한은 `lib/roles.ts` — Admin.Api 정책과 같게 유지.
  - 날짜는 `lib/format.ts` 로 KST 입력·표시. 되돌리기 어려운 작업은 `SubmitButton confirmMessage` 로 확인.
- **도메인 규칙은 엔티티 메서드에.** 서비스는 조율만 한다. 엔티티 setter 는 `private`.
- **에러**: 비즈니스 오류는 `DomainException(DomainError)`. 새 에러 코드는 `XxxErrors` 정적 클래스에 추가. 기존 코드 값 변경 금지(클라이언트 계약).
- **검증**: 요청 DTO 는 FluentValidation `AbstractValidator<T>` (Application 어셈블리에 두면 자동 등록).
- **EF Core**
  - 조회는 `AsNoTracking()` + `Select` 프로젝션.
  - 카운터/재고/수량은 절대 "읽고 더해서 저장"하지 말 것 → `ExecuteUpdateAsync` 조건부 UPDATE.
  - 명시적 트랜잭션은 `Database.CreateExecutionStrategy().ExecuteAsync(...)` 안에서 시작 (재시도 정책과 호환).
  - 엔티티 변경 시 마이그레이션 필수. CI 가 누락을 검사한다.
- **외부 호출(게임 서버 등)을 DB 트랜잭션 안에서 하지 말 것.** Outbox 메시지를 기록하고 Worker 가 전달한다.
- **운영툴 엔드포인트**는 `[Authorize(Policy = AdminPolicies.Xxx)]` 명시. 운영 데이터 엔티티는 `IAuditableEntity` 구현 + HiLo Id.
- **로그**: 구조화 템플릿만 (`logger.LogInformation("... {CampaignId}", id)`), 토큰/개인정보 금지.
- **테스트 이름**: 한글, "무엇을 보장하는지" (`선착순_수량을_초과해_지급되지_않는다`). DB 제약/동시성은 통합 테스트로.
- 주석은 "왜"를 쓴다. "무엇"은 코드가 말한다.

## 하지 말 것

- `appsettings*.json` 에 운영 비밀값 커밋
- 테스트를 스킵/삭제해서 CI 통과시키기
- 생성된 마이그레이션 파일 수동 편집 (필요하면 새 마이그레이션 추가)
- `InvariantGlobalization` 활성화 (Microsoft.Data.SqlClient 가 지원하지 않음)
