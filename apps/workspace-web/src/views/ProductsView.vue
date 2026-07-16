<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useMessage, useDialog } from 'naive-ui'
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
const router = useRouter()
const message = useMessage()
const dialog = useDialog()
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

function openEdit(p: CatalogSummaryDto) {
  if (p.id) router.push({ name: 'product-edit', params: { id: p.id } })
}

// 只有已上架商品可設為店長精選
function canFeature(p: CatalogSummaryDto) {
  return p.status === CatalogStatus.Published
}

// 只有未曾上架的草稿可刪除（軟刪除；上架過的商品僅能下架封存）
function canDelete(p: CatalogSummaryDto) {
  return p.status === CatalogStatus.Draft && !p.publishedAt
}
function onDelete(p: CatalogSummaryDto) {
  if (!p.id) return
  dialog.warning({
    title: t('products.deleteConfirmTitle'),
    content: t('products.deleteConfirmDesc', { name: p.name ?? '' }),
    positiveText: t('common.confirm'),
    negativeText: t('common.cancel'),
    onPositiveClick: async () => {
      const ok = await catalog.remove(p.id!)
      if (ok) message.success(t('products.msgDeleted'))
      else message.error(catalog.error ?? t('products.msgActionFailed'))
    },
  })
}
async function toggleFeatured(p: CatalogSummaryDto) {
  if (!p.id) return
  const next = !p.isStoreFeatured
  const ok = await catalog.setStoreFeatured(p.id, next)
  if (ok) message.success(next ? t('products.msgFeatured') : t('products.msgUnfeatured'))
  else message.error(catalog.error ?? t('products.msgActionFailed'))
}

// 店長精選排序 drawer
const reordering = ref(false)
const featuredList = ref<CatalogSummaryDto[]>([])
const savingOrder = ref(false)
async function openReorder() {
  reordering.value = true
  featuredList.value = await catalog.listStoreFeatured(storeId.value)
}
function moveFeatured(i: number, dir: -1 | 1) {
  const j = i + dir
  if (j < 0 || j >= featuredList.value.length) return
  const arr = featuredList.value
  ;[arr[i], arr[j]] = [arr[j], arr[i]]
}
async function saveOrder() {
  savingOrder.value = true
  const ids = featuredList.value.map((p) => p.id!).filter(Boolean)
  const ok = await catalog.reorderStoreFeatured(storeId.value, ids)
  savingOrder.value = false
  if (ok) {
    message.success(t('products.msgOrderSaved'))
    reordering.value = false
    await catalog.reload()
  } else {
    message.error(catalog.error ?? t('products.msgActionFailed'))
  }
}

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
async function changePageSize(size: number) { page.value = 1; await catalog.setPageSize(size) }

