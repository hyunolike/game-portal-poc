import type { Metadata } from "next";
import Link from "next/link";
import { CategoryChip, Chip, categoryLabel } from "@/components/Chip";
import { PageHeader } from "@/components/PageHeader";
import { Pager } from "@/components/Pager";
import { adminApi, withQuery } from "@/lib/api";
import { formatKst } from "@/lib/format";
import { can } from "@/lib/roles";
import { getSession } from "@/lib/session";
import type { AdminNotice, NoticeCategory, PagedResult } from "@/lib/types";

export const metadata: Metadata = { title: "공지 관리" };

const categories = Object.keys(categoryLabel) as NoticeCategory[];

export default async function NoticesPage({
  searchParams,
}: {
  searchParams: Promise<{ category?: string; page?: string; deleted?: string }>;
}) {
  const sp = await searchParams;
  const category = categories.includes(sp.category as NoticeCategory) ? sp.category : undefined;
  const page = Math.max(1, Number(sp.page) || 1);
  const session = (await getSession())!;

  const result = await adminApi<PagedResult<AdminNotice>>(withQuery("/api/v1/notices", { category, page, pageSize: 20 }));
  const now = Date.now();

  return (
    <>
      <PageHeader
        title="공지 관리"
        description="홈페이지 공지사항. 저장하면 캐시가 무효화되어 즉시 반영됩니다."
        actions={can(session.roles, "content") && <Link className="btn btn--primary" href="/notices/new">새 공지</Link>}
      />
      {sp.deleted && <div className="notice notice--ok"><p>공지를 삭제했습니다.</p></div>}

      <nav className="tabs" aria-label="분류">
        <Link href="/notices" className={!category ? "is-active" : undefined}>전체</Link>
        {categories.map((c) => (
          <Link key={c} href={`/notices?category=${c}`} className={category === c ? "is-active" : undefined}>
            {categoryLabel[c]}
          </Link>
        ))}
      </nav>

      <div className="table-wrap card card--flush">
        <table className="table">
          <thead>
            <tr>
              <th scope="col" className="num">번호</th>
              <th scope="col">분류</th>
              <th scope="col">제목</th>
              <th scope="col">상태</th>
              <th scope="col">게시 일시 (KST)</th>
              <th scope="col">최종 수정</th>
            </tr>
          </thead>
          <tbody>
            {result.items.length === 0 && (
              <tr><td colSpan={6} className="empty">공지가 없습니다.</td></tr>
            )}
            {result.items.map((n) => {
              const scheduled = n.isPublished && new Date(n.publishAt).getTime() > now;
              return (
                <tr key={n.id}>
                  <td className="num">{n.id}</td>
                  <td><CategoryChip category={n.category} /></td>
                  <td>
                    <Link href={`/notices/${n.id}`} className="table__link">{n.title}</Link>
                    {n.isPinned && <span className="pin" title="상단 고정">고정</span>}
                  </td>
                  <td>
                    {!n.isPublished ? <Chip>비공개</Chip> : scheduled ? <Chip tone="info">예약</Chip> : <Chip tone="ok">게시 중</Chip>}
                  </td>
                  <td className="nowrap">{formatKst(n.publishAt)}</td>
                  <td className="nowrap muted">{formatKst(n.updatedAt ?? n.createdAt)}</td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
      <Pager basePath="/notices" query={{ category }} page={result.page} totalPages={result.totalPages} />
    </>
  );
}
