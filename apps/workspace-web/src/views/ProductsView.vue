<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useMessage } from 'naive-ui'
import { useI18n } from 'vue-i18n'
import { storeToRefs } from 'pinia'
import { useDashboardStore } from '@/stores/dashboard'
import { useCatalogStore } from '@/stores/catalog'
import { useStoreApplicationStore } from '@/stores/storeApplication'
import { CatalogStatus, type CatalogSummaryDto } from '@/api/catalog-service'
import ReviewList from '@/components/ReviewList.vue'

const { t, locale } = useI18n()

const STATUS = {
  [CatalogStatus.Published]: { labelKey: 'catalogStatus.published', type: 'success' as const },
  [CatalogStatus.Draft]:     { labelKey: 'catalogStatus.draft',     type: 'default' as const },
  [CatalogStatus.Archived]:  { labelKey: 'catalogStatus.archived',  type: 'warning' as const },
  [CatalogStatus.Suspended]: { labelKey: 'catalogStatus.suspended', type: 'error' as const },
}
function statusOf(s?: CatalogStatus) {
  return (s != null && STATUS[s]) || { labelKey: 'catalogStatus.unknown', type: 'default' as const }
}

const dashboard = useDashboardStore()
const message = useMessage()
const catalog = useCatalogStore()
const storeApp = useStoreApplicationStore()
const { products, totalCount, loading, busyId } = storeToRefs(catalog)

const filterKey = ref<'all' | CatalogStatus>('all')
/** 關鍵字查詢（伺服器端，比對作品名稱）。 */
const keyword = ref('')
const page = ref(1)

// 目前登入創作者的商店 id（取第一間）
const storeId = computed(() => storeApp.stores[0]?.store?.id ?? '')

// 評論檢視 drawer
const reviewing = ref<CatalogSummaryDto | null>(null)
function openReviews(p: CatalogSummaryDto) { reviewing.value = p }

// 狀態下拉選項（對齊訂單管理的下拉篩選）
const statusOptions = computed(() => [
  { label: t('catalogStatus.all'), value: 'all' as const },
  { label: t('catalogStatus.published'), value: CatalogStatus.Published },
  { label: t('catalogStatus.draft'), value: CatalogStatus.Draft },
  { label: t('catalogStatus.archived'), value: CatalogStatus.Archived },
  { label: t('catalogStatus.suspended'), value: CatalogStatus.Suspended },
])

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / catalog.pageSize)))

async function applyFilter() {
  page.value = 1
  await catalog.applyFilter({ search: keyword.value, status: filterKey.value === 'all' ? null : filterKey.value })
}
// 關鍵字改由「搜尋」按鈕 / Enter 觸發；下拉狀態維持即時套用
watch(filterKey, () => { applyFilter() })
async function changePage(p: number) { page.value = p; await catalog.goPage(p) }

function fmtDate(v?: string | null) {
  return v ? new Date(v).toLocaleDateString(locale.value) : '—'
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
    ? await catalog.archive(p.id)
    : await catalog.publish(p.id)
  if (ok) message.success(wasPublished ? t('products.msgArchived') : t('products.msgPublished'))
  else message.error(catalog.error ?? t('products.msgActionFailed'))
}

async function load() {
  if (!storeApp.stores.length) await storeApp.load()
  await catalog.load(storeId.value)
}

onMounted(load)
</script>

