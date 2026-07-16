<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useMessage } from 'naive-ui'
import { useI18n } from 'vue-i18n'
import { storeToRefs } from 'pinia'
import { useStoreListStore } from '@/stores/storeList'
import { StoreStatus } from '@/api/store-service'
import { JFmt } from '@/utils/format'

const { t, locale } = useI18n()
const router = useRouter()
const message = useMessage()
const store = useStoreListStore()
const { items, totalCount, loading } = storeToRefs(store)

// 每筆商店的處理中狀態（避免重複點擊）
const busyId = ref<string | null>(null)

// 商店狀態 → 顯示用標籤
const STATUS = {
  [StoreStatus.Active]: { labelKey: 'storeStatus.active', type: 'success' as const },
  [StoreStatus.Suspended]: { labelKey: 'storeStatus.suspended', type: 'warning' as const },
  [StoreStatus.Closed]: { labelKey: 'storeStatus.closed', type: 'default' as const },
}
function statusOf(s?: StoreStatus) {
  return (s != null && STATUS[s as keyof typeof STATUS]) || { labelKey: 'appStatus.unknown', type: 'default' as const }
}

// ── 篩選狀態（伺服器端分頁）──────────────────────────────────
const keyword = ref('')
/** 狀態篩選：all | Active | Suspended | Closed */
const statusFilter = ref<'all' | StoreStatus>('all')
const page = ref(1)

const statusOptions = computed(() => [
  { label: t('stores.filterAll'), value: 'all' },
  { label: t('storeStatus.active'), value: StoreStatus.Active },
  { label: t('storeStatus.suspended'), value: StoreStatus.Suspended },
  { label: t('storeStatus.closed'), value: StoreStatus.Closed },
])
const columns = [
  { key: 'storeName', labelKey: 'review.colStoreName', hideSm: false },
  { key: 'storeSlug', labelKey: 'review.colSubdomain', hideSm: false },
  { key: 'createdAt', labelKey: 'orders.colCreatedAt', hideSm: true },
  { key: 'updatedAt', labelKey: 'stores.colUpdatedAt', hideSm: true },
] as const

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / store.pageSize)))
const hasFilter = computed(() => keyword.value.trim() !== '' || statusFilter.value !== 'all')

async function applyFilter() {
  page.value = 1
  await store.applyFilter({ search: keyword.value, status: statusFilter.value === 'all' ? null : statusFilter.value })
}
// 關鍵字改由「搜尋」按鈕 / Enter 觸發；下拉狀態維持即時套用
watch(statusFilter, () => { applyFilter() })
async function changePage(p: number) { page.value = p; await store.goPage(p) }
async function changePageSize(size: number) { page.value = 1; await store.setPageSize(size) }

function fmtDate(v?: string | null) {
  return v ? new Date(v).toLocaleString(locale.value, { hour12: false }) : '—'
}

/** 進入該商店的商品列表。 */
function openProducts(id?: string) {
  if (!id) return
  router.push({ name: 'store-products', params: { id } })
}

async function onSuspend(id?: string) {
  if (!id) return
  busyId.value = id
  const ok = await store.suspend(id)
  busyId.value = null
  if (ok) message.success(t('stores.msgSuspended'))
  else message.error(store.error ?? t('stores.msgSuspendFailed'))
}

async function onUnsuspend(id?: string) {
  if (!id) return
  busyId.value = id
  const ok = await store.unsuspend(id)
  busyId.value = null
  if (ok) message.success(t('stores.msgUnsuspended'))
  else message.error(store.error ?? t('stores.msgUnsuspendFailed'))
}

async function onClose(id?: string) {
  if (!id) return
  busyId.value = id
  const ok = await store.close(id)
  busyId.value = null
  if (ok) message.success(t('stores.msgClosed'))
  else message.error(store.error ?? t('stores.msgCloseFailed'))
}

onMounted(store.load)
</script>

