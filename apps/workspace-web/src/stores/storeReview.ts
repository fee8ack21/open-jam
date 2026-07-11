import { ref } from 'vue';
import { defineStore } from 'pinia';
import { storeApi } from '@/api';
import i18n from '@/i18n';
import {
  StoreApplicationStatus,
  type StoreApplicationDto,
} from '@/api/store-service';

/** 由後端 RFC 9457 Problem Details 取出可顯示的錯誤訊息。 */
function messageOf(err: unknown, fallback = i18n.global.t('storeError.actionFailed')): string {
  if (typeof err === 'string') return err;
  const problem = err as { detail?: string; title?: string } | null | undefined;
  return problem?.detail ?? problem?.title ?? fallback;
}

const PAGE_SIZE = 10;

/**
 * 平台管理員的店家審核 store：分頁載入並審核全平台開店申請。
 * 待審核（Pending）佇列與已審核（Approved / Rejected）紀錄各自獨立分頁。
 * 僅 Admin 使用（一般使用者改用 [[storeApplication]] 管理自己的申請）。
 */
export const useStoreReviewStore = defineStore('storeReview', () => {
  const items = ref<StoreApplicationDto[]>([]);   // 目前頁次的待審核申請
  const history = ref<StoreApplicationDto[]>([]); // 目前頁次的已審核紀錄
  const pendingTotal = ref(0);                    // 待審核總筆數（跨頁）
  const historyTotal = ref(0);                    // 已審核總筆數（跨頁）
  const pendingOffset = ref(0);
  const historyOffset = ref(0);
  const pageSize = ref(PAGE_SIZE);
  /** 已審核紀錄的結果篩選：null 表示全部（Approved + Rejected）。 */
  const historyStatus = ref<StoreApplicationStatus | null>(null);
  const loading = ref(false);
  const error = ref<string | null>(null);

  /** 待審核件數，供側欄徽章顯示（= 待審核總筆數）。 */
  const pendingCount = pendingTotal;

  /** 載入目前頁次的待審核申請。 */
  async function loadPending() {
    loading.value = true;
    error.value = null;
    try {
      const res = await storeApi.storeApplications.getAll({
        Status: StoreApplicationStatus.Pending,
        Offset: pendingOffset.value,
        Limit: pageSize.value,
      });
      items.value = res.data.items ?? [];
      pendingTotal.value = res.data.totalCount ?? items.value.length;
    } catch (err) {
      error.value = messageOf(err, i18n.global.t('storeError.loadReviewFailed'));
      items.value = [];
      pendingTotal.value = 0;
    } finally {
      loading.value = false;
    }
  }

  /** 載入目前頁次的已審核紀錄（依 historyStatus 篩選，未設則 Approved + Rejected）。 */
  async function loadHistory() {
    loading.value = true;
    error.value = null;
    try {
      const res = await storeApi.storeApplications.getAll({
        // 指定結果時以 Status 過濾，否則以 Reviewed 取全部已審核
        Status: historyStatus.value ?? undefined,
        Reviewed: historyStatus.value ? undefined : true,
        Offset: historyOffset.value,
        Limit: pageSize.value,
      });
      history.value = res.data.items ?? [];
      historyTotal.value = res.data.totalCount ?? history.value.length;
    } catch (err) {
      error.value = messageOf(err, i18n.global.t('storeError.loadReviewFailed'));
      history.value = [];
      historyTotal.value = 0;
    } finally {
      loading.value = false;
    }
  }

  /** 相容既有呼叫端（總覽 / 審核佇列）：載入待審核第一頁。 */
  async function load() {
    pendingOffset.value = 0;
    await loadPending();
  }

  /** 待審核佇列跳頁（1-based）。 */
  async function goPendingPage(page: number) {
    pendingOffset.value = Math.max(0, (page - 1) * pageSize.value);
    await loadPending();
  }

  /** 已審核紀錄跳頁（1-based）。 */
  async function goHistoryPage(page: number) {
    historyOffset.value = Math.max(0, (page - 1) * pageSize.value);
    await loadHistory();
  }

  /** 變更每頁筆數並回到待審核第一頁重新載入。 */
  async function setPendingPageSize(size: number) {
    pageSize.value = size;
    pendingOffset.value = 0;
    await loadPending();
  }

  /** 變更每頁筆數並回到已審核第一頁重新載入。 */
  async function setHistoryPageSize(size: number) {
    pageSize.value = size;
    historyOffset.value = 0;
    await loadHistory();
  }

  /** 套用已審核結果篩選（Approved / Rejected / null=全部）並回到第一頁。 */
  async function applyHistoryFilter(status: StoreApplicationStatus | null) {
    historyStatus.value = status;
    historyOffset.value = 0;
    await loadHistory();
  }

  /** 核准申請後重新載入待審核頁次；成功回傳 true。 */
  async function approve(id: string) {
    error.value = null;
    try {
      await storeApi.storeApplications.approve(id);
      await loadPending();
      return true;
    } catch (err) {
      error.value = messageOf(err, i18n.global.t('storeError.approveFailed'));
      return false;
    }
  }

  /** 駁回申請（附審核意見）後重新載入待審核頁次；成功回傳 true。 */
  async function reject(id: string, reviewComment: string) {
    error.value = null;
    try {
      await storeApi.storeApplications.reject(id, { reviewComment });
      await loadPending();
      return true;
    } catch (err) {
      error.value = messageOf(err, i18n.global.t('storeError.rejectFailed'));
      return false;
    }
  }

  return {
    items,
    history,
    pendingTotal,
    historyTotal,
    pendingOffset,
    historyOffset,
    pageSize,
    loading,
    error,
    pendingCount,
    load,
    loadPending,
    loadHistory,
    goPendingPage,
    goHistoryPage,
    setPendingPageSize,
    setHistoryPageSize,
    applyHistoryFilter,
    approve,
    reject,
  };
});
