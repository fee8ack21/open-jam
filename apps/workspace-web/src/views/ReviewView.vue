<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useMessage } from 'naive-ui'
import { useI18n } from 'vue-i18n'
import { storeToRefs } from 'pinia'
import { useStoreReviewStore } from '@/stores/storeReview'

const { t, locale } = useI18n()
const message = useMessage()
const store = useStoreReviewStore()
const { items, pendingTotal, loading } = storeToRefs(store)

// 每筆申請的處理中狀態（避免重複點擊）與駁回意見草稿
const busyId = ref<string | null>(null)
const rejectComment = ref<Record<string, string>>({})
const page = ref(1)
const keyword = ref('')
const appliedKeyword = ref('') // 已套用的關鍵字（按下搜尋才更新）

// 依已套用關鍵字過濾目前頁次的申請
const visible = computed(() => {
  const q = appliedKeyword.value.toLowerCase()
  if (!q) return items.value
  return items.value.filter((a) =>
    [a.storeName, a.storeSlug, a.email].some((f) => (f ?? '').toLowerCase().includes(q)),
  )
})

// 按下搜尋：套用目前關鍵字並重新載入列表
async function onSearch() {
  appliedKeyword.value = keyword.value.trim()
  page.value = 1
  await store.load()
}

const columns = [
  { key: 'storeName', labelKey: 'review.colStoreName', hideSm: false },
  { key: 'storeSlug', labelKey: 'review.colSubdomain', hideSm: false },
  { key: 'email', labelKey: 'review.colEmail', hideSm: true },
  { key: 'createdAt', labelKey: 'review.colCreatedAt', hideSm: true },
] as const

const totalPages = computed(() => Math.max(1, Math.ceil(pendingTotal.value / store.pageSize)))
async function changePage(p: number) { page.value = p; await store.goPendingPage(p) }

function fmtDate(v?: string | null) {
  return v ? new Date(v).toLocaleString(locale.value, { hour12: false }) : '—'
}

async function onApprove(id?: string) {
  if (!id) return
  busyId.value = id
  const ok = await store.approve(id)
  busyId.value = null
  if (ok) message.success(t('review.msgApproved'))
  else message.error(store.error ?? t('review.msgApproveFailed'))
}

async function onReject(id?: string) {
  if (!id) return
  const comment = (rejectComment.value[id] ?? '').trim()
  if (!comment) {
    message.warning(t('review.msgNeedReason'))
    return
  }
  busyId.value = id
  const ok = await store.reject(id, comment)
  busyId.value = null
  if (ok) {
    message.success(t('review.msgRejected'))
    delete rejectComment.value[id]
  } else {
    message.error(store.error ?? t('review.msgRejectFailed'))
  }
}

onMounted(store.load)
</script>

