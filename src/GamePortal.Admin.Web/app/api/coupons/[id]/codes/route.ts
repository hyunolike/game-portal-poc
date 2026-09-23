import { rawAdminApi } from "@/lib/api";
import { AdminApiError } from "@/lib/errors";

// 코드 CSV 는 수십만 행일 수 있으므로 Admin.Api 응답 스트림을 버퍼링 없이 그대로 전달한다.
export async function GET(_request: Request, { params }: { params: Promise<{ id: string }> }) {
  const id = Number((await params).id);
  if (!Number.isInteger(id)) return new Response("Bad Request", { status: 400 });

  try {
    const upstream = await rawAdminApi(`/api/v1/coupon-campaigns/${id}/codes.csv`);
    return new Response(upstream.body, {
      headers: {
        "Content-Type": upstream.headers.get("Content-Type") ?? "text/csv; charset=utf-8",
        "Content-Disposition": upstream.headers.get("Content-Disposition") ?? `attachment; filename=coupon-codes-${id}.csv`,
        "Cache-Control": "no-store",
      },
    });
  } catch (e) {
    if (e instanceof AdminApiError) return new Response(e.message || e.code, { status: e.status });
    throw e;
  }
}
