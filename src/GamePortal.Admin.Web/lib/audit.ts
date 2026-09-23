import { formatKst } from "./format.ts";
import { itemName } from "./items.ts";

// 감사 로그의 변경값은 DB 원시값(enum 숫자, UTC ISO, JSON)이다. 운영자가 바로 읽을 수 있게 변환한다.

const noticeCategory: Record<number, string> = { 1: "공지", 2: "업데이트", 3: "이벤트", 4: "점검" };
const couponType: Record<number, string> = { 1: "공용 코드", 2: "고유 코드" };

const isoDate = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?(Z|[+-]\d{2}:\d{2})$/;

export function formatAuditValue(entityName: string, field: string, value: unknown): string {
  if (value === null || value === undefined) return "∅";
  if (typeof value === "boolean") return value ? "예" : "아니오";

  if (field === "Category" && entityName === "Notice" && typeof value === "number") return noticeCategory[value] ?? String(value);
  if (field === "Type" && entityName === "CouponCampaign" && typeof value === "number") return couponType[value] ?? String(value);

  if (field === "Rewards" && Array.isArray(value)) {
    return value
      .map((r: { ItemId?: number; Quantity?: number }) => `${itemName(Number(r.ItemId))} ×${Number(r.Quantity).toLocaleString("ko-KR")}`)
      .join(", ");
  }

  if (typeof value === "string") {
    if (isoDate.test(value)) return `${formatKst(value)} KST`;
    return value.length > 120 ? `${value.slice(0, 120)}…` : value;
  }

  return JSON.stringify(value);
}
