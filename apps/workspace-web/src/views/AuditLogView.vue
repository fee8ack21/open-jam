<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { storeToRefs } from 'pinia'
import { useAuditLogStore } from '@/stores/auditLog'
import type { AuditLogDto } from '@/api/log-service'

const { t, locale } = useI18n()
const store = useAuditLogStore()
// 僅 ref / computed / getter 可由 storeToRefs 取出；pageSize 為常數純值，直接讀 store。
const { items, totalCount, loading } = storeToRefs(store)

// ── 篩選狀態（即時生效，以 debounce 收斂逐字輸入的 API 呼叫）─────
const actionFilter = ref('')
const targetFilter = ref('')
const page = ref(1)

const columns = [
  { key: 'occurredAt', labelKey: 'auditLog.colOccurredAt', hideSm: false },
  { key: 'action', labelKey: 'auditLog.colAction', hideSm: false },
  { key: 'target', labelKey: 'auditLog.colTarget', hideSm: true },
  { key: 'who', labelKey: 'auditLog.colWho', hideSm: true },
  { key: 'ip', labelKey: 'auditLog.colIp', hideSm: true },
] as const

function fmtDate(v?: string | null) {
  return v ? new Date(v).toLocaleString(locale.value, { hour12: false }) : '—'
}
/** 將 GUID 縮短顯示（前 8 碼），完整值以 title 提示。 */
function shortId(v?: string | null) {
  return v ? v.slice(0, 8) : '—'
}
/** 動作結果 → 標籤樣式。success 綠、failure 紅、其餘灰。 */
function resultTag(result?: string | null) {
  const r = (result ?? '').toLowerCase()
  if (r === 'success') return { label: t('auditLog.resultSuccess'), type: 'success' as const }
  if (r === 'failure') return { label: t('auditLog.resultFailure'), type: 'error' as const }
  return { label: result || '—', type: 'default' as const }
}

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / store.pageSize)))

async function applyFilter() {
  page.value = 1
  await store.applyFilter({ action: actionFilter.value, target: targetFilter.value })
}
// 篩選改由「搜尋」按鈕 / Enter 觸發（不再逐字自動打 API）
async function changePage(p: number) {
  page.value = p
  await store.goPage(p)
}

async function changePageSize(size: number) {
  page.value = 1
  await store.setPageSize(size)
}

// ── 明細檢視 ────────────────────────────────────────────────
const detail = ref<AuditLogDto | null>(null)
const detailOpen = ref(false)
function openDetail(row: AuditLogDto) {
  detail.value = row
  detailOpen.value = true
}
/** 美化 before/after 的 JSON 字串；非 JSON 則原樣回傳。 */
function prettyJson(v?: string | null) {
  if (!v) return '—'
  try {
    return JSON.stringify(JSON.parse(v), null, 2)
  } catch {
    return v
  }
}

onMounted(store.load)
</script>

