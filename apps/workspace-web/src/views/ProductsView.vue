<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useMessage } from 'naive-ui'
import { storeToRefs } from 'pinia'
import { useDashboardStore } from '@/stores/dashboard'
import { useCatalogStore } from '@/stores/catalog'
import { useStoreApplicationStore } from '@/stores/storeApplication'
import { CatalogStatus, type CatalogSummaryDto } from '@/api/catalog-service'

const STATUS = {
  [CatalogStatus.Published]: { label: '已上架', type: 'success' as const },
  [CatalogStatus.Draft]:     { label: '草稿',   type: 'default' as const },
  [CatalogStatus.Archived]:  { label: '已下架', type: 'warning' as const },
  [CatalogStatus.Suspended]: { label: '已停權', type: 'error' as const },
}
function statusOf(s?: CatalogStatus) {
  return (s != null && STATUS[s]) || { label: '—', type: 'default' as const }
}

const dashboard = useDashboardStore()
const message = useMessage()
const catalog = useCatalogStore()
const storeApp = useStoreApplicationStore()
const { products, loading, busyId } = storeToRefs(catalog)

const filterKey = ref<'all' | CatalogStatus>('all')
/** 日期範圍篩選（[起, 迄] 毫秒時間戳），null 表示不限，比對上架時間。 */
const dateRange = ref<[number, number] | null>(null)
/** 關鍵字查詢，比對作品名稱／摘要／slug。 */
const keyword = ref('')

// 目前登入創作者的商店 id（取第一間）
const storeId = computed(() => storeApp.stores[0]?.store?.id ?? '')

// 狀態下拉選項（含各狀態件數），對齊訂單管理的下拉篩選
const statusOptions = computed(() => [
  { label: `全部 (${products.value.length})`, value: 'all' as const },
  { label: `上架中 (${catalog.statusCount(CatalogStatus.Published)})`, value: CatalogStatus.Published },
  { label: `草稿 (${catalog.statusCount(CatalogStatus.Draft)})`, value: CatalogStatus.Draft },
  { label: `已下架 (${catalog.statusCount(CatalogStatus.Archived)})`, value: CatalogStatus.Archived },
  { label: `已停權 (${catalog.statusCount(CatalogStatus.Suspended)})`, value: CatalogStatus.Suspended },
])

