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

const PAGE_SIZE = 20;

/**
 * 一般使用者的「我的訂單」store：分頁查詢 OrderService `/v1/orders/mine`
 * （登入身分本人的訂單），支援以訂單狀態篩選，並可載入單筆訂單明細。
 */
export const useMyOrdersStore = defineStore('myOrders', () => {
  const items = ref<OrderSummaryDto[]>([]);
  const totalCount = ref(0);
  const offset = ref(0);
  const status = ref<OrderStatus | null>(null);
  const loading = ref(false);
  const error = ref<string | null>(null);

  // 單筆訂單明細（彈窗用）
  const detail = ref<OrderResponse | null>(null);
  const detailLoading = ref(false);
  const detailError = ref<string | null>(null);

  /** 載入目前 offset / 狀態篩選下的一頁訂單。 */
  async function load() {
    loading.value = true;
    error.value = null;
    try {
      const res = await orderApi.orders.listMine({
        Offset: offset.value,
        Limit: PAGE_SIZE,
        Status: status.value ?? undefined,
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

  /** 套用新的狀態篩選並回到第一頁重新載入。 */
  async function applyStatus(next: OrderStatus | null) {
    status.value = next;
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
    status,
    pageSize: PAGE_SIZE,
    loading,
    error,
    detail,
    detailLoading,
    detailError,
    load,
    applyStatus,
    goPage,
    loadDetail,
  };
});
