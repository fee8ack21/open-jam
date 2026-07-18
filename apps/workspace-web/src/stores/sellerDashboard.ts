import { ref, computed } from 'vue';
import { defineStore } from 'pinia';
import { catalogApi, orderApi, storeApi } from '@/api';
import i18n from '@/i18n';
import { OrderStatus, type OrderSummaryDto } from '@/api/order-service';
import type { CatalogSummaryDto, CatalogCategoryDto } from '@/api/catalog-service';
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

/** 近幾個月營收趨勢顯示的月份數。 */
const TREND_MONTHS = 8;
/** 平台抽成估算參數（對齊 PaymentService PlatformFeePercent / PlatformFeeFixed），
 * 僅供「抽成快照上線前的舊訂單」退補估算；新訂單一律取後端快照值。 */
const PLATFORM_FEE_RATE = 0.1;
/** 平台抽成固定費（最低貨幣單位，1200 = NT$12/筆）。 */
const PLATFORM_FEE_FIXED_MINOR = 1200;

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

/** 熱門商品（商品摘要 + 換算後的累計營收，最低貨幣單位）。 */
export interface TopProduct extends CatalogSummaryDto {
  revenueMinor: number;
}

/** 該筆訂單歸屬的年月鍵（YYYY-MM），優先取履約完成時間。 */
function monthKey(o: OrderSummaryDto): string {
  const iso = o.completedAt || o.createdAt;
  return iso ? iso.slice(0, 7) : '';
}

/**
 * 賣家儀表板統計 store：由 CatalogService（自家商品）、OrderService（本店訂單）與
 * StoreService（追蹤者數）即時彙總 KPI、營收趨勢、分類佔比、熱門商品與近期訂單。
 * 平台尚無專用分析端點，故於前端聚合現有資源；商店 ID 由呼叫端以 load(storeId) 指定。
 *
 * 營收基準：KPI（本月營收 / 預估撥款）取自「已完成訂單」金額（權威、含時間維度）；
 * 分類佔比與熱門商品取自「商品 price × 累計銷量」的終身營收估算（無需逐筆訂單明細）。
 */
