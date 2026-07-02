<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { storeToRefs } from 'pinia'
import { useStoreReviewStore } from '@/stores/storeReview'
import { StoreApplicationStatus } from '@/api/store-service'

const { t, locale } = useI18n()
const store = useStoreReviewStore()
const { history, historyTotal, loading } = storeToRefs(store)

// 審核結果 → 顯示用標籤
const RESULT = {
  [StoreApplicationStatus.Approved]: { labelKey: 'appStatus.approved', type: 'success' as const },
  [StoreApplicationStatus.Rejected]: { labelKey: 'appStatus.rejected', type: 'error' as const },
}
function resultOf(s?: StoreApplicationStatus) {
  return (s != null && RESULT[s as keyof typeof RESULT]) || { labelKey: 'appStatus.unknown', type: 'default' as const }
}

// ── 篩選狀態（結果為伺服器端分頁；關鍵字為目前頁次的客戶端過濾）──
const keyword = ref('')
const appliedKeyword = ref('') // 已套用的關鍵字（按下搜尋才更新）
/** 審核結果篩選：all | Approved | Rejected（伺服器端）。 */
const statusFilter = ref<'all' | StoreApplicationStatus>('all')
const page = ref(1)

const statusOptions = computed(() => [
  { label: t('reviewHistory.filterAll'), value: 'all' },
  { label: t('appStatus.approved'), value: StoreApplicationStatus.Approved },
  { label: t('appStatus.rejected'), value: StoreApplicationStatus.Rejected },
])
const columns = [
  { key: 'storeName', labelKey: 'review.colStoreName', hideSm: false },
  { key: 'storeSlug', labelKey: 'review.colSubdomain', hideSm: false },
  { key: 'email', labelKey: 'review.colEmail', hideSm: true },
  { key: 'createdAt', labelKey: 'reviewHistory.colAppliedAt', hideSm: true },
  { key: 'reviewedAt', labelKey: 'reviewHistory.colReviewedAt', hideSm: true },
] as const

function fmtDate(v?: string | null) {
  return v ? new Date(v).toLocaleString(locale.value, { hour12: false }) : '—'
}
function initial(email?: string | null) {
  return (email?.charAt(0) || '?').toUpperCase()
}

const totalPages = computed(() => Math.max(1, Math.ceil(historyTotal.value / store.pageSize)))
const hasFilter = computed(() => appliedKeyword.value !== '' || statusFilter.value !== 'all')

/** 依已套用關鍵字過濾目前頁次的已審核紀錄（結果篩選已於伺服器端套用）。 */
const visible = computed(() => {
  const q = appliedKeyword.value.toLowerCase()
  if (!q) return history.value
  return history.value.filter((a) =>
    [a.storeName, a.storeSlug, a.email].some((f) => (f ?? '').toLowerCase().includes(q)),
  )
})

async function refetch() {
  page.value = 1
  await store.applyHistoryFilter(statusFilter.value === 'all' ? null : statusFilter.value)
}
// 按下搜尋：套用目前關鍵字並重新載入
async function onSearch() {
  appliedKeyword.value = keyword.value.trim()
  await refetch()
}
// 下拉結果變更：即時重新載入
async function onStatusChange() {
  await refetch()
}
async function changePage(p: number) { page.value = p; await store.goHistoryPage(p) }

onMounted(store.loadHistory)
</script>

