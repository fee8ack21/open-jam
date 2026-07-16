import { ref } from 'vue';
import { defineStore } from 'pinia';
import { catalogApi } from '@/api';
import i18n from '@/i18n';
import {
  CatalogAssetType,
  CatalogStatus,
  CatalogSort,
  type CatalogSummaryDto,
  type CatalogDto,
  type CatalogCategoryDto,
} from '@/api/catalog-service';

/** 由後端 RFC 9457 Problem Details 取出可顯示的錯誤訊息。 */
function messageOf(err: unknown, fallback = i18n.global.t('storeError.actionFailed')): string {
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

/** 商品列表查詢條件（皆為選填；空字串 / null 視為未篩選）。 */
export interface CatalogListFilter {
  search?: string;
  status?: CatalogStatus | null;
}

/** 目前列表資料來源：'mine' 走 listMine（Owner），'store' 走 listByStore（Admin）。 */
interface CatalogSource {
  kind: 'mine' | 'store';
  storeId: string;
}

const PAGE_SIZE = 10;

/** 上架精靈的商品中繼資料快照（step-1 欄位，不含檔案）。 */
export interface DraftMeta {
  storeId: string;
  name: string;
  /** 分類 slug（music / photography / ebook…），會對應成 categoryId。 */
  categorySlug?: string | null;
  summary?: string | null;
  description?: string | null;
  coverHue?: number;
  price: number;
  tags?: string[];
}

/**
 * 創作者（商店 Owner）自家商品 store：串接 CatalogService。
 * 取代原 dashboard mock，提供商品列表與上 / 下架、建立新商品（含版本與檔案上傳）。
 */
export const useCatalogStore = defineStore('catalog', () => {
  const products = ref<CatalogSummaryDto[]>([]); // 目前頁次的商品
  const totalCount = ref(0);                     // 目前篩選條件下的總筆數
  const offset = ref(0);
  const pageSize = ref(PAGE_SIZE);
  const categories = ref<CatalogCategoryDto[]>([]);
  const loading = ref(false);
  const busyId = ref<string | null>(null);   // 進行中的商品 id（避免重複點擊）
  const creating = ref(false);
  const error = ref<string | null>(null);
  const filter = ref<CatalogListFilter>({ search: '', status: null });
  const source = ref<CatalogSource | null>(null);

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

  /** 依目前 source / offset / filter 取得一頁商品。 */
  async function fetchPage() {
    const src = source.value;
    if (!src || !src.storeId) {
      products.value = [];
      totalCount.value = 0;
      return;
    }
    loading.value = true;
    error.value = null;
    const query = {
      Offset: offset.value,
      Limit: pageSize.value,
      Search: filter.value.search?.trim() || undefined,
      Status: filter.value.status ?? undefined,
    };
    try {
      const res = src.kind === 'mine'
        ? await catalogApi.catalogs.listMine({ StoreId: src.storeId, ...query })
        : await catalogApi.catalogs.listByStore(src.storeId, query);
      products.value = res.data.items ?? [];
      totalCount.value = res.data.totalCount ?? products.value.length;
    } catch (err) {
      const fallback = src.kind === 'mine'
        ? i18n.global.t('storeError.loadCatalogFailed')
        : i18n.global.t('storeError.loadStoreCatalogsFailed');
      error.value = messageOf(err, fallback);
      products.value = [];
      totalCount.value = 0;
    } finally {
      loading.value = false;
    }
  }

  /** 載入指定商店的全部商品（含未上架，Owner 視角）；回到第一頁。 */
  async function load(storeId: string) {
    source.value = storeId ? { kind: 'mine', storeId } : null;
    offset.value = 0;
    await fetchPage();
  }

  /** （Admin）載入指定商店的全部商品（含草稿 / 已停權）；回到第一頁。 */
  async function loadByStore(storeId: string) {
    source.value = storeId ? { kind: 'store', storeId } : null;
    offset.value = 0;
    await fetchPage();
  }

  /** 重新載入目前頁次（動作後刷新，不重置頁碼）。 */
  async function reload() {
    await fetchPage();
  }

  /** 套用新的篩選條件並回到第一頁重新載入。 */
  async function applyFilter(next: CatalogListFilter) {
    filter.value = { ...filter.value, ...next };
    offset.value = 0;
    await fetchPage();
  }

  /** 跳至指定頁（1-based）。 */
  async function goPage(page: number) {
    offset.value = Math.max(0, (page - 1) * pageSize.value);
    await fetchPage();
  }

  /** 變更每頁筆數並回到第一頁重新載入。 */
  async function setPageSize(size: number) {
    pageSize.value = size;
    offset.value = 0;
    await fetchPage();
  }

  /** （Admin）停權商品（任一狀態 → Suspended），停權後重新載入目前頁次。 */
  async function suspend(id: string) {
    busyId.value = id;
    error.value = null;
    try {
      await catalogApi.catalogs.suspend(id);
      await reload();
      return true;
    } catch (err) {
      error.value = messageOf(err, i18n.global.t('storeError.suspendProductFailed'));
      return false;
    } finally {
      busyId.value = null;
    }
  }

  /** （Admin）解除停權（Suspended → Archived），完成後重新載入目前頁次。 */
  async function unsuspend(id: string) {
    busyId.value = id;
    error.value = null;
    try {
      await catalogApi.catalogs.unsuspend(id);
      await reload();
      return true;
    } catch (err) {
      error.value = messageOf(err, i18n.global.t('storeError.unsuspendFailed'));
      return false;
    } finally {
      busyId.value = null;
    }
  }

  /** 上架（Draft / Archived → Published）。需已有目前版本。 */
  async function publish(id: string) {
    busyId.value = id;
    error.value = null;
    try {
      await catalogApi.catalogs.publish(id);
      await reload();
      return true;
    } catch (err) {
      error.value = messageOf(err, i18n.global.t('storeError.publishNeedVersion'));
      return false;
    } finally {
      busyId.value = null;
    }
  }

  /** 下架封存（Published → Archived）。 */
  async function archive(id: string) {
    busyId.value = id;
    error.value = null;
    try {
      await catalogApi.catalogs.archive(id);
      await reload();
      return true;
    } catch (err) {
      error.value = messageOf(err, i18n.global.t('storeError.archiveFailed'));
      return false;
    } finally {
      busyId.value = null;
    }
  }

  /** 刪除商品（軟刪除；僅未曾上架的草稿可刪除），完成後重新載入目前頁次。 */
  async function remove(id: string) {
    busyId.value = id;
    error.value = null;
    try {
      await catalogApi.catalogs.delete(id);
      await reload();
      return true;
    } catch (err) {
      error.value = messageOf(err, i18n.global.t('storeError.deleteProductFailed'));
      return false;
    } finally {
      busyId.value = null;
    }
  }

  /** 設定 / 取消店長精選（僅已上架商品可設為精選），完成後重新載入目前頁次。 */
  async function setStoreFeatured(id: string, featured: boolean) {
    busyId.value = id;
    error.value = null;
    try {
      if (featured) await catalogApi.catalogs.storeFeature(id);
      else await catalogApi.catalogs.storeUnfeature(id);
      await reload();
      return true;
    } catch (err) {
      error.value = messageOf(err, i18n.global.t('storeError.storeFeatureFailed'));
      return false;
    } finally {
      busyId.value = null;
    }
  }

  /** 取得該商店目前的店長精選商品（依 storeFeaturedSortOrder 排序），供精選排序管理使用。 */
  async function listStoreFeatured(storeId: string): Promise<CatalogSummaryDto[]> {
    const res = await catalogApi.catalogs.listMine({
      StoreId: storeId,
      StoreFeatured: true,
      Sort: CatalogSort.StoreFeatured,
      Offset: 0,
      Limit: 100,
    });
    return res.data.items ?? [];
  }

  /** 重排店長精選顯示順序（catalogIds 須完整涵蓋該商店目前的店長精選商品）。 */
  async function reorderStoreFeatured(storeId: string, catalogIds: string[]) {
    error.value = null;
    try {
      await catalogApi.catalogs.reorderStoreFeatured({ storeId, catalogIds });
      return true;
    } catch (err) {
      error.value = messageOf(err, i18n.global.t('storeError.reorderStoreFeaturedFailed'));
      return false;
    }
  }

  /** 以 PUT 直傳檔案 bytes 到 StorageService 簽發的 uploadUrl。 */
  async function putFile(uploadUrl: string, file: File) {
    const res = await fetch(uploadUrl, {
      method: 'PUT',
      headers: { 'Content-Type': file.type || 'application/octet-stream' },
      body: file,
    });
    if (!res.ok) throw new Error(i18n.global.t('storeError.fileUploadFailed', { status: res.status, name: file.name }));
  }

  // ── 上架精靈：選檔即時上傳（Draft catalog + 版本承載，送出時才 confirm 計配額）─────
  // 使用者於 step 2 加入檔案時即簽 URL 直傳，但「不 confirm」——後端此時不建立資產
  // reference、不扣配額（未 reference 的暫存檔逾期由 StorageService 清理）。送出時才
  // 同步 step-1 欄位、逐一 confirm（此刻扣配額並建立 reference）並視需要上架。
  const draftCatalogId = ref<string | null>(null);
  const draftVersionId = ref<string | null>(null);
  // 單飛旗標：多檔同時加入時只建立一次 Draft catalog / 版本。
  let draftInit: Promise<{ catalogId: string; versionId: string } | null> | null = null;

  /** 清除目前的 Draft 上傳狀態（開啟精靈 / 送出成功後呼叫，重新開始）。 */
  function resetDraftUpload() {
    draftCatalogId.value = null;
    draftVersionId.value = null;
    draftInit = null;
  }

  /** 確保 Draft catalog 與版本存在（並發呼叫共用同一次建立）。 */
  async function ensureDraft(meta: DraftMeta): Promise<{ catalogId: string; versionId: string } | null> {
    if (draftCatalogId.value && draftVersionId.value)
      return { catalogId: draftCatalogId.value, versionId: draftVersionId.value };
    if (draftInit) return draftInit;
    draftInit = (async () => {
      try {
        await loadCategories();
        const categoryId = meta.categorySlug
          ? categories.value.find((c) => c.slug === meta.categorySlug)?.id ?? null
          : null;
        const created = await catalogApi.catalogs.create({
          storeId: meta.storeId,
          name: meta.name.trim(),
          slug: slugify(meta.name),
          summary: meta.summary?.trim() || null,
          description: meta.description?.trim() || null,
          coverHue: meta.coverHue,
          categoryId,
          price: meta.price,
          tags: meta.tags?.length ? meta.tags : null,
        });
        const catalogId = created.data.id!;
        const version = await catalogApi.catalogVersions.create(catalogId, {
          version: '1.0.0',
          releaseNote: i18n.global.t('storeError.firstRelease'),
        });
        draftCatalogId.value = catalogId;
        draftVersionId.value = version.data.id!;
        return { catalogId, versionId: draftVersionId.value };
      } catch (err) {
        error.value = messageOf(err, i18n.global.t('storeError.createProductFailed'));
        draftInit = null; // 允許重試
        return null;
      }
    })();
    return draftInit;
  }

  /**
   * 即時上傳單一下載檔到 Draft 版本：簽 URL + 直傳，但不 confirm（不計配額）。
   * 首檔會順帶建立 Draft catalog / 版本。成功回傳 assetId，失敗回傳 null。
   */
  async function uploadDraftAsset(meta: DraftMeta, file: File): Promise<string | null> {
    error.value = null;
    const draft = await ensureDraft(meta);
    if (!draft) return null;
    try {
      const urlRes = await catalogApi.catalogVersions.requestAssetUploadUrl(draft.catalogId, draft.versionId, {
        fileName: file.name,
        contentType: file.type || 'application/octet-stream',
        sizeBytes: file.size,
      });
      if (urlRes.data.uploadUrl) await putFile(urlRes.data.uploadUrl, file);
      return urlRes.data.assetId ?? null;
    } catch (err) {
      error.value = messageOf(err, i18n.global.t('storeError.createProductFailed'));
      return null;
    }
  }

  /**
   * 即時上傳封面圖（展示型 Thumbnail 資產）：簽 URL + 直傳，但不 confirm（不計配額）。
   * 尚未有 Draft catalog 時會順帶建立。成功回傳 assetId，失敗回傳 null。
   */
  async function uploadDraftCover(meta: DraftMeta, file: File): Promise<string | null> {
    error.value = null;
    const draft = await ensureDraft(meta);
    if (!draft) return null;
    try {
      const urlRes = await catalogApi.catalogs.requestAssetUploadUrl(draft.catalogId, {
        type: CatalogAssetType.Thumbnail,
        fileName: file.name,
        contentType: file.type || 'application/octet-stream',
        sizeBytes: file.size,
      });
      if (urlRes.data.uploadUrl) await putFile(urlRes.data.uploadUrl, file);
      return urlRes.data.assetId ?? null;
    } catch (err) {
      error.value = messageOf(err, i18n.global.t('storeError.uploadCoverFailed'));
      return null;
    }
  }

  /**
   * 即時上傳預覽媒體（圖片 → Screenshot、影片 → PreviewVideo）：簽 URL + 直傳，
   * 但不 confirm（不計配額）。尚未有 Draft catalog 時會順帶建立。
   * 成功回傳 assetId，失敗回傳 null。
   */
  async function uploadDraftPreviewMedia(meta: DraftMeta, file: File): Promise<string | null> {
    error.value = null;
    const draft = await ensureDraft(meta);
    if (!draft) return null;
    const type = file.type.startsWith('video/')
      ? CatalogAssetType.PreviewVideo
      : CatalogAssetType.Screenshot;
    try {
      const urlRes = await catalogApi.catalogs.requestAssetUploadUrl(draft.catalogId, {
        type,
        fileName: file.name,
        contentType: file.type || 'application/octet-stream',
        sizeBytes: file.size,
      });
      if (urlRes.data.uploadUrl) await putFile(urlRes.data.uploadUrl, file);
      return urlRes.data.assetId ?? null;
    } catch (err) {
      error.value = messageOf(err, i18n.global.t('storeError.uploadScreenshotFailed'));
      return null;
    }
  }

  /**
   * 加入 YouTube 外部影片嵌入（即時建立，不涉檔案上傳、不計配額）。
   * 尚未有 Draft catalog 時會順帶建立。成功回傳 assetId，失敗回傳 null。
   */
  async function addDraftExternalVideo(meta: DraftMeta, url: string): Promise<string | null> {
    error.value = null;
    const draft = await ensureDraft(meta);
    if (!draft) return null;
    try {
      const res = await catalogApi.catalogs.addExternalVideoAsset(draft.catalogId, { url });
      return res.data.id ?? null;
    } catch (err) {
      error.value = messageOf(err, i18n.global.t('storeError.addExternalVideoFailed'));
      return null;
    }
  }

  /** 刪除 Draft catalog 上已建立的展示型資產（精靈中移除 YouTube 嵌入用）。 */
  async function deleteDraftAsset(assetId: string): Promise<boolean> {
    const catalogId = draftCatalogId.value;
    if (!catalogId) return true;
    try {
      await catalogApi.catalogs.deleteAsset(catalogId, assetId);
      return true;
    } catch (err) {
      error.value = messageOf(err, i18n.global.t('storeError.deleteScreenshotFailed'));
      return false;
    }
  }

  /**
   * 送出：以目前 step-1 欄位同步 Draft catalog（草稿建立後可能已編輯），
   * 逐一 confirm 已上傳資產（此時才扣配額並建立 reference），視需要上架。
   * 成功回傳 CatalogDto 並清空 Draft 狀態；失敗將訊息寫入 error 並回傳 null。
   */
  async function finalizeDraft(
    meta: DraftMeta,
    assetIds: string[],
    publish: boolean,
    coverAssetId?: string | null,
    previewMedia?: { assetId: string; type: CatalogAssetType }[],
  ): Promise<CatalogDto | null> {
    const catalogId = draftCatalogId.value;
    const versionId = draftVersionId.value;
    if (!catalogId || !versionId) {
      error.value = i18n.global.t('storeError.createProductFailed');
      return null;
    }
    creating.value = true;
    error.value = null;
    try {
      await loadCategories();
      const categoryId = meta.categorySlug
        ? categories.value.find((c) => c.slug === meta.categorySlug)?.id ?? null
        : null;

      // 同步 step-1 欄位（slug 於建立時已定，維持不變以免草稿代稱漂移）。
      await catalogApi.catalogs.update(catalogId, {
        name: meta.name.trim(),
        summary: meta.summary?.trim() || '',
        coverHue: meta.coverHue,
        price: meta.price,
      });
      await catalogApi.catalogs.setCategory(catalogId, { categoryId });
      await catalogApi.catalogs.setTags(catalogId, { tags: meta.tags ?? [] });

      // confirm 已上傳資產（冪等；此刻扣配額並建立 reference）。
      for (const assetId of assetIds) {
        await catalogApi.catalogVersions.confirmAsset(catalogId, versionId, assetId);
      }
      if (coverAssetId) {
        await catalogApi.catalogs.confirmAsset(catalogId, coverAssetId, { type: CatalogAssetType.Thumbnail });
      }
      // 預覽媒體逐一 confirm（YouTube 嵌入建立當下已完成，不在此列）。
      for (const media of previewMedia ?? []) {
        await catalogApi.catalogs.confirmAsset(catalogId, media.assetId, { type: media.type });
      }

      if (publish) await catalogApi.catalogs.publish(catalogId);

      const dto = await catalogApi.catalogs.get(catalogId);
      resetDraftUpload();
      return dto.data;
    } catch (err) {
      error.value = messageOf(err, i18n.global.t('storeError.createProductFailed'));
      return null;
    } finally {
      creating.value = false;
    }
  }

  return {
    products,
    totalCount,
    offset,
    pageSize,
    setPageSize,
    categories,
    loading,
    busyId,
    creating,
    error,
    filter,
    loadCategories,
    load,
    loadByStore,
    reload,
    applyFilter,
    goPage,
    suspend,
    unsuspend,
    publish,
    archive,
    remove,
    setStoreFeatured,
    listStoreFeatured,
    reorderStoreFeatured,
    draftCatalogId,
    resetDraftUpload,
    uploadDraftAsset,
    uploadDraftCover,
    uploadDraftPreviewMedia,
    addDraftExternalVideo,
    deleteDraftAsset,
    finalizeDraft,
  };
});
