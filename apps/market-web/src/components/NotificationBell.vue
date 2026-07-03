<script setup lang="ts">
/* ============================================================
   NotificationBell — 導覽列鈴鐺：未讀數輪詢 + 通知下拉 + 已讀。
   僅登入使用者渲染（AppNav 以 v-if 控制），內容依通知 type +
   camelCase JSON payload 配 i18n 渲染。
   ============================================================ */
import { computed, onBeforeUnmount, onMounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import {
  useNotificationsStore,
  type CatalogPublishedPayload,
  type StoreAnnouncementPayload,
} from '@/stores/notifications';
import type { NotificationDto } from '@/api/notification-service';

const { t } = useI18n();
const store = useNotificationsStore();

const open = ref(false);

const badgeText = computed(() => (store.unreadCount > 99 ? '99+' : String(store.unreadCount)));

/** 依通知類型組出顯示文案。 */
function renderText(n: NotificationDto): string {
  try {
    const payload = JSON.parse(n.payload ?? '{}') as unknown;
    switch (n.type) {
      case 'catalog.published': {
        const p = payload as CatalogPublishedPayload;
        return t('notifications.catalogPublished', {
          storeName: p.storeName ?? '',
          catalogName: p.catalogName ?? '',
        });
      }
      case 'store.announcement': {
        const p = payload as StoreAnnouncementPayload;
        return t('notifications.storeAnnouncement', {
          storeName: p.storeName ?? '',
          title: p.title ?? '',
        });
      }
      default:
        return n.type ?? '';
    }
  } catch {
    return n.type ?? '';
  }
}

/** 相對時間。 */
function renderTime(n: NotificationDto): string {
  if (!n.createdAt) return '';
  const diffMs = Date.now() - new Date(n.createdAt).getTime();
  const mins = Math.floor(diffMs / 60_000);
  if (mins < 1) return t('notifications.justNow');
  if (mins < 60) return t('notifications.minsAgo', { n: mins });
  const hours = Math.floor(mins / 60);
  if (hours < 24) return t('notifications.hoursAgo', { n: hours });
  return t('notifications.daysAgo', { n: Math.floor(hours / 24) });
}

function toggle() {
  open.value = !open.value;
  if (open.value) void store.loadPanel();
}

function onItemClick(n: NotificationDto) {
  if (n.id) void store.markRead(n.id);
}

let outside: ((e: MouseEvent) => void) | null = null;
onMounted(() => {
  store.startPolling();
  outside = (e: MouseEvent) => {
    const target = e.target as Element | null;
    if (open.value && !(target && target.closest && target.closest('.notif-menu')))
      open.value = false;
  };
  document.addEventListener('click', outside);
});
onBeforeUnmount(() => {
  if (outside) document.removeEventListener('click', outside);
  store.stopPolling();
});
</script>

<template>
  <div class="notif-menu">
    <a
      class="nav-link notif-trigger"
      href="#"
      :title="t('notifications.title')"
      :aria-label="t('notifications.title')"
      @click.prevent="toggle"
    >
      <app-icon name="bell" :size="18" />
      <span v-if="store.unreadCount > 0" class="notif-badge">{{ badgeText }}</span>
    </a>
    <transition name="notif">
      <div v-if="open" class="notif-pop">
        <div class="notif-head">
          <span class="notif-title">{{ t('notifications.title') }}</span>
          <button v-if="store.unreadCount > 0" class="notif-readall" @click="store.markAllRead()">
            {{ t('notifications.markAllRead') }}
          </button>
        </div>
        <div class="notif-sep"></div>
        <div v-if="store.loading" class="notif-empty">{{ t('notifications.loading') }}</div>
        <div v-else-if="store.items.length === 0" class="notif-empty">
          {{ t('notifications.empty') }}
        </div>
        <template v-else>
          <button
            v-for="n in store.items"
            :key="n.id"
            class="notif-item"
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

<style scoped>
.notif-menu {
  position: relative;
}
.notif-trigger {
  position: relative;
}
.notif-badge {
  position: absolute;
  top: -7px;
  right: -7px;
  min-width: 18px;
  height: 18px;
  padding: 0 4px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  background: var(--c-pink);
  color: #fff;
  border: 1.5px solid var(--text);
  border-radius: 999px;
  font-family: var(--oj-mono);
  font-size: 10.5px;
  font-weight: 600;
  line-height: 1;
}
.notif-pop {
  position: absolute;
  right: 0;
  top: calc(100% + 12px);
  width: 340px;
  max-width: min(340px, 86vw);
  z-index: 80;
  background: var(--surface);
  border: 1.5px solid var(--text);
  border-radius: 16px;
  box-shadow: 5px 6px 0 var(--text);
  padding: 8px;
}
.notif-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 8px 8px 10px;
}
.notif-title {
  font-family: var(--oj-display);
  font-weight: 800;
  font-size: 15px;
}
.notif-readall {
  border: 0;
  background: transparent;
  cursor: pointer;
  padding: 4px 6px;
  border-radius: 8px;
  font-family: var(--oj-font);
  font-weight: 600;
  font-size: 12.5px;
  color: var(--oj-primary);
}
.notif-readall:hover {
  background: var(--oj-primary-wash);
}
.notif-sep {
  height: 1.5px;
  background: var(--border);
  margin: 2px 4px 6px;
}
.notif-empty {
  padding: 22px 8px;
  text-align: center;
  font-size: 13px;
  color: var(--text-faint);
}
.notif-item {
  display: flex;
  align-items: flex-start;
  gap: 10px;
  width: 100%;
  border: 0;
  background: transparent;
  cursor: pointer;
  padding: 10px;
  border-radius: 10px;
  font-family: var(--oj-font);
  text-align: left;
  transition: background 0.14s;
}
.notif-item:hover {
  background: var(--oj-primary-wash);
}
.notif-dot {
  flex: none;
  width: 7px;
  height: 7px;
  border-radius: 50%;
  margin-top: 6px;
  background: transparent;
}
.notif-item.unread .notif-dot {
  background: var(--oj-primary);
}
.notif-body {
  display: flex;
  flex-direction: column;
  gap: 3px;
  min-width: 0;
}
.notif-text {
  font-size: 13.5px;
  font-weight: 600;
  line-height: 1.45;
  color: var(--text);
  word-break: break-word;
}
.notif-item:not(.unread) .notif-text {
  font-weight: 500;
  color: var(--text-soft);
}
.notif-time {
  font-family: var(--oj-mono);
  font-size: 11px;
  color: var(--text-faint);
}
.notif-enter-active,
.notif-leave-active {
  transition:
    opacity 0.15s,
    transform 0.15s;
}
.notif-enter-from,
.notif-leave-to {
  opacity: 0;
  transform: translateY(-6px);
}
</style>
