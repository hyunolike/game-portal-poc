"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { runAction, type ActionState } from "@/lib/action";
import { adminApi } from "@/lib/api";
import { kstLocalToIso } from "@/lib/format";

export async function createCampaign(_prev: ActionState, formData: FormData): Promise<ActionState> {
  const type = String(formData.get("type"));
  const itemIds = formData.getAll("rewardItemId").map(Number);
  const quantities = formData.getAll("rewardQuantity").map(Number);
  const maxRedemptions = String(formData.get("maxRedemptions") ?? "").trim();

  let id = 0;
  const result = await runAction(async () => {
    const created = await adminApi<{ id: number }>("/api/v1/coupon-campaigns", {
      method: "POST",
      body: {
        name: String(formData.get("name") ?? ""),
        type,
        startsAt: kstLocalToIso(String(formData.get("startsAt"))),
        endsAt: kstLocalToIso(String(formData.get("endsAt"))),
        maxRedemptions: maxRedemptions ? Number(maxRedemptions) : null,
        rewards: itemIds.map((itemId, i) => ({ itemId, quantity: quantities[i] ?? 0 })),
        sharedCode: type === "Shared" ? String(formData.get("sharedCode") ?? "") : null,
        uniqueCodeCount: type === "Unique" ? Number(formData.get("uniqueCodeCount")) : null,
      },
    });
    id = created.id;
  });
  if (result.error) return result;

  revalidatePath("/coupons");
  redirect(`/coupons/${id}?created=1`);
}

export async function setCampaignEnabled(id: number, enabled: boolean, _prev: ActionState): Promise<ActionState> {
  const result = await runAction(() =>
    adminApi(`/api/v1/coupon-campaigns/${id}/${enabled ? "enable" : "disable"}`, { method: "POST" }),
  );
  if (result.error) return result;

  revalidatePath(`/coupons/${id}`);
  revalidatePath("/coupons");
  return { ok: enabled ? "사용을 재개했습니다." : "사용을 중지했습니다. 진행 중인 요청을 포함해 즉시 차단됩니다." };
}
