import type { Role } from "./types.ts";

// Admin.Api 의 AdminPolicies 와 동일한 규칙. 화면은 버튼 노출만 제어하고, 최종 권한 검사는 API 가 한다.
export type Permission = "view" | "content" | "reward";

const grants: Record<Permission, Role[]> = {
  view: ["Admin", "Operator", "CS"],
  content: ["Admin", "Operator"],
  reward: ["Admin"],
};

export function can(roles: readonly Role[], permission: Permission): boolean {
  return roles.some((r) => grants[permission].includes(r));
}

export const roleLabels: Record<Role, string> = {
  Admin: "관리자",
  Operator: "운영자",
  CS: "CS",
};
