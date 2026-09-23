"use server";

import { cookies } from "next/headers";
import { redirect } from "next/navigation";
import { adminApiUrl } from "@/lib/api";
import type { ActionState } from "@/lib/action";
import { TOKEN_COOKIE, decodeSession } from "@/lib/session";

/** 내부 경로만 허용 (//evil.com 같은 protocol-relative URL 로 인한 open redirect 방지) */
function safeNext(value: FormDataEntryValue | null): string {
  const next = typeof value === "string" ? value : "";
  return next.startsWith("/") && !next.startsWith("//") && !next.startsWith("/\\") ? next : "/";
}

export async function devLogin(_prev: ActionState, formData: FormData): Promise<ActionState> {
  if (process.env.ADMIN_DEV_LOGIN !== "true") {
    return { error: "개발용 로그인이 비활성화되어 있습니다. 사내 SSO 로 로그인해 주세요." };
  }

  const operatorId = Number(formData.get("operatorId"));
  const name = String(formData.get("name") ?? "").trim();
  const role = String(formData.get("role") ?? "");
  if (!Number.isInteger(operatorId) || operatorId <= 0 || !name || !["Admin", "Operator", "CS"].includes(role)) {
    return { error: "운영자 번호, 이름, 역할을 모두 입력해 주세요." };
  }

  let token: string;
  try {
    const response = await fetch(adminApiUrl("/dev/token"), {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ operatorId, name, roles: [role] }),
      cache: "no-store",
    });
    if (!response.ok) return { error: `토큰 발급에 실패했습니다 (HTTP ${response.status}).` };
    token = ((await response.json()) as { accessToken: string }).accessToken;
  } catch {
    return { error: "Admin API 에 연결하지 못했습니다. ADMIN_API_URL 설정을 확인해 주세요." };
  }

  const session = decodeSession(token);
  if (!session) return { error: "발급된 토큰을 해석하지 못했습니다." };

  (await cookies()).set(TOKEN_COOKIE, token, {
    httpOnly: true, // JS 접근 차단 → XSS 로 토큰 탈취 불가
    sameSite: "lax",
    secure: process.env.NODE_ENV === "production" && process.env.ADMIN_INSECURE_COOKIE !== "true",
    path: "/",
    expires: session.expiresAt,
  });

  redirect(safeNext(formData.get("next")));
}

export async function logout() {
  (await cookies()).delete(TOKEN_COOKIE);
  redirect("/login");
}
