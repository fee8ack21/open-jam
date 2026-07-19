import { ref, computed } from 'vue';
import { defineStore } from 'pinia';
import { catalogApi, orderApi, storeApi } from '@/api';
import i18n from '@/i18n';
import { OrderStatus, type OrderSummaryDto } from '@/api/order-service';
import { CatalogSort, type CatalogSummaryDto, type CatalogCategoryDto } from '@/api/catalog-service';
import { StoreStatus } from '@/api/store-service';
import { toMinorAmount, toMajorAmount } from '@/utils/order';

/** 由後端 RFC 9457 Problem Details 取出可顯示的錯誤訊息。 */
function messageOf(err: unknown, fallback: string): string {
  const problem = (err as { error?: { detail?: string; title?: string } } | null)?.error;
  return problem?.detail ?? problem?.title ?? fallback;
}

/** 分類佔比色盤（依 CSS token 循環套用）。 */
const SPLIT_COLORS = [
  'var(--c-violet)', 'var(--c-pink)', 'var(--c-cyan)',
  'var(--c-orange)', 'var(--c-lime)', 'var(--c-yellow)',
];

/** 近幾個月交易趨勢顯示的月份數。 */
const TREND_MONTHS = 8;
/** 分頁列表單頁筆數（後端上限 100）。 */
const PAGE_LIMIT = 100;
/** 已完成訂單最多抓取頁數（防平台訂單量過大時無止境翻頁；超出者不計入趨勢）。 */
const MAX_ORDER_PAGES = 10;

/** 折線圖資料點（與 TrendChart 一致）。 */
export interface TrendPoint {
  label: string;
  value: number;
}

/** 分類交易佔比資料點（value 為最低貨幣單位）。 */
export interface CategorySplit {
  label: string;
  value: number;
  color: string;
}

/** 該筆訂單歸屬的年月鍵（YYYY-MM），優先取履約完成時間。 */
function monthKey(o: OrderSummaryDto): string {
  const iso = o.completedAt || o.createdAt;
  return iso ? iso.slice(0, 7) : '';
}

