/* ============================================================
   公告通知任務顯示輔助：狀態標籤 / 樣式、篩選選項、時間格式化
   （對應 NotificationService 的 NotificationRequestStatus）
   ============================================================ */
import { NotificationRequestStatus } from '@/api/notification-service'
import i18n from '@/i18n'

/** n-tag 的語意色彩。 */
export type RequestStatusTagType = 'default' | 'info' | 'success' | 'warning' | 'error'

export interface RequestStatusMeta {
  /** i18n key under `requestStatus.*`；以 `t(labelKey)` 取得當前語系標籤。 */
  labelKey: string
  type: RequestStatusTagType
}

/** 通知任務狀態 → i18n 標籤鍵與標籤樣式。 */
export const REQUEST_STATUS_META: Record<NotificationRequestStatus, RequestStatusMeta> = {
  [NotificationRequestStatus.Pending]: { labelKey: 'requestStatus.pending', type: 'warning' },
  [NotificationRequestStatus.Dispatched]: { labelKey: 'requestStatus.dispatched', type: 'success' },
  [NotificationRequestStatus.Cancelled]: { labelKey: 'requestStatus.cancelled', type: 'default' },
  [NotificationRequestStatus.Failed]: { labelKey: 'requestStatus.failed', type: 'error' },
}

/** 取得通知任務狀態的顯示資訊；未知狀態退回灰色標籤。 */
export function requestStatusMeta(status?: NotificationRequestStatus): RequestStatusMeta {
  return (status && REQUEST_STATUS_META[status]) || { labelKey: 'requestStatus.unknown', type: 'default' }
}

/** 狀態篩選下拉選項（含「全部」＝ null）；標籤依當前語系產生。 */
export function requestStatusOptions(): { label: string; value: NotificationRequestStatus | null }[] {
  const t = i18n.global.t
  return [
    { label: t('requestStatus.all'), value: null },
    { label: t('requestStatus.pending'), value: NotificationRequestStatus.Pending },
    { label: t('requestStatus.dispatched'), value: NotificationRequestStatus.Dispatched },
    { label: t('requestStatus.cancelled'), value: NotificationRequestStatus.Cancelled },
    { label: t('requestStatus.failed'), value: NotificationRequestStatus.Failed },
  ]
}

/** 依當前語系格式化任務時間。 */
export function formatRequestTime(iso?: string | null): string {
  return iso ? new Date(iso).toLocaleString(i18n.global.locale.value, { hour12: false }) : '—'
}
