import { ref, computed } from 'vue';
import { defineStore } from 'pinia';

/** 折線圖資料點（與 TrendChart 一致）。 */
export interface TrendPoint {
  label: string;
  value: number;
}

/** 分類交易佔比資料點。 */
export interface CategorySplit {
  label: string;
  value: number;
  color: string;
}

// TODO(mock): 平台層級統計尚無彙總端點（需跨 Store / Catalog / Order 服務聚合）。
// 以下為展示用假資料，端點補上後移除 mock 區塊並啟用真實 API 呼叫。
const MOCK_MONTHLY: TrendPoint[] = [
  { label: '11月', value: 182000 },
  { label: '12月', value: 246000 },
  { label: '1月', value: 219000 },
  { label: '2月', value: 268000 },
  { label: '3月', value: 312000 },
  { label: '4月', value: 358000 },
  { label: '5月', value: 401000 },
  { label: '6月', value: 437500 },
];
const MOCK_BY_CATEGORY: CategorySplit[] = [
  { label: '音樂與音效', value: 168000, color: 'var(--c-violet)' },
  { label: '插畫與素材', value: 142500, color: 'var(--c-pink)' },
  { label: '電子書 / 文件', value: 84000, color: 'var(--c-cyan)' },
  { label: '其他', value: 43000, color: 'var(--c-orange)' },
];

/**
 * 平台管理員儀表板統計 store：彙總全平台開店數、上架商品數、交易金額等。
 * 僅 Admin 使用。
 *
 * NOTE(mock): 平台統計彙總端點尚未接上，目前以假資料呈現；
 * 串接時請移除 mock 區塊，並啟用 load() 中註解掉的真實 API 呼叫。
 */
export const useAdminStatsStore = defineStore('adminStats', () => {
  const loading = ref(false);
  const error = ref<string | null>(null);

  /** 平台累計開店數。 */
  const totalStores = ref(128);
  /** 營運中商店數。 */
  const activeStores = ref(112);
  /** 本月新開店數。 */
  const newStoresThisMonth = ref(9);
  /** 平台累計上架商品數。 */
  const totalProducts = ref(1843);
  /** 本月新上架商品數。 */
  const newProductsThisMonth = ref(146);
  /** 本月平台交易金額（成交總額，單位：元）。 */
  const monthRevenue = ref(437500);
  /** 上月平台交易金額，用於計算成長率。 */
  const prevMonthRevenue = ref(401000);
  /** 本月平台成交筆數。 */
  const monthOrders = ref(2317);
  /** 近 8 個月平台交易金額趨勢。 */
  const monthly = ref<TrendPoint[]>([...MOCK_MONTHLY]);
  /** 本月交易金額分類佔比。 */
  const byCategory = ref<CategorySplit[]>([...MOCK_BY_CATEGORY]);

  /** 本月交易金額較上月成長百分比（四捨五入整數）。 */
  const monthDelta = computed(() => {
    const prev = prevMonthRevenue.value;
    return prev ? Math.round(((monthRevenue.value - prev) / prev) * 100) : 0;
  });

  /** 載入平台統計。 */
  async function load() {
    // TODO(mock): 改回真實呼叫——
    // loading.value = true;
    // error.value = null;
    // try {
    //   const res = await reportApi.platform.summary();
    //   const d = res.data;
    //   totalStores.value = d.totalStores ?? 0;
    //   activeStores.value = d.activeStores ?? 0;
    //   newStoresThisMonth.value = d.newStoresThisMonth ?? 0;
    //   totalProducts.value = d.totalProducts ?? 0;
    //   newProductsThisMonth.value = d.newProductsThisMonth ?? 0;
    //   monthRevenue.value = d.monthRevenue ?? 0;
    //   prevMonthRevenue.value = d.prevMonthRevenue ?? 0;
    //   monthOrders.value = d.monthOrders ?? 0;
    //   monthly.value = d.monthly ?? [];
    //   byCategory.value = d.byCategory ?? [];
    // } catch (err) {
    //   error.value = '載入平台統計失敗。';
    // } finally {
    //   loading.value = false;
    // }
    loading.value = false;
  }

  return {
    loading,
    error,
    totalStores,
    activeStores,
    newStoresThisMonth,
    totalProducts,
    newProductsThisMonth,
    monthRevenue,
    prevMonthRevenue,
    monthOrders,
    monthly,
    byCategory,
    monthDelta,
    load,
  };
});
