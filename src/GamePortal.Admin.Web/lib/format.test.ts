import assert from "node:assert/strict";
import { test } from "node:test";
import { formatCouponCode, formatKst, isoToKstLocal, kstLocalToIso } from "./format.ts";

test("UTC 시각을 KST 로 표시한다 (날짜가 넘어가는 경우 포함)", () => {
  assert.equal(formatKst("2026-09-24T21:00:00Z"), "2026.09.25 06:00");
  assert.equal(formatKst(null), "-");
});

test("datetime-local 입력은 KST 로 해석해 +09:00 ISO 로 보낸다", () => {
  assert.equal(kstLocalToIso("2026-09-25T06:00"), "2026-09-25T06:00:00+09:00");
  assert.equal(new Date(kstLocalToIso("2026-09-25T06:00")).toISOString(), "2026-09-24T21:00:00.000Z");
});

test("ISO ↔ datetime-local 왕복 변환이 일치한다", () => {
  const local = "2026-12-31T23:30";
  assert.equal(isoToKstLocal(kstLocalToIso(local)), local);
});

test("잘못된 일시 형식은 거부한다", () => {
  assert.throws(() => kstLocalToIso("2026/09/25 06:00"));
});

test("고유 코드만 4자리마다 하이픈으로 나누고, 공용 코드는 그대로 둔다", () => {
  assert.equal(formatCouponCode("ABCDEFGH2345", "Unique"), "ABCD-EFGH-2345");
  assert.equal(formatCouponCode("CHUSEOK2026", "Shared"), "CHUSEOK2026");
});
