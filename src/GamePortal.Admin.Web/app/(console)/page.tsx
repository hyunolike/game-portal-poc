import Link from "next/link";
import { CategoryChip } from "@/components/Chip";
import { PageHeader } from "@/components/PageHeader";
import { adminApi } from "@/lib/api";
import { formatKst, formatNumber } from "@/lib/format";
import { can } from "@/lib/roles";
import { getSession } from "@/lib/session";
import type { AdminNotice, CampaignSummary, OutboxStats, PagedResult } from "@/lib/types";

export default async function DashboardPage() {
  const session = (await getSession())!;
  const canMonitor = can(session.roles, "content");

  // 서로 독립적인 조회는 병렬로
  const [stats, campaigns, notices] = await Promise.all([
    canMonitor ? adminApi<OutboxStats>("/api/v1/outbox/stats") : Promise.resolve(null),
    adminApi<PagedResult<CampaignSummary>>("/api/v1/coupon-campaigns?pageSize=5"),
    adminApi<PagedResult<AdminNotice>>("/api/v1/notices?pageSize=5"),
  ]);

  return (
    <>
      <PageHeader title="대시보드" description={`${session.name}님, 오늘도 무사고 운영을 기원합니다.`} />

      {stats && (
        <section className="stats" aria-label="게임 서버 지급 현황">
          <Link href="/outbox?status=Failed" className={`stat ${stats.failed > 0 ? "stat--alert" : ""}`}>
            <span className="stat__label">지급 실패</span>
            <span className="stat__value">{formatNumber(stats.failed)}</span>
            <span className="stat__hint">{stats.failed > 0 ? "확인 후 재처리 필요" : "이상 없음"}</span>
          </Link>
          <Link href="/outbox?status=Pending" className="stat">
            <span className="stat__label">지급 대기</span>
            <span className="stat__value">{formatNumber(stats.pending)}</span>
            <span className="stat__hint">Worker 처리 대기 중</span>
          </Link>
          <Link href="/outbox?status=Processed" className="stat">
            <span className="stat__label">최근 1시간 지급</span>
            <span className="stat__value">{formatNumber(stats.processedLastHour)}</span>
            <span className="stat__hint">게임 서버 전달 완료</span>
          </Link>
        </section>
      )}

      <div className="grid-2">
        <section className="card">
          <div className="card__head">
            <h2>최근 쿠폰 캠페인</h2>
            <Link href="/coupons">전체 보기</Link>
          </div>
          {campaigns.items.length === 0 ? (
            <p className="empty">발행된 캠페인이 없습니다.</p>
          ) : (
            <ul className="list">
              {campaigns.items.map((c) => (
                <li key={c.id}>
                  <Link href={`/coupons/${c.id}`}>{c.name}</Link>
                  <span className="list__meta">
                    {formatNumber(c.redeemedCount)}
                    {c.maxRedemptions ? ` / ${formatNumber(c.maxRedemptions)}` : ""} 사용
                    {!c.isEnabled && <strong className="text-danger"> · 중지됨</strong>}
                  </span>
                </li>
              ))}
            </ul>
          )}
        </section>

        <section className="card">
          <div className="card__head">
            <h2>최근 공지</h2>
            <Link href="/notices">전체 보기</Link>
          </div>
          {notices.items.length === 0 ? (
            <p className="empty">작성된 공지가 없습니다.</p>
          ) : (
            <ul className="list">
              {notices.items.map((n) => (
                <li key={n.id}>
                  <span className="list__title">
                    <CategoryChip category={n.category} />
                    <Link href={`/notices/${n.id}`}>{n.title}</Link>
                  </span>
                  <span className="list__meta">{formatKst(n.publishAt)}</span>
                </li>
              ))}
            </ul>
          )}
        </section>
      </div>
    </>
  );
}