<template>
  <div :data-screen-label="t('route.products')">
    <div class="page-head" style="justify-content:flex-end;">
      <n-button class="fb-search-btn" type="primary" @click="dashboard.go('upload')">
        <template #icon><app-icon name="plus" :size="16" :stroke="2.4" /></template>
        {{ t('products.newProduct') }}
      </n-button>
    </div>

    <n-spin :show="loading">
      <!-- 篩選列與商品表格合併為單一卡片：篩選在上、整寬分隔線、表格在下 -->
      <div class="card-pad history-table-card">
        <div class="list-filter">
          <div class="filter-bar">
            <div class="fb-group">
              <div class="fb-field" style="flex:1 1 140px;">
                <label class="fb-label">{{ t('common.status') }}</label>
                <n-select
                  v-model:value="filterKey"
                  :options="statusOptions" />
              </div>
              <div class="fb-field" style="flex:2 1 200px;">
                <label class="fb-label">{{ t('common.keyword') }}</label>
                <n-input
                  v-model:value="keyword"
                  clearable
                  :placeholder="t('products.searchPlaceholder')"
                  @keyup.enter="applyFilter">
                  <template #prefix><app-icon name="search" :size="15" /></template>
                </n-input>
              </div>
              <n-button class="fb-search-btn" type="primary" :loading="loading" @click="applyFilter">
                <template #icon><app-icon name="search" :size="16" /></template>
                {{ t('common.search') }}
              </n-button>
            </div>
          </div>
        </div>

        <!-- 商品表格：即使無資料仍顯示表頭，空狀態以 tbody 整列呈現 -->
        <div class="history-table-wrap">
          <table class="tbl history-table">
            <thead>
              <tr>
                <th>{{ t('products.colWork') }}</th>
                <th class="hide-sm">{{ t('common.status') }}</th>
                <th class="num hide-sm">{{ t('products.colPrice') }}</th>
                <th class="num hide-sm">{{ t('products.colViews') }}</th>
                <th class="num hide-sm">{{ t('products.colPublishedAt') }}</th>
                <th style="width:160px; text-align:right;">{{ t('products.colToggle') }}</th>
              </tr>
            </thead>
            <tbody>
              <!-- 無紀錄：整列佔滿全部欄位 -->
              <tr v-if="!loading && !products.length">
                <td colspan="6" style="text-align:center; padding:48px 24px;">
                  <span class="kpi-ic" style="background:var(--c-violet); margin:0 auto 14px;"><app-icon name="box" :size="22" /></span>
                  <div style="font-weight:700; font-size:15px;">{{ t('products.emptyTitle') }}</div>
                  <div style="font-size:13px; color:var(--text-faint); margin-top:4px;">
                    {{ t('products.emptyDesc') }}
                  </div>
                </td>
              </tr>
              <tr v-for="p in products" :key="p.id">
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
                <td class="hide-sm"><n-tag :type="statusOf(p.status).type" size="small" round>{{ t(statusOf(p.status).labelKey) }}</n-tag></td>
                <td class="num hide-sm">{{ p.price === 0 ? t('common.free') : '$' + p.price }}</td>
                <td class="num hide-sm">{{ (p.viewCount ?? 0).toLocaleString('en-US') }}</td>
                <td class="num hide-sm"><span class="history-mono" style="font-size:12px;">{{ fmtDate(p.publishedAt) }}</span></td>
                <td>
                  <div class="row-actions">
                    <n-switch v-if="canToggle(p)" :value="p.status === CatalogStatus.Published" :loading="busyId === p.id" :disabled="busyId === p.id" @update:value="toggle(p)" size="medium" />
                    <span v-else style="font-size:12px; color:var(--text-faint); font-family:var(--oj-mono); margin-right:6px;">
                      {{ p.status === CatalogStatus.Suspended ? t('catalogStatus.suspended') : t('catalogStatus.draft') }}
                    </span>
                    <button class="ic-act" :title="t('products.viewReviews')" @click="openReviews(p)"><app-icon name="star" :size="17" /></button>
                    <button class="ic-act" :title="t('common.edit')" @click="dashboard.go('upload')"><app-icon name="edit" :size="17" /></button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <div class="history-pager">
          <n-pagination :page="page" :page-count="totalPages" @update:page="changePage" />
        </div>
      </div>
    </n-spin>

    <!-- 評論檢視 -->
    <n-drawer :show="!!reviewing" :width="440" placement="right" @update:show="(v: boolean) => { if (!v) reviewing = null }">
      <n-drawer-content :title="reviewing?.name || t('products.reviewsTitle')" closable>
        <review-list v-if="reviewing?.id" :catalog-id="reviewing.id" />
      </n-drawer-content>
    </n-drawer>
  </div>
</template>

<style scoped>
/* 篩選區段：卡片頂部，底部整寬分隔線與表格分開 */
.list-filter {
  padding: 16px 18px;
  border-bottom: 1.5px solid var(--border);
}

.list-filter :deep(.n-date-picker),
.list-filter :deep(.n-input),
.list-filter :deep(.n-input-wrapper),
.list-filter :deep(.n-base-selection),
.list-filter :deep(.n-base-selection__border),
.list-filter :deep(.n-base-selection__state-border) {
  border-radius: 10px;
}

.list-filter :deep(.n-input__border),
.list-filter :deep(.n-input__state-border) {
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

/* 搜尋按鈕與輸入框同高、同圓角（Input heightMedium 於 App.vue 覆寫為 42px） */
.fb-search-btn {
  height: 42px;
  border-radius: 10px;
}

.history-table-card {
  padding: 0;
  border-radius: 10px;
  overflow: hidden;
}

.history-pager {
  display: flex;
  justify-content: flex-end;
  padding: 12px 8px;
}

.history-table-wrap {
  overflow-x: auto;
  padding: 8px 8px 4px;
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
