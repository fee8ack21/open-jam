import { ref, computed } from 'vue';
import { defineStore } from 'pinia';
import { storeApi } from '@/api';
import i18n from '@/i18n';
import {
  StoreApplicationStatus,
  type StoreApplicationDto,
  type MyStoreDto,
} from '@/api/store-service';

/** 由後端 RFC 9457 Problem Details 取出可顯示的錯誤訊息。 */
function messageOf(err: unknown, fallback = i18n.global.t('storeError.actionFailed')): string {
  if (typeof err === 'string') return err;
  const problem = err as { detail?: string; title?: string } | null | undefined;
  return problem?.detail ?? problem?.title ?? fallback;
}

export const useStoreApplicationStore = defineStore('storeApplication', () => {
  const applications = ref<StoreApplicationDto[]>([]);   // 自己的申請，新到舊
  const stores = ref<MyStoreDto[]>([]);                  // 核准後擁有的商店
  const loading = ref(false);
  const submitting = ref(false);
  const saving = ref(false);                             // 商店設定儲存中（更新資料 / 上傳圖片）
  const error = ref<string | null>(null);

  /** 最新一筆申請（後端已依建立時間 desc 排序）。 */
  const latestApplication = computed(() => applications.value[0] ?? null);

  /** 是否有待審核中的申請。 */
  const hasPending = computed(() =>
    applications.value.some((a) => a.status === StoreApplicationStatus.Pending),
  );

  /** 是否已擁有商店。 */
  const hasStore = computed(() => stores.value.length > 0);

  /** 目前商店（取第一筆）。 */
  const primaryStore = computed(() => stores.value[0]?.store ?? null);

  /** 目前商店顯示名稱；尚未載入時為 null。 */
  const storeName = computed(() => primaryStore.value?.storeName ?? null);

  /** 載入「我的申請」與「我的商店」。 */
  async function load() {
    loading.value = true;
    error.value = null;
    try {
      const [appsRes, storesRes] = await Promise.all([
        storeApi.storeApplications.getMine({ Offset: 0, Limit: 20 }),
        storeApi.stores.getMine(),
      ]);
      applications.value = appsRes.data.items ?? [];
      stores.value = storesRes.data ?? [];
    } catch (err) {
      error.value = messageOf(err, i18n.global.t('storeError.loadStoreDataFailed'));
    } finally {
      loading.value = false;
    }
  }

  /**
   * 提交開店申請。成功回傳 true 並重新載入；失敗將訊息寫入 error 並回傳 false。
   */
  async function submit({ storeName, storeSlug }: { storeName: string; storeSlug: string }) {
    submitting.value = true;
    error.value = null;
    try {
      await storeApi.storeApplications.submit({ storeName, storeSlug });
      await load();
      return true;
    } catch (err) {
      error.value = messageOf(err, i18n.global.t('storeError.submitAppFailed'));
      return false;
    } finally {
      submitting.value = false;
    }
  }

  /** 撤回待審核申請後重新載入。 */
  async function withdraw(id: string) {
    error.value = null;
    try {
      await storeApi.storeApplications.withdraw(id);
      await load();
      return true;
    } catch (err) {
      error.value = messageOf(err, i18n.global.t('storeError.withdrawAppFailed'));
      return false;
    }
  }

  /** 更新目前商店的名稱 / 描述。成功回傳 true 並重新載入。 */
  async function updateStore(input: { storeName?: string; description?: string }) {
    const id = primaryStore.value?.id;
    if (!id) {
      error.value = i18n.global.t('storeError.noStore');
      return false;
    }
    saving.value = true;
    error.value = null;
    try {
      await storeApi.stores.update(id, {
        storeName: input.storeName,
        description: input.description,
      });
      await load();
      return true;
    } catch (err) {
      error.value = messageOf(err, i18n.global.t('storeError.updateStoreFailed'));
      return false;
    } finally {
      saving.value = false;
    }
  }

  /** 以 PUT 直傳檔案 bytes 到 StorageService 簽發的 uploadUrl。 */
  async function putFile(uploadUrl: string, file: File) {
    const res = await fetch(uploadUrl, {
      method: 'PUT',
      headers: { 'Content-Type': file.type || 'application/octet-stream' },
      body: file,
    });
    if (!res.ok) throw new Error(i18n.global.t('storeError.imageUploadFailed', { status: res.status }));
  }

  /**
   * 上傳商店頭像 / 橫幅：申請簽章 URL（後端即綁定該資產到商店）→ 直傳 bytes →
   * confirm（後端轉呼叫 StorageService 驗證物件存在並標記 Ready）→ 重新載入。
   * confirm 為 GCS 模式下唯一觸發處理 pipeline 的路徑，不可省略。成功回傳 true。
   */
  async function uploadStoreImage(file: File, kind: 'avatar' | 'banner') {
    const id = primaryStore.value?.id;
    if (!id) {
      error.value = i18n.global.t('storeError.noStore');
      return false;
    }
    saving.value = true;
    error.value = null;
    try {
      const payload = {
        fileName: file.name,
        contentType: file.type || 'application/octet-stream',
        sizeBytes: file.size,
      };
      const res = kind === 'avatar'
        ? await storeApi.stores.requestAvatarUploadUrl(id, payload)
        : await storeApi.stores.requestBannerUploadUrl(id, payload);

      const { uploadUrl, assetId } = res.data;
      if (!uploadUrl || !assetId) throw new Error(i18n.global.t('storeError.uploadImageFailed'));

      await putFile(uploadUrl, file);

      const confirmed = kind === 'avatar'
        ? await storeApi.stores.confirmAvatarUpload(id, { assetId })
        : await storeApi.stores.confirmBannerUpload(id, { assetId });

      // confirm 回傳更新後的 StoreDto，直接就地更新避免多一次往返。
      const i = stores.value.findIndex((s) => s.store?.id === id);
      if (i >= 0) stores.value[i] = { ...stores.value[i], store: confirmed.data };
      else await load();
      return true;
    } catch (err) {
      error.value = messageOf(err, i18n.global.t('storeError.uploadImageFailed'));
      return false;
    } finally {
      saving.value = false;
    }
  }

  return {
    applications,
    stores,
    loading,
    submitting,
    saving,
    error,
    latestApplication,
    hasPending,
    hasStore,
    primaryStore,
    storeName,
    load,
    submit,
    withdraw,
    updateStore,
    uploadStoreImage,
  };
});