/** 指定年月偏移（0 = 本月，-1 = 上月）的 YYYY-MM 鍵。 */
function offsetMonthKey(offset: number): string {
  const now = new Date();
  const d = new Date(now.getFullYear(), now.getMonth() + offset, 1);
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`;
}

/**
 * 平台管理員儀表板統計 store：由 StoreService（全平台商店）、CatalogService（上架商品）
 * 與 OrderService（全平台訂單，Admin 視角）即時彙總開店數、商品數、交易金額趨勢與分類佔比。
 * 平台尚無專用彙總端點，故比照 [[sellerDashboard]] 於前端聚合現有 Admin 列表資源。僅 Admin 使用。
 *
 * 營收基準：KPI 與趨勢取自「已完成訂單」金額（權威、含時間維度）；
 * 分類佔比取自「銷量前 100 商品的 price × 累計銷量」終身營收估算（無逐筆訂單商品明細可用）。
 * 「本月新增」計數取列表首頁（CreatedAt / PublishedAt 倒序前 100 筆），單月增量破百時為下限值。
 */
export const useAdminStatsStore = defineStore('adminStats', () => {
  const loading = ref(false);
  const error = ref<string | null>(null);

  /** 平台累計開店數。 */
  const totalStores = ref(0);
  /** 營運中商店數。 */
  const activeStores = ref(0);
  /** 本月新開店數。 */
  const newStoresThisMonth = ref(0);
  /** 平台上架中商品數。 */
  const totalProducts = ref(0);
  /** 本月新上架商品數。 */
  const newProductsThisMonth = ref(0);

  /** 趨勢視窗內的已完成訂單（CreatedAt 倒序，最多 MAX_ORDER_PAGES 頁）。 */
  const completedOrders = ref<OrderSummaryDto[]>([]);
  /** 銷量前 100 商品（分類佔比估算基礎）。 */
  const popularProducts = ref<CatalogSummaryDto[]>([]);
  const categories = ref<CatalogCategoryDto[]>([]);

  /** 平台主要幣別（取第一筆已完成訂單，退回第一項商品，再退回 TWD）。 */
  const currency = computed(() =>
    completedOrders.value[0]?.currency
    ?? popularProducts.value.find((p) => p.currency)?.currency
    ?? 'TWD');

  /** 某年月鍵下已完成訂單的金額總和（最低貨幣單位）。 */
  function revenueInMonth(key: string): number {
    return completedOrders.value
      .filter((o) => monthKey(o) === key)
      .reduce((s, o) => s + (o.totalAmount ?? 0), 0);
  }
  /** 某年月鍵下已完成訂單的筆數。 */
  function ordersInMonth(key: string): number {
    return completedOrders.value.filter((o) => monthKey(o) === key).length;
  }

  /** 本月平台交易金額（最低貨幣單位）。 */
  const monthRevenueMinor = computed(() => revenueInMonth(offsetMonthKey(0)));
  const prevMonthRevenueMinor = computed(() => revenueInMonth(offsetMonthKey(-1)));
  /** 本月交易金額較上月成長百分比（四捨五入整數）。 */
  const monthDelta = computed(() => {
    const prev = prevMonthRevenueMinor.value;
    return prev ? Math.round(((monthRevenueMinor.value - prev) / prev) * 100) : 0;
  });

  /** 本月平台成交筆數。 */
  const monthOrders = computed(() => ordersInMonth(offsetMonthKey(0)));
  const prevMonthOrders = computed(() => ordersInMonth(offsetMonthKey(-1)));
  /** 本月成交筆數較上月成長百分比（四捨五入整數）。 */
  const ordersDelta = computed(() => {
    const prev = prevMonthOrders.value;
    return prev ? Math.round(((monthOrders.value - prev) / prev) * 100) : 0;
  });

  /** 近 8 個月平台交易金額趨勢（面額，供折線圖）。 */
  const monthly = computed<TrendPoint[]>(() => {
    const fmt = new Intl.DateTimeFormat(i18n.global.locale.value, { month: 'short' });
    const now = new Date();
    const points: TrendPoint[] = [];
    for (let i = TREND_MONTHS - 1; i >= 0; i--) {
      const d = new Date(now.getFullYear(), now.getMonth() - i, 1);
      const key = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`;
      points.push({ label: fmt.format(d), value: toMajorAmount(revenueInMonth(key), currency.value) });
    }
    return points;
  });

  /** 分類交易佔比：以銷量前 100 商品的終身營收估算依分類彙總（未分類歸「其他」）。 */
  const byCategory = computed<CategorySplit[]>(() => {
    const nameOf = new Map(categories.value.map((c) => [c.id, c.name ?? '']));
    const other = i18n.global.t('overview.categoryOther');
    const totals = new Map<string, number>();
    for (const p of popularProducts.value) {
      const label = (p.categoryId && nameOf.get(p.categoryId)) || other;
      const revenue = toMinorAmount((p.price ?? 0) * (p.salesCount ?? 0), p.currency ?? currency.value);
      totals.set(label, (totals.get(label) ?? 0) + revenue);
    }
    return [...totals.entries()]
      .filter(([, v]) => v > 0)
      .sort((a, b) => b[1] - a[1])
      .map(([label, value], i) => ({ label, value, color: SPLIT_COLORS[i % SPLIT_COLORS.length] }));
  });

  /** 逐頁抓取趨勢視窗內的已完成訂單（列表為 CreatedAt 倒序，翻到視窗外或頁數上限即停）。 */
  async function fetchCompletedOrders(): Promise<OrderSummaryDto[]> {
    const windowStart = offsetMonthKey(-(TREND_MONTHS - 1));
    const orders: OrderSummaryDto[] = [];
    for (let page = 0; page < MAX_ORDER_PAGES; page++) {
      const res = await orderApi.orders.list({
        Status: OrderStatus.Completed,
        Offset: page * PAGE_LIMIT,
        Limit: PAGE_LIMIT,
      });
      const items = res.data.items ?? [];
      orders.push(...items);
      const oldest = items[items.length - 1];
      if (items.length < PAGE_LIMIT) break;
      if (oldest && monthKey(oldest) < windowStart) break;
    }
    return orders;
  }

  /** 載入平台統計（並行請求）。 */
  async function load() {
    loading.value = true;
    error.value = null;
    try {
      const [storesRes, activeRes, newestRes, popularRes, catRes, orders] = await Promise.all([
        storeApi.stores.list({ Offset: 0, Limit: PAGE_LIMIT }),
        storeApi.stores.list({ Status: StoreStatus.Active, Offset: 0, Limit: 1 }),
        catalogApi.catalogs.list({ Sort: CatalogSort.Newest, Offset: 0, Limit: PAGE_LIMIT }),
        catalogApi.catalogs.list({ Sort: CatalogSort.Popular, Offset: 0, Limit: PAGE_LIMIT }),
        catalogApi.catalogCategories.list(),
        fetchCompletedOrders(),
      ]);

      const thisMonth = offsetMonthKey(0);
      totalStores.value = storesRes.data.totalCount ?? 0;
      newStoresThisMonth.value = (storesRes.data.items ?? [])
        .filter((s) => (s.createdAt ?? '').slice(0, 7) === thisMonth).length;
      activeStores.value = activeRes.data.totalCount ?? 0;

      totalProducts.value = newestRes.data.totalCount ?? 0;
      newProductsThisMonth.value = (newestRes.data.items ?? [])
        .filter((p) => (p.publishedAt ?? '').slice(0, 7) === thisMonth).length;

      popularProducts.value = popularRes.data.items ?? [];
      categories.value = catRes.data ?? [];
      completedOrders.value = orders;
    } catch (err) {
      error.value = messageOf(err, i18n.global.t('storeError.loadDashboardFailed'));
      totalStores.value = 0;
      activeStores.value = 0;
      newStoresThisMonth.value = 0;
      totalProducts.value = 0;
      newProductsThisMonth.value = 0;
      popularProducts.value = [];
      completedOrders.value = [];
    } finally {
      loading.value = false;
    }
  }

  return {
    loading,
    error,
    currency,
    totalStores,
    activeStores,
    newStoresThisMonth,
    totalProducts,
    newProductsThisMonth,
    monthRevenueMinor,
    monthDelta,
    monthOrders,
    ordersDelta,
    monthly,
    byCategory,
    load,
  };
});
