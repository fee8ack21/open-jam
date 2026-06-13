/* ============================================================
   Formatting helpers + status label map
   ============================================================ */

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
    if (mins < 60) return mins + ' 分鐘前'
    const h = Math.round(mins / 60); if (h < 24) return h + ' 小時前'
    const days = Math.round(h / 24); if (days < 30) return days + ' 天前'
    return d.toISOString().slice(5, 10).replace('-', '/')
  },
  date(iso: string) { return (iso || '').slice(0, 10).replace(/-/g, '/') },
  initials(name: string) { return (name || '').split(' ').map(s => s[0]).slice(0, 2).join('') },
}

export const STATUS_LABEL: Record<string, string> = {
  live: '上架中', draft: '草稿', off: '已下架', review: '審核中',
  paid: '已付款', refunded: '已退款',
}
