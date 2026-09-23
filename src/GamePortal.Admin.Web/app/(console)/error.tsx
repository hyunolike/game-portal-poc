"use client";

// Admin.Api 장애 등 예상하지 못한 오류. 원인 추적용 digest 를 함께 보여준다.
export default function ConsoleError({ error, reset }: { error: Error & { digest?: string }; reset: () => void }) {
  return (
    <div className="card error-page">
      <h1>화면을 불러오지 못했습니다</h1>
      <p>Admin API 에 일시적인 문제가 있을 수 있습니다. 잠시 후 다시 시도해 주세요.</p>
      {error.digest && <p className="small muted">오류 ID: <code>{error.digest}</code></p>}
      <button type="button" className="btn btn--primary" onClick={reset}>다시 시도</button>
    </div>
  );
}
