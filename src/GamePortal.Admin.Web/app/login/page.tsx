import type { Metadata } from "next";
import { LoginForm } from "./LoginForm";

export const metadata: Metadata = { title: "로그인" };

export default async function LoginPage({ searchParams }: { searchParams: Promise<{ next?: string; expired?: string }> }) {
  const { next, expired } = await searchParams;
  const devLoginEnabled = process.env.ADMIN_DEV_LOGIN === "true";

  return (
    <div className="login">
      <div className="login__card">
        <div className="login__brand">
          <span className="sidebar__logo">ETHERFALL</span>
          <span>운영툴</span>
        </div>
        {expired && <div className="notice notice--warn"><p>로그인이 만료되었습니다. 다시 로그인해 주세요.</p></div>}
        {devLoginEnabled ? (
          <>
            <p className="hint">개발용 로그인입니다. 운영 환경에서는 사내 SSO 로그인으로 대체됩니다.</p>
            <LoginForm next={next} />
          </>
        ) : (
          <p>사내 SSO 로그인이 필요합니다. (이 환경에서는 개발용 로그인이 꺼져 있습니다)</p>
        )}
      </div>
    </div>
  );
}
