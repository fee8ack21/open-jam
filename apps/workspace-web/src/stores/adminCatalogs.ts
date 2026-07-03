import { ref } from 'vue';
import { defineStore } from 'pinia';
import { catalogApi } from '@/api';
import i18n from '@/i18n';
import { CatalogStatus, type CatalogSummaryDto } from '@/api/catalog-service';

/** 由後端 RFC 9457 Problem Details 取出可顯示的錯誤訊息。 */
function messageOf(err: unknown, fallback = i18n.global.t('storeError.actionFailed')): string {
  if (typeof err === 'string') return err;
  const problem = (err as { error?: { detail?: string; title?: string } })?.error
    ?? (err as { detail?: string; title?: string } | null | undefined);
  return problem?.detail ?? problem?.title ?? fallback;
}

/** 查詢條件（皆為選填；空字串 / null 視為未篩選）。 */
export interface AdminCatalogFilter {
  search?: string;
  status?: CatalogStatus | null;
}

const PAGE_SIZE = 10;

/**
 * 平台管理員的全平台商品列表 store：分頁跨商店載入商品（唯讀總覽）。
 * 串接 CatalogService `GET /v1/catalogs`（可帶 Status / 關鍵字過濾）。僅 Admin 使用。
 */
export const useAdminCatalogStore = defineStore('adminCatalogs', () => {
  const items = ref<CatalogSummaryDto[]>([]); // 目前頁次的商品
  const totalCount = ref(0);                  // 目前篩選條件下的總筆數
  const offset = ref(0);
  const loading = ref(false);
  const error = ref<string | null>(null);
  const filter = ref<AdminCatalogFilter>({ search: '', status: null });

  /** 載入目前 offset / 篩選條件下的一頁商品。 */
  async function load() {
    loading.value = true;
    error.value = null;
    try {
      const res = await catalogApi.catalogs.list({
        Offset: offset.value,
        Limit: PAGE_SIZE,
        Search: filter.value.search?.trim() || undefined,
        Status: filter.value.status ?? undefined,
      });
      items.value = res.data.items ?? [];
      totalCount.value = res.data.totalCount ?? items.value.length;
    } catch (err) {
      error.value = messageOf(err, i18n.global.t('storeError.loadProductsFailed'));
      items.value = [];
      totalCount.value = 0;
    } finally {
      loading.value = false;
    }
  }

  /** 套用新的篩選條件並回到第一頁重新載入。 */
  async function applyFilter(next: AdminCatalogFilter) {
    filter.value = { ...filter.value, ...next };
    offset.value = 0;
    await load();
  }

  /** 跳至指定頁（1-based）。 */
  async function goPage(page: number) {
    offset.value = Math.max(0, (page - 1) * PAGE_SIZE);
    await load();
  }

  /**
   * 設定 / 取消商品的編輯精選旗標（Admin）。樂觀更新，失敗則還原並回報錯誤。
   * 回傳是否成功。
   */
  async function setFeatured(id: string, featured: boolean): Promise<boolean> {
    const item = items.value.find((p) => p.id === id);
    if (!item) return false;
    const prev = item.isFeatured ?? false;
    item.isFeatured = featured; // 樂觀更新
    try {
      if (featured) await catalogApi.catalogs.feature(id);
      else await catalogApi.catalogs.unfeature(id);
      return true;
    } catch (err) {
      item.isFeatured = prev; // 還原
      error.value = messageOf(err, i18n.global.t('storeError.updateFeaturedFailed'));
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
    load,
    applyFilter,
    goPage,
    setFeatured,
  };
});
