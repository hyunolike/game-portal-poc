import type { Metadata } from "next";
import Link from "next/link";
import { Chip } from "@/components/Chip";
import { PageHeader } from "@/components/PageHeader";
import { Pager } from "@/components/Pager";
import { Usage } from "@/components/Usage";
import { adminApi, withQuery } from "@/lib/api";
import { formatKst } from "@/lib/format";
import { can } from "@/lib/roles";
import { getSession } from "@/lib/session";
import type { CampaignSummary, PagedResult } from "@/lib/types";

export const metadata: Metadata = { title: "쿠폰 캠페인" };

function periodState(c: CampaignSummary, now: number) {
  if (!c.isEnabled) return <Chip tone="danger">중지됨</Chip>;
  if (now < new Date(c.startsAt).getTime()) return <Chip tone="info">시작 전</Chip>;
  if (now >= new Date(c.endsAt).getTime()) return <Chip>종료</Chip>;
  if (c.maxRedemptions && c.redeemedCount >= c.maxRedemptions) return <Chip tone="warn">소진</Chip>;
  return <Chip tone="ok">진행 중</Chip>;
}

export default async function CouponsPage({ searchParams }: { searchParams: Promise<{ page?: string }> }) {
  const page = Math.max(1, Number((await searchParams).page) || 1);
  const session = (await getSession())!;
  const result = await adminApi<PagedResult<CampaignSummary>>(withQuery("/api/v1/coupon-campaigns", { page, pageSize: 20 }));
  const now = Date.now();

  return (
    <>
      <PageHeader
        title="쿠폰 캠페인"
        description="쿠폰 발행과 사용 현황. 보상 지급은 되돌릴 수 없으므로 발행은 관리자만 할 수 있습니다."
        actions={can(session.roles, "reward") && <Link className="btn btn--primary" href="/coupons/new">쿠폰 발행</Link>}
      />
      <div className="table-wrap card card--flush">
        <table className="table">
          <thead>
            <tr>
              <th scope="col" className="num">번호</th>
              <th scope="col">캠페인</th>
              <th scope="col">유형</th>
              <th scope="col">상태</th>
              <th scope="col">사용 현황</th>
              <th scope="col">기간 (KST)</th>
            </tr>
          </thead>
          <tbody>
            {result.items.length === 0 && <tr><td colSpan={6} className="empty">발행된 캠페인이 없습니다.</td></tr>}
            {result.items.map((c) => (
              <tr key={c.id}>
                <td className="num">{c.id}</td>
                <td><Link href={`/coupons/${c.id}`} className="table__link">{c.name}</Link></td>
                <td>{c.type === "Shared" ? "공용" : "고유"}</td>
                <td>{periodState(c, now)}</td>
                <td className="nowrap">
                  <Usage used={c.redeemedCount} max={c.maxRedemptions} />
                </td>
                <td className="nowrap muted">{formatKst(c.startsAt)} ~ {formatKst(c.endsAt)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <Pager basePath="/coupons" query={{}} page={result.page} totalPages={result.totalPages} />
    </>
  );
}
