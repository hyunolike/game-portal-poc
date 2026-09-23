"use client";

import { useFormStatus } from "react-dom";

/** 제출 중 비활성화 → 더블 클릭으로 인한 중복 요청(쿠폰 이중 발행 등) 방지 */
export function SubmitButton({
  children,
  pendingText = "처리 중…",
  variant = "primary",
  confirmMessage,
}: {
  children: React.ReactNode;
  pendingText?: string;
  variant?: "primary" | "secondary" | "danger";
  confirmMessage?: string;
}) {
  const { pending } = useFormStatus();
  return (
    <button
      type="submit"
      className={`btn btn--${variant}`}
      disabled={pending}
      onClick={(e) => {
        if (confirmMessage && !window.confirm(confirmMessage)) e.preventDefault();
      }}
    >
      {pending ? pendingText : children}
    </button>
  );
}