export const useSellerDashboardStore = defineStore('sellerDashboard', () => {
  const loading = ref(false);
  const error = ref<string | null>(null);

  const products = ref<CatalogSummaryDto[]>([]);
  const completedOrders = ref<OrderSummaryDto[]>([]);
  const recentOrders = ref<OrderSummaryDto[]>([]);
  const categories = ref<CatalogCategoryDto[]>([]);
  const followerCount = ref(0);

  /** 本店主要幣別（取第一筆已完成訂單，退回第一項商品，再退回 TWD）。 */
  const currency = computed(() =>
    completedOrders.value[0]?.currency
    ?? products.value.find((p) => p.currency)?.currency
    ?? 'TWD');

  // ── 商品面 KPI（即時累計值）─────────────────────────────
  const totalSales = computed(() => products.value.reduce((s, p) => s + (p.salesCount ?? 0), 0));
  const totalViews = computed(() => products.value.reduce((s, p) => s + (p.viewCount ?? 0), 0));
  const convRate = computed(() => {
    const v = totalViews.value;
    return v ? ((totalSales.value / v) * 100).toFixed(1) : '0';
  });

  /** 單一商品的終身營收估算（price × 累計銷量），換算為最低貨幣單位。 */
  function productRevenueMinor(p: CatalogSummaryDto): number {
    return toMinorAmount((p.price ?? 0) * (p.salesCount ?? 0), p.currency ?? currency.value);
  }

  /** 熱門商品：依終身營收估算排序取前 5。 */
  const topProducts = computed<TopProduct[]>(() =>
    products.value
      .map((p) => ({ ...p, revenueMinor: productRevenueMinor(p) }))
      .sort((a, b) => b.revenueMinor - a.revenueMinor)
      .slice(0, 5));

  /** 分類佔比：以商品終身營收估算依分類彙總（未分類歸「其他」）。 */
  const categorySplit = computed<CategorySplit[]>(() => {
    const nameOf = new Map(categories.value.map((c) => [c.id, c.name ?? '']));
    const other = i18n.global.t('overview.categoryOther');
    const totals = new Map<string, number>();
    for (const p of products.value) {
      const label = (p.categoryId && nameOf.get(p.categoryId)) || other;
      totals.set(label, (totals.get(label) ?? 0) + productRevenueMinor(p));
    }
    return [...totals.entries()]
      .filter(([, v]) => v > 0)
      .sort((a, b) => b[1] - a[1])
      .map(([label, value], i) => ({ label, value, color: SPLIT_COLORS[i % SPLIT_COLORS.length] }));
  });

  // ── 訂單面 KPI（含時間維度）─────────────────────────────
  /** 指定年月偏移（0 = 本月，-1 = 上月）的 YYYY-MM 鍵。 */
  function offsetMonthKey(offset: number): string {
    const now = new Date();
    const d = new Date(now.getFullYear(), now.getMonth() + offset, 1);
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`;
  }

  /** 某年月鍵下已完成訂單的金額總和（最低貨幣單位）。 */
  function revenueInMonth(key: string): number {
    return completedOrders.value
      .filter((o) => monthKey(o) === key)
      .reduce((s, o) => s + (o.totalAmount ?? 0), 0);
  }
  /** 某年月鍵下已完成訂單的筆數。 */
  function salesInMonth(key: string): number {
    return completedOrders.value.filter((o) => monthKey(o) === key).length;
  }

  const monthRevenueMinor = computed(() => revenueInMonth(offsetMonthKey(0)));
  const prevMonthRevenueMinor = computed(() => revenueInMonth(offsetMonthKey(-1)));
  const monthDelta = computed(() => {
    const prev = prevMonthRevenueMinor.value;
    return prev ? Math.round(((monthRevenueMinor.value - prev) / prev) * 100) : 0;
  });

  const monthSales = computed(() => salesInMonth(offsetMonthKey(0)));
  const prevMonthSales = computed(() => salesInMonth(offsetMonthKey(-1)));
  const salesDelta = computed(() => {
    const prev = prevMonthSales.value;
    return prev ? Math.round(((monthSales.value - prev) / prev) * 100) : 0;
  });

  /** 該筆訂單的平台抽成（最低貨幣單位）：優先取付款成功時的後端快照，快照上線前的舊訂單以現行費率（百分比＋固定費）估算。 */
  function orderFeeMinor(o: OrderSummaryDto): number {
    if (o.platformFeeAmount) return o.platformFeeAmount;
    const total = o.totalAmount ?? 0;
    if (!total) return 0;
    return Math.min(total, Math.round(total * PLATFORM_FEE_RATE) + PLATFORM_FEE_FIXED_MINOR);
  }

  /** 本期預估可撥款金額 = 本月已完成訂單逐筆（總額 − 平台抽成）加總（非 Stripe 即時餘額）。 */
  const pendingPayoutMinor = computed(() =>
    completedOrders.value
      .filter((o) => monthKey(o) === offsetMonthKey(0))
      .reduce((s, o) => s + Math.max(0, (o.totalAmount ?? 0) - orderFeeMinor(o)), 0));

  /** 近 8 個月營收趨勢（面額，供折線圖）。 */
  const monthlyTrend = computed<TrendPoint[]>(() => {
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

  /** 載入儀表板所需的所有資料（並行請求；追蹤者數失敗僅降級為 0）。 */
  async function load(storeId: string) {
    if (!storeId) return;
    loading.value = true;
    error.value = null;
    try {
      const [prodRes, completedRes, recentRes, catRes] = await Promise.all([
        catalogApi.catalogs.listMine({ StoreId: storeId, Offset: 0, Limit: 100 }),
        orderApi.orders.listByStore(storeId, { Status: OrderStatus.Completed, Offset: 0, Limit: 100 }),
        orderApi.orders.listByStore(storeId, { Offset: 0, Limit: 6 }),
        catalogApi.catalogCategories.list(),
      ]);
      products.value = prodRes.data.items ?? [];
      completedOrders.value = completedRes.data.items ?? [];
      recentOrders.value = recentRes.data.items ?? [];
      categories.value = catRes.data ?? [];
      // 追蹤者數為次要指標，查詢失敗不阻斷主要儀表板。
      try {
        const followersRes = await storeApi.storeFollowers.getFollowers(storeId, { Offset: 0, Limit: 1 });
        followerCount.value = followersRes.data.totalCount ?? 0;
      } catch {
        followerCount.value = 0;
      }
    } catch (err) {
      error.value = messageOf(err, i18n.global.t('storeError.loadDashboardFailed'));
      products.value = [];
      completedOrders.value = [];
      recentOrders.value = [];
    } finally {
      loading.value = false;
    }
  }

  return {
    loading,
    error,
    products,
    recentOrders,
    followerCount,
    currency,
    totalSales,
    totalViews,
    convRate,
    topProducts,
    categorySplit,
    monthRevenueMinor,
    monthDelta,
    monthSales,
    salesDelta,
    pendingPayoutMinor,
    monthlyTrend,
    load,
  };
});
