import { ref } from 'vue';
import { defineStore } from 'pinia';
import { orderApi } from '@/api';
import { OrderStatus, type OrderResponse, type OrderSummaryDto } from '@/api/order-service';

/** 由後端 RFC 9457 Problem Details 取出可顯示的錯誤訊息。 */
function messageOf(err: unknown, fallback = '載入訂單失敗，請稍後再試。'): string {
  const response = err as { error?: { detail?: string; title?: string } } | null;
  const problem = response?.error;
  return problem?.detail ?? problem?.title ?? fallback;
}

/** 查詢條件（皆為選填；空字串視為未篩選）。 */
export interface AdminOrderFilter {
  buyerEmail?: string;
  status?: OrderStatus | null;
}

const PAGE_SIZE = 20;

/**
 * 平台管理員的訂單列表 store：分頁查詢 OrderService `/v1/orders`（全部訂單，
 * Admin 專用），支援以買家 Email / 訂單狀態篩選，並可載入單筆訂單明細。
 */
export const useAdminOrdersStore = defineStore('adminOrders', () => {
  const items = ref<OrderSummaryDto[]>([]);
  const totalCount = ref(0);
  const offset = ref(0);
  const loading = ref(false);
  const error = ref<string | null>(null);
  const filter = ref<AdminOrderFilter>({ buyerEmail: '', status: null });

  // 單筆訂單明細（彈窗用）
  const detail = ref<OrderResponse | null>(null);
  const detailLoading = ref(false);
  const detailError = ref<string | null>(null);

  /** 載入目前 offset / 篩選條件下的一頁訂單。 */
  async function load() {
    loading.value = true;
    error.value = null;
    try {
      const res = await orderApi.orders.list({
        Offset: offset.value,
        Limit: PAGE_SIZE,
        BuyerEmail: filter.value.buyerEmail?.trim() || undefined,
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
  async function applyFilter(next: AdminOrderFilter) {
    filter.value = { ...filter.value, ...next };
    offset.value = 0;
    await load();
  }

  /** 跳至指定頁（1-based）。 */
  async function goPage(page: number) {
    offset.value = Math.max(0, (page - 1) * PAGE_SIZE);
    await load();
  }

  /** 載入單筆訂單完整明細（含項目與狀態歷程）。 */
  async function loadDetail(id: string) {
    detail.value = null;
    detailError.value = null;
    detailLoading.value = true;
    try {
      const res = await orderApi.orders.get(id);
      detail.value = res.data;
    } catch (err) {
      detailError.value = messageOf(err, '載入訂單明細失敗，請稍後再試。');
    } finally {
      detailLoading.value = false;
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
    detail,
    detailLoading,
    detailError,
    load,
    applyFilter,
    goPage,
    loadDetail,
  };
});
