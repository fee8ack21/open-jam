<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { storeToRefs } from 'pinia'
import { useAuditLogStore } from '@/stores/auditLog'
import type { AuditLogDto } from '@/api/log-service'

const store = useAuditLogStore()
// 僅 ref / computed / getter 可由 storeToRefs 取出；pageSize 為常數純值，直接讀 store。
const { items, totalCount, loading } = storeToRefs(store)

// ── 篩選狀態（送出後才查詢，避免逐字打字打 API）─────────────
const actionFilter = ref('')
const targetFilter = ref('')
const page = ref(1)

const columns = [
  { key: 'occurredAt', label: '發生時間', hideSm: false },
  { key: 'action', label: '動作', hideSm: false },
  { key: 'target', label: '對象', hideSm: true },
  { key: 'who', label: '操作者', hideSm: true },
  { key: 'ip', label: 'IP', hideSm: true },
] as const

function fmtDate(v?: string | null) {
  return v ? new Date(v).toLocaleString('zh-TW', { hour12: false }) : '—'
}
/** 將 GUID 縮短顯示（前 8 碼），完整值以 title 提示。 */
function shortId(v?: string | null) {
  return v ? v.slice(0, 8) : '—'
}
/** 動作結果 → 標籤樣式。success 綠、failure 紅、其餘灰。 */
function resultTag(result?: string | null) {
  const r = (result ?? '').toLowerCase()
  if (r === 'success') return { label: '成功', type: 'success' as const }
  if (r === 'failure') return { label: '失敗', type: 'error' as const }
  return { label: result || '—', type: 'default' as const }
}

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / store.pageSize)))

async function applyFilter() {
  page.value = 1
  await store.applyFilter({ action: actionFilter.value, target: targetFilter.value })
}
async function resetFilter() {
  actionFilter.value = ''
  targetFilter.value = ''
  await applyFilter()
}
async function changePage(p: number) {
  page.value = p
  await store.goPage(p)
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
  <div data-screen-label="稽核日誌">
    <div class="page-head">
      <div>
        <p class="h-eyebrow">平台管理</p>
        <h1 class="h-title">稽核日誌</h1>
        <p class="h-sub">共 {{ totalCount }} 筆稽核事件</p>
      </div>
    </div>

    <!-- 篩選工具列 -->
    <div class="card-pad history-toolbar">
      <div class="filter-bar">
        <div class="fb-group">
          <div class="fb-field" style="flex:1 1 220px;">
            <label class="fb-label">動作類型</label>
            <n-input
              v-model:value="actionFilter"
              clearable
              placeholder="動作類型，如 auth.login.success"
              @keyup.enter="applyFilter">
              <template #prefix><app-icon name="search" :size="16" /></template>
            </n-input>
          </div>
          <div class="fb-field" style="flex:1 1 180px;">
            <label class="fb-label">對象類型</label>
            <n-input
              v-model:value="targetFilter"
              clearable
              placeholder="對象資源類型，如 User"
              @keyup.enter="applyFilter">
              <template #prefix><app-icon name="tag" :size="16" /></template>
            </n-input>
          </div>
        </div>
        <div class="fb-group fb-actions">
          <n-button type="primary" :loading="loading" @click="applyFilter">查詢</n-button>
          <n-button quaternary @click="resetFilter">重設</n-button>
        </div>
      </div>
    </div>

    <n-spin :show="loading">
      <!-- 日誌表格：即使無資料仍顯示表頭，空狀態以 tbody 整列呈現 -->
      <div class="card-pad history-table-card" style="padding:8px 8px 4px;">
        <div class="history-table-wrap">
          <table class="tbl history-table">
            <thead>
              <tr>
                <th v-for="col in columns" :key="col.key" :class="{ 'hide-sm': col.hideSm }">{{ col.label }}</th>
                <th style="width:90px; text-align:center;">結果</th>
                <th style="width:64px; text-align:right;">明細</th>
              </tr>
            </thead>
            <tbody>
              <!-- 無紀錄：整列佔滿全部欄位 -->
              <tr v-if="!items.length">
                <td :colspan="columns.length + 2" style="text-align:center; padding:48px 24px;">
                  <span class="kpi-ic" style="background:var(--c-violet); margin:0 auto 14px;"><app-icon name="note" :size="22" /></span>
                  <div style="font-weight:700; font-size:15px;">尚無稽核日誌</div>
                  <div style="font-size:13px; color:var(--text-faint); margin-top:4px;">
                    沒有符合條件的稽核事件，試試調整或重設篩選條件。
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
                  <span class="history-mono" :title="a.who ?? '系統'">{{ a.who ? shortId(a.who) : '系統' }}</span>
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

        <div v-if="totalPages > 1" class="audit-pager">
          <n-pagination
            :page="page"
            :page-count="totalPages"
            @update:page="changePage" />
        </div>
      </div>
    </n-spin>

    <!-- 明細彈窗：完整欄位與 before/after JSON -->
    <n-modal v-model:show="detailOpen" preset="card" title="稽核事件明細" style="max-width:680px;">
      <div v-if="detail" class="audit-detail">
        <dl class="audit-dl">
          <div><dt>動作</dt><dd class="audit-action">{{ detail.action || '—' }}</dd></div>
          <div><dt>結果</dt><dd><n-tag :type="resultTag(detail.result).type" size="small" round>{{ resultTag(detail.result).label }}</n-tag></dd></div>
          <div><dt>對象</dt><dd>{{ detail.target || '—' }}</dd></div>
          <div><dt>對象 ID</dt><dd class="history-mono">{{ detail.targetId || '—' }}</dd></div>
          <div><dt>操作者</dt><dd class="history-mono">{{ detail.who || '系統' }}</dd></div>
          <div><dt>租戶</dt><dd class="history-mono">{{ detail.tenant || '—' }}</dd></div>
          <div><dt>IP</dt><dd class="history-mono">{{ detail.ip || '—' }}</dd></div>
          <div><dt>發生時間</dt><dd>{{ fmtDate(detail.occurredAt) }}</dd></div>
          <div><dt>Correlation ID</dt><dd class="history-mono">{{ detail.correlationId || '—' }}</dd></div>
        </dl>
        <div class="audit-json-group">
          <div>
            <div class="audit-json-label">變更前 (before)</div>
            <pre class="audit-json">{{ prettyJson(detail.before) }}</pre>
          </div>
          <div>
            <div class="audit-json-label">變更後 (after)</div>
            <pre class="audit-json">{{ prettyJson(detail.after) }}</pre>
          </div>
        </div>
      </div>
    </n-modal>
  </div>
</template>

<style scoped>
.history-toolbar {
  margin-bottom: 16px;
  border-radius: 10px;
}

.history-toolbar :deep(.n-input),
.history-toolbar :deep(.n-input-wrapper) {
  border-radius: 10px;
}

.history-toolbar :deep(.n-input__border),
.history-toolbar :deep(.n-input__state-border) {
  border-radius: 10px;
}

/* 篩選列：兩組並排，按鈕組對齊輸入框底部；不足時整組換行成最多兩行 */
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

/* 按鈕組：不撐滿、靠右對齊欄位底部 */
.fb-actions {
  flex: 0 0 auto;
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
  min-width: 880px;
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
  padding: 12px 8px;
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
  font-weight: 600;
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
  font-weight: 600;
  margin-bottom: 4px;
}

.audit-json {
  margin: 0;
  padding: 10px;
  border-radius: 8px;
  background: var(--oj-primary-wash, rgba(0, 0, 0, 0.04));
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