function fmtDate(v?: string | null) {
  return v ? new Date(v).toLocaleDateString(locale.value) : '—'
}
const COVER_PASTELS = ['#dff5d3', '#e4f6ff', '#ffe3f6', '#fff3c4', '#ede6ff']
function coverStyle(hue?: number) {
  const n = Math.abs(hue ?? 256)
  return { background: COVER_PASTELS[n % COVER_PASTELS.length] }
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
    <div class="page-head" style="justify-content:flex-end; gap:10px;">
      <n-button class="fb-search-btn" @click="openReorder">
        <template #icon><app-icon name="star" :size="16" /></template>
        {{ t('products.manageFeatured') }}
      </n-button>
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
                    <button
                      v-if="canFeature(p)"
                      class="ic-act"
                      :class="{ 'is-featured': p.isStoreFeatured }"
                      :disabled="busyId === p.id"
                      :title="p.isStoreFeatured ? t('products.unfeature') : t('products.feature')"
                      @click="toggleFeatured(p)">
                      <app-icon name="star" :size="17" :fill="!!p.isStoreFeatured" />
                    </button>
                    <button class="ic-act" :title="t('products.viewReviews')" @click="openReviews(p)"><app-icon name="chat" :size="17" /></button>
                    <button class="ic-act" :title="t('common.edit')" @click="openEdit(p)"><app-icon name="edit" :size="17" /></button>
                    <button v-if="canDelete(p)" class="ic-act danger" :title="t('common.delete')" :disabled="busyId === p.id" @click="onDelete(p)"><app-icon name="trash" :size="17" /></button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <div class="history-pager">
          <list-pager
            :page="page"
            :page-count="totalPages"
            :page-size="catalog.pageSize"
            @update:page="changePage"
            @update:page-size="changePageSize" />
        </div>
      </div>
    </n-spin>

    <!-- 評論檢視 -->
    <n-drawer :show="!!reviewing" :width="440" placement="right" @update:show="(v: boolean) => { if (!v) reviewing = null }">
      <n-drawer-content :title="reviewing?.name || t('products.reviewsTitle')" closable>
        <review-list v-if="reviewing?.id" :catalog-id="reviewing.id" />
      </n-drawer-content>
    </n-drawer>

    <!-- 店長精選排序 -->
    <n-drawer :show="reordering" :width="440" placement="right" @update:show="(v: boolean) => { reordering = v }">
      <n-drawer-content :title="t('products.manageFeatured')" closable>
        <p style="font-size:13px; color:var(--text-soft); margin:0 0 16px;">{{ t('products.reorderHint') }}</p>
        <div v-if="!featuredList.length" style="text-align:center; padding:40px 24px; color:var(--text-faint); font-size:13px;">
          {{ t('products.noFeatured') }}
        </div>
        <ul v-else class="feat-list">
          <li v-for="(p, i) in featuredList" :key="p.id" class="feat-row">
            <span class="feat-idx">{{ i + 1 }}</span>
            <span class="prod-cover" :style="coverStyle(p.coverHue)">
              <img v-if="p.thumbnailUrl" :src="p.thumbnailUrl" alt="" />
            </span>
            <span class="feat-name">{{ p.name }}</span>
            <span class="feat-moves">
              <button class="ic-act" :disabled="i === 0" :title="t('products.moveUp')" @click="moveFeatured(i, -1)"><app-icon name="chevronU" :size="16" /></button>
              <button class="ic-act" :disabled="i === featuredList.length - 1" :title="t('products.moveDown')" @click="moveFeatured(i, 1)"><app-icon name="chevronD" :size="16" /></button>
            </span>
          </li>
        </ul>
        <template #footer>
          <n-button @click="reordering = false">{{ t('common.cancel') }}</n-button>
          <n-button type="primary" :loading="savingOrder" :disabled="!featuredList.length" @click="saveOrder" style="margin-left:10px;">
            {{ t('common.save') }}
          </n-button>
        </template>
      </n-drawer-content>
    </n-drawer>
  </div>
</template>

<style scoped>
/* 篩選區段：卡片頂部，底部整寬分隔線與表格分開 */
.list-filter {
  padding: 18px 20px;
  border-bottom: var(--bw) solid var(--border-strong);
  background: var(--bg);
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
  font-size: 12px;
  font-weight: 900;
  color: var(--text);
}

/* 搜尋按鈕與輸入框同高、同圓角（Input heightMedium 於 App.vue 覆寫為 42px） */
.fb-search-btn {
  height: 40px;
}

.history-table-card {
  padding: 0;
  border-radius: var(--r-lg);
  overflow: hidden;
}

.history-pager {
  display: flex;
  justify-content: flex-end;
  padding: 14px 20px;
}

.history-table-wrap {
  overflow-x: auto;
  padding: 0 10px;
}

.history-table {
  min-width: 760px;
}

.history-table thead th {
  font-size: 12.5px;
  padding-top: 12px;
  vertical-align: middle;
}



.history-mono {
  font-family: var(--oj-mono);
  color: var(--text-soft);
}

.prod-cover {
  width: 42px;
  height: 42px;
  border-radius: 10px;
  border: var(--bw) solid var(--border-strong);
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

/* 店長精選：亮起的星星 = 黃底貼紙 */
.ic-act.is-featured,
.ic-act.is-featured:hover {
  background: var(--c-yellow);
  color: var(--text);
}

/* 店長精選排序清單 */
.feat-list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 8px;
}
.feat-row {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 8px 10px;
  border: var(--bw) solid var(--border-strong);
  border-radius: 10px;
}
.feat-idx {
  font-family: var(--oj-mono);
  font-size: 12px;
  font-weight: 700;
  color: var(--text-faint);
  width: 18px;
  text-align: center;
  flex: none;
}
.feat-name {
  flex: 1 1 auto;
  min-width: 0;
  font-size: 13.5px;
  font-weight: 700;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.feat-moves {
  display: flex;
  gap: 2px;
  flex: none;
}
</style>
