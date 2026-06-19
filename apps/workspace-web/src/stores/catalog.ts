import { ref, computed } from 'vue';
import { defineStore } from 'pinia';
import { catalogApi } from '@/api';
import {
  CatalogStatus,
  type CatalogSummaryDto,
  type CatalogDto,
  type CatalogCategoryDto,
} from '@/api/catalog-service';

/** 由後端 RFC 9457 Problem Details 取出可顯示的錯誤訊息。 */
function messageOf(err: unknown, fallback = '操作失敗，請稍後再試。'): string {
  if (typeof err === 'string') return err;
  const problem = (err as { error?: { detail?: string; title?: string; errors?: Record<string, string[]> } })?.error
    ?? (err as { detail?: string; title?: string } | null | undefined);
  const validation = (problem as { errors?: Record<string, string[]> })?.errors;
  const first = validation ? Object.values(validation).flat()[0] : null;
  return first ?? problem?.detail ?? problem?.title ?? fallback;
}

/** 由名稱產生符合後端格式的 slug（小寫英數+連字號，3–100 字）。 */
function slugify(name: string): string {
  const base = name
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');
  const padded = base.length >= 3 ? base : `item-${base}`.replace(/^-+|-+$/g, '');
  return `${padded}-${Date.now().toString(36)}`.slice(0, 100);
}

/** 建立新商品所需的草稿資料。 */
export interface NewCatalogInput {
  storeId: string;
  name: string;
  /** 分類 slug（music / photography / ebook…），會對應成 categoryId。 */
  categorySlug?: string | null;
  summary?: string | null;
  description?: string | null;
  coverHue?: number;
  price: number;
  tags?: string[];
  /** 買家付款後可下載的檔案。 */
  files: File[];
  /** true 立即送審上架；false 留為草稿。 */
  publish: boolean;
}

/**
 * 創作者（商店 Owner）自家商品 store：串接 CatalogService。
 * 取代原 dashboard mock，提供商品列表與上 / 下架、建立新商品（含版本與檔案上傳）。
 */
