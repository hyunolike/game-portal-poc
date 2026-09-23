import "server-only";
import { cookies } from "next/headers";
import type { Role } from "./types.ts";

export const TOKEN_COOKIE = "gp_admin_token";

export interface Session {
  operatorId: number;
  name: string;
  roles: Role[];
  expiresAt: Date;
}

/**
 * 쿠키의 JWT 에서 표시용 정보만 읽는다. 서명 검증은 토큰을 실제로 받는 Admin.Api 가 한다
 * (여기서 위조 토큰을 받아도 API 호출은 모두 401 이 되므로 권한 상승이 불가능).
 */
export async function getSession(): Promise<Session | null> {
  const token = (await cookies()).get(TOKEN_COOKIE)?.value;
  if (!token) return null;
  return decodeSession(token);
}

export function decodeSession(token: string): Session | null {
  try {
    const payload = JSON.parse(Buffer.from(token.split(".")[1] ?? "", "base64url").toString("utf8")) as {
      sub?: string;
      name?: string;
      role?: string | string[];
      exp?: number;
    };
    if (!payload.sub || !payload.exp) return null;
    const expiresAt = new Date(payload.exp * 1000);
    if (expiresAt.getTime() <= Date.now()) return null;
    const roles = (Array.isArray(payload.role) ? payload.role : payload.role ? [payload.role] : []) as Role[];
    return { operatorId: Number(payload.sub), name: payload.name ?? `operator${payload.sub}`, roles, expiresAt };
  } catch {
    return null;
  }
}
