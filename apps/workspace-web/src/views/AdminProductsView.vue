<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { storeToRefs } from 'pinia'
import { useAdminCatalogStore } from '@/stores/adminCatalogs'
import { CatalogStatus, type CatalogSummaryDto } from '@/api/catalog-service'

const store = useAdminCatalogStore()
const { items, loading } = storeToRefs(store)

// 商品狀態 → 顯示用標籤
const STATUS = {
  [CatalogStatus.Published]: { label: '已發佈', type: 'success' as const },
  [CatalogStatus.Draft]:     { label: '草稿',   type: 'default' as const },
  [CatalogStatus.Archived]:  { label: '已封存', type: 'warning' as const },
  [CatalogStatus.Suspended]: { label: '已停權', type: 'error' as const },
}
function statusOf(s?: CatalogStatus) {
  return (s != null && STATUS[s]) || { label: '—', type: 'default' as const }
}

// ── 篩選 / 排序狀態 ──────────────────────────────────────────
const keyword = ref('')
/** 狀態篩選：all | Published | Draft | Archived | Suspended */
const statusFilter = ref<'all' | CatalogStatus>('all')
const sortKey = ref<'name' | 'price' | 'publishedAt'>('publishedAt')
const sortDesc = ref(true)

const statusOptions = [
  { label: '全部狀態', value: 'all' },
  { label: '已發佈', value: CatalogStatus.Published },
  { label: '草稿', value: CatalogStatus.Draft },
  { label: '已封存', value: CatalogStatus.Archived },
  { label: '已停權', value: CatalogStatus.Suspended },
]
const columns = [
  { key: 'name', label: '商品', hideSm: false, num: false },
  { key: 'price', label: '售價', hideSm: true, num: true },
  { key: 'publishedAt', label: '上架時間', hideSm: true, num: true },
] as const

function fmtDate(v?: string | null) {
  return v ? new Date(v).toLocaleDateString('zh-TW') : '—'
}
function fmtPrice(p?: number) {
  return p === 0 ? '免費' : '$' + (p ?? 0).toLocaleString('en-US')
}
function shortId(id?: string | null) {
  return id ? id.slice(0, 8) : '—'
}

function toggleSort(key: typeof sortKey.value) {
  if (sortKey.value === key) {
    sortDesc.value = !sortDesc.value
    return
  }
  sortKey.value = key
  sortDesc.value = key === 'publishedAt' || key === 'price'
}

/** 套用關鍵字、狀態篩選與排序後的清單。 */
const visible = computed<CatalogSummaryDto[]>(() => {
  const q = keyword.value.trim().toLowerCase()
  let list = items.value.slice()

  if (statusFilter.value !== 'all') {
    list = list.filter((p) => p.status === statusFilter.value)
  }
  if (q) {
    list = list.filter((p) =>
      [p.name, p.slug, p.summary].some((f) => (f ?? '').toLowerCase().includes(q)),
    )
  }

  const dir = sortDesc.value ? -1 : 1
  list.sort((a, b) => {
    const key = sortKey.value
    if (key === 'price') return ((a.price ?? 0) - (b.price ?? 0)) * dir
    const av = (a[key] ?? '') as string
    const bv = (b[key] ?? '') as string
    if (key === 'name') return av.localeCompare(bv, 'zh-Hant') * dir
    return (av < bv ? -1 : av > bv ? 1 : 0) * dir
  })
  return list
})

/** 切換中的商品 ID（避免重複點擊）。 */
const featuring = ref<string | null>(null)
async function onToggleFeatured(p: CatalogSummaryDto) {
  if (!p.id || featuring.value) return
  featuring.value = p.id
  try {
    await store.setFeatured(p.id, !(p.isFeatured ?? false))
  } finally {
    featuring.value = null
  }
}

onMounted(store.load)
</script>

