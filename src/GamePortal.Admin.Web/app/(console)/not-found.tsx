import Link from "next/link";

export default function NotFound() {
  return (
    <div className="card error-page">
      <h1>찾을 수 없습니다</h1>
      <p>삭제되었거나 존재하지 않는 항목입니다.</p>
      <Link href="/" className="btn btn--secondary">대시보드로</Link>
    </div>
  );
}
