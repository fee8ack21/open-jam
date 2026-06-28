/* ============================================================
   訂單顯示輔助：金額格式化、狀態標籤 / 樣式、篩選選項
   （對應 OrderService 的 OrderStatus 與最低貨幣單位金額）
   ============================================================ */
import { OrderStatus } from '@/api/order-service'
import i18n from '@/i18n'

/** Stripe 零位小數貨幣：金額即為實際面額，不需除以 100。 */
const ZERO_DECIMAL_CURRENCIES = new Set([
  'bif', 'clp', 'djf', 'gnf', 'jpy', 'kmf', 'krw', 'mga',
  'pyg', 'rwf', 'ugx', 'vnd', 'vuv', 'xaf', 'xof', 'xpf',
])

/**
 * 將「最低貨幣單位」金額（如 cents）格式化為帶貨幣符號的字串。
 * 零位小數貨幣（如 JPY）不除以 100；未知貨幣代碼退回以代碼前綴顯示。
 */
export function formatOrderAmount(minor?: number, currency?: string | null): string {
  const cur = (currency || 'usd').toLowerCase()
  const amount = (minor ?? 0) / (ZERO_DECIMAL_CURRENCIES.has(cur) ? 1 : 100)
  try {
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: cur.toUpperCase() }).format(amount)
  } catch {
    return `${cur.toUpperCase()} ${amount.toLocaleString('en-US')}`
  }
}

/** n-tag 的語意色彩。 */
export type OrderStatusTagType = 'default' | 'info' | 'success' | 'warning' | 'error'

export interface OrderStatusMeta {
  /** i18n key under `orderStatus.*`；以 `t(labelKey)` 取得當前語系標籤。 */
  labelKey: string
  type: OrderStatusTagType
}

/** 訂單狀態 → i18n 標籤鍵與標籤樣式。 */
export const ORDER_STATUS_META: Record<OrderStatus, OrderStatusMeta> = {
  [OrderStatus.Pending]: { labelKey: 'orderStatus.pending', type: 'warning' },
  [OrderStatus.Paid]: { labelKey: 'orderStatus.paid', type: 'info' },
  [OrderStatus.Completed]: { labelKey: 'orderStatus.completed', type: 'success' },
  [OrderStatus.Cancelled]: { labelKey: 'orderStatus.cancelled', type: 'default' },
  [OrderStatus.Refunded]: { labelKey: 'orderStatus.refunded', type: 'error' },
}

/** 取得訂單狀態的顯示資訊；未知狀態退回灰色標籤。 */
export function orderStatusMeta(status?: OrderStatus): OrderStatusMeta {
  return (status && ORDER_STATUS_META[status]) || { labelKey: 'orderStatus.unknown', type: 'default' }
}

/** 狀態篩選下拉選項（含「全部」＝ null）；標籤依當前語系產生。 */
export function orderStatusOptions(): { label: string; value: OrderStatus | null }[] {
  const t = i18n.global.t
  return [
    { label: t('orderStatus.all'), value: null },
    { label: t('orderStatus.pending'), value: OrderStatus.Pending },
    { label: t('orderStatus.paid'), value: OrderStatus.Paid },
    { label: t('orderStatus.completed'), value: OrderStatus.Completed },
    { label: t('orderStatus.cancelled'), value: OrderStatus.Cancelled },
    { label: t('orderStatus.refunded'), value: OrderStatus.Refunded },
  ]
}

/** ISO 時間字串 → 當地可讀日期時間；空值回傳「—」。 */
export function formatOrderTime(iso?: string | null): string {
  return iso ? new Date(iso).toLocaleString(i18n.global.locale.value, { hour12: false }) : '—'
}
