import { ref } from 'vue'
import { defineStore } from 'pinia'
import { notificationApi } from '@/api'
import type { NotificationDto } from '@/api/notification-service'

/** 未讀數輪詢間隔（毫秒）。 */
const POLL_INTERVAL_MS = 60_000

/** 鈴鐺下拉一次載入的通知筆數。 */
const PANEL_LIMIT = 10

/** catalog.published 通知的 Payload（camelCase JSON，見後端 NotificationPayloads）。 */
export interface CatalogPublishedPayload {
  catalogId: string
  catalogName: string
  catalogSlug: string
  price: number
  currency: string
  storeName?: string | null
  storeSlug?: string | null
}

/** catalog.version_released 通知的 Payload（已購商品發布新版本）。 */
export interface CatalogVersionReleasedPayload {
  catalogId: string
  catalogName: string
  catalogSlug: string
  versionId: string
  version: string
  releaseNote?: string | null
  storeName?: string | null
  storeSlug?: string | null
}

/** store.announcement 通知的 Payload。 */
export interface StoreAnnouncementPayload {
  title: string
  message: string
  storeName?: string | null
  storeSlug?: string | null
}

/** in-app 通知：未讀數輪詢 + 鈴鐺下拉列表 + 已讀操作。 */
export const useNotificationsStore = defineStore('notifications', () => {
  const unreadCount = ref(0)
  const items = ref<NotificationDto[]>([])
  const totalCount = ref(0)
  const loading = ref(false)

  let pollTimer: ReturnType<typeof setInterval> | null = null

  async function refreshUnreadCount(): Promise<void> {
    try {
      const res = await notificationApi.notifications.getUnreadCount()
      unreadCount.value = res.data.count ?? 0
    } catch {
      // 輪詢失敗（未登入 / 網路瞬斷）靜默略過，下一輪再試
    }
  }

  /** 開始輪詢未讀數；重複呼叫為 no-op。呼叫端須確保已登入。 */
  function startPolling(): void {
    if (pollTimer) return
    void refreshUnreadCount()
    pollTimer = setInterval(() => void refreshUnreadCount(), POLL_INTERVAL_MS)
  }

  function stopPolling(): void {
    if (pollTimer) {
      clearInterval(pollTimer)
      pollTimer = null
    }
  }

  /** 載入鈴鐺下拉的最近通知。 */
  async function loadPanel(): Promise<void> {
    loading.value = true
    try {
      const res = await notificationApi.notifications.listMine({ Offset: 0, Limit: PANEL_LIMIT })
      items.value = res.data.items ?? []
      totalCount.value = res.data.totalCount ?? 0
    } catch (error) {
      console.error('loadPanel failed:', error)
    } finally {
      loading.value = false
    }
  }

  async function markRead(id: string): Promise<void> {
    const target = items.value.find((n) => n.id === id)
    if (!target || target.readAt) return
    try {
      await notificationApi.notifications.markRead(id)
      target.readAt = new Date().toISOString()
      unreadCount.value = Math.max(0, unreadCount.value - 1)
    } catch (error) {
      console.error('markRead failed:', error)
    }
  }

  async function markAllRead(): Promise<void> {
    try {
      await notificationApi.notifications.markAllRead()
      const now = new Date().toISOString()
      items.value.forEach((n) => { n.readAt ??= now })
      unreadCount.value = 0
    } catch (error) {
      console.error('markAllRead failed:', error)
    }
  }

  return {
    unreadCount,
    items,
    totalCount,
    loading,
    refreshUnreadCount,
    startPolling,
    stopPolling,
    loadPanel,
    markRead,
    markAllRead,
  }
})
