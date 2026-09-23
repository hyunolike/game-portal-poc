import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { Chip } from "@/components/Chip";
import { PageHeader } from "@/components/PageHeader";
import { Pager } from "@/components/Pager";
import { adminApi, withQuery } from "@/lib/api";
import { formatAuditValue } from "@/lib/audit";
import { formatKst } from "@/lib/format";
import { can } from "@/lib/roles";
import { getSession } from "@/lib/session";
import type { AuditLog, PagedResult } from "@/lib/types";

export const metadata: Metadata = { title: "감사 로그" };

const actionLabel = { Added: "생성", Modified: "수정", Deleted: "삭제" } as const;
const actionTone = { Added: "ok", Modified: "info", Deleted: "danger" } as const;
const entityLabel: Record<string, string> = { Notice: "공지", CouponCampaign: "쿠폰 캠페인" };

type Change = unknown | { before: unknown; after: unknown };

function Changes({ entityName, json }: { entityName: string; json: string | null }) {
  const renderValue = (field: string, v: unknown) => formatAuditValue(entityName, field, v);
  if (!json) return <span className="muted">-</span>;
  let parsed: Record<string, Change>;
  try {
    parsed = JSON.parse(json) as Record<string, Change>;
  } catch {
    return <code className="small">{json}</code>;
  }
  return (
    <dl className="changes">
      {Object.entries(parsed).map(([field, change]) => {
        const diff = change && typeof change === "object" && "before" in change && "after" in change;
        return (
          <div key={field}>
            <dt>{field}</dt>
            <dd>
              {diff ? (
                <>
                  <del>{renderValue(field, (change as { before: unknown }).before)}</del> → <ins>{renderValue(field, (change as { after: unknown }).after)}</ins>
                </>
              ) : (
                renderValue(field, change)
              )}
            </dd>
          </div>
        );
      })}
    </dl>
  );
}

export default async function AuditLogsPage({
  searchParams,
}: {
  searchParams: Promise<{ entityName?: string; entityId?: string; operatorId?: string; page?: string }>;
}) {
  const session = (await getSession())!;
  if (!can(session.roles, "reward")) redirect("/");

  const sp = await searchParams;
  const page = Math.max(1, Number(sp.page) || 1);
  const query = { entityName: sp.entityName || undefined, entityId: sp.entityId || undefined, operatorId: sp.operatorId || undefined };
  const result = await adminApi<PagedResult<AuditLog>>(withQuery("/api/v1/audit-logs", { ...query, page, pageSize: 30 }));

  return (
    <>
      <PageHeader title="감사 로그" description="운영 데이터(공지, 쿠폰 캠페인)의 모든 변경 기록. 수정·삭제할 수 없습니다." />
      <form className="search card" role="search">
        <div className="field">
          <label htmlFor="entityName">대상</label>
          <select id="entityName" name="entityName" defaultValue={query.entityName ?? ""}>
            <option value="">전체</option>
            <option value="Notice">공지</option>
            <option value="CouponCampaign">쿠폰 캠페인</option>
          </select>
        </div>
        <div className="field">
          <label htmlFor="entityId">대상 번호</label>
          <input id="entityId" name="entityId" inputMode="numeric" defaultValue={query.entityId} />
        </div>
        <div className="field">
          <label htmlFor="operatorId">운영자 번호</label>
          <input id="operatorId" name="operatorId" inputMode="numeric" defaultValue={query.operatorId} />
        </div>
        <button type="submit" className="btn btn--primary">검색</button>
      </form>

      <div className="table-wrap card card--flush">
        <table className="table table--top">
          <thead>
            <tr>
              <th scope="col">일시 (KST)</th>
              <th scope="col">운영자</th>
              <th scope="col">작업</th>
              <th scope="col">대상</th>
              <th scope="col">변경 내용</th>
              <th scope="col">IP</th>
            </tr>
          </thead>
          <tbody>
            {result.items.length === 0 && <tr><td colSpan={6} className="empty">기록이 없습니다.</td></tr>}
            {result.items.map((a) => (
              <tr key={a.id}>
                <td className="nowrap">{formatKst(a.occurredAt)}</td>
                <td className="nowrap">{a.operatorName} <span className="muted">#{a.operatorId}</span></td>
                <td><Chip tone={actionTone[a.action]}>{actionLabel[a.action]}</Chip></td>
                <td className="nowrap">{entityLabel[a.entityName] ?? a.entityName} #{a.entityId}</td>
                <td><Changes entityName={a.entityName} json={a.changes} /></td>
                <td className="mono small muted">{a.ipAddress ?? "-"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <Pager basePath="/audit-logs" query={query} page={result.page} totalPages={result.totalPages} />
    </>
  );
}
