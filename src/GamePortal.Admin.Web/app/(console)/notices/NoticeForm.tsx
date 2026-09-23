"use client";

import { useActionState } from "react";
import { FormMessage } from "@/components/FormMessage";
import { SubmitButton } from "@/components/SubmitButton";
import type { ActionState } from "@/lib/action";
import type { NoticeCategory } from "@/lib/types";

export interface NoticeFormValues {
  category: NoticeCategory;
  title: string;
  content: string;
  isPinned: boolean;
  isPublished: boolean;
  publishAtLocal: string;
}

export function NoticeForm({
  action,
  defaults,
  mode,
  readOnly,
}: {
  action: (prev: ActionState, formData: FormData) => Promise<ActionState>;
  defaults?: NoticeFormValues;
  mode: "create" | "edit";
  readOnly: boolean;
}) {
  const [state, formAction] = useActionState(action, {});

  return (
    <form action={formAction} className="form card">
      <FormMessage state={state} />
      <fieldset disabled={readOnly} className="form__fields">
        <div className="form__row">
          <div className="field field--narrow">
            <label htmlFor="category">분류</label>
            <select id="category" name="category" defaultValue={defaults?.category ?? "Notice"}>
              <option value="Notice">공지</option>
              <option value="Update">업데이트</option>
              <option value="Event">이벤트</option>
              <option value="Maintenance">점검</option>
            </select>
          </div>
          <div className="field">
            <label htmlFor="title">제목</label>
            <input id="title" name="title" defaultValue={defaults?.title} maxLength={200} required />
          </div>
        </div>
        <div className="field">
          <label htmlFor="content">본문</label>
          <textarea id="content" name="content" rows={12} defaultValue={defaults?.content} required />
          <p className="hint">평문으로 입력합니다. HTML 태그는 홈페이지에서 글자 그대로 보입니다. 빈 줄로 문단을 나눕니다.</p>
        </div>
        <div className="form__row">
          <div className="field field--narrow">
            <label htmlFor="publishAt">게시 일시 (KST)</label>
            <input id="publishAt" name="publishAt" type="datetime-local" defaultValue={defaults?.publishAtLocal} />
            <p className="hint">{mode === "create" ? "비워 두면 즉시 게시됩니다." : "미래 시각이면 예약 게시됩니다."}</p>
          </div>
          <div className="field">
            <span className="label">옵션</span>
            <label className="check">
              <input type="checkbox" name="isPinned" defaultChecked={defaults?.isPinned} /> 상단 고정
              <span className="hint"> · 점검 공지를 고정하면 홈페이지 상단 배너로 노출됩니다</span>
            </label>
            {mode === "edit" && (
              <label className="check">
                <input type="checkbox" name="isPublished" defaultChecked={defaults?.isPublished} /> 게시
              </label>
            )}
          </div>
        </div>
      </fieldset>
      {!readOnly && (
        <div className="form__actions">
          <SubmitButton pendingText="저장 중…">{mode === "create" ? "등록" : "저장"}</SubmitButton>
        </div>
      )}
    </form>
  );
}
