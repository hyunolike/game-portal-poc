import type { NoticeCategory, OutboxStatus } from "@/lib/types";

type Tone = "neutral" | "ok" | "warn" | "danger" | "info";

export function Chip({ tone = "neutral", children }: { tone?: Tone; children: React.ReactNode }) {
  return <span className={`chip chip--${tone}`}>{children}</span>;
}

const grantTone: Record<OutboxStatus, Tone> = { Processed: "ok", Pending: "warn", Failed: "danger" };
const grantLabel: Record<OutboxStatus, string> = { Processed: "지급 완료", Pending: "지급 대기", Failed: "지급 실패" };

export function GrantStatusChip({ status }: { status: OutboxStatus | null }) {
  if (!status) return <Chip>기록 없음</Chip>;
  return <Chip tone={grantTone[status]}>{grantLabel[status]}</Chip>;
}

export const categoryLabel: Record<NoticeCategory, string> = {
  Notice: "공지",
  Update: "업데이트",
  Event: "이벤트",
  Maintenance: "점검",
};

const categoryTone: Record<NoticeCategory, Tone> = { Notice: "neutral", Update: "info", Event: "ok", Maintenance: "warn" };

export function CategoryChip({ category }: { category: NoticeCategory }) {
  return <Chip tone={categoryTone[category]}>{categoryLabel[category]}</Chip>;
}
