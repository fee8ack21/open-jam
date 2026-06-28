<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { storeToRefs } from 'pinia'
import { useStoreReviewStore } from '@/stores/storeReview'
import { StoreApplicationStatus } from '@/api/store-service'

const { t, locale } = useI18n()
const store = useStoreReviewStore()
const { history, loading } = storeToRefs(store)

// 審核結果 → 顯示用標籤
const RESULT = {
  [StoreApplicationStatus.Approved]: { labelKey: 'appStatus.approved', type: 'success' as const },
  [StoreApplicationStatus.Rejected]: { labelKey: 'appStatus.rejected', type: 'error' as const },
}
function resultOf(s?: StoreApplicationStatus) {
  return (s != null && RESULT[s as keyof typeof RESULT]) || { labelKey: 'appStatus.unknown', type: 'default' as const }
}

// ── 篩選 / 排序狀態 ──────────────────────────────────────────
const keyword = ref('')
/** 審核結果篩選：all | Approved | Rejected */
const statusFilter = ref<'all' | StoreApplicationStatus>('all')
/** 排序欄位 + 方向 */
const sortKey = ref<'reviewedAt' | 'createdAt' | 'storeName' | 'storeSlug' | 'email'>('reviewedAt')
const sortDesc = ref(true)

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

function toggleSort(key: typeof sortKey.value) {
  if (sortKey.value === key) {
    sortDesc.value = !sortDesc.value
    return
  }
  sortKey.value = key
  sortDesc.value = key === 'reviewedAt' || key === 'createdAt'
}

/** 套用關鍵字、結果篩選與排序後的清單。 */
const visible = computed(() => {
  const q = keyword.value.trim().toLowerCase()
  let list = history.value.slice()

  if (statusFilter.value !== 'all') {
    list = list.filter((a) => a.status === statusFilter.value)
  }
  if (q) {
    list = list.filter((a) =>
      [a.storeName, a.storeSlug, a.email]
        .some((f) => (f ?? '').toLowerCase().includes(q)),
    )
  }

  const dir = sortDesc.value ? -1 : 1
  list.sort((a, b) => {
    const key = sortKey.value
    const av = (a[key] ?? '') as string
    const bv = (b[key] ?? '') as string
    if (key === 'storeName') return av.localeCompare(bv, 'zh-Hant') * dir
    // 時間欄位以 ISO 字串可直接字典序比較
    return (av < bv ? -1 : av > bv ? 1 : 0) * dir
  })
  return list
})

onMounted(store.load)
</script>

<template>
  <div :data-screen-label="t('route.reviewHistory')">
    <div class="page-head">
      <div>
        <p class="h-eyebrow">{{ t('sidebar.platformAdmin') }}</p>
        <h1 class="h-title">{{ t('route.reviewHistory') }}</h1>
        <p class="h-sub">{{ t('reviewHistory.subStats', { count: history.length }) }}</p>
      </div>
    </div>

    <!-- 篩選 / 排序工具列 -->
    <div class="card-pad history-toolbar">
      <div class="filter-bar">
        <div class="fb-group">
          <div class="fb-field" style="flex:2 1 220px;">
            <label class="fb-label">{{ t('common.keyword') }}</label>
            <n-input
              v-model:value="keyword"
              clearable
              :placeholder="t('reviewHistory.searchPlaceholder')">
              <template #prefix><app-icon name="search" :size="16" /></template>
            </n-input>
          </div>
          <div class="fb-field" style="flex:1 1 140px;">
            <label class="fb-label">{{ t('reviewHistory.resultLabel') }}</label>
            <n-select
              v-model:value="statusFilter"
              :options="statusOptions" />
          </div>
        </div>
      </div>
    </div>

    <n-spin :show="loading">
      <!-- 紀錄表格：即使無資料仍顯示表頭，空狀態以 tbody 整列呈現 -->
      <div class="card-pad history-table-card" style="padding:8px 8px 4px;">
        <div class="history-table-wrap">
          <table class="tbl history-table">
            <thead>
              <tr>
                <th v-for="col in columns" :key="col.key" :class="{ 'hide-sm': col.hideSm }">
                  <button class="sort-head" type="button" @click="toggleSort(col.key)">
                    <span>{{ t(col.labelKey) }}</span>
                    <app-icon
                      v-if="sortKey === col.key"
                      :name="sortDesc ? 'chevronD' : 'chevronU'"
                      :size="15" />
                  </button>
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
                    {{ history.length ? t('reviewHistory.emptyFilteredTitle') : t('reviewHistory.emptyTitle') }}
                  </div>
                  <div style="font-size:13px; color:var(--text-faint); margin-top:4px;">
                    {{ history.length ? t('reviewHistory.emptyFilteredDesc') : t('reviewHistory.emptyDesc') }}
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
    </n-spin>
  </div>
</template>

<style scoped>
.history-toolbar {
  margin-bottom: 16px;
  border-radius: 10px;
}

.history-toolbar :deep(.n-input),
.history-toolbar :deep(.n-input-wrapper),
.history-toolbar :deep(.n-base-selection),
.history-toolbar :deep(.n-base-selection-label) {
  border-radius: 10px;
}

.history-toolbar :deep(.n-input__border),
.history-toolbar :deep(.n-input__state-border),
.history-toolbar :deep(.n-base-selection__border),
.history-toolbar :deep(.n-base-selection__state-border) {
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

.history-table-wrap {
  overflow-x: auto;
}

.history-table {
  min-width: 940px;
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

.history-table thead th {
  font-size: 12.5px;
  padding-top: 12px;
  vertical-align: middle;
}

.history-table-card {
  border-radius: 10px;
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