<template>
  <div :data-screen-label="t('route.review')">

    <n-spin :show="loading">
      <!-- 篩選列與待審核表格合併為單一卡片：篩選在上、整寬分隔線、表格在下 -->
      <div class="card-pad review-card">
        <div class="review-filter">
          <div class="filter-bar">
            <div class="fb-group">
              <div class="fb-field fb-keyword">
                <label class="fb-label">{{ t('common.keyword') }}</label>
                <n-input
                  v-model:value="keyword"
                  clearable
                  :placeholder="t('review.searchPlaceholder')"
                  @keyup.enter="onSearch">
                  <template #prefix><app-icon name="search" :size="16" /></template>
                </n-input>
              </div>
              <n-button class="fb-search-btn" type="primary" :loading="loading" @click="onSearch">
                <template #icon><app-icon name="search" :size="16" /></template>
                {{ t('common.search') }}
              </n-button>
            </div>
          </div>
        </div>

        <!-- 待審核表格：即使無資料仍顯示表頭，空狀態以 tbody 整列呈現 -->
        <div class="review-table-wrap">
          <table class="tbl review-table">
            <thead>
              <tr>
                <th v-for="col in columns" :key="col.key" :class="{ 'hide-sm': col.hideSm }">
                  <span>{{ t(col.labelKey) }}</span>
                </th>
                <th class="hide-sm">{{ t('common.status') }}</th>
                <th style="width:170px; text-align:right;">{{ t('review.colActions') }}</th>
              </tr>
            </thead>
            <tbody>
              <!-- 無待審核申請：整列佔滿全部欄位 -->
              <tr v-if="!loading && !visible.length">
                <td :colspan="columns.length + 2" style="text-align:center; padding:48px 24px;">
                  <span class="kpi-ic" style="background:var(--c-violet); margin:0 auto 14px;"><app-icon name="shield" :size="22" /></span>
                  <div style="font-weight:700; font-size:15px;">
                    {{ appliedKeyword ? t('review.emptyFilteredTitle') : t('review.emptyTitle') }}
                  </div>
                  <div style="font-size:13px; color:var(--text-faint); margin-top:4px;">
                    {{ appliedKeyword ? t('review.emptyFilteredDesc') : t('review.emptyDesc') }}
                  </div>
                </td>
              </tr>
              <tr v-for="a in visible" v-else :key="a.id">
                <td>
                  <div class="prod-cell">
                    <span class="review-rank">{{ t('review.rankGlyph') }}</span>
                    <div style="min-width:0;">
                      <div class="pc-title">{{ a.storeName }}</div>
                      <div class="pc-meta">{{ t('review.pendingLabel') }}</div>
                    </div>
                  </div>
                </td>
                <td>
                  <span class="review-mono">{{ a.storeSlug }}.openjam.co</span>
                </td>
                <td class="hide-sm">
                  <span class="review-mono">{{ a.email }}</span>
                </td>
                <td class="hide-sm">
                  <span class="review-mono">{{ fmtDate(a.createdAt) }}</span>
                </td>
                <td class="hide-sm">
                  <n-tag type="warning" size="small" round>{{ t('appStatus.pending') }}</n-tag>
                </td>
                <td>
                  <div class="row-actions review-actions">
                    <!-- 駁回（需填原因） -->
                    <n-popover trigger="click" placement="bottom-end" :show-arrow="false" to=".oj-root">
                      <template #trigger>
                        <n-button size="small" tertiary :disabled="busyId === a.id">
                          <template #icon><app-icon name="close" :size="15" /></template>
                          {{ t('review.reject') }}
                        </n-button>
                      </template>
                      <div style="display:grid; gap:10px; width:280px; padding:4px;">
                        <div style="font-weight:600; font-size:13px;">{{ t('review.rejectReason') }}</div>
                        <n-input
                          v-model:value="rejectComment[a.id!]"
                          type="textarea"
                          :rows="3"
                          maxlength="500"
                          show-count
                          :placeholder="t('review.rejectReasonPlaceholder')" />
                        <div style="display:flex; justify-content:flex-end;">
                          <n-button size="small" type="error" :loading="busyId === a.id" @click="onReject(a.id)">
                            {{ t('review.confirmReject') }}
                          </n-button>
                        </div>
                      </div>
                    </n-popover>

                    <!-- 核准 -->
                    <n-popconfirm @positive-click="onApprove(a.id)">
                      <template #trigger>
                        <n-button size="small" type="primary" :disabled="busyId === a.id" :loading="busyId === a.id">
                          <template #icon><app-icon name="check" :size="15" /></template>
                          {{ t('review.approve') }}
                        </n-button>
                      </template>
                      {{ t('review.approveConfirm') }}
                    </n-popconfirm>
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
  </div>
</template>

<style scoped>
/* 單一卡片：外框由 .card-pad 提供，內距歸零由內部區段各自管理 */
.review-card {
  padding: 0;
  border-radius: 10px;
  overflow: hidden;
}

/* 篩選區段：卡片頂部，底部整寬分隔線與表格分開 */
.review-filter {
  padding: 16px 18px;
  border-bottom: 1.5px solid var(--border);
}

.review-filter :deep(.n-input),
.review-filter :deep(.n-input-wrapper) {
  border-radius: 10px;
}

.review-filter :deep(.n-input__border),
.review-filter :deep(.n-input__state-border) {
  border-radius: 10px;
}

/* 篩選列：僅單一關鍵字欄位，靠右對齊、不滿版 */
.filter-bar {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  align-items: center;
  justify-content: flex-end;
}

.fb-group {
  display: flex;
  gap: 12px;
  align-items: flex-end;
  flex: 1 1 auto;
  min-width: 0;
}

/* 關鍵字欄位滿版，撐滿按鈕以外的剩餘寬度 */
.fb-keyword {
  flex: 1 1 auto;
  min-width: 0;
}

/* 搜尋按鈕與輸入框同高、同圓角（Input heightMedium 於 App.vue 覆寫為 42px） */
.fb-search-btn {
  height: 42px;
  border-radius: 10px;
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

.review-table-wrap {
  overflow-x: auto;
  padding: 8px 8px 4px;
}

.review-table {
  min-width: 860px;
}

.history-pager {
  display: flex;
  justify-content: flex-end;
  padding: 12px 8px;
}

.review-table thead th {
  font-size: 12.5px;
  padding-top: 12px;
  vertical-align: middle;
}

.review-table thead th + th {
  border-left: 1.5px solid var(--border);
}

.review-table tbody td + td {
  border-left: 1.5px solid var(--border);
}

.review-rank {
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

.review-mono {
  font-family: var(--oj-mono);
  color: var(--text-soft);
}
</style>
