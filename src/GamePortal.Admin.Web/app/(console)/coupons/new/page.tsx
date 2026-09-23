import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { PageHeader } from "@/components/PageHeader";
import { isoToKstLocal } from "@/lib/format";
import { can } from "@/lib/roles";
import { getSession } from "@/lib/session";
import { CampaignForm } from "../CampaignForm";

export const metadata: Metadata = { title: "쿠폰 발행" };

export default async function NewCampaignPage() {
  const session = (await getSession())!;
  if (!can(session.roles, "reward")) redirect("/coupons");

  const now = new Date();
  now.setUTCSeconds(0, 0);
  const inAWeek = new Date(now.getTime() + 7 * 24 * 60 * 60 * 1000);

  return (
    <>
      <PageHeader title="쿠폰 발행" description="발행 즉시 감사 로그에 기록됩니다. 설정 오류가 발견되면 상세 화면에서 즉시 사용 중지할 수 있습니다." />
      <CampaignForm defaultStart={isoToKstLocal(now.toISOString())} defaultEnd={isoToKstLocal(inAWeek.toISOString())} />
    </>
  );
}
