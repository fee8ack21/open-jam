import { ref } from 'vue';
import { defineStore } from 'pinia';
import { catalogApi } from '@/api';
import i18n from '@/i18n';
import {
  CatalogAssetType,
  type CatalogDto,
  type CatalogVersionDto,
} from '@/api/catalog-service';

/** 由後端 RFC 9457 Problem Details 取出可顯示的錯誤訊息。 */
function messageOf(err: unknown, fallback = i18n.global.t('storeError.actionFailed')): string {
  if (typeof err === 'string') return err;
  // 直傳失敗由 putFile 以 Error 拋出（非 Problem Details），訊息本身已可顯示。
  if (err instanceof Error) return err.message;
  const problem = (err as { error?: { detail?: string; title?: string; errors?: Record<string, string[]> } })?.error
    ?? (err as { detail?: string; title?: string } | null | undefined);
  const validation = (problem as { errors?: Record<string, string[]> })?.errors;
  const first = validation ? Object.values(validation).flat()[0] : null;
  return first ?? problem?.detail ?? problem?.title ?? fallback;
}

/** 商品基本資料編輯欄位（對應 PATCH catalog + 分類 + 標籤三支 API）。 */
export interface CatalogBasics {
  name: string;
  summary: string;
  description: string;
  coverHue: number;
  price: number;
  categoryId: string | null;
  tags: string[];
}

/**
 * 單一商品的編輯 store：載入完整商品（含展示資產與版本），
 * 提供基本資料、封面圖與版本 / 可下載檔的編輯操作。
 * 與列表用的 catalog store 分開，避免列表狀態與編輯狀態互相干擾。
 */
