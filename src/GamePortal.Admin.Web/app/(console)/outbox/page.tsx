import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";
import { GrantStatusChip } from "@/components/Chip";
import { PageHeader } from "@/components/PageHeader";
import { Pager } from "@/components/Pager";
import { adminApi, withQuery } from "@/lib/api";
import { formatKst, formatNumber } from "@/lib/format";
import { itemName } from "@/lib/items";
import { can } from "@/lib/roles";
import { getSession } from "@/lib/session";
import type { OutboxMessage, OutboxStats, OutboxStatus, PagedResult } from "@/lib/types";
import { RetryButton } from "./RetryButton";

export const metadata: Metadata = { title: "지급 모니터링" };

const statuses: { value: OutboxStatus; label: string }[] = [
  { value: "Failed", label: "실패" },
  { value: "Pending", label: "대기" },
  { value: "Processed", label: "완료" },
];

interface GrantPayload {
  AccountId: number;
  Items: { ItemId: number; Quantity: number }[];
  Reason: string;
}

function parsePayload(payload: string): GrantPayload | null {
  try {
    return JSON.parse(payload) as GrantPayload;
  } catch {
    return null;
  }
}

export default async function OutboxPage({ searchParams }: { searchParams: Promise<{ status?: string; page?: string; retried?: string }> }) {
  const session = (await getSession())!;
  if (!can(session.roles, "content")) redirect("/");

  const sp = await searchParams;
  const status = statuses.some((s) => s.value === sp.status) ? (sp.status as OutboxStatus) : "Failed";
  const page = Math.max(1, Number(sp.page) || 1);
  const canRetry = can(session.roles, "reward");

  const [stats, result] = await Promise.all([
    adminApi<OutboxStats>("/api/v1/outbox/stats"),
    adminApi<PagedResult<OutboxMessage>>(withQuery("/api/v1/outbox", { status, page, pageSize: 20 })),
  ]);
  const counts: Record<OutboxStatus, string> = {
    Failed: formatNumber(stats.failed),
    Pending: formatNumber(stats.pending),
    Processed: `최근 1시간 ${formatNumber(stats.processedLastHour)}`,
  };

  return (
    <>
      <PageHeader
        title="지급 모니터링"
        description="쿠폰 보상의 게임 서버 우편함 전달 현황. 실패 건은 자동 재시도(최대 10회)가 모두 실패한 요청입니다."
      />
      {sp.retried && (
        <div className="notice notice--ok" role="status">
          <p>
            지급 요청 #{sp.retried} 을(를) 재처리 대기열에 넣었습니다. 진행 상황은 <Link href="/outbox?status=Pending">대기</Link> ·{" "}
            <Link href="/outbox?status=Processed">완료</Link> 탭에서 확인할 수 있습니다.
          </p>
        </div>
      )}
      <nav className="tabs" aria-label="상태">
        {statuses.map((s) => (
          <Link key={s.value} href={`/outbox?status=${s.value}`} className={status === s.value ? "is-active" : undefined}>
            {s.label} <span className="tabs__count">{counts[s.value]}</span>
          </Link>
        ))}
      </nav>

      <div className="table-wrap card card--flush">
        <table className="table">
          <thead>
            <tr>
              <th scope="col">상태</th>
              <th scope="col">계정</th>
              <th scope="col">지급 내용</th>
              <th scope="col" className="num">시도</th>
              <th scope="col">생성 (KST)</th>
              <th scope="col">{status === "Processed" ? "완료 (KST)" : "다음 시도 / 마지막 오류"}</th>
              {status === "Failed" && canRetry && <th scope="col"><span className="sr-only">작업</span></th>}
            </tr>
          </thead>
          <tbody>
            {result.items.length === 0 && (
              <tr><td colSpan={7} className="empty">{status === "Failed" ? "실패한 지급 요청이 없습니다." : "해당 상태의 요청이 없습니다."}</td></tr>
            )}
            {result.items.map((m) => {
              const p = parsePayload(m.payload);
              return (
                <tr key={m.id}>
                  <td><GrantStatusChip status={m.status} /></td>
                  <td>{p ? <Link href={`/cs?accountId=${p.AccountId}`} className="table__link tabular">{p.AccountId}</Link> : "-"}</td>
                  <td>
                    {p ? p.Items.map((i) => `${itemName(i.ItemId)} ×${formatNumber(i.Quantity)}`).join(", ") : m.type}
                    {p && <div className="small muted">{p.Reason}</div>}
                  </td>
                  <td className="num">{m.attemptCount}</td>
                  <td className="nowrap">{formatKst(m.createdAt)}</td>
                  <td>
                    {m.status === "Processed" ? (
                      <span className="nowrap">{formatKst(m.processedAt)}</span>
                    ) : (
                      <>
                        {m.status === "Pending" && <div className="nowrap">{formatKst(m.nextAttemptAt)}</div>}
                        {m.lastError && <div className="error-text" title={m.lastError}>{m.lastError}</div>}
                      </>
                    )}
                  </td>
                  {status === "Failed" && canRetry && <td><RetryButton id={m.id} /></td>}
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
      <Pager basePath="/outbox" query={{ status }} page={result.page} totalPages={result.totalPages} />
    </>
  );
}
