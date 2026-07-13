<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { storeToRefs } from 'pinia'
import { useI18n } from 'vue-i18n'
import { useDashboardStore } from '@/stores/dashboard'
import { useStoreApplicationStore } from '@/stores/storeApplication'
import { JFmt as F, statusLabel } from '@/utils/format'
import { ME as me, REVENUE as revenue } from '@/data/products'

const { t } = useI18n()
const store = useDashboardStore()
const g = store

const storeApplication = useStoreApplicationStore()
const { storeName: apiStoreName, hasStore } = storeToRefs(storeApplication)
onMounted(() => { if (!hasStore.value) storeApplication.load() })

// 店名以 /v1/stores/me 為準，尚未載入時暫以 mock 名稱墊檔
const storeName = computed(() => apiStoreName.value ?? me.storeName)

const catMax = computed(() => Math.max(...revenue.byCategory.map(c => c.value)) || 1)
const kpis = computed(() => [
  { key: 'rev', label: t('overview.kpiRevenue'), val: F.money(g.monthRevenue), ic: 'wallet', bg: 'var(--c-pink)', delta: g.monthDelta, up: g.monthDelta >= 0 },
  { key: 'sales', label: t('overview.kpiSales'), val: g.totalSales.toLocaleString('en-US'), ic: 'bag', bg: 'var(--c-yellow)', delta: 12, up: true },
  { key: 'payout', label: t('overview.kpiPayout'), val: F.money(g.pendingPayout), ic: 'dollar', bg: 'var(--c-lime)', delta: null, sub: t('overview.payoutDate') },
  { key: 'views', label: t('overview.kpiViews'), val: F.compact(g.totalViews), ic: 'eye', bg: 'var(--c-cyan)', delta: 8, up: true },
])
</script>

<template>
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
        <trend-chart :data="revenue.monthly" :height="232" />
      </div>

      <div class="card-pad">
        <div class="card-head"><h3>{{ t('overview.categorySplit') }}</h3></div>
        <div class="bars">
          <div v-for="c in revenue.byCategory" :key="c.label" class="bar-row">
            <span class="bl">{{ c.label }}</span>
            <span class="bar-track"><span class="bar-fill" :style="{ width: Math.max((c.value/catMax)*100, 2) + '%', background: c.color }"></span></span>
            <span class="bv">{{ F.money(c.value) }}</span>
          </div>
        </div>
        <div style="margin-top:22px; padding-top:18px; border-top:1px dashed var(--border); display:flex; align-items:center; justify-content:space-between;">
          <div>
            <div class="kpi-label" style="margin:0">{{ t('overview.convRate') }}</div>
            <div style="font-family:var(--oj-display); font-weight:700; font-size:24px;">{{ g.convRate }}%</div>
          </div>
          <div style="text-align:right;">
            <div class="kpi-label" style="margin:0">{{ t('overview.followers') }}</div>
            <div style="font-family:var(--oj-display); font-weight:700; font-size:24px;">{{ F.compact(me.followers) }}</div>
          </div>
        </div>
      </div>
    </div>

    <!-- top products + recent orders -->
    <div class="dash-grid cols-2">
      <div class="card-pad">
        <div class="card-head">
          <h3>{{ t('overview.topProducts') }}</h3>
          <button class="ch-link" @click="store.go('products')">{{ t('overview.viewAll') }} <app-icon name="arrowRight" :size="13" :stroke="2.6" /></button>
        </div>
        <div class="list-rows">
          <div v-for="(p,i) in g.topProducts" :key="p.id" class="lrow">
            <span class="rank" :class="'r' + (i < 3 ? i+1 : 'x')">{{ i+1 }}</span>
            <product-thumb :product="p" :glyph-size="20" :show-cat="false" hide-label />
            <div class="lr-body">
              <div class="lr-title">{{ p.title }}</div>
              <div class="lr-meta">{{ t('overview.salesViews', { sales: p.sales.toLocaleString('en-US'), views: F.compact(p.views) }) }}</div>
            </div>
            <div class="lr-right">
              <div class="lr-amount">{{ F.money(p.revenue) }}</div>
              <span class="pill" :class="p.status" style="margin-top:4px;"><span class="pdot"></span>{{ statusLabel(p.status) }}</span>
            </div>
          </div>
        </div>
      </div>

      <div class="card-pad">
        <div class="card-head">
          <h3>{{ t('overview.recentOrders') }}</h3>
          <button class="ch-link" @click="store.go('orders')">{{ t('overview.viewAll') }} <app-icon name="arrowRight" :size="13" :stroke="2.6" /></button>
        </div>
        <div class="list-rows">
          <div v-for="o in g.recentOrders" :key="o.id" class="lrow">
            <span class="avatar" :style="{ background: o.avatar, width:'40px', height:'40px', fontSize:'14px' }">{{ F.initials(o.buyer) }}</span>
            <div class="lr-body">
              <div class="lr-title">{{ o.buyer }}</div>
              <div class="lr-meta">{{ (store.product(o.productId) || {}).title }}</div>
            </div>
            <div class="lr-right">
              <div class="lr-amount" :style="o.status === 'refunded' ? 'color:var(--text-faint); text-decoration:line-through' : ''">{{ F.money(o.amount) }}</div>
              <div class="lr-time">{{ F.relTime(o.date) }}</div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
