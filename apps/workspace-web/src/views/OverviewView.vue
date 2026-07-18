<script setup lang="ts">
import { computed, onMounted, watch } from 'vue'
import { storeToRefs } from 'pinia'
import { useMessage } from 'naive-ui'
import { useI18n } from 'vue-i18n'
import { useDashboardStore } from '@/stores/dashboard'
import { useSellerDashboardStore } from '@/stores/sellerDashboard'
import { useStoreApplicationStore } from '@/stores/storeApplication'
import { JFmt as F } from '@/utils/format'
import { formatOrderAmount, formatOrderTime, orderStatusMeta } from '@/utils/order'
import { OrderStatus } from '@/api/order-service'
import { CatalogStatus, type CatalogSummaryDto } from '@/api/catalog-service'

const { t } = useI18n()
const message = useMessage()
const nav = useDashboardStore()          // 僅用於 go() 路由跳轉
const dash = useSellerDashboardStore()
const storeApp = useStoreApplicationStore()

const {
  loading, error, currency,
  totalSales, totalViews, convRate, followerCount,
  topProducts, categorySplit, recentOrders,
  monthRevenueMinor, monthDelta, salesDelta, pendingPayoutMinor, monthlyTrend,
} = storeToRefs(dash)

// 賣家本人商店 ID（開店資料由 router guard 先載入）；變動時重新載入儀表板。
const myStoreId = computed(() => storeApp.primaryStore?.id ?? null)
watch(myStoreId, (id) => { if (id) dash.load(id) })
onMounted(() => { if (myStoreId.value) dash.load(myStoreId.value) })

// 載入錯誤以彈出 message 呈現，畫面維持空狀態
watch(error, (msg) => { if (msg) message.error(msg) })

const catMax = computed(() => Math.max(...categorySplit.value.map(c => c.value), 1))

interface Kpi { key: string; label: string; val: string; ic: string; bg: string; delta: number | null; up?: boolean; sub?: string }
const kpis = computed<Kpi[]>(() => [
  { key: 'rev', label: t('overview.kpiRevenue'), val: formatOrderAmount(monthRevenueMinor.value, currency.value), ic: 'wallet', bg: 'var(--c-pink)', delta: monthDelta.value, up: monthDelta.value >= 0 },
  { key: 'sales', label: t('overview.kpiSales'), val: totalSales.value.toLocaleString('en-US'), ic: 'bag', bg: 'var(--c-yellow)', delta: salesDelta.value, up: salesDelta.value >= 0 },
  { key: 'payout', label: t('overview.kpiPayout'), val: formatOrderAmount(pendingPayoutMinor.value, currency.value), ic: 'dollar', bg: 'var(--c-lime)', delta: null, sub: t('overview.payoutEstimate') },
  { key: 'views', label: t('overview.kpiViews'), val: F.compact(totalViews.value), ic: 'eye', bg: 'var(--c-cyan)', delta: null, sub: t('overview.convRate') + ' ' + convRate.value + '%' },
])

// 商品狀態 → neo-brutalism pill 樣式 + i18n 標籤
const STATUS_PILL: Record<CatalogStatus, { cls: string; key: string }> = {
  [CatalogStatus.Published]: { cls: 'live', key: 'catalogStatus.published' },
  [CatalogStatus.Draft]: { cls: 'draft', key: 'catalogStatus.draft' },
  [CatalogStatus.Archived]: { cls: 'off', key: 'catalogStatus.archived' },
  [CatalogStatus.Suspended]: { cls: 'off', key: 'catalogStatus.suspended' },
}
function statusPill(s?: CatalogStatus) {
  return (s != null && STATUS_PILL[s]) || { cls: 'draft', key: 'catalogStatus.unknown' }
}

/** 熱門商品縮圖所需的最小欄位（ProductThumb 相容）。 */
function thumbOf(p: CatalogSummaryDto) {
  return { hue: p.coverHue ?? 256, cat: '', formats: [] as string[], totalSize: '—' }
}

/** 訂單摘要不含買家資訊，列表以訂單編號為主標。 */
function orderLabel(orderNumber?: string | null, id?: string) {
  return orderNumber || (id ? id.slice(0, 8) : '—')
}
</script>

