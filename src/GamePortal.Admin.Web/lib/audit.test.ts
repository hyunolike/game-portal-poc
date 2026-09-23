import assert from "node:assert/strict";
import { test } from "node:test";
import { formatAuditValue } from "./audit.ts";

test("enum 숫자를 운영자용 이름으로 바꾼다", () => {
  assert.equal(formatAuditValue("Notice", "Category", 4), "점검");
  assert.equal(formatAuditValue("CouponCampaign", "Type", 1), "공용 코드");
});

test("UTC ISO 시각은 KST 로 표시한다", () => {
  assert.equal(formatAuditValue("Notice", "PublishAt", "2026-09-24T21:00:00+00:00"), "2026.09.25 06:00 KST");
  assert.equal(formatAuditValue("Notice", "UpdatedAt", "2026-09-23T06:28:42.4382985+00:00"), "2026.09.23 15:28 KST");
});

test("보상 JSON 은 아이템 이름과 수량으로 표시한다", () => {
  assert.equal(
    formatAuditValue("CouponCampaign", "Rewards", [{ ItemId: 1001, Quantity: 100 }, { ItemId: 9999, Quantity: 1 }]),
    "에테르 주화 ×100, 아이템 #9999 ×1",
  );
});

test("불리언과 null 도 읽기 쉽게 표시한다", () => {
  assert.equal(formatAuditValue("CouponCampaign", "IsEnabled", false), "아니오");
  assert.equal(formatAuditValue("Notice", "UpdatedBy", null), "∅");
});
