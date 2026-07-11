import { ref } from 'vue';
import { defineStore } from 'pinia';
import { catalogApi, orderApi } from '@/api';
import i18n from '@/i18n';
import { OrderStatus, type OrderResponse, type OrderSummaryDto } from '@/api/order-service';
import type { PurchasedVersionAssetDto } from '@/api/catalog-service';

/** 由後端 RFC 9457 Problem Details 取出可顯示的錯誤訊息。 */
function messageOf(err: unknown, fallback = i18n.global.t('storeError.loadPurchasesFailed')): string {
  const response = err as { error?: { detail?: string; title?: string } } | null;
  const problem = response?.error;
  return problem?.detail ?? problem?.title ?? fallback;
}

const PAGE_SIZE = 10;

/** 單一訂單項目的可下載檔案載入狀態。 */
export interface ItemDownloads {
  loading: boolean;
  error: string | null;
  files: PurchasedVersionAssetDto[];
}

/**
 * 買家「購買紀錄」store：分頁查詢 OrderService `/v1/orders/mine`
 * （登入身分本人的訂單），支援以訂單狀態篩選。開啟明細時載入單筆訂單，
 * 並對已完成訂單逐項向 CatalogService 取得可下載檔案（含短效下載 URL）。
 */
export const usePurchasesStore = defineStore('purchases', () => {
  const items = ref<OrderSummaryDto[]>([]);
  const totalCount = ref(0);
  const offset = ref(0);
  const pageSize = ref(PAGE_SIZE);
  const status = ref<OrderStatus | null>(null);
  const loading = ref(false);
  const error = ref<string | null>(null);

  // 單筆訂單明細（彈窗用）
  const detail = ref<OrderResponse | null>(null);
  const detailLoading = ref(false);
  const detailError = ref<string | null>(null);
  // 各訂單項目（OrderItemResponse.id）對應的可下載檔案。
  const downloads = ref<Record<string, ItemDownloads>>({});

  /** 載入目前 offset / 狀態篩選下的一頁訂單。 */
  async function load() {
    loading.value = true;
    error.value = null;
    try {
      const res = await orderApi.orders.listMine({
        Offset: offset.value,
        Limit: pageSize.value,
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
    offset.value = Math.max(0, (page - 1) * pageSize.value);
    await load();
  }

  /** 變更每頁筆數並回到第一頁重新載入。 */
  async function setPageSize(size: number) {
    pageSize.value = size;
    offset.value = 0;
    await load();
  }

  /** 載入單筆訂單完整明細；已完成訂單另載入各項目的可下載檔案。 */
  async function loadDetail(id: string) {
    detail.value = null;
    detailError.value = null;
    downloads.value = {};
    detailLoading.value = true;
    try {
      const res = await orderApi.orders.get(id);
      detail.value = res.data;
      // 僅已完成訂單具下載權；其餘狀態不嘗試（後端會以未購買回 403）。
      if (res.data.status === OrderStatus.Completed) {
        await loadDownloads(res.data);
      }
    } catch (err) {
      detailError.value = messageOf(err, i18n.global.t('storeError.loadOrderDetailFailed'));
    } finally {
      detailLoading.value = false;
    }
  }

  /** 逐項向 CatalogService 取得該版本可下載檔案（並行）。 */
  async function loadDownloads(order: OrderResponse) {
    const orderItems = order.items ?? [];
    await Promise.all(
      orderItems.map(async (it) => {
        if (!it.id || !it.catalogId || !it.catalogVersionId) return;
        downloads.value[it.id] = { loading: true, error: null, files: [] };
        try {
          const res = await catalogApi.catalogVersions.listPurchasedDownloads(
            it.catalogId,
            it.catalogVersionId,
          );
          downloads.value[it.id] = { loading: false, error: null, files: res.data ?? [] };
        } catch (err) {
          downloads.value[it.id] = {
            loading: false,
            error: messageOf(err, i18n.global.t('storeError.loadDownloadsFailed')),
            files: [],
          };
        }
      }),
    );
  }

  return {
    items,
    totalCount,
    offset,
    status,
    pageSize,
    setPageSize,
    loading,
    error,
    detail,
    detailLoading,
    detailError,
    downloads,
    load,
    applyStatus,
    goPage,
    loadDetail,
  };
});
