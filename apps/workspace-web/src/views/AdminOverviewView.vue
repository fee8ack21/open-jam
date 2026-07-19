<script setup lang="ts">
import { computed, onMounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { storeToRefs } from 'pinia'
import { useMessage } from 'naive-ui'
import { useAdminStatsStore } from '@/stores/adminStats'
import { useStoreReviewStore } from '@/stores/storeReview'
import { useDashboardStore } from '@/stores/dashboard'
import { JFmt as F } from '@/utils/format'
import { formatOrderAmount } from '@/utils/order'

const { t } = useI18n()
const message = useMessage()
const stats = useAdminStatsStore()
const review = useStoreReviewStore()
const dashboard = useDashboardStore()
const { monthly, byCategory, error } = storeToRefs(stats)

// 載入錯誤以彈出 message 呈現，畫面維持空狀態
watch(error, (msg) => { if (msg) message.error(msg) })

const catMax = computed(() => Math.max(...byCategory.value.map((c) => c.value)) || 1)

interface Kpi { key: string; label: string; val: string; ic: string; bg: string; delta: number | null; up?: boolean; sub?: string }
const kpis = computed<Kpi[]>(() => [
  {
    key: 'stores',
    label: t('adminOverview.kpiStores'),
    val: stats.totalStores.toLocaleString('en-US'),
    ic: 'home',
    bg: 'var(--c-pink)',
    sub: t('adminOverview.kpiStoresSub', { count: stats.activeStores }),
    delta: null,
  },
  {
    key: 'products',
    label: t('adminOverview.kpiProducts'),
    val: stats.totalProducts.toLocaleString('en-US'),
    ic: 'box',
    bg: 'var(--c-yellow)',
    sub: t('adminOverview.kpiProductsSub', { count: stats.newProductsThisMonth }),
    delta: null,
  },
  {
    key: 'revenue',
    label: t('adminOverview.kpiRevenue'),
    val: formatOrderAmount(stats.monthRevenueMinor, stats.currency),
    ic: 'wallet',
    bg: 'var(--c-lime)',
    delta: stats.monthDelta,
    up: stats.monthDelta >= 0,
  },
  {
    key: 'orders',
    label: t('adminOverview.kpiOrders'),
    val: stats.monthOrders.toLocaleString('en-US'),
    ic: 'receipt',
    bg: 'var(--c-cyan)',
    delta: stats.ordersDelta,
    up: stats.ordersDelta >= 0,
  },
])

onMounted(() => {
  stats.load()
  review.load()
})
</script>

<template>
  <n-spin :show="stats.loading">
  <div :data-screen-label="t('route.adminOverview')">

    <!-- KPI -->
    <div class="kpi-grid">
      <div v-for="k in kpis" :key="k.key" class="kpi">
        <div class="kpi-top">
          <div class="kpi-ic" :style="{ background: k.bg }"><app-icon :name="k.ic" :size="20" /></div>
          <span v-if="k.delta !== null" class="kpi-delta" :class="k.up ? 'up' : 'down'">
            <app-icon :name="k.up ? 'trend' : 'chevronD'" :size="13" :stroke="2.2" /> {{ Math.abs(k.delta) }}%
          </span>
          <span v-else class="kpi-delta">
            {{ k.sub }}
          </span>
        </div>
        <div class="kpi-val">{{ k.val }}</div>
        <div class="kpi-label">{{ k.label }}</div>
      </div>
    </div>

    <!-- 交易趨勢 + 分類佔比 -->
    <div class="dash-grid cols-2" style="margin-bottom:22px;">
      <div class="card-pad">
        <div class="card-head">
          <div>
            <h3>{{ t('adminOverview.revenueTrend') }}</h3>
            <div class="ch-sub">{{ t('adminOverview.revenueTrendSub') }}</div>
          </div>
          <div class="chart-legend">
            <span class="lg"><i style="background:var(--c-pink)"></i>{{ t('adminOverview.transactionAmount') }}</span>
          </div>
        </div>
        <trend-chart :data="monthly" :height="232" />
      </div>

      <div class="card-pad">
        <div class="card-head"><h3>{{ t('adminOverview.categorySplit') }}</h3></div>
        <div class="bars">
          <div v-for="c in byCategory" :key="c.label" class="bar-row">
            <span class="bl">{{ c.label }}</span>
            <span class="bar-track"><span class="bar-fill" :style="{ width: Math.max((c.value/catMax)*100, 2) + '%', background: c.color }"></span></span>
            <span class="bv">{{ formatOrderAmount(c.value, stats.currency) }}</span>
          </div>
        </div>
        <div style="margin-top:22px; padding-top:18px; border-top:1px dashed var(--border); display:flex; align-items:center; justify-content:space-between;">
          <div>
            <div class="kpi-label" style="margin:0">{{ t('adminOverview.newStores') }}</div>
            <div style="font-family:var(--oj-display); font-weight:700; font-size:24px;">{{ stats.newStoresThisMonth }}</div>
          </div>
          <div style="text-align:right;">
            <div class="kpi-label" style="margin:0">{{ t('adminOverview.activeStores') }}</div>
            <div style="font-family:var(--oj-display); font-weight:700; font-size:24px;">{{ stats.activeStores }}</div>
          </div>
        </div>
      </div>
    </div>

    <!-- 待審核開店申請 -->
    <div class="card-pad">
      <div class="card-head">
        <h3>{{ t('adminOverview.pendingTitle') }}</h3>
        <button class="ch-link" @click="dashboard.go('review')">{{ t('adminOverview.goReview') }} <app-icon name="arrowRight" :size="13" :stroke="2.6" /></button>
      </div>
      <div v-if="review.pendingCount" class="list-rows">
        <div v-for="a in review.items" :key="a.id" class="lrow">
          <span class="rank rx">{{ t('review.rankGlyph') }}</span>
          <div class="lr-body">
            <div class="lr-title">{{ a.storeName }}</div>
            <div class="lr-meta">{{ a.storeSlug }}.openjam.co · {{ a.email }}</div>
          </div>
          <div class="lr-right">
            <div class="lr-time">{{ F.relTime(a.createdAt ?? '') }}</div>
          </div>
        </div>
      </div>
      <div v-else class="empty-box">
        <div class="eb-ic"><app-icon name="shield" :size="30" /></div>
        <div class="eb-t">{{ t('adminOverview.pendingEmpty') }}</div>
        <div class="eb-hand">all clear, nice work!</div>
      </div>
    </div>
  </div>
  </n-spin>
</template>
