import "server-only";
import { cookies } from "next/headers";
import { redirect } from "next/navigation";
import { toAdminApiError } from "./errors.ts";
import { TOKEN_COOKIE } from "./session.ts";

export function adminApiUrl(path: string): string {
  const base = (process.env.ADMIN_API_URL ?? "http://localhost:5200").replace(/\/$/, "");
  return `${base}${path}`;
}

type Query = Record<string, string | number | boolean | null | undefined>;

export function withQuery(path: string, query: Query): string {
  const params = new URLSearchParams();
  for (const [k, v] of Object.entries(query)) {
    if (v !== undefined && v !== null && v !== "") params.set(k, String(v));
  }
  const qs = params.toString();
  return qs ? `${path}?${qs}` : path;
}

/**
 * Admin.Api 호출 (서버 전용). 운영자 토큰은 httpOnly 쿠키에서 꺼내 Bearer 로 전달한다.
 * 브라우저는 Admin.Api 주소도, 토큰도 알지 못한다.
 */
export async function adminApi<T>(path: string, init: { method?: string; body?: unknown } = {}): Promise<T> {
  const response = await rawAdminApi(path, init);
  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}

export async function rawAdminApi(path: string, init: { method?: string; body?: unknown } = {}): Promise<Response> {
  const token = (await cookies()).get(TOKEN_COOKIE)?.value;
  if (!token) redirect("/login");

  const response = await fetch(adminApiUrl(path), {
    method: init.method ?? "GET",
    headers: {
      Authorization: `Bearer ${token}`,
      Accept: "application/json",
      ...(init.body !== undefined ? { "Content-Type": "application/json" } : {}),
    },
    body: init.body !== undefined ? JSON.stringify(init.body) : undefined,
    cache: "no-store", // 운영툴은 항상 최신 상태를 본다
  });

  // 토큰 만료/폐기 → 다시 로그인
  if (response.status === 401) redirect("/login?expired=1");

  if (!response.ok) {
    throw toAdminApiError(response.status, await response.text());
  }

  return response;
}
