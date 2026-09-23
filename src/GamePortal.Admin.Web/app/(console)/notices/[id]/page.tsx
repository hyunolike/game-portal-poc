import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { PageHeader } from "@/components/PageHeader";
import { adminApi } from "@/lib/api";
import { AdminApiError } from "@/lib/errors";
import { formatKst, isoToKstLocal } from "@/lib/format";
import { can } from "@/lib/roles";
import { getSession } from "@/lib/session";
import type { AdminNotice } from "@/lib/types";
import { updateNotice } from "../actions";
import { DeleteNoticeButton } from "../DeleteNoticeButton";
import { NoticeForm } from "../NoticeForm";

export const metadata: Metadata = { title: "공지 수정" };

export default async function NoticeDetailPage({
  params,
  searchParams,
}: {
  params: Promise<{ id: string }>;
  searchParams: Promise<{ saved?: string }>;
}) {
  const id = Number((await params).id);
  if (!Number.isInteger(id)) notFound();
  const { saved } = await searchParams;
  const session = (await getSession())!;
  const editable = can(session.roles, "content");

  let notice: AdminNotice;
  try {
    notice = await adminApi<AdminNotice>(`/api/v1/notices/${id}`);
  } catch (e) {
    if (e instanceof AdminApiError && e.status === 404) notFound();
    throw e;
  }

  return (
    <>
      <PageHeader
        title={editable ? "공지 수정" : "공지 보기"}
        description={`#${notice.id} · 작성 ${formatKst(notice.createdAt)} (운영자 #${notice.createdBy})${
          notice.updatedAt ? ` · 수정 ${formatKst(notice.updatedAt)} (운영자 #${notice.updatedBy})` : ""
        }`}
        actions={
          <>
            {can(session.roles, "reward") && (
              <Link className="btn btn--secondary" href={`/audit-logs?entityName=Notice&entityId=${notice.id}`}>변경 이력</Link>
            )}
            {editable && <DeleteNoticeButton id={notice.id} />}
          </>
        }
      />
      {saved && <div className="notice notice--ok"><p>등록했습니다. 홈페이지에 즉시 반영됩니다.</p></div>}
      <NoticeForm
        action={updateNotice.bind(null, notice.id)}
        mode="edit"
        readOnly={!editable}
        defaults={{
          category: notice.category,
          title: notice.title,
          content: notice.content,
          isPinned: notice.isPinned,
          isPublished: notice.isPublished,
          publishAtLocal: isoToKstLocal(notice.publishAt),
        }}
      />
    </>
  );
}
