import { NextResponse, type NextRequest } from "next/server";

const TOKEN_COOKIE = "gp_admin_token";

// 로그인하지 않은 요청은 페이지 렌더링 전에 로그인 화면으로 보낸다.
export function middleware(request: NextRequest) {
  if (request.cookies.has(TOKEN_COOKIE)) return NextResponse.next();

  const login = new URL("/login", request.url);
  const next = request.nextUrl.pathname + request.nextUrl.search;
  if (next !== "/") login.searchParams.set("next", next);
  return NextResponse.redirect(login);
}

export const config = {
  matcher: ["/((?!login|api/health|_next/static|_next/image|favicon.ico).*)"],
};
