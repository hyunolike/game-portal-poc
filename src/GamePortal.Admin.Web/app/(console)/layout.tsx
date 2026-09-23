import { redirect } from "next/navigation";
import { logout } from "@/app/login/actions";
import { Nav, type NavItem } from "@/components/Nav";
import { can, roleLabels } from "@/lib/roles";
import { getSession } from "@/lib/session";

export default async function ConsoleLayout({ children }: { children: React.ReactNode }) {
  const session = await getSession();
  if (!session) redirect("/login?expired=1");

  // 메뉴는 역할에 따라 노출. (숨김은 편의일 뿐, 실제 차단은 Admin.Api 정책이 한다)
  const items: NavItem[] = [
    { href: "/", label: "대시보드" },
    { href: "/notices", label: "공지 관리" },
    { href: "/coupons", label: "쿠폰 캠페인" },
    { href: "/cs", label: "CS 조회" },
    ...(can(session.roles, "content") ? [{ href: "/outbox", label: "지급 모니터링" }] : []),
    ...(can(session.roles, "reward") ? [{ href: "/audit-logs", label: "감사 로그" }] : []),
  ];

  return (
    <div className="shell">
      <aside className="sidebar">
        <div className="sidebar__brand">
          <span className="sidebar__logo">ETHERFALL</span>
          <span className="sidebar__sub">운영툴</span>
        </div>
        <Nav items={items} />
        <div className="sidebar__user">
          <div>
            <strong>{session.name}</strong>
            <span>
              #{session.operatorId} · {session.roles.map((r) => roleLabels[r] ?? r).join(", ")}
            </span>
          </div>
          <form action={logout}>
            <button type="submit" className="link-button">로그아웃</button>
          </form>
        </div>
      </aside>
      <main className="main">{children}</main>
    </div>
  );
}
