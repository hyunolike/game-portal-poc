"use client";

import { useActionState, useState } from "react";
import { FormMessage } from "@/components/FormMessage";
import { SubmitButton } from "@/components/SubmitButton";
import { formatNumber } from "@/lib/format";
import { itemCatalog, itemName } from "@/lib/items";
import { createCampaign } from "./actions";

interface RewardRow {
  key: number;
  itemId: number;
  quantity: number;
}

let nextKey = 1;

export function CampaignForm({ defaultStart, defaultEnd }: { defaultStart: string; defaultEnd: string }) {
  const [state, action] = useActionState(createCampaign, {});
  const [type, setType] = useState<"Shared" | "Unique">("Shared");
  const [sharedCode, setSharedCode] = useState("");
  const [count, setCount] = useState(1000);
  const [max, setMax] = useState("");
  const [rewards, setRewards] = useState<RewardRow[]>([{ key: 0, itemId: 1001, quantity: 100 }]);

  const updateRow = (key: number, patch: Partial<RewardRow>) =>
    setRewards((rows) => rows.map((r) => (r.key === key ? { ...r, ...patch } : r)));

  // 보상 지급은 되돌리기 어려우므로 발행 직전에 요약을 한 번 더 확인받는다
  const summary = [
    type === "Shared" ? `공용 코드 ${sharedCode.toUpperCase() || "(미입력)"}` : `고유 코드 ${formatNumber(count)}개`,
    max ? `선착순 ${formatNumber(Number(max))}명` : "수량 무제한",
    `보상: ${rewards.map((r) => `${itemName(r.itemId)} ×${formatNumber(r.quantity)}`).join(", ")}`,
  ].join("\n");

  return (
    <form action={action} className="form card">
      <FormMessage state={state} />

      <div className="field">
        <label htmlFor="name">캠페인 이름</label>
        <input id="name" name="name" maxLength={100} placeholder="예: 시즌 3 오픈 기념 쿠폰" required />
        <p className="hint">유저 쿠폰 사용 내역에 그대로 보입니다.</p>
      </div>

      <fieldset className="field">
        <legend>코드 유형</legend>
        <div className="segmented">
          <label>
            <input type="radio" name="type" value="Shared" checked={type === "Shared"} onChange={() => setType("Shared")} />
            공용 코드 <span className="hint">방송·커뮤니티 배포, 계정당 1회</span>
          </label>
          <label>
            <input type="radio" name="type" value="Unique" checked={type === "Unique"} onChange={() => setType("Unique")} />
            고유 코드 <span className="hint">패키지·제휴처 배포, 코드당 1회</span>
          </label>
        </div>
      </fieldset>

      <div className="form__row">
        {type === "Shared" ? (
          <div className="field">
            <label htmlFor="sharedCode">공용 코드</label>
            <input
              id="sharedCode"
              name="sharedCode"
              value={sharedCode}
              onChange={(e) => setSharedCode(e.target.value)}
              placeholder="OPEN2026"
              pattern="[A-Za-z0-9\- ]{4,24}"
              className="mono upper"
              required
            />
            <p className="hint">영문·숫자 4~20자. 하이픈과 대소문자는 구분하지 않습니다.</p>
          </div>
        ) : (
          <div className="field">
            <label htmlFor="uniqueCodeCount">발행 수량</label>
            <input
              id="uniqueCodeCount"
              name="uniqueCodeCount"
              type="number"
              min={1}
              max={100000}
              value={count}
              onChange={(e) => setCount(Number(e.target.value))}
              required
            />
            <p className="hint">최대 100,000개. 발행 후 상세 화면에서 CSV 로 내려받습니다.</p>
          </div>
        )}
        <div className="field">
          <label htmlFor="maxRedemptions">선착순 수량 (선택)</label>
          <input id="maxRedemptions" name="maxRedemptions" type="number" min={1} value={max} onChange={(e) => setMax(e.target.value)} placeholder="무제한" />
        </div>
      </div>

      <div className="form__row">
        <div className="field">
          <label htmlFor="startsAt">시작 (KST)</label>
          <input id="startsAt" name="startsAt" type="datetime-local" defaultValue={defaultStart} required />
        </div>
        <div className="field">
          <label htmlFor="endsAt">종료 (KST)</label>
          <input id="endsAt" name="endsAt" type="datetime-local" defaultValue={defaultEnd} required />
          <p className="hint">종료 시각 정각부터 사용할 수 없습니다.</p>
        </div>
      </div>

      <fieldset className="field">
        <legend>보상 아이템</legend>
        <div className="rewards">
          {rewards.map((r) => (
            <div className="rewards__row" key={r.key}>
              <select
                name="rewardItemId"
                value={r.itemId}
                onChange={(e) => updateRow(r.key, { itemId: Number(e.target.value) })}
                aria-label="아이템"
              >
                {Object.entries(itemCatalog).map(([id, label]) => (
                  <option key={id} value={id}>
                    {label} (#{id})
                  </option>
                ))}
              </select>
              <input
                name="rewardQuantity"
                type="number"
                min={1}
                max={99999}
                value={r.quantity}
                onChange={(e) => updateRow(r.key, { quantity: Number(e.target.value) })}
                aria-label="수량"
              />
              <button
                type="button"
                className="btn btn--secondary btn--sm"
                onClick={() => setRewards((rows) => rows.filter((x) => x.key !== r.key))}
                disabled={rewards.length === 1}
              >
                삭제
              </button>
            </div>
          ))}
        </div>
        <button
          type="button"
          className="btn btn--secondary btn--sm"
          onClick={() => setRewards((rows) => [...rows, { key: nextKey++, itemId: 1002, quantity: 1 }])}
        >
          + 아이템 추가
        </button>
      </fieldset>

      <div className="form__actions">
        <SubmitButton pendingText="발행 중…" confirmMessage={`아래 내용으로 쿠폰을 발행할까요?\n\n${summary}`}>
          쿠폰 발행
        </SubmitButton>
      </div>
    </form>
  );
}
