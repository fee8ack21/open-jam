<script setup lang="ts">
import { computed } from 'vue'
import { useDashboardStore } from '@/stores/dashboard'
import { JFmt as F, STATUS_LABEL } from '@/utils/format'
import { ME as me, REVENUE as revenue } from '@/data'

const store = useDashboardStore()
const g = store

const catMax = computed(() => Math.max(...revenue.byCategory.map(c => c.value)) || 1)
const kpis = computed(() => [
  { key: 'rev', label: '本月淨收入', val: F.money(g.monthRevenue), ic: 'wallet', bg: 'var(--c-violet)', delta: g.monthDelta, up: g.monthDelta >= 0 },
  { key: 'sales', label: '總銷售件數', val: g.totalSales.toLocaleString('en-US'), ic: 'bag', bg: 'var(--c-pink)', delta: 12, up: true },
  { key: 'payout', label: '待結算金額', val: F.money(g.pendingPayout), ic: 'dollar', bg: 'var(--c-orange)', delta: null, sub: '6/05 撥款' },
  { key: 'views', label: '作品總瀏覽', val: F.compact(g.totalViews), ic: 'eye', bg: 'var(--c-cyan)', delta: 8, up: true },
])

function statusLabel(s: string) { return STATUS_LABEL[s] || s }
</script>

<template>
  <div data-screen-label="儀表板">
    <div class="page-intro">
      <p class="h-eyebrow">總覽 · {{ me.handle }}</p>
      <h1 class="h-title">嗨 Mira，今天表現不錯 👋</h1>
      <p class="h-sub">本月收入較上月成長 {{ g.monthDelta }}%，有 {{ g.statusCount('review') }} 件作品正在審核中。</p>
    </div>

    <!-- KPI -->
    <div class="kpi-grid">
      <div v-for="k in kpis" :key="k.key" class="kpi">
        <div class="kpi-top">
          <div class="kpi-ic" :style="{ background: k.bg }"><j-icon :name="k.ic" :size="20" /></div>
          <span v-if="k.delta !== null" class="kpi-delta" :class="k.up ? 'up' : 'down'">
            <j-icon :name="k.up ? 'trend' : 'chevronD'" :size="13" :stroke="2.2" /> {{ Math.abs(k.delta) }}%
          </span>
          <span v-else class="kpi-delta up" style="background:var(--oj-primary-wash); color:var(--oj-primary)">
            <j-icon name="clock" :size="13" /> {{ k.sub }}
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
            <h3>收入趨勢</h3>
            <div class="ch-sub">過去 8 個月・淨收入</div>
          </div>
          <div class="chart-legend">
            <span class="lg"><i style="background:var(--c-violet)"></i>淨收入</span>
          </div>
        </div>
        <trend-chart :data="revenue.monthly" :height="232" />
      </div>

      <div class="card-pad">
        <div class="card-head"><h3>分類佔比</h3></div>
        <div class="bars">
          <div v-for="c in revenue.byCategory" :key="c.label" class="bar-row">
            <span class="bl">{{ c.label }}</span>
            <span class="bar-track"><span class="bar-fill" :style="{ width: Math.max((c.value/catMax)*100, 2) + '%', background: c.color }"></span></span>
            <span class="bv">{{ F.money(c.value) }}</span>
          </div>
        </div>
        <div style="margin-top:22px; padding-top:18px; border-top:1.5px solid var(--border); display:flex; align-items:center; justify-content:space-between;">
          <div>
            <div class="kpi-label" style="margin:0">轉換率</div>
            <div style="font-family:var(--oj-display); font-weight:800; font-size:24px; letter-spacing:-.6px;">{{ g.convRate }}%</div>
          </div>
          <div style="text-align:right;">
            <div class="kpi-label" style="margin:0">粉絲</div>
            <div style="font-family:var(--oj-display); font-weight:800; font-size:24px; letter-spacing:-.6px;">{{ F.compact(me.followers) }}</div>
          </div>
        </div>
      </div>
    </div>

    <!-- top products + recent orders -->
    <div class="dash-grid cols-2">
      <div class="card-pad">
        <div class="card-head">
          <h3>熱銷作品</h3>
          <button class="link-btn" style="color:var(--oj-primary)" @click="store.go('products')">查看全部 <j-icon name="chevron" :size="13" /></button>
        </div>
        <div class="list-rows">
          <div v-for="(p,i) in g.topProducts" :key="p.id" class="lrow">
            <span class="rank" :class="'r' + (i < 3 ? i+1 : 'x')">{{ i+1 }}</span>
            <product-thumb :product="p" :glyph-size="20" :show-cat="false" hide-label />
            <div class="lr-body">
              <div class="lr-title">{{ p.title }}</div>
              <div class="lr-meta">{{ p.sales.toLocaleString('en-US') }} 次售出 · {{ F.compact(p.views) }} 瀏覽</div>
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
          <h3>最新訂單</h3>
          <button class="link-btn" style="color:var(--oj-primary)" @click="store.go('orders')">查看全部 <j-icon name="chevron" :size="13" /></button>
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
