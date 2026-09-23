"use client";

import { useActionState } from "react";
import { SubmitButton } from "@/components/SubmitButton";
import { retryOutbox } from "./actions";

export function RetryButton({ id }: { id: number }) {
  const [state, action] = useActionState(retryOutbox.bind(null, id), {});
  return (
    <form action={action}>
      <SubmitButton variant="secondary" pendingText="요청 중…" confirmMessage="이 지급 요청을 다시 게임 서버로 보낼까요? (게임 서버는 요청 ID 로 중복 지급을 막습니다)">
        재처리
      </SubmitButton>
      {state.error && <p className="text-danger small">{state.error}</p>}
    </form>
  );
}
