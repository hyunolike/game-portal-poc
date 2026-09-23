// 컨테이너/LB 헬스체크용 (인증 불필요)
export function GET() {
  return Response.json({ status: "Healthy" });
}