<template>
  <div :data-screen-label="t('route.stores')">

    <n-spin :show="loading">
      <!-- 篩選列與商店表格合併為單一卡片：篩選在上、整寬分隔線、表格在下 -->
      <div class="card-pad store-table-card">
        <div class="list-filter">
          <div class="filter-bar">
            <div class="fb-group">
              <div class="fb-field" style="flex:2 1 220px;">
                <label class="fb-label">{{ t('common.keyword') }}</label>
                <n-input
                  v-model:value="keyword"
                  clearable
                  :placeholder="t('stores.searchPlaceholder')"
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

        <!-- 商店表格：即使無資料仍顯示表頭，空狀態以 tbody 整列呈現 -->
        <div class="store-table-wrap">
          <table class="tbl store-table">
            <thead>
              <tr>
                <th v-for="col in columns" :key="col.key" :class="{ 'hide-sm': col.hideSm }">
                  <span>{{ t(col.labelKey) }}</span>
                </th>
                <th class="hide-sm">{{ t('common.status') }}</th>
                <th style="width:190px; text-align:right;">{{ t('stores.colActions') }}</th>
              </tr>
            </thead>
            <tbody>
              <!-- 無符合商店：整列佔滿全部欄位 -->
              <tr v-if="!loading && !items.length">
                <td :colspan="columns.length + 2" style="text-align:center; padding:48px 24px;">
                  <span class="kpi-ic" style="background:var(--c-violet); margin:0 auto 14px;"><app-icon name="home" :size="22" /></span>
                  <div style="font-weight:700; font-size:15px;">
                    {{ hasFilter ? t('stores.emptyFilteredTitle') : t('stores.emptyTitle') }}
                  </div>
                  <div style="font-size:13px; color:var(--text-faint); margin-top:4px;">
                    {{ hasFilter ? t('stores.emptyFilteredDesc') : t('stores.emptyDesc') }}
                  </div>
                </td>
              </tr>
              <tr v-for="s in items" v-else :key="s.id">
                <td>
                  <div class="store-cell">
                    <div class="store-avatar"
                         :style="s.avatarUrl ? { backgroundImage: `url(${s.avatarUrl})` } : {}">
                      <span v-if="!s.avatarUrl">{{ JFmt.initials(s.storeName || 'S') }}</span>
                    </div>
                    <button class="store-name-button" @click="openProducts(s.id)">{{ s.storeName }}</button>
                  </div>
                </td>
                <td>
                  <span class="store-mono">{{ s.storeSlug }}.openjam.co</span>
                </td>
                <td class="hide-sm">
                  <span class="store-mono">{{ fmtDate(s.createdAt) }}</span>
                </td>
                <td class="hide-sm">
                  <span class="store-mono">{{ fmtDate(s.updatedAt) }}</span>
                </td>
                <td class="hide-sm">
                  <n-tag :type="statusOf(s.status).type" size="small" round>{{ t(statusOf(s.status).labelKey) }}</n-tag>
                </td>
                <td>
                  <div class="row-actions store-actions">
                    <!-- 已關閉為終態，不提供操作 -->
                    <template v-if="s.status === StoreStatus.Closed">
                      <span class="store-closed-hint">{{ t('storeStatus.closed') }}</span>
                    </template>
                    <template v-else>
                      <!-- 停權 / 解除停權 -->
                      <n-popconfirm
                        v-if="s.status === StoreStatus.Active"
                        @positive-click="onSuspend(s.id)">
                        <template #trigger>
                          <n-button size="small" tertiary :disabled="busyId === s.id">
                            <template #icon><app-icon name="lock" :size="15" /></template>
                            {{ t('stores.suspend') }}
                          </n-button>
                        </template>
                        {{ t('stores.suspendConfirm') }}
                      </n-popconfirm>
                      <n-button
                        v-else
                        size="small"
                        tertiary
                        :disabled="busyId === s.id"
                        :loading="busyId === s.id"
                        @click="onUnsuspend(s.id)">
                        <template #icon><app-icon name="refresh" :size="15" /></template>
                        {{ t('stores.unsuspend') }}
                      </n-button>

                      <!-- 關閉（終態不可逆） -->
                      <n-popconfirm @positive-click="onClose(s.id)">
                        <template #trigger>
                          <n-button size="small" type="error" :disabled="busyId === s.id">
                            <template #icon><app-icon name="close" :size="15" /></template>
                            {{ t('stores.close') }}
                          </n-button>
                        </template>
                        {{ t('stores.closeConfirm') }}
                      </n-popconfirm>
                    </template>
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
            :page-size="store.pageSize"
            @update:page="changePage"
            @update:page-size="changePageSize" />
        </div>
      </div>
    </n-spin>
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

.store-table-wrap {
  overflow-x: auto;
  padding: 0 10px;
}

.store-table {
  min-width: 940px;
}

/* 各欄位一律不折行（子網域 / 建立時間 / 最後更新等），寬度不足時由外層容器水平捲動 */
.store-table th,
.store-table td {
  white-space: nowrap;
}

.history-pager {
  display: flex;
  justify-content: flex-end;
  padding: 14px 20px;
}

.store-table thead th {
  font-size: 12.5px;
  padding-top: 12px;
  vertical-align: middle;
}

.store-table-card {
  padding: 0;
  border-radius: var(--r-lg);
  overflow: hidden;
}



.store-mono {
  font-family: var(--oj-mono);
  color: var(--text-soft);
}

/* 商店名稱欄：頭像 + 名稱並排 */
.store-cell {
  display: flex;
  align-items: center;
  gap: 10px;
  min-width: 0;
}

/* 列表用小頭像（同 StoreSettingsView st-avatar 語彙，縮小版）；無圖以縮寫字補位 */
.store-avatar {
  box-sizing: border-box;
  width: 34px; height: 34px;
  flex: 0 0 auto;
  border-radius: 50%;
  background-size: cover;
  background-position: center;
  background-color: var(--c-violet);
  border: var(--bw) solid var(--border-strong);
  display: grid; place-items: center;
  color: #fff;
  font-family: var(--oj-display);
  font-weight: 700;
  font-size: 13px;
}

.store-name-button {
  border: 0;
  background: transparent;
  color: var(--oj-primary);
  cursor: pointer;
  font-family: var(--oj-display);
  font-size: 14.5px;
  font-weight: 700;
  line-height: 1.25;
  padding: 0;
  text-align: left;
}

.store-name-button:hover {
  color: var(--c-pink-deep);
}

.store-closed-hint {
  font-size: 12.5px;
  color: var(--text-faint);
}
</style>
