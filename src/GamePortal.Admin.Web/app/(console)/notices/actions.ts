"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { runAction, type ActionState } from "@/lib/action";
import { adminApi } from "@/lib/api";
import { kstLocalToIso } from "@/lib/format";

function readNotice(formData: FormData) {
  const publishAt = String(formData.get("publishAt") ?? "");
  return {
    category: String(formData.get("category")),
    title: String(formData.get("title") ?? ""),
    content: String(formData.get("content") ?? ""),
    isPinned: formData.get("isPinned") === "on",
    isPublished: formData.get("isPublished") === "on",
    publishAt: publishAt ? kstLocalToIso(publishAt) : null,
  };
}

export async function createNotice(_prev: ActionState, formData: FormData): Promise<ActionState> {
  let id = 0;
  const result = await runAction(async () => {
    const n = readNotice(formData);
    const created = await adminApi<{ id: number }>("/api/v1/notices", {
      method: "POST",
      body: { category: n.category, title: n.title, content: n.content, isPinned: n.isPinned, publishAt: n.publishAt },
    });
    id = created.id;
  });
  if (result.error) return result;

  revalidatePath("/notices");
  redirect(`/notices/${id}?saved=1`);
}

export async function updateNotice(id: number, _prev: ActionState, formData: FormData): Promise<ActionState> {
  const result = await runAction(async () => {
    const n = readNotice(formData);
    await adminApi(`/api/v1/notices/${id}`, {
      method: "PUT",
      body: { ...n, publishAt: n.publishAt ?? new Date().toISOString() },
    });
  });
  if (result.error) return result;

  revalidatePath("/notices");
  return { ok: "저장했습니다. 홈페이지에 즉시 반영됩니다." };
}

export async function deleteNotice(id: number, _prev: ActionState): Promise<ActionState> {
  const result = await runAction(() => adminApi(`/api/v1/notices/${id}`, { method: "DELETE" }));
  if (result.error) return result;

  revalidatePath("/notices");
  redirect("/notices?deleted=1");
}
