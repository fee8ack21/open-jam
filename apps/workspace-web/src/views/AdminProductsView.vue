<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { storeToRefs } from 'pinia'
import { useAdminCatalogStore } from '@/stores/adminCatalogs'
import { CatalogStatus, type CatalogSummaryDto } from '@/api/catalog-service'

const { t, locale } = useI18n()
const store = useAdminCatalogStore()
const { items, totalCount, loading } = storeToRefs(store)

// 商品狀態 → 顯示用標籤
const STATUS = {
  [CatalogStatus.Published]: { labelKey: 'storeProducts.statusPublished', type: 'success' as const },
  [CatalogStatus.Draft]:     { labelKey: 'storeProducts.statusDraft',     type: 'default' as const },
  [CatalogStatus.Archived]:  { labelKey: 'storeProducts.statusArchived',  type: 'warning' as const },
  [CatalogStatus.Suspended]: { labelKey: 'storeProducts.statusSuspended', type: 'error' as const },
}
function statusOf(s?: CatalogStatus) {
  return (s != null && STATUS[s]) || { labelKey: 'catalogStatus.unknown', type: 'default' as const }
}

// ── 篩選狀態（伺服器端分頁）──────────────────────────────────
const keyword = ref('')
/** 狀態篩選：all | Published | Draft | Archived | Suspended */
const statusFilter = ref<'all' | CatalogStatus>('all')
const page = ref(1)

const statusOptions = computed(() => [
  { label: t('stores.filterAll'), value: 'all' },
  { label: t('storeProducts.statusPublished'), value: CatalogStatus.Published },
  { label: t('storeProducts.statusDraft'), value: CatalogStatus.Draft },
  { label: t('storeProducts.statusArchived'), value: CatalogStatus.Archived },
  { label: t('storeProducts.statusSuspended'), value: CatalogStatus.Suspended },
])
const columns = [
  { key: 'name', labelKey: 'products.colWork', hideSm: false, num: false },
  { key: 'price', labelKey: 'products.colPrice', hideSm: true, num: true },
  { key: 'publishedAt', labelKey: 'products.colPublishedAt', hideSm: true, num: true },
] as const

function fmtDate(v?: string | null) {
  return v ? new Date(v).toLocaleDateString(locale.value) : '—'
}
function fmtPrice(p?: number) {
  return p === 0 ? t('common.free') : '$' + (p ?? 0).toLocaleString('en-US')
}
function shortId(id?: string | null) {
  return id ? id.slice(0, 8) : '—'
}

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / store.pageSize)))
const hasFilter = computed(() => keyword.value.trim() !== '' || statusFilter.value !== 'all')

async function applyFilter() {
  page.value = 1
  await store.applyFilter({ search: keyword.value, status: statusFilter.value === 'all' ? null : statusFilter.value })
}
// 關鍵字改由「搜尋」按鈕 / Enter 觸發；下拉狀態維持即時套用
watch(statusFilter, () => { applyFilter() })
async function changePage(p: number) { page.value = p; await store.goPage(p) }

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
  <div :data-screen-label="t('route.adminProducts')">

    <n-spin :show="loading">
      <!-- 篩選列與表格合併為單一卡片：篩選在上、整寬分隔線、表格在下 -->
      <div class="card-pad store-table-card">
        <div class="list-filter">
          <div class="filter-bar">
            <div class="fb-group">
              <div class="fb-field" style="flex:2 1 220px;">
                <label class="fb-label">{{ t('common.keyword') }}</label>
                <n-input
                  v-model:value="keyword"
                  clearable
                  :placeholder="t('adminProducts.searchPlaceholder')"
                  @keyup.enter="applyFilter">
                  <template #prefix><app-icon name="search" :size="16" /></template>
                </n-input>
              </div>
              <div class="fb-field" style="flex:1 1 140px;">
                <label class="fb-label">{{ t('common.status') }}</label>
                <n-select
                  v-model:value="statusFilter"
                  :options="statusOptions" />
              </div>
              <n-button class="fb-search-btn" type="primary" :loading="loading" @click="applyFilter">
                <template #icon><app-icon name="search" :size="16" /></template>
                {{ t('common.search') }}
              </n-button>
            </div>
          </div>
        </div>

        <div class="store-table-wrap">
          <table class="tbl store-table">
            <thead>
              <tr>
                <th v-for="col in columns" :key="col.key" :class="{ 'hide-sm': col.hideSm, num: col.num }">
                  <span>{{ t(col.labelKey) }}</span>
                </th>
                <th class="hide-sm">{{ t('adminProducts.colStore') }}</th>
                <th class="hide-sm">{{ t('common.status') }}</th>
                <th>{{ t('adminProducts.colFeatured') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="!loading && !items.length">
                <td :colspan="columns.length + 3" style="text-align:center; padding:48px 24px;">
                  <span class="kpi-ic" style="background:var(--c-pink); margin:0 auto 14px;"><app-icon name="box" :size="22" /></span>
                  <div style="font-weight:700; font-size:15px;">
                    {{ hasFilter ? t('adminProducts.emptyFilteredTitle') : t('adminProducts.emptyTitle') }}
                  </div>
                  <div style="font-size:13px; color:var(--text-faint); margin-top:4px;">
                    {{ hasFilter ? t('adminProducts.emptyFilteredDesc') : t('adminProducts.emptyDesc') }}
                  </div>
                </td>
              </tr>
              <tr v-for="p in items" v-else :key="p.id">
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
                  <n-tag :type="statusOf(p.status).type" size="small" round>{{ t(statusOf(p.status).labelKey) }}</n-tag>
                </td>
                <td>
                  <button
                    type="button"
                    class="feat-toggle"
                    :class="{ on: p.isFeatured }"
                    :disabled="featuring === p.id"
                    :title="p.isFeatured ? t('adminProducts.unfeature') : t('adminProducts.feature')"
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

      <div v-if="totalPages > 1" class="history-pager">
        <n-pagination :page="page" :page-count="totalPages" @update:page="changePage" />
      </div>
    </n-spin>
  </div>
</template>

<style scoped>
/* 篩選區段：卡片頂部，底部整寬分隔線與表格分開 */
.list-filter {
  padding: 16px 18px;
  border-bottom: 1.5px solid var(--border);
}

.list-filter :deep(.n-input),
.list-filter :deep(.n-base-selection) {
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

/* 搜尋按鈕與輸入框同高、同圓角（Input heightMedium 於 App.vue 覆寫為 42px） */
.fb-search-btn {
  height: 42px;
  border-radius: 10px;
}

.store-table-wrap {
  overflow-x: auto;
  padding: 8px 8px 4px;
}

.store-table {
  min-width: 820px;
}

.store-table-card {
  padding: 0;
  border-radius: 10px;
  overflow: hidden;
}

.history-pager {
  display: flex;
  justify-content: flex-end;
  padding: 12px 8px;
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