<template>
  <n-spin :show="loading">
  <div :data-screen-label="t('route.overview')">

    <!-- KPI -->
    <div class="kpi-grid">
      <div v-for="k in kpis" :key="k.key" class="kpi">
        <div class="kpi-top">
          <div class="kpi-ic" :style="{ background: k.bg }"><app-icon :name="k.ic" :size="20" /></div>
          <span v-if="k.delta !== null" class="kpi-delta" :class="k.up ? 'up' : 'down'">
            <app-icon :name="k.up ? 'trend' : 'chevronD'" :size="13" :stroke="2.2" /> {{ Math.abs(k.delta) }}%
          </span>
          <span v-else class="kpi-delta">
            <app-icon name="clock" :size="13" /> {{ k.sub }}
          </span>
        </div>
        <div class="kpi-val">{{ k.val }}</div>
        <div class="kpi-label">{{ k.label }}</div>
      </div>
    </div>

    <!-- chart + category split -->
    <div class="dash-grid cols-2" style="margin-bottom:22px;">
      <div class="card-pad">
        <div class="card-head">
          <div>
            <h3>{{ t('overview.revenueTrend') }}</h3>
            <div class="ch-sub">{{ t('overview.revenueTrendSub') }}</div>
          </div>
          <div class="chart-legend">
            <span class="lg"><i style="background:var(--c-pink)"></i>{{ t('overview.netRevenue') }}</span>
          </div>
        </div>
        <trend-chart :data="monthlyTrend" :height="232" />
      </div>

      <div class="card-pad">
        <div class="card-head"><h3>{{ t('overview.categorySplit') }}</h3></div>
        <div class="bars">
          <div v-if="!categorySplit.length" class="dash-empty">{{ t('overview.emptyProducts') }}</div>
          <div v-for="c in categorySplit" :key="c.label" class="bar-row">
            <span class="bl">{{ c.label }}</span>
            <span class="bar-track"><span class="bar-fill" :style="{ width: Math.max((c.value/catMax)*100, 2) + '%', background: c.color }"></span></span>
            <span class="bv">{{ formatOrderAmount(c.value, currency) }}</span>
          </div>
        </div>
        <div style="margin-top:22px; padding-top:18px; border-top:1px dashed var(--border); display:flex; align-items:center; justify-content:space-between;">
          <div>
            <div class="kpi-label" style="margin:0">{{ t('overview.convRate') }}</div>
            <div style="font-family:var(--oj-display); font-weight:700; font-size:24px;">{{ convRate }}%</div>
          </div>
          <div style="text-align:right;">
            <div class="kpi-label" style="margin:0">{{ t('overview.followers') }}</div>
            <div style="font-family:var(--oj-display); font-weight:700; font-size:24px;">{{ F.compact(followerCount) }}</div>
          </div>
        </div>
      </div>
    </div>

    <!-- top products + recent orders -->
    <div class="dash-grid cols-2">
      <div class="card-pad">
        <div class="card-head">
          <h3>{{ t('overview.topProducts') }}</h3>
          <button class="ch-link" @click="nav.go('products')">{{ t('overview.viewAll') }} <app-icon name="arrowRight" :size="13" :stroke="2.6" /></button>
        </div>
        <div class="list-rows">
          <div v-if="!topProducts.length" class="dash-empty">{{ t('overview.emptyProducts') }}</div>
          <div v-for="(p,i) in topProducts" :key="p.id" class="lrow">
            <span class="rank" :class="'r' + (i < 3 ? i+1 : 'x')">{{ i+1 }}</span>
            <product-thumb :product="thumbOf(p)" :image="p.thumbnailUrl || ''" :glyph-size="20" :show-cat="false" hide-label />
            <div class="lr-body">
              <div class="lr-title">{{ p.name }}</div>
              <div class="lr-meta">{{ t('overview.salesViews', { sales: (p.salesCount ?? 0).toLocaleString('en-US'), views: F.compact(p.viewCount ?? 0) }) }}</div>
            </div>
            <div class="lr-right">
              <div class="lr-amount">{{ formatOrderAmount(p.revenueMinor, currency) }}</div>
              <span class="pill" :class="statusPill(p.status).cls" style="margin-top:4px;"><span class="pdot"></span>{{ t(statusPill(p.status).key) }}</span>
            </div>
          </div>
        </div>
      </div>

      <div class="card-pad">
        <div class="card-head">
          <h3>{{ t('overview.recentOrders') }}</h3>
          <button class="ch-link" @click="nav.go('orders')">{{ t('overview.viewAll') }} <app-icon name="arrowRight" :size="13" :stroke="2.6" /></button>
        </div>
        <div class="list-rows">
          <div v-if="!recentOrders.length" class="dash-empty">{{ t('overview.emptyOrders') }}</div>
          <div v-for="o in recentOrders" :key="o.id" class="lrow">
            <span class="kpi-ic" style="background:var(--c-violet); width:40px; height:40px; flex:none;"><app-icon name="receipt" :size="18" /></span>
            <div class="lr-body">
              <div class="lr-title" style="font-family:var(--oj-mono); font-size:13px;">{{ orderLabel(o.orderNumber, o.id) }}</div>
              <div class="lr-meta">{{ t(orderStatusMeta(o.status).labelKey) }}</div>
            </div>
            <div class="lr-right">
              <div class="lr-amount" :style="o.status === OrderStatus.Refunded ? 'color:var(--text-faint); text-decoration:line-through' : ''">{{ formatOrderAmount(o.totalAmount, o.currency) }}</div>
              <div class="lr-time">{{ formatOrderTime(o.createdAt) }}</div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
  </n-spin>
</template>

<style scoped>
.dash-empty {
  padding: 32px 8px;
  text-align: center;
  font-size: 13px;
  color: var(--text-faint);
}
</style>
