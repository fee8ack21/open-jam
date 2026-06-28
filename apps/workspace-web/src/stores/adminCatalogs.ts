import { ref, computed } from 'vue';
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

/**
 * 平台管理員的全平台商品列表 store：跨商店載入商品（唯讀總覽）。
 * 串接 CatalogService `GET /v1/catalogs`（市集瀏覽端點，可帶 Store / 分類 / 關鍵字過濾）。
 * 僅 Admin 使用。
 */
export const useAdminCatalogStore = defineStore('adminCatalogs', () => {
  const items = ref<CatalogSummaryDto[]>([]); // 全平台商品
  const totalCount = ref(0);
  const loading = ref(false);
  const error = ref<string | null>(null);

  /** 已發佈商品數，供側欄徽章與標題顯示。 */
  const publishedCount = computed(
    () => items.value.filter((p) => p.status === CatalogStatus.Published).length,
  );

  /** 載入全平台商品列表。 */
  async function load() {
    loading.value = true;
    error.value = null;
    try {
      const res = await catalogApi.catalogs.list({ Offset: 0, Limit: 100 });
      items.value = res.data.items ?? [];
      totalCount.value = res.data.totalCount ?? items.value.length;
    } catch (err) {
      error.value = messageOf(err, i18n.global.t('storeError.loadProductsFailed'));
    } finally {
      loading.value = false;
    }
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
    loading,
    error,
    publishedCount,
    load,
    setFeatured,
  };
});
