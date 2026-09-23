import type { Metadata } from "next";
import Link from "next/link";
import { GrantStatusChip } from "@/components/Chip";
import { PageHeader } from "@/components/PageHeader";
import { Pager } from "@/components/Pager";
import { adminApi, withQuery } from "@/lib/api";
import { formatCouponCode, formatKst } from "@/lib/format";
import type { PagedResult, Redemption } from "@/lib/types";

export const metadata: Metadata = { title: "CS 조회" };

export default async function CsPage({ searchParams }: { searchParams: Promise<{ accountId?: string; page?: string }> }) {
  const sp = await searchParams;
  const accountId = sp.accountId && /^\d+$/.test(sp.accountId.trim()) ? sp.accountId.trim() : undefined;
  const page = Math.max(1, Number(sp.page) || 1);

  const result = accountId
    ? await adminApi<PagedResult<Redemption>>(withQuery("/api/v1/coupon-redemptions", { accountId, page, pageSize: 20 }))
    : null;

  return (
    <>
      <PageHeader title="CS 조회" description="“쿠폰을 썼는데 아이템이 안 왔어요” 문의 대응. 계정의 쿠폰 사용 이력과 게임 서버 지급 상태를 함께 확인합니다." />

      <form className="search card" role="search">
        <div className="field">
          <label htmlFor="accountId">계정 번호</label>
          <input id="accountId" name="accountId" inputMode="numeric" defaultValue={accountId} placeholder="예: 10001" autoFocus />
        </div>
        <button type="submit" className="btn btn--primary">조회</button>
      </form>
      {sp.accountId && !accountId && <div className="notice notice--error"><p>계정 번호는 숫자만 입력해 주세요.</p></div>}

      {result && (
        <>
          <h2 className="section-title">계정 #{accountId} 쿠폰 사용 이력 <span className="muted">{result.totalCount}건</span></h2>
          <div className="table-wrap card card--flush">
            <table className="table">
              <thead>
                <tr>
                  <th scope="col">사용 일시 (KST)</th>
                  <th scope="col">캠페인</th>
                  <th scope="col">코드</th>
                  <th scope="col">게임 서버 지급</th>
                  <th scope="col">지급 요청 ID</th>
                </tr>
              </thead>
              <tbody>
                {result.items.length === 0 && <tr><td colSpan={5} className="empty">쿠폰 사용 이력이 없습니다. 유저가 입력한 코드가 실패했을 가능성이 높습니다.</td></tr>}
                {result.items.map((r) => (
                  <tr key={r.redemptionId}>
                    <td className="nowrap">{formatKst(r.redeemedAt)}</td>
                    <td><Link href={`/coupons/${r.campaignId}`} className="table__link">{r.campaignName}</Link></td>
                    <td className="mono">{formatCouponCode(r.code)}</td>
                    <td><GrantStatusChip status={r.grantStatus} /></td>
                    <td className="mono small muted">{r.grantRequestId}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <Pager basePath="/cs" query={{ accountId }} page={result.page} totalPages={result.totalPages} />
          <aside className="guide card">
            <h3>안내 가이드</h3>
            <ul>
              <li><strong>지급 완료</strong>: 게임 내 우편함에 도착했습니다. 우편함 보관 기간(30일) 경과 여부를 확인해 주세요.</li>
              <li><strong>지급 대기</strong>: 게임 서버 전달 중입니다. 보통 수 초~수 분 내 처리됩니다.</li>
              <li><strong>지급 실패</strong>: 자동 재시도가 모두 실패했습니다. 운영자에게 지급 모니터링 화면에서 재처리를 요청해 주세요.</li>
            </ul>
          </aside>
        </>
      )}
    </>
  );
}