<template>
  <div :data-screen-label="t('route.auditLog')">

    <n-spin :show="loading">
      <!-- 篩選列與日誌表格合併為單一卡片：篩選在上、整寬分隔線、表格在下 -->
      <div class="card-pad history-table-card">
        <div class="list-filter">
          <div class="filter-bar">
            <div class="fb-group">
              <div class="fb-field" style="flex:1 1 220px;">
                <label class="fb-label">{{ t('auditLog.actionLabel') }}</label>
                <n-input
                  v-model:value="actionFilter"
                  clearable
                  :placeholder="t('auditLog.actionPlaceholder')"
                  @keyup.enter="applyFilter">
                  <template #prefix><app-icon name="search" :size="16" /></template>
                </n-input>
              </div>
              <div class="fb-field" style="flex:1 1 180px;">
                <label class="fb-label">{{ t('auditLog.targetLabel') }}</label>
                <n-input
                  v-model:value="targetFilter"
                  clearable
                  :placeholder="t('auditLog.targetPlaceholder')"
                  @keyup.enter="applyFilter">
                  <template #prefix><app-icon name="tag" :size="16" /></template>
                </n-input>
              </div>
              <n-button class="fb-search-btn" type="primary" :loading="loading" @click="applyFilter">
                <template #icon><app-icon name="search" :size="16" /></template>
                {{ t('common.search') }}
              </n-button>
            </div>
          </div>
        </div>

        <!-- 日誌表格：即使無資料仍顯示表頭，空狀態以 tbody 整列呈現 -->
        <div class="history-table-wrap">
          <table class="tbl history-table">
            <thead>
              <tr>
                <th v-for="col in columns" :key="col.key" :class="{ 'hide-sm': col.hideSm }">{{ t(col.labelKey) }}</th>
                <th style="width:90px; text-align:center;">{{ t('auditLog.colResult') }}</th>
                <th style="width:64px; text-align:right;">{{ t('auditLog.colDetail') }}</th>
              </tr>
            </thead>
            <tbody>
              <!-- 無紀錄：整列佔滿全部欄位 -->
              <tr v-if="!items.length">
                <td :colspan="columns.length + 2" style="text-align:center; padding:48px 24px;">
                  <span class="kpi-ic" style="background:var(--c-violet); margin:0 auto 14px;"><app-icon name="note" :size="22" /></span>
                  <div style="font-weight:700; font-size:15px;">{{ t('auditLog.emptyTitle') }}</div>
                  <div style="font-size:13px; color:var(--text-faint); margin-top:4px;">
                    {{ t('auditLog.emptyDesc') }}
                  </div>
                </td>
              </tr>
              <tr v-for="a in items" :key="a.id">
                <td><span class="history-mono">{{ fmtDate(a.occurredAt) }}</span></td>
                <td><span class="audit-action">{{ a.action || '—' }}</span></td>
                <td class="hide-sm">
                  <div style="min-width:0;">
                    <div class="pc-title">{{ a.target || '—' }}</div>
                    <div v-if="a.targetId" class="pc-meta history-mono" :title="a.targetId">{{ shortId(a.targetId) }}</div>
                  </div>
                </td>
                <td class="hide-sm">
                  <span class="history-mono" :title="a.who ?? t('auditLog.system')">{{ a.who ? shortId(a.who) : t('auditLog.system') }}</span>
                </td>
                <td class="hide-sm"><span class="history-mono">{{ a.ip || '—' }}</span></td>
                <td style="text-align:center;">
                  <n-tag :type="resultTag(a.result).type" size="small" round>{{ resultTag(a.result).label }}</n-tag>
                </td>
                <td style="text-align:right;">
                  <n-button text @click="openDetail(a)"><app-icon name="eye" :size="18" /></n-button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <div class="audit-pager">
          <list-pager
            :page="page"
            :page-count="totalPages"
            :page-size="store.pageSize"
            @update:page="changePage"
            @update:page-size="changePageSize" />
        </div>
      </div>
    </n-spin>

    <!-- 明細彈窗：完整欄位與 before/after JSON -->
    <n-modal v-model:show="detailOpen" preset="card" :title="t('auditLog.detailTitle')" style="max-width:680px;">
      <div v-if="detail" class="audit-detail">
        <dl class="audit-dl">
          <div><dt>{{ t('auditLog.colAction') }}</dt><dd class="audit-action">{{ detail.action || '—' }}</dd></div>
          <div><dt>{{ t('auditLog.colResult') }}</dt><dd><n-tag :type="resultTag(detail.result).type" size="small" round>{{ resultTag(detail.result).label }}</n-tag></dd></div>
          <div><dt>{{ t('auditLog.colTarget') }}</dt><dd>{{ detail.target || '—' }}</dd></div>
          <div><dt>{{ t('auditLog.targetId') }}</dt><dd class="history-mono">{{ detail.targetId || '—' }}</dd></div>
          <div><dt>{{ t('auditLog.colWho') }}</dt><dd class="history-mono">{{ detail.who || t('auditLog.system') }}</dd></div>
          <div><dt>{{ t('auditLog.tenant') }}</dt><dd class="history-mono">{{ detail.tenant || '—' }}</dd></div>
          <div><dt>{{ t('auditLog.colIp') }}</dt><dd class="history-mono">{{ detail.ip || '—' }}</dd></div>
          <div><dt>{{ t('auditLog.colOccurredAt') }}</dt><dd>{{ fmtDate(detail.occurredAt) }}</dd></div>
          <div><dt>{{ t('auditLog.correlationId') }}</dt><dd class="history-mono">{{ detail.correlationId || '—' }}</dd></div>
        </dl>
        <div class="audit-json-group">
          <div>
            <div class="audit-json-label">{{ t('auditLog.beforeLabel') }}</div>
            <pre class="audit-json">{{ prettyJson(detail.before) }}</pre>
          </div>
          <div>
            <div class="audit-json-label">{{ t('auditLog.afterLabel') }}</div>
            <pre class="audit-json">{{ prettyJson(detail.after) }}</pre>
          </div>
        </div>
      </div>
    </n-modal>
  </div>
</template>

<style scoped>
/* 篩選區段：卡片頂部，底部整寬分隔線與表格分開 */
.list-filter {
  padding: 18px 20px;
  border-bottom: var(--bw) solid var(--border-strong);
  background: var(--bg);
}



/* 篩選列：欄位並排撐滿；不足時換行 */
.filter-bar {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  align-items: flex-end;
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

.history-table-wrap {
  overflow-x: auto;
  padding: 0 10px;
}

.history-table {
  min-width: 880px;
}

.history-table thead th {
  font-size: 12.5px;
  padding-top: 12px;
  vertical-align: middle;
}

.history-table-card {
  padding: 0;
  border-radius: var(--r-lg);
  overflow: hidden;
}



.history-mono {
  font-family: var(--oj-mono);
  color: var(--text-soft);
}

.audit-action {
  font-family: var(--oj-mono);
  font-size: 12.5px;
  font-weight: 700;
  color: var(--oj-primary);
}

.audit-pager {
  display: flex;
  justify-content: flex-end;
  padding: 14px 20px;
}

.audit-dl {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 10px 24px;
  margin: 0 0 18px;
}

.audit-dl > div {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}

.audit-dl dt {
  font-size: 11.5px;
  color: var(--text-faint);
  font-weight: 700;
}

.audit-dl dd {
  margin: 0;
  font-size: 13px;
  word-break: break-all;
}

.audit-json-group {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.audit-json-label {
  font-size: 11.5px;
  color: var(--text-faint);
  font-weight: 700;
  margin-bottom: 4px;
}

.audit-json {
  margin: 0;
  padding: 10px 12px;
  border-radius: 10px;
  border: 1px solid var(--border);
  background: var(--bg);
  font-family: var(--oj-mono);
  font-size: 12px;
  line-height: 1.5;
  white-space: pre-wrap;
  word-break: break-all;
  max-height: 240px;
  overflow: auto;
}

@media (max-width: 600px) {
  .audit-dl,
  .audit-json-group {
    grid-template-columns: 1fr;
  }
}
</style>
