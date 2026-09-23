import { formatNumber } from "@/lib/format";

/** 선착순 캠페인은 소진율 막대, 무제한 캠페인은 사용 수만 */
export function Usage({ used, max }: { used: number; max: number | null }) {
  if (!max) return <span className="tabular">{formatNumber(used)} 사용</span>;
  const pct = Math.min(100, Math.round((used / max) * 100));
  return (
    <span className="usage">
      <span className="usage__bar" aria-hidden="true"><span style={{ width: `${pct}%` }} /></span>
      <span className="tabular">{formatNumber(used)} / {formatNumber(max)}</span>
    </span>
  );
}
