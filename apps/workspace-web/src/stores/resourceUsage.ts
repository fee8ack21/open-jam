import { ref, computed } from 'vue';
import { defineStore } from 'pinia';

/** 各商店的儲存用量列。 */
export interface StoreUsageRow {
  storeId: string;
  storeName: string;
  /** 檔案數量。 */
  fileCount: number;
  /** 已使用位元組數。 */
  bytes: number;
}

/** 單月儲存 / 流量趨勢資料點。 */
export interface UsageTrendPoint {
  label: string;
  /** 該月底累計已用位元組數。 */
  bytes: number;
}

// TODO(mock): StorageService 尚無「資源用量彙總」端點（需聚合 blob 物件大小 /
// 數量與下載流量）。以下為展示用假資料，端點補上後移除 mock 區塊，
// 並啟用 load() 中註解掉的真實 API 呼叫。
const GiB = 1024 ** 3;

const MOCK_STORE_USAGE: StoreUsageRow[] = [
  { storeId: '00000000-0000-0000-0000-000000000301', storeName: '小明的數位商店', fileCount: 312, bytes: 18.4 * GiB },
  { storeId: '00000000-0000-0000-0000-000000000302', storeName: 'Aria 手作工作室', fileCount: 268, bytes: 12.1 * GiB },
  { storeId: '00000000-0000-0000-0000-000000000303', storeName: '夜貓子音樂', fileCount: 504, bytes: 31.7 * GiB },
  { storeId: '00000000-0000-0000-0000-000000000304', storeName: '舊倉庫文創', fileCount: 96, bytes: 4.3 * GiB },
];
const MOCK_TREND: UsageTrendPoint[] = [
  { label: '1月', bytes: 28 * GiB },
  { label: '2月', bytes: 34 * GiB },
  { label: '3月', bytes: 41 * GiB },
  { label: '4月', bytes: 52 * GiB },
  { label: '5月', bytes: 60 * GiB },
  { label: '6月', bytes: 66.5 * GiB },
];

/**
 * 平台管理員的資源用量 store：彙總全平台儲存空間與下載流量。
 * 僅 Admin 使用。
 *
 * NOTE(mock): StorageService 尚無用量彙總端點，目前以假資料呈現；
 * 串接時請移除 mock 區塊，並啟用 load() 中註解掉的真實 API 呼叫。
 */
export const useResourceUsageStore = defineStore('resourceUsage', () => {
  const loading = ref(false);
  const error = ref<string | null>(null);

  /** 已使用儲存空間（位元組）。 */
  const usedBytes = ref(66.5 * GiB);
  /** 平台儲存容量上限（位元組）。 */
  const quotaBytes = ref(100 * GiB);
  /** 檔案總數。 */
  const fileCount = ref(1180);
  /** 展示型（公開）資產用量（位元組）。 */
  const publicBytes = ref(19.8 * GiB);
  /** 版本可下載（私有）資產用量（位元組）。 */
  const privateBytes = ref(46.7 * GiB);
  /** 本月下載流量（位元組）。 */
  const monthBandwidthBytes = ref(214 * GiB);
  /** 待清理的孤兒檔案數量（保留期過後將硬刪除）。 */
  const orphanFileCount = ref(23);
  /** 各商店用量明細。 */
  const byStore = ref<StoreUsageRow[]>([...MOCK_STORE_USAGE]);
  /** 近 6 個月累計儲存用量趨勢。 */
  const trend = ref<UsageTrendPoint[]>([...MOCK_TREND]);

  /** 已用容量百分比（0–100，四捨五入整數）。 */
  const usedPercent = computed(() => {
    const q = quotaBytes.value;
    return q ? Math.round((usedBytes.value / q) * 100) : 0;
  });

  /** 載入平台資源用量。 */
  async function load() {
    // TODO(mock): 改回真實呼叫——
    // loading.value = true;
    // error.value = null;
    // try {
    //   const res = await storageApi.usage.summary();
    //   const d = res.data;
    //   usedBytes.value = d.usedBytes ?? 0;
    //   quotaBytes.value = d.quotaBytes ?? 0;
    //   fileCount.value = d.fileCount ?? 0;
    //   publicBytes.value = d.publicBytes ?? 0;
    //   privateBytes.value = d.privateBytes ?? 0;
    //   monthBandwidthBytes.value = d.monthBandwidthBytes ?? 0;
    //   orphanFileCount.value = d.orphanFileCount ?? 0;
    //   byStore.value = d.byStore ?? [];
    //   trend.value = d.trend ?? [];
    // } catch (err) {
    //   error.value = '載入資源用量失敗。';
    // } finally {
    //   loading.value = false;
    // }
    loading.value = false;
  }

  return {
    loading,
    error,
    usedBytes,
    quotaBytes,
    fileCount,
    publicBytes,
    privateBytes,
    monthBandwidthBytes,
    orphanFileCount,
    byStore,
    trend,
    usedPercent,
    load,
  };
});
