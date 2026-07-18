import { ref } from 'vue';
import { defineStore } from 'pinia';
import { paymentApi } from '@/api';
import i18n from '@/i18n';
import type { PaymentResponse, PaymentStatus } from '@/api/payment-service';

/** 由後端 RFC 9457 Problem Details 取出可顯示的錯誤訊息。 */
function messageOf(err: unknown, fallback = i18n.global.t('storeError.loadPaymentsFailed')): string {
  const response = err as { error?: { detail?: string; title?: string } } | null;
  const problem = response?.error;
  return problem?.detail ?? problem?.title ?? fallback;
}

/** 查詢條件（皆為選填；空字串視為未篩選）。 */
export interface AdminPaymentFilter {
  email?: string;
  status?: PaymentStatus | null;
}

const PAGE_SIZE = 10;

/**
 * 平台管理員的付款列表 store：分頁查詢 PaymentService `/v1/payments`（Admin 專用），
 * 支援以買家 Email / 付款狀態篩選。列表項即完整付款紀錄，詳情彈窗不需另外查詢。
 */
export const useAdminPaymentsStore = defineStore('adminPayments', () => {
  const items = ref<PaymentResponse[]>([]);
  const totalCount = ref(0);
  const offset = ref(0);
  const pageSize = ref(PAGE_SIZE);
  const loading = ref(false);
  const error = ref<string | null>(null);
  const filter = ref<AdminPaymentFilter>({ email: '', status: null });

  /** 載入目前 offset / 篩選條件下的一頁付款紀錄。 */
  async function load() {
    loading.value = true;
    error.value = null;
    try {
      const res = await paymentApi.payments.list({
        Offset: offset.value,
        Limit: pageSize.value,
        Email: filter.value.email?.trim() || undefined,
        Status: filter.value.status ?? undefined,
      });
      items.value = res.data.items ?? [];
      totalCount.value = res.data.totalCount ?? 0;
    } catch (err) {
      error.value = messageOf(err);
      items.value = [];
      totalCount.value = 0;
    } finally {
      loading.value = false;
    }
  }

  /** 套用新的篩選條件並回到第一頁重新載入。 */
  async function applyFilter(next: AdminPaymentFilter) {
    filter.value = { ...filter.value, ...next };
    offset.value = 0;
    await load();
  }

  /** 跳至指定頁（1-based）。 */
  async function goPage(page: number) {
    offset.value = Math.max(0, (page - 1) * pageSize.value);
    await load();
  }

  /** 變更每頁筆數並回到第一頁重新載入。 */
  async function setPageSize(size: number) {
    pageSize.value = size;
    offset.value = 0;
    await load();
  }

  return {
    items,
    totalCount,
    offset,
    pageSize,
    setPageSize,
    loading,
    error,
    filter,
    load,
    applyFilter,
    goPage,
  };
});
