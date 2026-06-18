import { ref, computed } from 'vue';
import { defineStore } from 'pinia';
import { storeApi } from '@/api';
import {
  StoreApplicationStatus,
  type StoreApplicationDto,
} from '@/api/store-service';

/** 由後端 RFC 9457 Problem Details 取出可顯示的錯誤訊息。 */
function messageOf(err: unknown, fallback = '操作失敗，請稍後再試。'): string {
  if (typeof err === 'string') return err;
  const problem = err as { detail?: string; title?: string } | null | undefined;
  return problem?.detail ?? problem?.title ?? fallback;
}

/**
 * 平台管理員的店家審核 store：載入並審核全平台「待審核（Pending）」開店申請。
 * 僅 Admin 使用（一般使用者改用 [[storeApplication]] 管理自己的申請）。
 */
export const useStoreReviewStore = defineStore('storeReview', () => {
  const items = ref<StoreApplicationDto[]>([]);   // 待審核申請，新到舊
  const history = ref<StoreApplicationDto[]>([]); // 已審核紀錄（核准／駁回），新到舊
  const totalCount = ref(0);
  const loading = ref(false);
  const error = ref<string | null>(null);

  /** 待審核件數，供側欄徽章顯示。 */
  const pendingCount = computed(() => items.value.length);

  /** 載入全平台待審核申請與審核紀錄。 */
  async function load() {
    loading.value = true;
    error.value = null;
    try {
      const [pendingRes, reviewedRes] = await Promise.all([
        storeApi.storeApplications.getAll({ Status: StoreApplicationStatus.Pending, Offset: 0, Limit: 100 }),
        storeApi.storeApplications.getAll({ Offset: 0, Limit: 100 }),
      ]);
      items.value = pendingRes.data.items ?? [];
      totalCount.value = pendingRes.data.totalCount ?? items.value.length;
      history.value = (reviewedRes.data.items ?? []).filter(
        (a) => a.status === StoreApplicationStatus.Approved || a.status === StoreApplicationStatus.Rejected,
      );
    } catch (err) {
      error.value = messageOf(err, '載入待審核申請失敗。');
    } finally {
      loading.value = false;
    }
  }

  /** 核准申請後重新載入；成功回傳 true。 */
  async function approve(id: string) {
    error.value = null;
    try {
      await storeApi.storeApplications.approve(id);
      await load();
      return true;
    } catch (err) {
      error.value = messageOf(err, '核准申請失敗。');
      return false;
    }
  }

  /** 駁回申請（附審核意見）後重新載入；成功回傳 true。 */
  async function reject(id: string, reviewComment: string) {
    error.value = null;
    try {
      await storeApi.storeApplications.reject(id, { reviewComment });
      await load();
      return true;
    } catch (err) {
      error.value = messageOf(err, '駁回申請失敗。');
      return false;
    }
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
