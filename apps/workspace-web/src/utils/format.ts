/* ============================================================
   Formatting helpers + status label map
   ============================================================ */
import i18n from '@/i18n'

export const JFmt = {
  money(n: number) { return '$' + (n || 0).toLocaleString('en-US') },
  compact(n: number) {
    n = n || 0
    if (n >= 1000) return (n / 1000).toFixed(n % 1000 === 0 ? 0 : 1) + 'k'
    return '' + n
  },
  relTime(iso: string) {
    const d = new Date(iso), now = new Date('2026-05-30T12:00:00')
    const mins = Math.round((now.getTime() - d.getTime()) / 60000)
    const t = i18n.global.t
    if (mins < 60) return t('time.minsAgo', { n: mins })
    const h = Math.round(mins / 60); if (h < 24) return t('time.hoursAgo', { n: h })
    const days = Math.round(h / 24); if (days < 30) return t('time.daysAgo', { n: days })
    return d.toISOString().slice(5, 10).replace('-', '/')
  },
  date(iso: string) { return (iso || '').slice(0, 10).replace(/-/g, '/') },
  initials(name: string) { return (name || '').split(' ').map(s => s[0]).slice(0, 2).join('') },
}

/** 商品 / 訂單狀態 → i18n 鍵（`statusLabel.*`）。 */
const STATUS_KEY: Record<string, string> = {
  live: 'statusLabel.live', draft: 'statusLabel.draft', off: 'statusLabel.off', review: 'statusLabel.review',
  paid: 'statusLabel.paid', refunded: 'statusLabel.refunded',
}

/** 取得狀態的當前語系標籤；未知狀態原樣回傳。 */
export function statusLabel(s: string): string {
  return STATUS_KEY[s] ? i18n.global.t(STATUS_KEY[s]) : s
}
