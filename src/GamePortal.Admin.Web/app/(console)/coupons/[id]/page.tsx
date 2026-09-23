import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { GrantStatusChip } from "@/components/Chip";
import { PageHeader } from "@/components/PageHeader";
import { Usage } from "@/components/Usage";
import { Pager } from "@/components/Pager";
import { adminApi, withQuery } from "@/lib/api";
import { AdminApiError } from "@/lib/errors";
import { formatCouponCode, formatKst, formatNumber } from "@/lib/format";
import { itemName } from "@/lib/items";
import { can } from "@/lib/roles";
import { getSession } from "@/lib/session";
import type { CampaignDetail, PagedResult, Redemption } from "@/lib/types";
import { EnableToggle } from "../EnableToggle";

export const metadata: Metadata = { title: "쿠폰 캠페인 상세" };

export default async function CampaignDetailPage({
  params,
  searchParams,
}: {
  params: Promise<{ id: string }>;
  searchParams: Promise<{ created?: string; page?: string }>;
}) {
  const id = Number((await params).id);
  if (!Number.isInteger(id)) notFound();
  const sp = await searchParams;
  const page = Math.max(1, Number(sp.page) || 1);
  const session = (await getSession())!;
  const isAdmin = can(session.roles, "reward");

  let campaign: CampaignDetail;
  let redemptions: PagedResult<Redemption>;
  try {
    [campaign, redemptions] = await Promise.all([
      adminApi<CampaignDetail>(`/api/v1/coupon-campaigns/${id}`),
      adminApi<PagedResult<Redemption>>(withQuery("/api/v1/coupon-redemptions", { campaignId: id, page, pageSize: 20 })),
    ]);
  } catch (e) {
    if (e instanceof AdminApiError && e.status === 404) notFound();
    throw e;
  }

  return (
    <>
      <PageHeader
        title={campaign.name}
        description={`#${campaign.id} · ${campaign.type === "Shared" ? "공용 코드" : "고유 코드"} · 발행 ${formatKst(campaign.createdAt)} (운영자 #${campaign.createdBy})`}
        actions={
          isAdmin && (
            <>
              {campaign.type === "Unique" && (
                // 파일 다운로드는 Route Handler 가 Admin.Api 스트림을 그대로 중계 (토큰은 서버에만)
                <a className="btn btn--secondary" href={`/api/coupons/${campaign.id}/codes`} download>
                  코드 CSV 다운로드
                </a>
              )}
              <Link className="btn btn--secondary" href={`/audit-logs?entityName=CouponCampaign&entityId=${campaign.id}`}>변경 이력</Link>
              <EnableToggle id={campaign.id} enabled={campaign.isEnabled} />
            </>
          )
        }
      />
      {sp.created && <div className="notice notice--ok"><p>쿠폰을 발행했습니다. 사용 기간이 되면 홈페이지에서 바로 등록할 수 있습니다.</p></div>}
      {!campaign.isEnabled && <div className="notice notice--error"><p>사용 중지된 캠페인입니다. 유저가 코드를 입력하면 “사용이 중지된 쿠폰” 안내가 나갑니다.</p></div>}

      <section className="summary card">
        <dl>
          <div>
            <dt>사용 현황</dt>
            <dd><Usage used={campaign.redeemedCount} max={campaign.maxRedemptions} /></dd>
          </div>
          <div>
            <dt>발행 코드 수</dt>
            <dd className="tabular">{formatNumber(campaign.codeCount)}</dd>
          </div>
          <div>
            <dt>기간 (KST)</dt>
            <dd>{formatKst(campaign.startsAt)} ~ {formatKst(campaign.endsAt)}</dd>
          </div>
          <div>
            <dt>보상</dt>
            <dd>
              <ul className="reward-list">
                {campaign.rewards.map((r) => (
                  <li key={r.itemId}>{itemName(r.itemId)} <span className="tabular">×{formatNumber(r.quantity)}</span></li>
                ))}
              </ul>
            </dd>
          </div>
        </dl>
      </section>

      <h2 className="section-title">사용 내역</h2>
      <div className="table-wrap card card--flush">
        <table className="table">
          <thead>
            <tr>
              <th scope="col">계정</th>
              <th scope="col">코드</th>
              <th scope="col">사용 일시 (KST)</th>
              <th scope="col">게임 서버 지급</th>
            </tr>
          </thead>
          <tbody>
            {redemptions.items.length === 0 && <tr><td colSpan={4} className="empty">아직 사용 내역이 없습니다.</td></tr>}
            {redemptions.items.map((r) => (
              <tr key={r.redemptionId}>
                <td><Link href={`/cs?accountId=${r.accountId}`} className="table__link tabular">{r.accountId}</Link></td>
                <td className="mono">{formatCouponCode(r.code, r.campaignType)}</td>
                <td className="nowrap">{formatKst(r.redeemedAt)}</td>
                <td><GrantStatusChip status={r.grantStatus} /></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <Pager basePath={`/coupons/${campaign.id}`} query={{}} page={redemptions.page} totalPages={redemptions.totalPages} />
    </>
  );
}
