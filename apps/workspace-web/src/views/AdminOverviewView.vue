<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { storeToRefs } from 'pinia'
import { useAdminStatsStore } from '@/stores/adminStats'
import { useStoreReviewStore } from '@/stores/storeReview'
import { useDashboardStore } from '@/stores/dashboard'
import { JFmt as F } from '@/utils/format'

const stats = useAdminStatsStore()
const review = useStoreReviewStore()
const dashboard = useDashboardStore()
const { monthly, byCategory } = storeToRefs(stats)

const catMax = computed(() => Math.max(...byCategory.value.map((c) => c.value)) || 1)

const kpis = computed(() => [
  {
    key: 'stores',
    label: '平台開店數量',
    val: stats.totalStores.toLocaleString('en-US'),
    ic: 'home',
    bg: 'var(--c-violet)',
    sub: `${stats.activeStores} 間營運中`,
    delta: null as number | null,
  },
  {
    key: 'products',
    label: '上架商品數量',
    val: stats.totalProducts.toLocaleString('en-US'),
    ic: 'box',
    bg: 'var(--c-pink)',
    sub: `本月新增 ${stats.newProductsThisMonth} 件`,
    delta: null,
  },
  {
    key: 'revenue',
    label: '本月平台交易金額',
    val: F.money(stats.monthRevenue),
    ic: 'wallet',
    bg: 'var(--c-orange)',
    delta: stats.monthDelta,
    up: stats.monthDelta >= 0,
  },
  {
    key: 'orders',
    label: '本月成交筆數',
    val: stats.monthOrders.toLocaleString('en-US'),
    ic: 'receipt',
    bg: 'var(--c-cyan)',
    sub: '較上月持平',
    delta: null,
  },
])

onMounted(() => {
  stats.load()
  review.load()
})
</script>

<template>
  <div data-screen-label="儀表板">
    <div class="page-intro">
      <p class="h-eyebrow">平台管理 · 總覽</p>
      <h1 class="h-title">平台儀表板</h1>
      <p class="h-sub">
        本月平台交易金額較上月成長 {{ stats.monthDelta }}%，目前有
        {{ review.pendingCount }} 筆開店申請待審核。
      </p>
    </div>

    <!-- KPI -->
    <div class="kpi-grid">
      <div v-for="k in kpis" :key="k.key" class="kpi">
        <div class="kpi-top">
          <div class="kpi-ic" :style="{ background: k.bg }"><app-icon :name="k.ic" :size="20" /></div>
          <span v-if="k.delta !== null" class="kpi-delta" :class="k.up ? 'up' : 'down'">
            <app-icon :name="k.up ? 'trend' : 'chevronD'" :size="13" :stroke="2.2" /> {{ Math.abs(k.delta) }}%
          </span>
          <span v-else class="kpi-delta up" style="background:var(--oj-primary-wash); color:var(--oj-primary)">
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
            <h3>平台交易金額趨勢</h3>
            <div class="ch-sub">過去 8 個月・成交總額</div>
          </div>
          <div class="chart-legend">
            <span class="lg"><i style="background:var(--c-violet)"></i>交易金額</span>
          </div>
        </div>
        <trend-chart :data="monthly" :height="232" />
      </div>

      <div class="card-pad">
        <div class="card-head"><h3>分類交易佔比</h3></div>
        <div class="bars">
          <div v-for="c in byCategory" :key="c.label" class="bar-row">
            <span class="bl">{{ c.label }}</span>
            <span class="bar-track"><span class="bar-fill" :style="{ width: Math.max((c.value/catMax)*100, 2) + '%', background: c.color }"></span></span>
            <span class="bv">{{ F.money(c.value) }}</span>
          </div>
        </div>
        <div style="margin-top:22px; padding-top:18px; border-top:1.5px solid var(--border); display:flex; align-items:center; justify-content:space-between;">
          <div>
            <div class="kpi-label" style="margin:0">本月新開店</div>
            <div style="font-family:var(--oj-display); font-weight:800; font-size:24px; letter-spacing:-.6px;">{{ stats.newStoresThisMonth }}</div>
          </div>
          <div style="text-align:right;">
            <div class="kpi-label" style="margin:0">營運中商店</div>
            <div style="font-family:var(--oj-display); font-weight:800; font-size:24px; letter-spacing:-.6px;">{{ stats.activeStores }}</div>
          </div>
        </div>
      </div>
    </div>

    <!-- 待審核開店申請 -->
    <div class="card-pad">
      <div class="card-head">
        <h3>待審核開店申請</h3>
        <button class="link-btn" style="color:var(--oj-primary)" @click="dashboard.go('review')">前往審核 <app-icon name="chevron" :size="13" /></button>
      </div>
      <div v-if="review.pendingCount" class="list-rows">
        <div v-for="a in review.items" :key="a.id" class="lrow">
          <span class="rank rx">審</span>
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
        <div class="eb-t">目前沒有待審核的開店申請</div>
      </div>
    </div>
  </div>
</template>
