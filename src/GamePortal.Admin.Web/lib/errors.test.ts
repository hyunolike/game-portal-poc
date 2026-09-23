import assert from "node:assert/strict";
import { test } from "node:test";
import { AdminApiError, describeError, toAdminApiError } from "./errors.ts";
import { can } from "./roles.ts";

test("ProblemDetails 의 code/traceId/errors 를 읽는다", () => {
  const e = toAdminApiError(
    400,
    JSON.stringify({ code: "VALIDATION_FAILED", title: "요청 값이 올바르지 않습니다.", traceId: "abc", errors: { Title: ["필수"] } }),
  );
  assert.equal(e.code, "VALIDATION_FAILED");
  assert.equal(e.traceId, "abc");
  assert.deepEqual(e.fieldErrors, { Title: ["필수"] });
});

test("JSON 이 아닌 403 응답은 FORBIDDEN 으로 분류한다", () => {
  assert.equal(toAdminApiError(403, "<html>Forbidden</html>").code, "FORBIDDEN");
});

test("알려진 코드는 운영자용 문구, 모르는 코드는 서버 문구 + 추적 ID", () => {
  assert.match(describeError(new AdminApiError(409, "COUPON_CODE_DUPLICATED", "x")), /이미 사용 중인 쿠폰 코드/);
  assert.equal(describeError(new AdminApiError(500, "INTERNAL_ERROR", "일시적인 오류", "t-1")), "일시적인 오류 (추적 ID: t-1)");
  assert.match(describeError(new TypeError("fetch failed")), /서버에 연결하지 못했습니다/);
});

test("역할별 권한은 Admin.Api 정책과 같다", () => {
  assert.equal(can(["CS"], "view"), true);
  assert.equal(can(["CS"], "content"), false);
  assert.equal(can(["Operator"], "content"), true);
  assert.equal(can(["Operator"], "reward"), false);
  assert.equal(can(["Admin"], "reward"), true);
});
