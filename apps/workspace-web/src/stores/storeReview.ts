import { ref, computed } from 'vue';
import { defineStore } from 'pinia';
// import { storeApi } from '@/api'; // TODO(mock): 後端審核 API 接上後啟用
import {
  StoreApplicationStatus,
  type StoreApplicationDto,
} from '@/api/store-service';

// /** 由後端 RFC 9457 Problem Details 取出可顯示的錯誤訊息。 */
// function messageOf(err: unknown, fallback = '操作失敗，請稍後再試。'): string {
//   if (typeof err === 'string') return err;
//   const problem = err as { detail?: string; title?: string } | null | undefined;
//   return problem?.detail ?? problem?.title ?? fallback;
// }

// TODO(mock): 後端審核 API 接上前的展示用假資料，串接後移除。
const MOCK_ITEMS: StoreApplicationDto[] = [
  {
    id: '00000000-0000-0000-0000-000000000001',
    userId: '11111111-1111-1111-1111-111111111111',
    email: 'creator@example.com',
    storeName: '小明的數位商店',
    storeSlug: 'xiaoming-shop',
    status: StoreApplicationStatus.Pending,
    createdAt: '2026-06-17T09:30:00Z',
  },
];

// TODO(mock): 審核紀錄展示用假資料（已核准／已駁回），串接後移除。
const MOCK_HISTORY: StoreApplicationDto[] = [
  {
    id: '00000000-0000-0000-0000-000000000101',
    userId: '22222222-2222-2222-2222-222222222222',
    email: 'aria@example.com',
    storeName: 'Aria 手作工作室',
    storeSlug: 'aria-studio',
    status: StoreApplicationStatus.Approved,
    createdAt: '2026-06-15T08:00:00Z',
    reviewedAt: '2026-06-15T14:20:00Z',
  },
  {
    id: '00000000-0000-0000-0000-000000000102',
    userId: '33333333-3333-3333-3333-333333333333',
    email: 'copycat@example.com',
    storeName: 'Open Jam 官方商店',
    storeSlug: 'open-jam-official',
    status: StoreApplicationStatus.Rejected,
    createdAt: '2026-06-14T03:10:00Z',
    reviewedAt: '2026-06-14T10:45:00Z',
    reviewComment: '商店名稱與平台官方名稱過於相似，請調整後重新申請。',
  },
];

/**
 * 平台管理員的店家審核 store：載入並審核全平台「待審核（Pending）」開店申請。
 * 僅 Admin 使用（一般使用者改用 [[storeApplication]] 管理自己的申請）。
 *
 * NOTE(mock): 後端審核 API 尚未接上，目前以 MOCK_ITEMS 假資料呈現。
 * 串接時請移除 mock 區塊，並啟用各方法中註解掉的真實 API 呼叫。
 */
export const useStoreReviewStore = defineStore('storeReview', () => {
  const items = ref<StoreApplicationDto[]>([...MOCK_ITEMS]);   // 待審核申請，新到舊
  const history = ref<StoreApplicationDto[]>([...MOCK_HISTORY]); // 已審核紀錄（核准／駁回），新到舊
  const totalCount = ref(MOCK_ITEMS.length);
  const loading = ref(false);
  const error = ref<string | null>(null);

  /** 待審核件數，供側欄徽章顯示。 */
  const pendingCount = computed(() => items.value.length);

  /**
   * 將待審申請依審核結果移入紀錄列表（mock 用）。
   * 從 items 取出該筆，套用新狀態與審核時間後 unshift 到 history 最前。
   */
  function moveToHistory(
    id: string,
    status: StoreApplicationStatus,
    reviewComment?: string,
  ) {
    const target = items.value.find((a) => a.id === id);
    items.value = items.value.filter((a) => a.id !== id);
    totalCount.value = items.value.length;
    if (target) {
      history.value = [
        { ...target, status, reviewedAt: new Date().toISOString(), reviewComment },
        ...history.value,
      ];
    }
  }

  /** 載入全平台待審核申請與審核紀錄。 */
  async function load() {
    // TODO(mock): 改回真實呼叫——
    // loading.value = true;
    // error.value = null;
    // try {
    //   const [pendingRes, reviewedRes] = await Promise.all([
    //     storeApi.storeApplications.getAll({ Status: StoreApplicationStatus.Pending, Offset: 0, Limit: 100 }),
    //     storeApi.storeApplications.getAll({ Offset: 0, Limit: 100 }),
    //   ]);
    //   items.value = pendingRes.data.items ?? [];
    //   totalCount.value = pendingRes.data.totalCount ?? items.value.length;
    //   history.value = (reviewedRes.data.items ?? []).filter(
    //     (a) => a.status === StoreApplicationStatus.Approved || a.status === StoreApplicationStatus.Rejected,
    //   );
    // } catch (err) {
    //   error.value = messageOf(err, '載入待審核申請失敗。');
    // } finally {
    //   loading.value = false;
    // }
    items.value = [...MOCK_ITEMS];
    totalCount.value = MOCK_ITEMS.length;
    history.value = [...MOCK_HISTORY];
  }

  /** 核准申請後重新載入；成功回傳 true。 */
  async function approve(id: string) {
    // TODO(mock): 改回真實呼叫——
    // error.value = null;
    // try {
    //   await storeApi.storeApplications.approve(id);
    //   await load();
    //   return true;
    // } catch (err) {
    //   error.value = messageOf(err, '核准申請失敗。');
    //   return false;
    // }
    moveToHistory(id, StoreApplicationStatus.Approved);
    return true;
  }

  /** 駁回申請（附審核意見）後重新載入；成功回傳 true。 */
  async function reject(id: string, reviewComment: string) {
    // TODO(mock): 改回真實呼叫——
    // error.value = null;
    // try {
    //   await storeApi.storeApplications.reject(id, { reviewComment });
    //   await load();
    //   return true;
    // } catch (err) {
    //   error.value = messageOf(err, '駁回申請失敗。');
    //   return false;
    // }
    moveToHistory(id, StoreApplicationStatus.Rejected, reviewComment);
    return true;
  }

  return {
    items,
    history,
    totalCount,
    loading,
    error,
    pendingCount,
    load,
    approve,
    reject,
  };
});
