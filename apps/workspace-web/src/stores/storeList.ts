import { ref } from 'vue';
import { defineStore } from 'pinia';
import { storeApi } from '@/api';
import i18n from '@/i18n';
import { StoreStatus, type StoreDto } from '@/api/store-service';

/** 由後端 RFC 9457 Problem Details 取出可顯示的錯誤訊息。 */
function messageOf(err: unknown, fallback = i18n.global.t('storeError.actionFailed')): string {
  if (typeof err === 'string') return err;
  const problem = (err as { error?: { detail?: string; title?: string } })?.error
    ?? (err as { detail?: string; title?: string } | null | undefined);
  return problem?.detail ?? problem?.title ?? fallback;
}

/** 查詢條件（皆為選填；空字串 / null 視為未篩選）。 */
export interface StoreListFilter {
  search?: string;
  status?: StoreStatus | null;
}

const PAGE_SIZE = 10;

/**
 * 平台管理員的商店列表 store：分頁載入並管理全平台商店（停權／解除停權／關閉）。
 * 串接 StoreService `GET /v1/stores`（Admin，支援 Status / Search 過濾）與管理動作。
 * 僅 Admin 使用。
 */
export const useStoreListStore = defineStore('storeList', () => {
  const items = ref<StoreDto[]>([]);   // 目前頁次的商店
  const totalCount = ref(0);           // 目前篩選條件下的總筆數
  const offset = ref(0);
  const loading = ref(false);
  const error = ref<string | null>(null);
  const filter = ref<StoreListFilter>({ search: '', status: null });

  /** 營運中商店總數（跨頁），供側欄徽章顯示；由獨立 count 查詢維護。 */
  const activeCount = ref(0);

  /** 以獨立查詢取得營運中商店總數（不受目前分頁影響）。 */
  async function refreshActiveCount() {
    try {
      const res = await storeApi.stores.list({ Status: StoreStatus.Active, Offset: 0, Limit: 1 });
      activeCount.value = res.data.totalCount ?? 0;
    } catch {
      /* 徽章數字非關鍵，靜默失敗 */
    }
  }

  /** 載入目前 offset / 篩選條件下的一頁商店。 */
  async function load() {
    loading.value = true;
    error.value = null;
    try {
      const res = await storeApi.stores.list({
        Offset: offset.value,
        Limit: PAGE_SIZE,
        Search: filter.value.search?.trim() || undefined,
        Status: filter.value.status ?? undefined,
      });
      items.value = res.data.items ?? [];
      totalCount.value = res.data.totalCount ?? items.value.length;
    } catch (err) {
      error.value = messageOf(err, i18n.global.t('storeError.loadStoresFailed'));
      items.value = [];
      totalCount.value = 0;
    } finally {
      loading.value = false;
    }
    await refreshActiveCount();
  }

  /** 套用新的篩選條件並回到第一頁重新載入。 */
  async function applyFilter(next: StoreListFilter) {
    filter.value = { ...filter.value, ...next };
    offset.value = 0;
    await load();
  }

  /** 跳至指定頁（1-based）。 */
  async function goPage(page: number) {
    offset.value = Math.max(0, (page - 1) * PAGE_SIZE);
    await load();
  }

  /** 停權商店（Active → Suspended）；成功回傳 true。 */
  async function suspend(id: string) {
    error.value = null;
    try {
      await storeApi.stores.suspend(id);
      await load();
      return true;
    } catch (err) {
      error.value = messageOf(err, i18n.global.t('storeError.suspendStoreFailed'));
      return false;
    }
  }

  /** 解除停權（Suspended → Active）；成功回傳 true。 */
  async function unsuspend(id: string) {
    error.value = null;
    try {
      await storeApi.stores.unsuspend(id);
      await load();
      return true;
    } catch (err) {
      error.value = messageOf(err, i18n.global.t('storeError.unsuspendFailed'));
      return false;
    }
  }

  /** 關閉商店（→ Closed，終態不可逆）；成功回傳 true。 */
  async function close(id: string) {
    error.value = null;
    try {
      await storeApi.stores.close(id);
      await load();
      return true;
    } catch (err) {
      error.value = messageOf(err, i18n.global.t('storeError.closeStoreFailed'));
      return false;
    }
  }

  return {
    items,
    totalCount,
    offset,
    pageSize: PAGE_SIZE,
    loading,
    error,
    filter,
    activeCount,
    load,
    applyFilter,
    goPage,
    suspend,
    unsuspend,
    close,
  };
});
