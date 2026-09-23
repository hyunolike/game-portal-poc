"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { runAction, type ActionState } from "@/lib/action";
import { adminApi } from "@/lib/api";

export async function retryOutbox(id: number, _prev: ActionState): Promise<ActionState> {
  const result = await runAction(() => adminApi(`/api/v1/outbox/${id}/retry`, { method: "POST" }));
  if (result.error) return result;

  // 재처리하면 해당 행이 "실패" 목록에서 사라지므로(버튼과 함께 언마운트) 결과는 페이지 단위로 알린다
  revalidatePath("/outbox");
  redirect(`/outbox?status=Failed&retried=${id}`);
}
