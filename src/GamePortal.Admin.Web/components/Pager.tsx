import Link from "next/link";

/** 서버 컴포넌트 페이지네이션. 현재 검색 조건(query)을 유지한 채 page 만 바꾼다. */
export function Pager({
  basePath,
  query,
  page,
  totalPages,
}: {
  basePath: string;
  query: Record<string, string | undefined>;
  page: number;
  totalPages: number;
}) {
  if (totalPages <= 1) return null;
  const href = (p: number) => {
    const params = new URLSearchParams();
    for (const [k, v] of Object.entries(query)) if (v) params.set(k, v);
    params.set("page", String(p));
    return `${basePath}?${params.toString()}`;
  };
  const first = Math.max(1, page - 2);
  const last = Math.min(totalPages, first + 4);
  const pages = Array.from({ length: last - first + 1 }, (_, i) => first + i);

  return (
    <nav className="pager" aria-label="페이지">
      {page > 1 && <Link href={href(page - 1)} aria-label="이전 페이지">‹</Link>}
      {pages.map((p) => (
        <Link key={p} href={href(p)} className={p === page ? "is-active" : undefined} aria-current={p === page ? "page" : undefined}>
          {p}
        </Link>
      ))}
      {page < totalPages && <Link href={href(page + 1)} aria-label="다음 페이지">›</Link>}
    </nav>
  );
}
