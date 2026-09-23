/** Admin.Api 가 ProblemDetails 로 응답한 오류. 화면 분기는 code 로 한다. */
export class AdminApiError extends Error {
  readonly status: number;
  readonly code: string;
  readonly traceId?: string;
  readonly fieldErrors?: Record<string, string[]>;

  constructor(status: number, code: string, message: string, traceId?: string, fieldErrors?: Record<string, string[]>) {
    super(message);
    this.name = "AdminApiError";
    this.status = status;
    this.code = code;
    this.traceId = traceId;
    this.fieldErrors = fieldErrors;
  }
}

const messages: Record<string, string> = {
  VALIDATION_FAILED: "입력값을 확인해 주세요.",
  NOTICE_NOT_FOUND: "공지를 찾을 수 없습니다. 이미 삭제되었을 수 있습니다.",
  NOTICE_ALREADY_DELETED: "이미 삭제된 공지입니다.",
  COUPON_CAMPAIGN_NOT_FOUND: "캠페인을 찾을 수 없습니다.",
  COUPON_CODE_DUPLICATED: "이미 사용 중인 쿠폰 코드입니다. 다른 코드를 입력해 주세요.",
  COUPON_INVALID_PERIOD: "종료 시각은 시작 시각보다 늦어야 합니다.",
  COUPON_REWARD_REQUIRED: "보상 아이템을 1개 이상 추가해 주세요.",
  OUTBOX_NOT_FOUND: "지급 요청을 찾을 수 없습니다.",
  OUTBOX_NOT_FAILED: "실패 상태인 요청만 재처리할 수 있습니다. 목록을 새로고침해 주세요.",
  FORBIDDEN: "이 작업을 할 권한이 없습니다.",
  UNAUTHORIZED: "로그인이 만료되었습니다. 다시 로그인해 주세요.",
};

/** 운영자에게 보여줄 문구. 모르는 코드는 서버 title + 추적 ID 로 대체 (개발팀 문의용). */
export function describeError(error: unknown): string {
  if (error instanceof AdminApiError) {
    const known = messages[error.code];
    if (known) return known;
    const trace = error.traceId ? ` (추적 ID: ${error.traceId})` : "";
    return `${error.message || "요청을 처리하지 못했습니다."}${trace}`;
  }
  return "서버에 연결하지 못했습니다. 잠시 후 다시 시도해 주세요.";
}

/** ProblemDetails 본문 → AdminApiError. JSON 이 아닌 응답(게이트웨이 HTML 등)도 처리한다. */
export function toAdminApiError(status: number, bodyText: string): AdminApiError {
  try {
    const body = JSON.parse(bodyText) as {
      code?: string;
      title?: string;
      traceId?: string;
      errors?: Record<string, string[]>;
    };
    return new AdminApiError(
      status,
      body.code ?? (status === 403 ? "FORBIDDEN" : status === 401 ? "UNAUTHORIZED" : `HTTP_${status}`),
      body.title ?? "",
      body.traceId,
      body.errors,
    );
  } catch {
    return new AdminApiError(status, status === 403 ? "FORBIDDEN" : status === 401 ? "UNAUTHORIZED" : `HTTP_${status}`, "");
  }
}
