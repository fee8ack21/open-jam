import { ref, computed } from 'vue';
import { defineStore } from 'pinia';
import { storeApi } from '@/api';
import {
  StoreApplicationStatus,
  type StoreApplicationDto,
  type MyStoreDto,
} from '@/api/store-service';

/** 由後端 RFC 9457 Problem Details 取出可顯示的錯誤訊息。 */
function messageOf(err: unknown, fallback = '操作失敗，請稍後再試。'): string {
  if (typeof err === 'string') return err;
  const problem = err as { detail?: string; title?: string } | null | undefined;
  return problem?.detail ?? problem?.title ?? fallback;
}

export const useStoreApplicationStore = defineStore('storeApplication', () => {
  const applications = ref<StoreApplicationDto[]>([]);   // 自己的申請，新到舊
  const stores = ref<MyStoreDto[]>([]);                  // 核准後擁有的商店
  const loading = ref(false);
  const submitting = ref(false);
  const error = ref<string | null>(null);

  /** 最新一筆申請（後端已依建立時間 desc 排序）。 */
  const latestApplication = computed(() => applications.value[0] ?? null);

  /** 是否有待審核中的申請。 */
  const hasPending = computed(() =>
    applications.value.some((a) => a.status === StoreApplicationStatus.Pending),
  );

  /** 是否已擁有商店。 */
  const hasStore = computed(() => stores.value.length > 0);

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
      error.value = messageOf(err, '載入開店資料失敗。');
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
      error.value = messageOf(err, '提交開店申請失敗。');
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
      error.value = messageOf(err, '撤回申請失敗。');
      return false;
    }
  }

  return {
    applications,
    stores,
    loading,
    submitting,
    error,
    latestApplication,
    hasPending,
    hasStore,
    load,
    submit,
    withdraw,
  };
});
