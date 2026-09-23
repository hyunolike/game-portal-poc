// 서버는 UTC(ISO 8601) 로 주고받고, 운영툴 표시·입력은 한국 시간(KST, UTC+9) 기준.

const KST_OFFSET_MS = 9 * 60 * 60 * 1000;

function pad(n: number): string {
  return String(n).padStart(2, "0");
}

function kstParts(iso: string) {
  const d = new Date(new Date(iso).getTime() + KST_OFFSET_MS);
  return {
    y: d.getUTCFullYear(),
    m: pad(d.getUTCMonth() + 1),
    d: pad(d.getUTCDate()),
    hh: pad(d.getUTCHours()),
    mm: pad(d.getUTCMinutes()),
  };
}

export function formatKst(iso: string | null | undefined): string {
  if (!iso) return "-";
  const p = kstParts(iso);
  return `${p.y}.${p.m}.${p.d} ${p.hh}:${p.mm}`;
}

/** <input type="datetime-local"> 값(KST 로 입력받음) → ISO(+09:00) */
export function kstLocalToIso(local: string): string {
  if (!/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}$/.test(local)) {
    throw new Error(`잘못된 일시 형식: ${local}`);
  }
  return `${local}:00+09:00`;
}

/** ISO → <input type="datetime-local"> 기본값(KST) */
export function isoToKstLocal(iso: string): string {
  const p = kstParts(iso);
  return `${p.y}-${p.m}-${p.d}T${p.hh}:${p.mm}`;
}

export function formatNumber(n: number): string {
  return n.toLocaleString("ko-KR");
}

/** 쿠폰 코드 배포용 표기: ABCDEFGH2345 → ABCD-EFGH-2345 */
export function formatCouponCode(code: string): string {
  return code.match(/.{1,4}/g)?.join("-") ?? code;
}
