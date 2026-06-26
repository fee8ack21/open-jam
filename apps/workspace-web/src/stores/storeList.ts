import { ref, computed } from 'vue';
import { defineStore } from 'pinia';
import { storeApi } from '@/api';
import { StoreStatus, type StoreDto } from '@/api/store-service';

/** 由後端 RFC 9457 Problem Details 取出可顯示的錯誤訊息。 */
function messageOf(err: unknown, fallback = '操作失敗，請稍後再試。'): string {
  if (typeof err === 'string') return err;
  const problem = (err as { error?: { detail?: string; title?: string } })?.error
    ?? (err as { detail?: string; title?: string } | null | undefined);
  return problem?.detail ?? problem?.title ?? fallback;
}

/**
 * 平台管理員的商店列表 store：載入並管理全平台商店（停權／解除停權／關閉）。
 * 串接 StoreService `GET /v1/stores`（Admin）與 suspend/unsuspend/close 管理動作。
 * 僅 Admin 使用。
 */
export const useStoreListStore = defineStore('storeList', () => {
  const items = ref<StoreDto[]>([]); // 全平台商店，新到舊
  const totalCount = ref(0);
  const loading = ref(false);
  const error = ref<string | null>(null);

  /** 營運中商店數，供側欄徽章顯示。 */
  const activeCount = computed(
    () => items.value.filter((s) => s.status === StoreStatus.Active).length,
  );

  /** 載入全平台商店列表。 */
  async function load() {
    loading.value = true;
    error.value = null;
    try {
      const res = await storeApi.stores.list({ Offset: 0, Limit: 100 });
      items.value = res.data.items ?? [];
      totalCount.value = res.data.totalCount ?? items.value.length;
    } catch (err) {
      error.value = messageOf(err, '載入商店列表失敗。');
    } finally {
      loading.value = false;
    }
  }

  /** 停權商店（Active → Suspended）；成功回傳 true。 */
  async function suspend(id: string) {
    error.value = null;
    try {
      await storeApi.stores.suspend(id);
      await load();
      return true;
    } catch (err) {
      error.value = messageOf(err, '停權商店失敗。');
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
      error.value = messageOf(err, '解除停權失敗。');
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
      error.value = messageOf(err, '關閉商店失敗。');
      return false;
    }
  }

  return {
    items,
    totalCount,
    loading,
    error,
    activeCount,
    load,
    suspend,
    unsuspend,
    close,
  };
});
