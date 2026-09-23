"use client";

import { useActionState } from "react";
import { FormMessage } from "@/components/FormMessage";
import { SubmitButton } from "@/components/SubmitButton";
import { devLogin } from "./actions";

export function LoginForm({ next }: { next?: string }) {
  const [state, action] = useActionState(devLogin, {});
  return (
    <form action={action} className="form">
      <input type="hidden" name="next" value={next ?? "/"} />
      <FormMessage state={state} />
      <div className="field">
        <label htmlFor="operatorId">운영자 번호</label>
        <input id="operatorId" name="operatorId" type="number" min={1} defaultValue={1} required />
      </div>
      <div className="field">
        <label htmlFor="name">이름</label>
        <input id="name" name="name" defaultValue="김운영" maxLength={20} required />
      </div>
      <fieldset className="field">
        <legend>역할</legend>
        <div className="segmented">
          <label>
            <input type="radio" name="role" value="Admin" defaultChecked /> 관리자
          </label>
          <label>
            <input type="radio" name="role" value="Operator" /> 운영자
          </label>
          <label>
            <input type="radio" name="role" value="CS" /> CS
          </label>
        </div>
        <p className="hint">관리자: 쿠폰 발행·지급 재처리·감사 로그 / 운영자: 공지 관리·지급 모니터링 / CS: 조회만</p>
      </fieldset>
      <SubmitButton pendingText="로그인 중…">로그인</SubmitButton>
    </form>
  );
}
