"use client";

import { useActionState } from "react";
import { FormMessage } from "@/components/FormMessage";
import { SubmitButton } from "@/components/SubmitButton";
import { setCampaignEnabled } from "./actions";

export function EnableToggle({ id, enabled }: { id: number; enabled: boolean }) {
  const [state, action] = useActionState(setCampaignEnabled.bind(null, id, !enabled), {});
  return (
    <form action={action} className="inline-form">
      {enabled ? (
        <SubmitButton
          variant="danger"
          pendingText="중지 중…"
          confirmMessage="쿠폰 사용을 즉시 중지할까요? (코드 유출·보상 설정 오류 대응용)"
        >
          사용 중지
        </SubmitButton>
      ) : (
        <SubmitButton variant="secondary" pendingText="재개 중…">사용 재개</SubmitButton>
      )}
      <FormMessage state={state} />
    </form>
  );
}
