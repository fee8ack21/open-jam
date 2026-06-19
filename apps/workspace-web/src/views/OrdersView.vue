<script setup lang="ts">
import { computed, ref } from 'vue'
import { useDashboardStore } from '@/stores/dashboard'
import { JFmt as F, STATUS_LABEL } from '@/utils/format'
import type { Product } from '@/data/products'

const store = useDashboardStore()
const g = store
const s = store

const tab = ref('all')
const statusOptions = [
  { label: '全部', value: 'all' },
  { label: '已付款', value: 'paid' },
  { label: '已退款', value: 'refunded' },
]
/** 日期範圍篩選（[起, 迄] 毫秒時間戳），null 表示不限。 */
const dateRange = ref<[number, number] | null>(null)
/** 關鍵字查詢，比對訂單編號／買家名稱／帳號／作品名稱。 */
const keyword = ref('')

/** 將毫秒時間戳轉為當地 YYYY-MM-DD，供與訂單日期做同格式字串比較。 */
function dayKey(ts: number): string {
  const d = new Date(ts)
  const p = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}`
}

const rows = computed(() => {
  let list = s.orders
  if (tab.value !== 'all') list = list.filter(o => o.status === tab.value)
  const kw = keyword.value.trim().toLowerCase()
  if (kw) {
    list = list.filter(o =>
      [o.id, o.buyer, o.buyerHandle, prod(o.productId).title].some(v => v?.toLowerCase().includes(kw)),
    )
  }
  if (dateRange.value) {
    const [from, to] = [dayKey(dateRange.value[0]), dayKey(dateRange.value[1])]
    list = list.filter(o => {
      const k = dayKey(new Date(o.date).getTime())
      return k >= from && k <= to
    })
  }
  return list
})
const paidTotal = computed(() => g.paidOrders.reduce((s, o) => s + o.amount, 0))

function statusLabel(s: string) { return STATUS_LABEL[s] || s }
function prod(id: string): Product { return store.product(id) || ({} as Product) }
function dt(iso: string) { const d = new Date(iso); return d.toISOString().slice(5, 10).replace('-', '/') + ' ' + d.toTimeString().slice(0, 5) }
</script>

<template>
  <div data-screen-label="訂單管理">
    <div class="page-head">
      <div>
        <p class="h-eyebrow">賣家工作室</p>
        <h1 class="h-title">訂單管理</h1>
        <p class="h-sub">{{ g.paidOrders.length }} 筆已付款 · 收入合計 {{ F.money(paidTotal) }}</p>
      </div>
    </div>

    <!-- 篩選工具列：單行時四欄平均分布、間隔一致；空間不足時整組換行，最多兩行並填滿寬度 -->
    <div class="card-pad history-toolbar">
      <div class="filter-bar">
        <div class="fb-group">
          <div class="fb-field" style="flex:1 1 140px;">
            <label class="fb-label">狀態</label>
            <n-select
              v-model:value="tab"
              :options="statusOptions" />
          </div>
          <div class="fb-field" style="flex:2 1 200px;">
            <label class="fb-label">關鍵字</label>
            <n-input
              v-model:value="keyword"
              clearable
              placeholder="搜尋訂單編號、買家或作品">
              <template #prefix><app-icon name="search" :size="15" /></template>
            </n-input>
          </div>
        </div>
        <div class="fb-group">
          <div class="fb-field" style="flex:1 1 240px;">
            <label class="fb-label">訂單時間</label>
            <n-date-picker
              v-model:value="dateRange"
              type="daterange"
              clearable
              start-placeholder="起始日期"
              end-placeholder="結束日期" />
          </div>
        </div>
      </div>
    </div>

    <!-- 訂單表格：即使無資料仍顯示表頭，空狀態以 tbody 整列呈現 -->
    <div class="card-pad history-table-card" style="padding:8px 8px 4px;">
      <div class="history-table-wrap">
        <table class="tbl history-table">
          <thead>
            <tr>
              <th>訂單編號</th>
              <th>買家</th>
              <th class="hide-sm">作品</th>
              <th class="hide-sm">狀態</th>
              <th class="hide-sm">時間</th>
              <th class="num">金額</th>
            </tr>
          </thead>
          <tbody>
            <!-- 無紀錄：整列佔滿全部欄位 -->
            <tr v-if="!rows.length">
              <td colspan="6" style="text-align:center; padding:48px 24px;">
                <span class="kpi-ic" style="background:var(--c-violet); margin:0 auto 14px;"><app-icon name="receipt" :size="22" /></span>
                <div style="font-weight:700; font-size:15px;">沒有符合的訂單</div>
                <div style="font-size:13px; color:var(--text-faint); margin-top:4px;">
                  沒有符合所選條件的訂單，試試調整或重設篩選條件。
                </div>
              </td>
            </tr>
            <tr v-for="o in rows" :key="o.id">
              <td><span class="history-mono" style="font-size:12.5px;">{{ o.id }}</span></td>
              <td>
                <div style="display:flex; align-items:center; gap:10px;">
                  <span class="avatar" :style="{ background: o.avatar, width:'30px', height:'30px', fontSize:'11px' }">{{ F.initials(o.buyer) }}</span>
                  <div style="min-width:0;">
                    <div style="font-weight:600; font-size:13.5px;">{{ o.buyer }}</div>
                    <div style="font-family:var(--oj-mono); font-size:11px; color:var(--text-faint);">{{ o.buyerHandle }}</div>
                  </div>
                </div>
              </td>
              <td class="hide-sm" style="max-width:240px;">
                <div style="display:-webkit-box; -webkit-line-clamp:1; -webkit-box-orient:vertical; overflow:hidden; font-size:13.5px; color:var(--text-soft);">{{ prod(o.productId).title }}</div>
              </td>
              <td class="hide-sm"><span class="pill" :class="o.status"><span class="pdot"></span>{{ statusLabel(o.status) }}</span></td>
              <td class="hide-sm"><span class="history-mono" style="font-size:12px;">{{ dt(o.date) }}</span></td>
              <td class="num" style="font-weight:700;" :style="o.status === 'refunded' ? 'color:var(--text-faint); text-decoration:line-through' : ''">{{ F.money(o.amount) }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* 對齊 admin 頁面（AuditLogView）的 10px 圓角與表格樣式 */
.history-toolbar {
  margin-bottom: 16px;
  border-radius: 10px;
}

.history-toolbar :deep(.n-date-picker),
.history-toolbar :deep(.n-input),
.history-toolbar :deep(.n-input-wrapper),
.history-toolbar :deep(.n-base-selection),
.history-toolbar :deep(.n-base-selection__border),
.history-toolbar :deep(.n-base-selection__state-border) {
  border-radius: 10px;
}

.history-toolbar :deep(.n-input__border),
.history-toolbar :deep(.n-input__state-border) {
  border-radius: 10px;
}

/* 篩選列：兩組各佔一半，單行並排（共四欄平均分布），不足時整組換行成最多兩行 */
.filter-bar {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  align-items: center;
}

.fb-group {
  display: flex;
  gap: 12px;
  align-items: flex-end;
  flex: 1 1 360px;
  min-width: 0;
}

/* 欄位：標籤在上、控制項在下，撐滿配置的 flex 寬度 */
.fb-field {
  display: flex;
  flex-direction: column;
  gap: 6px;
  min-width: 0;
}

.fb-label {
  font-size: 12.5px;
  font-weight: 600;
  color: var(--text-soft);
}

.history-table-card {
  border-radius: 10px;
}

.history-table-wrap {
  overflow-x: auto;
}

.history-table {
  min-width: 760px;
}

.history-table thead th {
  font-size: 12.5px;
  padding-top: 12px;
  vertical-align: middle;
}

.history-table thead th + th {
  border-left: 1.5px solid var(--border);
}

.history-table tbody td + td {
  border-left: 1.5px solid var(--border);
}

.history-mono {
  font-family: var(--oj-mono);
  color: var(--text-soft);
}
</style>
