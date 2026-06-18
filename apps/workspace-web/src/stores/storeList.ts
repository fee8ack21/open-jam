import { ref, computed } from 'vue';
import { defineStore } from 'pinia';
// import { storeApi } from '@/api'; // TODO(mock): 後端商店列表 API 接上後啟用
import { StoreStatus, type StoreDto } from '@/api/store-service';

// /** 由後端 RFC 9457 Problem Details 取出可顯示的錯誤訊息。 */
// function messageOf(err: unknown, fallback = '操作失敗，請稍後再試。'): string {
//   const response = err as { error?: { detail?: string; title?: string } } | null;
//   const problem = response?.error;
//   return problem?.detail ?? problem?.title ?? fallback;
// }

// TODO(mock): 後端尚無「列出全平台商店」端點（StoreService 目前僅有單筆查詢與
// suspend/unsuspend/close 管理動作）。以下為展示用假資料，端點補上後移除 mock 區塊，
// 並啟用各方法中註解掉的真實 API 呼叫。
const MOCK_STORES: StoreDto[] = [
  {
    id: '00000000-0000-0000-0000-000000000301',
    storeName: '小明的數位商店',
    storeSlug: 'xiaoming-shop',
    description: '專注於數位插畫與素材販售。',
    avatarUrl: null,
    bannerUrl: null,
    status: StoreStatus.Active,
    createdAt: '2026-05-02T08:00:00Z',
    updatedAt: '2026-06-10T12:30:00Z',
  },
  {
    id: '00000000-0000-0000-0000-000000000302',
    storeName: 'Aria 手作工作室',
    storeSlug: 'aria-studio',
    description: '手繪字型與貼圖。',
    avatarUrl: null,
    bannerUrl: null,
    status: StoreStatus.Active,
    createdAt: '2026-04-18T03:20:00Z',
    updatedAt: '2026-06-01T09:00:00Z',
  },
  {
    id: '00000000-0000-0000-0000-000000000303',
    storeName: '夜貓子音樂',
    storeSlug: 'nightowl-audio',
    description: '遊戲背景音樂與音效包。',
    avatarUrl: null,
    bannerUrl: null,
    status: StoreStatus.Suspended,
    createdAt: '2026-03-09T15:45:00Z',
    updatedAt: '2026-06-12T07:10:00Z',
  },
  {
    id: '00000000-0000-0000-0000-000000000304',
    storeName: '舊倉庫文創',
    storeSlug: 'old-warehouse',
    description: null,
    avatarUrl: null,
    bannerUrl: null,
    status: StoreStatus.Closed,
    createdAt: '2026-01-22T11:00:00Z',
    updatedAt: '2026-05-20T18:00:00Z',
  },
];

/**
 * 平台管理員的商店列表 store：載入並管理全平台商店（停權／解除停權／關閉）。
 * 僅 Admin 使用。
 *
 * NOTE(mock): 後端尚無列表端點，目前以 MOCK_STORES 假資料呈現；
 * 串接時請移除 mock 區塊，並啟用各方法中註解掉的真實 API 呼叫。
 */
export const useStoreListStore = defineStore('storeList', () => {
  const items = ref<StoreDto[]>([...MOCK_STORES]); // 全平台商店，新到舊
  const totalCount = ref(MOCK_STORES.length);
  const loading = ref(false);
  const error = ref<string | null>(null);

  /** 營運中商店數，供側欄徽章顯示。 */
  const activeCount = computed(
    () => items.value.filter((s) => s.status === StoreStatus.Active).length,
  );

  /** 套用新狀態到對應商店（mock 用）。 */
  function applyStatus(id: string, status: StoreStatus) {
    const target = items.value.find((s) => s.id === id);
    if (target) {
      target.status = status;
      target.updatedAt = new Date().toISOString();
    }
  }

  /** 載入全平台商店列表。 */
  async function load() {
    // TODO(mock): 改回真實呼叫——
    // loading.value = true;
    // error.value = null;
    // try {
    //   const res = await storeApi.stores.getAll({ Offset: 0, Limit: 100 });
    //   items.value = res.data.items ?? [];
    //   totalCount.value = res.data.totalCount ?? items.value.length;
    // } catch (err) {
    //   error.value = messageOf(err, '載入商店列表失敗。');
    // } finally {
    //   loading.value = false;
    // }
    items.value = [...MOCK_STORES];
    totalCount.value = MOCK_STORES.length;
  }

  /** 停權商店（Active → Suspended）；成功回傳 true。 */
  async function suspend(id: string) {
    // TODO(mock): 改回真實呼叫——
    // error.value = null;
    // try {
    //   await storeApi.stores.suspend(id);
    //   await load();
    //   return true;
    // } catch (err) {
    //   error.value = messageOf(err, '停權商店失敗。');
    //   return false;
    // }
    applyStatus(id, StoreStatus.Suspended);
    return true;
  }

  /** 解除停權（Suspended → Active）；成功回傳 true。 */
  async function unsuspend(id: string) {
    // TODO(mock): 改回真實呼叫——
    // error.value = null;
    // try {
    //   await storeApi.stores.unsuspend(id);
    //   await load();
    //   return true;
    // } catch (err) {
    //   error.value = messageOf(err, '解除停權失敗。');
    //   return false;
    // }
    applyStatus(id, StoreStatus.Active);
    return true;
  }

  /** 關閉商店（→ Closed，終態不可逆）；成功回傳 true。 */
  async function close(id: string) {
    // TODO(mock): 改回真實呼叫——
    // error.value = null;
    // try {
    //   await storeApi.stores.close(id);
    //   await load();
    //   return true;
    // } catch (err) {
    //   error.value = messageOf(err, '關閉商店失敗。');
    //   return false;
    // }
    applyStatus(id, StoreStatus.Closed);
    return true;
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
