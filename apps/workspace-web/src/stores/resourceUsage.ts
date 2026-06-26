import { ref, computed } from 'vue';
import { defineStore } from 'pinia';
import { storageApi } from '@/api';

/** 各創作者的儲存用量列。 */
export interface CreatorUsageRow {
  creatorId: string;
  /** 顯示名稱（StorageService 僅知創作者 ID，故以短碼呈現）。 */
  label: string;
  /** 檔案數量。 */
  fileCount: number;
  /** 已使用位元組數。 */
  bytes: number;
}

function messageOf(err: unknown, fallback = '操作失敗，請稍後再試。'): string {
  if (typeof err === 'string') return err;
  const problem = (err as { error?: { detail?: string; title?: string } })?.error
    ?? (err as { detail?: string; title?: string } | null | undefined);
  return problem?.detail ?? problem?.title ?? fallback;
}

const GiB = 1024 ** 3;

/**
 * 平台管理員的資源用量 store：彙總全平台儲存空間。
 * 串接 StorageService `GET /v1/files/usage/summary`（Admin）。僅 Admin 使用。
 *
 * 注意：下載流量（bandwidth）與歷史趨勢（trend）需另建指標管線，StorageService
 * 目前無從統計，故不顯示假資料（bandwidthTracked / trend 反映此狀態）。
 * quotaBytes 為平台容量上限，屬營運設定值（非 StorageService 統計），暫以常數呈現。
 */
export const useResourceUsageStore = defineStore('resourceUsage', () => {
  const loading = ref(false);
  const error = ref<string | null>(null);

  /** 已使用儲存空間（位元組）。 */
  const usedBytes = ref(0);
  /** 平台儲存容量上限（位元組）——營運設定值，非統計。 */
  const quotaBytes = ref(100 * GiB);
  /** 檔案總數。 */
  const fileCount = ref(0);
  /** 展示型（公開）資產用量（位元組）。 */
  const publicBytes = ref(0);
  /** 版本可下載（私有）資產用量（位元組）。 */
  const privateBytes = ref(0);
  /** 待清理的孤兒檔案數量。 */
  const orphanFileCount = ref(0);
  /** 孤兒檔案占用空間（位元組）。 */
  const orphanBytes = ref(0);
  /** 各創作者用量明細（Top）。 */
  const byCreator = ref<CreatorUsageRow[]>([]);

  /** 下載流量是否已被追蹤（StorageService 目前未追蹤）。 */
  const bandwidthTracked = ref(false);

  /** 已用容量百分比（0–100，四捨五入整數）。 */
  const usedPercent = computed(() => {
    const q = quotaBytes.value;
    return q ? Math.round((usedBytes.value / q) * 100) : 0;
  });

  /** 載入平台資源用量。 */
  async function load() {
    loading.value = true;
    error.value = null;
    try {
      const res = await storageApi.files.getPlatformUsage();
      const d = res.data;
      usedBytes.value = d.usedBytes ?? 0;
      fileCount.value = d.fileCount ?? 0;
      publicBytes.value = d.publicBytes ?? 0;
      privateBytes.value = d.privateBytes ?? 0;
      orphanFileCount.value = d.orphanFileCount ?? 0;
      orphanBytes.value = d.orphanBytes ?? 0;
      byCreator.value = (d.byCreator ?? []).map((c) => ({
        creatorId: c.creatorId ?? '',
        label: '創作者 ' + (c.creatorId ?? '').slice(0, 8),
        fileCount: c.fileCount ?? 0,
        bytes: c.bytes ?? 0,
      }));
    } catch (err) {
      error.value = messageOf(err, '載入資源用量失敗。');
    } finally {
      loading.value = false;
    }
  }

  return {
    loading,
    error,
    usedBytes,
    quotaBytes,
    fileCount,
    publicBytes,
    privateBytes,
    orphanFileCount,
    orphanBytes,
    byCreator,
    bandwidthTracked,
    usedPercent,
    load,
  };
});