export const useCatalogEditStore = defineStore('catalogEdit', () => {
  const catalog = ref<CatalogDto | null>(null);
  const versions = ref<CatalogVersionDto[]>([]);
  const loading = ref(false);
  const saving = ref(false);
  /** 資產 / 版本操作進行中（上傳、刪除、建立版本），供按鈕禁用。 */
  const busy = ref(false);
  const error = ref<string | null>(null);

  /** 以 PUT 直傳檔案 bytes 到 StorageService 簽發的 uploadUrl。 */
  async function putFile(uploadUrl: string, file: File) {
    const res = await fetch(uploadUrl, {
      method: 'PUT',
      headers: { 'Content-Type': file.type || 'application/octet-stream' },
      body: file,
    });
    if (!res.ok) throw new Error(i18n.global.t('storeError.fileUploadFailed', { status: res.status, name: file.name }));
  }

  /** 目前的封面資產（後端一次僅綁定一張 Thumbnail）。 */
  function thumbnailAssets() {
    return (catalog.value?.assets ?? []).filter((a) => a.type === CatalogAssetType.Thumbnail);
  }

  /** 重新取得商品完整資訊（動作後刷新）。 */
  async function refreshCatalog(id: string) {
    const res = await catalogApi.catalogs.get(id);
    catalog.value = res.data;
  }

  /** 重新取得版本清單（新到舊）。 */
  async function refreshVersions(id: string) {
    const res = await catalogApi.catalogVersions.list(id);
    versions.value = res.data ?? [];
  }

  /** 載入待編輯的商品與其版本；失敗時 catalog 保持 null 由頁面顯示錯誤。 */
  async function load(id: string) {
    loading.value = true;
    error.value = null;
    catalog.value = null;
    versions.value = [];
    try {
      await refreshCatalog(id);
      await refreshVersions(id);
    } catch (err) {
      error.value = messageOf(err, i18n.global.t('storeError.loadCatalogFailed'));
    } finally {
      loading.value = false;
    }
  }

  /**
   * 儲存基本資料：欄位 PATCH、分類與標籤各自全量覆蓋（slug 不變，避免商品網址漂移）。
   */
  async function saveBasics(id: string, basics: CatalogBasics) {
    saving.value = true;
    error.value = null;
    try {
      await catalogApi.catalogs.update(id, {
        name: basics.name.trim(),
        summary: basics.summary.trim(),
        description: basics.description.trim(),
        coverHue: basics.coverHue,
        price: basics.price,
      });
      await catalogApi.catalogs.setCategory(id, { categoryId: basics.categoryId });
      await catalogApi.catalogs.setTags(id, { tags: basics.tags });
      await refreshCatalog(id);
      return true;
    } catch (err) {
      error.value = messageOf(err, i18n.global.t('storeError.saveCatalogFailed'));
      return false;
    } finally {
      saving.value = false;
    }
  }

  /**
   * 設定 / 更換封面圖。後端僅在商品尚未綁定封面時，才會把 confirm 的 Thumbnail 資產設為封面，
   * 因此順序為：先直傳新檔（此時不 confirm、不計配額）→ 刪掉舊封面解除綁定 → 再 confirm 新檔。
   * 把刪除排在直傳之後，可將「舊封面已刪、新封面未成」的空窗縮到最小。
   */
  async function uploadCover(id: string, file: File) {
    busy.value = true;
    error.value = null;
    try {
      const urlRes = await catalogApi.catalogs.requestAssetUploadUrl(id, {
        type: CatalogAssetType.Thumbnail,
        fileName: file.name,
        contentType: file.type || 'application/octet-stream',
        sizeBytes: file.size,
      });
      const assetId = urlRes.data.assetId!;
      if (urlRes.data.uploadUrl) await putFile(urlRes.data.uploadUrl, file);

      for (const asset of thumbnailAssets()) await catalogApi.catalogs.deleteAsset(id, asset.id!);
      await catalogApi.catalogs.confirmAsset(id, assetId, { type: CatalogAssetType.Thumbnail });

      await refreshCatalog(id);
      return true;
    } catch (err) {
      error.value = messageOf(err, i18n.global.t('storeError.uploadCoverFailed'));
      return false;
    } finally {
      busy.value = false;
    }
  }

  /** 移除封面圖（商品改以色調封面呈現）。 */
  async function removeCover(id: string) {
    busy.value = true;
    error.value = null;
    try {
      for (const asset of thumbnailAssets()) await catalogApi.catalogs.deleteAsset(id, asset.id!);
      await refreshCatalog(id);
      return true;
    } catch (err) {
      error.value = messageOf(err, i18n.global.t('storeError.removeCoverFailed'));
      return false;
    } finally {
      busy.value = false;
    }
  }

  /**
   * 新增預覽媒體（商品頁圖庫展示）：簽 URL 直傳後 confirm。
   * 依 MIME 決定資產型別（image/* → Screenshot、video/* → PreviewVideo）。
   */
  async function uploadPreviewMedia(id: string, file: File) {
    busy.value = true;
    error.value = null;
    const type = file.type.startsWith('video/')
      ? CatalogAssetType.PreviewVideo
      : CatalogAssetType.Screenshot;
    try {
      const urlRes = await catalogApi.catalogs.requestAssetUploadUrl(id, {
        type,
        fileName: file.name,
        contentType: file.type || 'application/octet-stream',
        sizeBytes: file.size,
      });
      const assetId = urlRes.data.assetId!;
      if (urlRes.data.uploadUrl) await putFile(urlRes.data.uploadUrl, file);
      await catalogApi.catalogs.confirmAsset(id, assetId, { type });
      await refreshCatalog(id);
      return true;
    } catch (err) {
      error.value = messageOf(err, i18n.global.t('storeError.uploadScreenshotFailed'));
      return false;
    } finally {
      busy.value = false;
    }
  }

  /** 新增外部影片嵌入（YouTube）預覽媒體：不涉檔案上傳、不計配額。 */
  async function addExternalVideo(id: string, url: string) {
    busy.value = true;
    error.value = null;
    try {
      await catalogApi.catalogs.addExternalVideoAsset(id, { url });
      await refreshCatalog(id);
      return true;
    } catch (err) {
      error.value = messageOf(err, i18n.global.t('storeError.addExternalVideoFailed'));
      return false;
    } finally {
      busy.value = false;
    }
  }

  /** 刪除預覽媒體。 */
  async function deletePreviewMedia(id: string, assetId: string) {
    busy.value = true;
    error.value = null;
    try {
      await catalogApi.catalogs.deleteAsset(id, assetId);
      await refreshCatalog(id);
      return true;
    } catch (err) {
      error.value = messageOf(err, i18n.global.t('storeError.deleteScreenshotFailed'));
      return false;
    } finally {
      busy.value = false;
    }
  }

  /** 建立新版本；後端會將其設為商品目前對外版本（買家取得的即是此版本的檔案）。 */
  async function createVersion(id: string, version: string, releaseNote: string) {
    busy.value = true;
    error.value = null;
    try {
      await catalogApi.catalogVersions.create(id, {
        version: version.trim(),
        releaseNote: releaseNote.trim() || null,
      });
      await refreshVersions(id);
      await refreshCatalog(id);
      return true;
    } catch (err) {
      error.value = messageOf(err, i18n.global.t('storeError.createVersionFailed'));
      return false;
    } finally {
      busy.value = false;
    }
  }

  /** 上傳版本可下載檔：簽 URL 直傳後 confirm（此刻才扣配額並建立 reference）。 */
  async function uploadVersionFile(id: string, versionId: string, file: File) {
    busy.value = true;
    error.value = null;
    try {
      const urlRes = await catalogApi.catalogVersions.requestAssetUploadUrl(id, versionId, {
        fileName: file.name,
        contentType: file.type || 'application/octet-stream',
        sizeBytes: file.size,
      });
      const assetId = urlRes.data.assetId!;
      if (urlRes.data.uploadUrl) await putFile(urlRes.data.uploadUrl, file);
      await catalogApi.catalogVersions.confirmAsset(id, versionId, assetId);
      await refreshVersions(id);
      return true;
    } catch (err) {
      error.value = messageOf(err, i18n.global.t('storeError.uploadFileFailed', { name: file.name }));
      return false;
    } finally {
      busy.value = false;
    }
  }

  /** 刪除版本可下載檔。 */
  async function deleteVersionFile(id: string, versionId: string, assetId: string) {
    busy.value = true;
    error.value = null;
    try {
      await catalogApi.catalogVersions.deleteAsset(id, versionId, assetId);
      await refreshVersions(id);
      return true;
    } catch (err) {
      error.value = messageOf(err, i18n.global.t('storeError.deleteFileFailed'));
      return false;
    } finally {
      busy.value = false;
    }
  }

  /** 取得版本可下載檔的短效下載 URL（管理用途，供 Owner 自行檢查內容）。 */
  async function versionFileDownloadUrl(id: string, versionId: string, assetId: string): Promise<string | null> {
    error.value = null;
    try {
      const res = await catalogApi.catalogVersions.getAssetDownloadUrl(id, versionId, assetId);
      return res.data.downloadUrl ?? null;
    } catch (err) {
      error.value = messageOf(err, i18n.global.t('storeError.loadDownloadsFailed'));
      return null;
    }
  }

  return {
    catalog,
    versions,
    loading,
    saving,
    busy,
    error,
    load,
    saveBasics,
    uploadCover,
    removeCover,
    uploadPreviewMedia,
    addExternalVideo,
    deletePreviewMedia,
    createVersion,
    uploadVersionFile,
    deleteVersionFile,
    versionFileDownloadUrl,
  };
});
