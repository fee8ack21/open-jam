import i18n from '@/i18n'
import { PaymentStatus } from '@/api/payment-service'

/** 付款狀態的顯示資訊。 */
export interface PaymentStatusMeta {
  labelKey: string
  type: 'default' | 'info' | 'success' | 'warning' | 'error'
}

/** 付款狀態 → i18n 標籤鍵與標籤樣式（Processing / Cancelled 為後端保留值，目前流程不會產生）。 */
export const PAYMENT_STATUS_META: Record<PaymentStatus, PaymentStatusMeta> = {
  [PaymentStatus.Pending]: { labelKey: 'paymentStatus.pending', type: 'warning' },
  [PaymentStatus.Processing]: { labelKey: 'paymentStatus.processing', type: 'info' },
  [PaymentStatus.Succeeded]: { labelKey: 'paymentStatus.succeeded', type: 'success' },
  [PaymentStatus.Failed]: { labelKey: 'paymentStatus.failed', type: 'error' },
  [PaymentStatus.Cancelled]: { labelKey: 'paymentStatus.cancelled', type: 'default' },
  [PaymentStatus.Expired]: { labelKey: 'paymentStatus.expired', type: 'default' },
}

/** 取得付款狀態的顯示資訊；未知狀態退回灰色標籤。 */
export function paymentStatusMeta(status?: PaymentStatus): PaymentStatusMeta {
  return (status && PAYMENT_STATUS_META[status]) || { labelKey: 'paymentStatus.unknown', type: 'default' }
}

/** 狀態篩選下拉選項（含「全部」＝ 'all' sentinel）；僅列目前流程會產生的狀態。 */
export function paymentStatusOptions(): { label: string; value: PaymentStatus | 'all' }[] {
  const t = i18n.global.t
  return [
    { label: t('paymentStatus.all'), value: 'all' },
    { label: t('paymentStatus.pending'), value: PaymentStatus.Pending },
    { label: t('paymentStatus.succeeded'), value: PaymentStatus.Succeeded },
    { label: t('paymentStatus.failed'), value: PaymentStatus.Failed },
    { label: t('paymentStatus.expired'), value: PaymentStatus.Expired },
  ]
}