/** 將毫秒時間戳轉為當地 YYYY-MM-DD，供與上架時間做同格式字串比較。 */
function dayKey(ts: number): string {
  const d = new Date(ts)
  const p = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}`
}

const rows = computed(() => {
  let list = products.value
  if (filterKey.value !== 'all') list = list.filter((p) => p.status === filterKey.value)
  const kw = keyword.value.trim().toLowerCase()
  if (kw) {
    list = list.filter((p) =>
      [p.name, p.summary, p.slug].some((v) => v?.toLowerCase().includes(kw)),
    )
  }
  if (dateRange.value) {
    const [from, to] = [dayKey(dateRange.value[0]), dayKey(dateRange.value[1])]
    list = list.filter((p) => {
      if (!p.publishedAt) return false
      const k = dayKey(new Date(p.publishedAt).getTime())
      return k >= from && k <= to
    })
  }
  return list
})

function fmtDate(v?: string | null) {
  return v ? new Date(v).toLocaleDateString('zh-TW') : '—'
}
function coverStyle(hue?: number) {
  const h = hue ?? 256
  return { background: `linear-gradient(135deg, hsl(${h} 88% 62%), hsl(${(h + 42) % 360} 90% 54%))` }
}
// 只有 Published / Archived 可用開關切換上下架；Draft 需先補版本後於精靈上架，Suspended 僅 Admin 可解
function canToggle(p: CatalogSummaryDto) {
  return p.status === CatalogStatus.Published || p.status === CatalogStatus.Archived
}

async function toggle(p: CatalogSummaryDto) {
  if (!p.id) return
  const wasPublished = p.status === CatalogStatus.Published
  const ok = wasPublished
    ? await catalog.archive(p.id, storeId.value)
    : await catalog.publish(p.id, storeId.value)
  if (ok) message.success(wasPublished ? '已下架。' : '已上架。')
  else message.error(catalog.error ?? '操作失敗')
}

async function load() {
  if (!storeApp.stores.length) await storeApp.load()
  await catalog.load(storeId.value)
}

onMounted(load)
</script>

<template>
  <div data-screen-label="商品管理">
    <div class="page-head">
      <div>
        <p class="h-eyebrow">賣家工作室</p>
        <h1 class="h-title">商品管理</h1>
        <p class="h-sub">{{ products.length }} 件作品 · {{ catalog.publishedCount }} 件上架中</p>
      </div>
      <button class="cta-pop" @click="dashboard.go('upload')"><app-icon name="plus" :size="16" :stroke="2.4" />上架新商品</button>
    </div>

    <!-- 篩選工具列：單行時四欄平均分布、間隔一致；空間不足時整組換行，最多兩行並填滿寬度 -->
    <div class="card-pad history-toolbar">
      <div class="filter-bar">
        <div class="fb-group">
          <div class="fb-field" style="flex:1 1 140px;">
            <label class="fb-label">狀態</label>
            <n-select
              v-model:value="filterKey"
              :options="statusOptions" />
          </div>
          <div class="fb-field" style="flex:2 1 200px;">
            <label class="fb-label">關鍵字</label>
            <n-input
              v-model:value="keyword"
              clearable
              placeholder="搜尋作品名稱、摘要或 slug">
              <template #prefix><app-icon name="search" :size="15" /></template>
            </n-input>
          </div>
        </div>
        <div class="fb-group">
          <div class="fb-field" style="flex:1 1 240px;">
            <label class="fb-label">上架時間</label>
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

    <n-spin :show="loading">
      <!-- 商品表格：即使無資料仍顯示表頭，空狀態以 tbody 整列呈現 -->
      <div class="card-pad history-table-card" style="padding:8px 8px 4px;">
        <div class="history-table-wrap">
          <table class="tbl history-table">
            <thead>
              <tr>
                <th>作品</th>
                <th class="hide-sm">狀態</th>
                <th class="num hide-sm">售價</th>
                <th class="num hide-sm">上架時間</th>
                <th style="width:160px; text-align:right;">上架</th>
              </tr>
            </thead>
            <tbody>
              <!-- 無紀錄：整列佔滿全部欄位 -->
              <tr v-if="!loading && !rows.length">
                <td colspan="5" style="text-align:center; padding:48px 24px;">
                  <span class="kpi-ic" style="background:var(--c-violet); margin:0 auto 14px;"><app-icon name="box" :size="22" /></span>
                  <div style="font-weight:700; font-size:15px;">沒有符合的作品</div>
                  <div style="font-size:13px; color:var(--text-faint); margin-top:4px;">
                    沒有符合所選條件的作品，試試調整或重設篩選條件。
                  </div>
                </td>
              </tr>
              <tr v-for="p in rows" :key="p.id">
                <td>
                  <div class="prod-cell">
                    <span class="prod-cover" :style="coverStyle(p.coverHue)">
                      <img v-if="p.thumbnailUrl" :src="p.thumbnailUrl" alt="" />
                    </span>
                    <div style="min-width:0;">
                      <div class="pc-title">{{ p.name }}</div>
                      <div class="pc-meta">{{ p.summary || p.slug }}</div>
                    </div>
                  </div>
                </td>
                <td class="hide-sm"><n-tag :type="statusOf(p.status).type" size="small" round>{{ statusOf(p.status).label }}</n-tag></td>
                <td class="num hide-sm">{{ p.price === 0 ? '免費' : '$' + p.price }}</td>
                <td class="num hide-sm"><span class="history-mono" style="font-size:12px;">{{ fmtDate(p.publishedAt) }}</span></td>
                <td>
                  <div class="row-actions">
                    <n-switch v-if="canToggle(p)" :value="p.status === CatalogStatus.Published" :loading="busyId === p.id" :disabled="busyId === p.id" @update:value="toggle(p)" size="medium" />
                    <span v-else style="font-size:12px; color:var(--text-faint); font-family:var(--oj-mono); margin-right:6px;">
                      {{ p.status === CatalogStatus.Suspended ? '已停權' : '草稿' }}
                    </span>
                    <button class="ic-act" title="編輯" @click="dashboard.go('upload')"><app-icon name="edit" :size="17" /></button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </n-spin>
  </div>
</template>

<style scoped>
/* 對齊訂單管理（OrdersView）／admin 頁面的 10px 圓角與表格樣式 */
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

.prod-cover {
  width: 42px;
  height: 42px;
  border-radius: 10px;
  flex: none;
  overflow: hidden;
  display: grid;
  place-items: center;
}
.prod-cover img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}
</style>
