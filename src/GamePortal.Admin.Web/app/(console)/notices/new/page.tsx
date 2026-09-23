import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { PageHeader } from "@/components/PageHeader";
import { can } from "@/lib/roles";
import { getSession } from "@/lib/session";
import { createNotice } from "../actions";
import { NoticeForm } from "../NoticeForm";

export const metadata: Metadata = { title: "새 공지" };

export default async function NewNoticePage() {
  const session = (await getSession())!;
  if (!can(session.roles, "content")) redirect("/notices");

  return (
    <>
      <PageHeader title="새 공지" />
      <NoticeForm action={createNotice} mode="create" readOnly={false} />
    </>
  );
}
