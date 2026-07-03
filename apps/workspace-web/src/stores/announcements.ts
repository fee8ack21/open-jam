import { ref } from 'vue';
import { defineStore } from 'pinia';
import { notificationApi } from '@/api';
import i18n from '@/i18n';
import {
  NotificationRequestStatus,
  type NotificationRequestDto,
} from '@/api/notification-service';

const PAGE_SIZE = 10;

/** 由後端 RFC 9457 Problem Details 取出可顯示的錯誤訊息（相容 HttpResponse.error 與直接 body 兩種丟出形態）。 */
function messageOf(err: unknown, fallback = i18n.global.t('storeError.actionFailed')): string {
  if (typeof err === 'string') return err;
  type Problem = { detail?: string; title?: string };
  const res = err as (Problem & { error?: Problem }) | null | undefined;
  return res?.error?.detail ?? res?.error?.title ?? res?.detail ?? res?.title ?? fallback;
}

/**
 * 賣家商店公告任務 store：分頁查詢 NotificationService `/v1/notification-requests`、
 * 建立公告（可預定發送時間）與取消 Pending 任務。查詢一律以綁定的商店 ID 限縮。
 */
export const useAnnouncementsStore = defineStore('announcements', () => {
  const storeId = ref<string | null>(null);
  const items = ref<NotificationRequestDto[]>([]);
  const totalCount = ref(0);
  const loading = ref(false);
  const submitting = ref(false);
  const error = ref<string | null>(null);

  const pageSize = PAGE_SIZE;
  let offset = 0;
  let statusFilter: NotificationRequestStatus | null = null;

  /** 綁定查詢來源商店並載入第一頁。 */
  async function bindStore(id: string) {
    storeId.value = id;
    offset = 0;
    await load();
  }

  async function load() {
    if (!storeId.value) return;
    loading.value = true;
    error.value = null;
    try {
      const res = await notificationApi.notificationRequests.list({
        StoreId: storeId.value,
        Status: statusFilter ?? undefined,
        Offset: offset,
        Limit: pageSize,
      });
      items.value = res.data.items ?? [];
      totalCount.value = res.data.totalCount ?? 0;
    } catch (err) {
      error.value = messageOf(err, i18n.global.t('announcements.msgLoadFailed'));
    } finally {
      loading.value = false;
    }
  }

  /** 套用狀態篩選並回到第一頁。 */
  async function applyFilter(status: NotificationRequestStatus | null) {
    statusFilter = status;
    offset = 0;
    await load();
  }

  async function goPage(page: number) {
    offset = (page - 1) * pageSize;
    await load();
  }

  /**
   * 建立公告任務；scheduledAt 為 null 表示立即發送。
   * 成功回傳 true 並重新載入列表；失敗將訊息寫入 error 並回傳 false。
   */
  async function create(input: { title: string; message: string; scheduledAt: string | null }) {
    if (!storeId.value) return false;
    submitting.value = true;
    error.value = null;
    try {
      await notificationApi.notificationRequests.create({
        storeId: storeId.value,
        title: input.title,
        message: input.message,
        scheduledAt: input.scheduledAt,
      });
      offset = 0;
      await load();
      return true;
    } catch (err) {
      error.value = messageOf(err, i18n.global.t('announcements.msgCreateFailed'));
      return false;
    } finally {
      submitting.value = false;
    }
  }

  /** 取消 Pending 任務後重新載入。成功回傳 true。 */
  async function cancel(id: string) {
    error.value = null;
    try {
      await notificationApi.notificationRequests.cancel(id);
      await load();
      return true;
    } catch (err) {
      error.value = messageOf(err, i18n.global.t('announcements.msgCancelFailed'));
      return false;
    }
  }

  return {
    storeId,
    items,
    totalCount,
    loading,
    submitting,
    error,
    pageSize,
    bindStore,
    load,
    applyFilter,
    goPage,
    create,
    cancel,
  };
});