export const useCatalogStore = defineStore('catalog', () => {
  const products = ref<CatalogSummaryDto[]>([]);
  const categories = ref<CatalogCategoryDto[]>([]);
  const loading = ref(false);
  const busyId = ref<string | null>(null);   // 進行中的商品 id（避免重複點擊）
  const creating = ref(false);
  const error = ref<string | null>(null);

  const publishedCount = computed(() => products.value.filter((p) => p.status === CatalogStatus.Published).length);
  const statusCount = (status: CatalogStatus) => products.value.filter((p) => p.status === status).length;

  /** 載入分類（建立商品時將分類 slug 對應成 id）。 */
  async function loadCategories() {
    if (categories.value.length) return;
    try {
      const res = await catalogApi.catalogCategories.list();
      categories.value = res.data ?? [];
    } catch {
      categories.value = [];
    }
  }

  /** 載入指定商店的全部商品（含未上架）。 */
  async function load(storeId: string) {
    if (!storeId) {
      products.value = [];
      return;
    }
    loading.value = true;
    error.value = null;
    try {
      const res = await catalogApi.catalogs.listMine({ StoreId: storeId, Offset: 0, Limit: 100 });
      products.value = res.data.items ?? [];
    } catch (err) {
      error.value = messageOf(err, '載入商品失敗。');
    } finally {
      loading.value = false;
    }
  }

  /** （Admin）載入指定商店的全部商品（含草稿 / 已停權）。 */
  async function loadByStore(storeId: string) {
    if (!storeId) {
      products.value = [];
      return;
    }
    loading.value = true;
    error.value = null;
    try {
      const res = await catalogApi.catalogs.listByStore(storeId, { Offset: 0, Limit: 100 });
      products.value = res.data.items ?? [];
    } catch (err) {
      error.value = messageOf(err, '載入商店商品失敗。');
    } finally {
      loading.value = false;
    }
  }

  /** （Admin）停權商品（任一狀態 → Suspended），停權後重新載入該商店。 */
  async function suspend(id: string, storeId: string) {
    busyId.value = id;
    error.value = null;
    try {
      await catalogApi.catalogs.suspend(id);
      await loadByStore(storeId);
      return true;
    } catch (err) {
      error.value = messageOf(err, '停權商品失敗。');
      return false;
    } finally {
      busyId.value = null;
    }
  }

  /** （Admin）解除停權（Suspended → Archived），完成後重新載入該商店。 */
  async function unsuspend(id: string, storeId: string) {
    busyId.value = id;
    error.value = null;
    try {
      await catalogApi.catalogs.unsuspend(id);
      await loadByStore(storeId);
      return true;
    } catch (err) {
      error.value = messageOf(err, '解除停權失敗。');
      return false;
    } finally {
      busyId.value = null;
    }
  }

  /** 上架（Draft / Archived → Published）。需已有目前版本。 */
  async function publish(id: string, storeId: string) {
    busyId.value = id;
    error.value = null;
    try {
      await catalogApi.catalogs.publish(id);
      await load(storeId);
      return true;
    } catch (err) {
      error.value = messageOf(err, '上架失敗；商品需先有可下載版本。');
      return false;
    } finally {
      busyId.value = null;
    }
  }

  /** 下架封存（Published → Archived）。 */
  async function archive(id: string, storeId: string) {
    busyId.value = id;
    error.value = null;
    try {
      await catalogApi.catalogs.archive(id);
      await load(storeId);
      return true;
    } catch (err) {
      error.value = messageOf(err, '下架失敗。');
      return false;
    } finally {
      busyId.value = null;
    }
  }

  /** 以 PUT 直傳檔案 bytes 到 StorageService 簽發的 uploadUrl。 */
  async function putFile(uploadUrl: string, file: File) {
    const res = await fetch(uploadUrl, {
      method: 'PUT',
      headers: { 'Content-Type': file.type || 'application/octet-stream' },
      body: file,
    });
    if (!res.ok) throw new Error(`檔案上傳失敗（${res.status}）：${file.name}`);
  }

  /**
   * 建立新商品的完整流程：
   *   1. 建立商品（Draft）
   *   2. 建立版本 1.0.0 並設為目前版本
   *   3. 逐一申請版本檔案簽章 URL 並直傳
   *   4. 視需要送審上架
   * 成功回傳建立後的 CatalogDto；失敗將訊息寫入 error 並回傳 null。
   */
  async function createProduct(input: NewCatalogInput): Promise<CatalogDto | null> {
    creating.value = true;
    error.value = null;
    try {
      await loadCategories();
      const categoryId = input.categorySlug
        ? categories.value.find((c) => c.slug === input.categorySlug)?.id ?? null
        : null;

      const created = await catalogApi.catalogs.create({
        storeId: input.storeId,
        name: input.name.trim(),
        slug: slugify(input.name),
        summary: input.summary?.trim() || null,
        description: input.description?.trim() || null,
        coverHue: input.coverHue,
        categoryId,
        price: input.price,
        tags: input.tags?.length ? input.tags : null,
      });
      const catalogId = created.data.id!;

      const version = await catalogApi.catalogVersions.create(catalogId, { version: '1.0.0', releaseNote: '首次發行。' });
      const versionId = version.data.id!;

      for (const file of input.files) {
        const urlRes = await catalogApi.catalogVersions.requestAssetUploadUrl(catalogId, versionId, {
          fileName: file.name,
          contentType: file.type || 'application/octet-stream',
          sizeBytes: file.size,
        });
        if (urlRes.data.uploadUrl) await putFile(urlRes.data.uploadUrl, file);
      }

      if (input.publish) await catalogApi.catalogs.publish(catalogId);

      return created.data;
    } catch (err) {
      error.value = messageOf(err, '建立商品失敗。');
      return null;
    } finally {
      creating.value = false;
    }
  }

  return {
    products,
    categories,
    loading,
    busyId,
    creating,
    error,
    publishedCount,
    statusCount,
    loadCategories,
    load,
    loadByStore,
    suspend,
    unsuspend,
    publish,
    archive,
    createProduct,
  };
});
