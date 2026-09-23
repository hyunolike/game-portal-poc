// Admin.Api 응답 계약 (enum 은 서버에서 문자열로 직렬화됨)

export type Role = "Admin" | "Operator" | "CS";

export type NoticeCategory = "Notice" | "Update" | "Event" | "Maintenance";
export type CouponType = "Shared" | "Unique";
export type OutboxStatus = "Pending" | "Processed" | "Failed";

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface AdminNotice {
  id: number;
  category: NoticeCategory;
  title: string;
  content: string;
  isPinned: boolean;
  isPublished: boolean;
  publishAt: string;
  createdBy: number;
  createdAt: string;
  updatedBy: number | null;
  updatedAt: string | null;
}

export interface RewardItem {
  itemId: number;
  quantity: number;
}

export interface CampaignSummary {
  id: number;
  name: string;
  type: CouponType;
  startsAt: string;
  endsAt: string;
  maxRedemptions: number | null;
  redeemedCount: number;
  isEnabled: boolean;
}

export interface CampaignDetail extends CampaignSummary {
  rewards: RewardItem[];
  codeCount: number;
  createdBy: number;
  createdAt: string;
}

export interface Redemption {
  redemptionId: number;
  campaignId: number;
  campaignName: string;
  code: string;
  accountId: number;
  redeemedAt: string;
  grantRequestId: string;
  grantStatus: OutboxStatus | null;
  campaignType: CouponType;
}

export interface OutboxStats {
  pending: number;
  failed: number;
  processedLastHour: number;
}

export interface OutboxMessage {
  id: number;
  messageId: string;
  type: string;
  status: OutboxStatus;
  attemptCount: number;
  createdAt: string;
  nextAttemptAt: string;
  processedAt: string | null;
  lastError: string | null;
  payload: string;
}

export interface AuditLog {
  id: number;
  operatorId: number;
  operatorName: string;
  action: "Added" | "Modified" | "Deleted";
  entityName: string;
  entityId: string;
  changes: string | null;
  ipAddress: string | null;
  occurredAt: string;
}