<template>
  <div :data-screen-label="t('route.reviewHistory')">

    <n-spin :show="loading">
      <!-- 篩選列與紀錄表格合併為單一卡片：篩選在上、整寬分隔線、表格在下 -->
      <div class="card-pad history-table-card">
        <div class="list-filter">
          <div class="filter-bar">
            <div class="fb-group">
              <div class="fb-field" style="flex:2 1 220px;">
                <label class="fb-label">{{ t('common.keyword') }}</label>
                <n-input
                  v-model:value="keyword"
                  clearable
                  :placeholder="t('reviewHistory.searchPlaceholder')"
                  @keyup.enter="onSearch">
                  <template #prefix><app-icon name="search" :size="16" /></template>
                </n-input>
              </div>
              <div class="fb-field" style="flex:1 1 140px;">
                <label class="fb-label">{{ t('reviewHistory.resultLabel') }}</label>
                <n-select
                  v-model:value="statusFilter"
                  :options="statusOptions"
                  @update:value="onStatusChange" />
              </div>
              <n-button class="fb-search-btn" type="primary" :loading="loading" @click="onSearch">
                <template #icon><app-icon name="search" :size="16" /></template>
                {{ t('common.search') }}
              </n-button>
            </div>
          </div>
        </div>

        <!-- 紀錄表格：即使無資料仍顯示表頭，空狀態以 tbody 整列呈現 -->
        <div class="history-table-wrap">
          <table class="tbl history-table">
            <thead>
              <tr>
                <th v-for="col in columns" :key="col.key" :class="{ 'hide-sm': col.hideSm }">
                  <span>{{ t(col.labelKey) }}</span>
                </th>
                <th style="width:120px; text-align:right;">{{ t('reviewHistory.colResult') }}</th>
              </tr>
            </thead>
            <tbody>
              <!-- 無符合紀錄：整列佔滿全部欄位 -->
              <tr v-if="!loading && !visible.length">
                <td :colspan="columns.length + 1" style="text-align:center; padding:48px 24px;">
                  <span class="kpi-ic" style="background:var(--c-violet); margin:0 auto 14px;"><app-icon name="receipt" :size="22" /></span>
                  <div style="font-weight:700; font-size:15px;">
                    {{ hasFilter ? t('reviewHistory.emptyFilteredTitle') : t('reviewHistory.emptyTitle') }}
                  </div>
                  <div style="font-size:13px; color:var(--text-faint); margin-top:4px;">
                    {{ hasFilter ? t('reviewHistory.emptyFilteredDesc') : t('reviewHistory.emptyDesc') }}
                  </div>
                </td>
              </tr>
              <tr v-for="a in visible" v-else :key="a.id">
                <td>
                  <div class="prod-cell">
                    <span class="history-rank">{{ initial(a.email) }}</span>
                    <div style="min-width:0;">
                      <div class="pc-title">{{ a.storeName }}</div>
                      <div class="pc-meta">{{ a.reviewComment ? t('reviewHistory.rejectReasonPrefix', { reason: a.reviewComment }) : t('reviewHistory.reviewed') }}</div>
                    </div>
                  </div>
                </td>
                <td>
                  <span class="history-mono">{{ a.storeSlug }}.openjam.co</span>
                </td>
                <td class="hide-sm">
                  <span class="history-mono">{{ a.email }}</span>
                </td>
                <td class="hide-sm">
                  <span class="history-mono">{{ fmtDate(a.createdAt) }}</span>
                </td>
                <td class="hide-sm">
                  <span class="history-mono">{{ fmtDate(a.reviewedAt) }}</span>
                </td>
                <td style="text-align:right;">
                  <n-tag :type="resultOf(a.status).type" size="small" round>{{ t(resultOf(a.status).labelKey) }}</n-tag>
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
.list-filter :deep(.n-input-wrapper),
.list-filter :deep(.n-base-selection),
.list-filter :deep(.n-base-selection-label) {
  border-radius: 10px;
}

.list-filter :deep(.n-input__border),
.list-filter :deep(.n-input__state-border),
.list-filter :deep(.n-base-selection__border),
.list-filter :deep(.n-base-selection__state-border) {
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

.history-table-wrap {
  overflow-x: auto;
  padding: 8px 8px 4px;
}

.history-table {
  min-width: 940px;
}

.history-pager {
  display: flex;
  justify-content: flex-end;
  padding: 12px 8px;
}

.history-table thead th {
  font-size: 12.5px;
  padding-top: 12px;
  vertical-align: middle;
}

.history-table-card {
  padding: 0;
  border-radius: 10px;
  overflow: hidden;
}

.history-table thead th + th {
  border-left: 1.5px solid var(--border);
}

.history-table tbody td + td {
  border-left: 1.5px solid var(--border);
}

.history-rank {
  width: 30px;
  height: 30px;
  border-radius: 10px;
  display: grid;
  place-items: center;
  flex: none;
  background: var(--oj-primary-wash);
  color: var(--oj-primary);
  font-size: 12px;
  font-weight: 800;
}

.history-mono {
  font-family: var(--oj-mono);
  color: var(--text-soft);
}
</style>
