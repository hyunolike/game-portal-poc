"use client";

import { useActionState } from "react";
import { FormMessage } from "@/components/FormMessage";
import { SubmitButton } from "@/components/SubmitButton";
import { deleteNotice } from "./actions";

export function DeleteNoticeButton({ id }: { id: number }) {
  const [state, action] = useActionState(deleteNotice.bind(null, id), {});
  return (
    <form action={action}>
      <FormMessage state={state} />
      <SubmitButton variant="danger" pendingText="삭제 중…" confirmMessage="이 공지를 삭제할까요? 홈페이지에서 즉시 내려갑니다.">
        삭제
      </SubmitButton>
    </form>
  );
}