<template>
  <div data-screen-label="商品列表">
    <div class="page-head">
      <div>
        <p class="h-eyebrow">平台管理</p>
        <h1 class="h-title">商品列表</h1>
        <p class="h-sub">共 {{ items.length }} 件商品，其中 {{ store.publishedCount }} 件已發佈</p>
      </div>
    </div>

    <!-- 篩選 / 排序工具列 -->
    <div class="card-pad store-toolbar">
      <div class="filter-bar">
        <div class="fb-group">
          <div class="fb-field" style="flex:2 1 220px;">
            <label class="fb-label">關鍵字</label>
            <n-input
              v-model:value="keyword"
              clearable
              placeholder="搜尋商品名稱、代稱或簡介">
              <template #prefix><app-icon name="search" :size="16" /></template>
            </n-input>
          </div>
          <div class="fb-field" style="flex:1 1 140px;">
            <label class="fb-label">狀態</label>
            <n-select
              v-model:value="statusFilter"
              :options="statusOptions" />
          </div>
        </div>
      </div>
    </div>

    <n-spin :show="loading">
      <div class="card-pad store-table-card" style="padding:8px 8px 4px;">
        <div class="store-table-wrap">
          <table class="tbl store-table">
            <thead>
              <tr>
                <th v-for="col in columns" :key="col.key" :class="{ 'hide-sm': col.hideSm, num: col.num }">
                  <button class="sort-head" type="button" @click="toggleSort(col.key)">
                    <span>{{ col.label }}</span>
                    <app-icon
                      v-if="sortKey === col.key"
                      :name="sortDesc ? 'chevronD' : 'chevronU'"
                      :size="15" />
                  </button>
                </th>
                <th class="hide-sm">商店</th>
                <th class="hide-sm">狀態</th>
                <th>精選</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="!loading && !visible.length">
                <td :colspan="columns.length + 3" style="text-align:center; padding:48px 24px;">
                  <span class="kpi-ic" style="background:var(--c-pink); margin:0 auto 14px;"><app-icon name="box" :size="22" /></span>
                  <div style="font-weight:700; font-size:15px;">
                    {{ items.length ? '沒有符合條件的商品' : '尚無商品' }}
                  </div>
                  <div style="font-size:13px; color:var(--text-faint); margin-top:4px;">
                    {{ items.length ? '試試調整關鍵字或篩選條件。' : '商家上架作品後會顯示於此。' }}
                  </div>
                </td>
              </tr>
              <tr v-for="p in visible" v-else :key="p.id">
                <td>
                  <div class="prod-cell">
                    <img v-if="p.thumbnailUrl" class="store-avatar" :src="p.thumbnailUrl" :alt="p.name ?? ''" />
                    <span v-else class="store-rank"><app-icon name="box" :size="16" /></span>
                    <div style="min-width:0;">
                      <div class="pc-title">{{ p.name }}</div>
                      <div class="pc-meta store-mono">{{ p.slug }}</div>
                    </div>
                  </div>
                </td>
                <td class="num hide-sm">{{ fmtPrice(p.price) }}</td>
                <td class="num hide-sm store-mono">{{ fmtDate(p.publishedAt) }}</td>
                <td class="hide-sm">
                  <span class="store-mono">{{ shortId(p.storeId) }}</span>
                </td>
                <td class="hide-sm">
                  <n-tag :type="statusOf(p.status).type" size="small" round>{{ statusOf(p.status).label }}</n-tag>
                </td>
                <td>
                  <button
                    type="button"
                    class="feat-toggle"
                    :class="{ on: p.isFeatured }"
                    :disabled="featuring === p.id"
                    :title="p.isFeatured ? '取消精選' : '設為精選'"
                    :aria-pressed="p.isFeatured ?? false"
                    @click="onToggleFeatured(p)">
                    <app-icon :name="p.isFeatured ? 'star' : 'sparkle'" :size="16" />
                  </button>
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
.store-toolbar {
  margin-bottom: 16px;
  border-radius: 10px;
}

.store-toolbar :deep(.n-input),
.store-toolbar :deep(.n-base-selection) {
  border-radius: 10px;
}

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

.store-table-wrap {
  overflow-x: auto;
}

.store-table {
  min-width: 820px;
}

.store-table-card {
  border-radius: 10px;
}

.sort-head {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 0;
  border: 0;
  background: transparent;
  color: inherit;
  font: inherit;
  cursor: pointer;
}

.store-table thead th {
  font-size: 12.5px;
  padding-top: 12px;
  vertical-align: middle;
}

.store-table thead th + th {
  border-left: 1.5px solid var(--border);
}

.store-table tbody td + td {
  border-left: 1.5px solid var(--border);
}

.store-rank {
  width: 30px;
  height: 30px;
  border-radius: 10px;
  display: grid;
  place-items: center;
  flex: none;
  background: var(--oj-primary-wash);
  color: var(--oj-primary);
}

.store-avatar {
  width: 30px;
  height: 30px;
  border-radius: 10px;
  object-fit: cover;
  flex: none;
}

.store-mono {
  font-family: var(--oj-mono);
  color: var(--text-soft);
}

.feat-toggle {
  display: grid;
  place-items: center;
  width: 30px;
  height: 30px;
  border-radius: 9px;
  border: 1.5px solid var(--border);
  background: transparent;
  color: var(--text-faint);
  cursor: pointer;
  transition: color 0.15s, border-color 0.15s, background 0.15s;
}

.feat-toggle:hover:not(:disabled) {
  color: var(--oj-primary);
  border-color: var(--oj-primary);
}

.feat-toggle.on {
  color: #f0a020;
  border-color: #f0a020;
  background: rgba(240, 160, 32, 0.1);
}

.feat-toggle:disabled {
  opacity: 0.5;
  cursor: default;
}
</style>
