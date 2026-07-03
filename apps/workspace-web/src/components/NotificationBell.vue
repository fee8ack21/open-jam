<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth'
import {
  useNotificationsStore,
  type CatalogPublishedPayload,
  type StoreAnnouncementPayload,
} from '@/stores/notifications'
import type { NotificationDto } from '@/api/notification-service'

const { t } = useI18n()
const authStore = useAuthStore()
const store = useNotificationsStore()

const open = ref(false)

const badgeText = computed(() =>
  store.unreadCount > 99 ? '99+' : String(store.unreadCount),
)

/** 依通知類型組出顯示文案（type + camelCase JSON payload 配 i18n）。 */
function renderText(n: NotificationDto): string {
  try {
    const payload = JSON.parse(n.payload ?? '{}') as unknown
    switch (n.type) {
      case 'catalog.published': {
        const p = payload as CatalogPublishedPayload
        return t('notifications.catalogPublished', {
          storeName: p.storeName ?? '',
          catalogName: p.catalogName ?? '',
        })
      }
      case 'store.announcement': {
        const p = payload as StoreAnnouncementPayload
        return t('notifications.storeAnnouncement', {
          storeName: p.storeName ?? '',
          title: p.title ?? '',
        })
      }
      default:
        return n.type ?? ''
    }
  } catch {
    return n.type ?? ''
  }
}

/** 相對時間：沿用 time.* 既有鍵。 */
function renderTime(n: NotificationDto): string {
  if (!n.createdAt) return ''
  const diffMs = Date.now() - new Date(n.createdAt).getTime()
  const mins = Math.floor(diffMs / 60_000)
  if (mins < 1) return t('notifications.justNow')
  if (mins < 60) return t('time.minsAgo', { n: mins })
  const hours = Math.floor(mins / 60)
  if (hours < 24) return t('time.hoursAgo', { n: hours })
  return t('time.daysAgo', { n: Math.floor(hours / 24) })
}

function toggle() {
  open.value = !open.value
  if (open.value) void store.loadPanel()
}

function onItemClick(n: NotificationDto) {
  if (n.id) void store.markRead(n.id)
}

watch(
  () => authStore.isAuthenticated,
  (authed) => {
    if (authed) store.startPolling()
    else store.stopPolling()
  },
  { immediate: true },
)

let outside: ((e: MouseEvent) => void) | null = null
onMounted(() => {
  outside = (e: MouseEvent) => {
    const target = e.target as Element | null
    if (open.value && !(target && target.closest && target.closest('.notif-menu'))) open.value = false
  }
  document.addEventListener('click', outside)
})
onBeforeUnmount(() => {
  if (outside) document.removeEventListener('click', outside)
  store.stopPolling()
})
</script>

<template>
  <div class="notif-menu">
    <button
      class="icon-btn notif-trigger"
      :class="{ on: open }"
      @click="toggle"
      :title="t('notifications.title')"
      :aria-label="t('notifications.title')"
    >
      <app-icon name="bell" :size="20" />
      <span v-if="store.unreadCount > 0" class="cart-badge">{{ badgeText }}</span>
    </button>
    <transition name="um">
      <div v-if="open" class="user-pop notif-pop">
        <div class="notif-head">
          <span class="notif-title">{{ t('notifications.title') }}</span>
          <button
            v-if="store.unreadCount > 0"
            class="notif-readall"
            @click="store.markAllRead()"
          >{{ t('notifications.markAllRead') }}</button>
        </div>
        <div class="um-sep"></div>
        <div v-if="store.loading" class="notif-empty">{{ t('common.loading') }}</div>
        <div v-else-if="store.items.length === 0" class="notif-empty">{{ t('notifications.empty') }}</div>
        <template v-else>
          <button
            v-for="n in store.items"
            :key="n.id"
            class="um-item notif-item"
            :class="{ unread: !n.readAt }"
            @click="onItemClick(n)"
          >
            <span class="notif-dot" aria-hidden="true"></span>
            <span class="notif-body">
              <span class="notif-text">{{ renderText(n) }}</span>
              <span class="notif-time">{{ renderTime(n) }}</span>
            </span>
          </button>
        </template>
      </div>
    </transition>
  </div>
</template>
