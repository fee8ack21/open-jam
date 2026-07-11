<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useMessage } from 'naive-ui'
import { useI18n } from 'vue-i18n'
import { contentApi } from '@/api'
import {
  LegalDocumentStatus,
  LegalDocumentType,
  type LegalDocumentDto,
  type LegalDocumentSummaryDto,
} from '@/api/content-service'

const { t, locale } = useI18n()
const message = useMessage()

// 文件狀態 → 顯示用標籤
const STATUS = {
  [LegalDocumentStatus.Draft]:    { labelKey: 'legalDocs.statusDraft', type: 'info' as const },
  [LegalDocumentStatus.Active]:   { labelKey: 'legalDocs.statusActive', type: 'success' as const },
  [LegalDocumentStatus.Inactive]: { labelKey: 'legalDocs.statusInactive', type: 'default' as const },
}
const TYPE = {
  [LegalDocumentType.TermsOfService]: { labelKey: 'legalDocs.typeTerms', icon: 'note' },
  [LegalDocumentType.PrivacyPolicy]:  { labelKey: 'legalDocs.typePrivacy', icon: 'shield' },
}
function statusOf(s?: LegalDocumentStatus) {
  return (s != null && STATUS[s]) || { labelKey: 'appStatus.unknown', type: 'default' as const }
}
function typeOf(v?: LegalDocumentType) {
  return (v != null && TYPE[v]) || { labelKey: 'appStatus.unknown', icon: 'file' }
}

function messageOf(err: unknown, fallback = t('legalDocs.msgActionFailed')) {
  const response = err as { error?: { detail?: string; title?: string; errors?: Record<string, string[]> } } | null
  const problem = response?.error
  const firstValidation = problem?.errors ? Object.values(problem.errors).flat()[0] : null
  return firstValidation ?? problem?.detail ?? problem?.title ?? fallback
}

// ── 列表（伺服器端分頁）────────────────────────────────────
const pageSize = ref(10)
const items = ref<LegalDocumentSummaryDto[]>([])
const totalCount = ref(0)
const loading = ref(false)
const page = ref(1)
/** 類型篩選：all | <LegalDocumentType> */
const typeFilter = ref<'all' | LegalDocumentType>('all')
/** 狀態篩選：all | <LegalDocumentStatus> */
const statusFilter = ref<'all' | LegalDocumentStatus>('all')

const typeOptions = computed(() => [
  { label: t('legalDocs.filterAllTypes'), value: 'all' },
  { label: t('legalDocs.typeTerms'), value: LegalDocumentType.TermsOfService },
  { label: t('legalDocs.typePrivacy'), value: LegalDocumentType.PrivacyPolicy },
])
const statusOptions = computed(() => [
  { label: t('legalDocs.filterAllStatuses'), value: 'all' },
  { label: t('legalDocs.statusDraft'), value: LegalDocumentStatus.Draft },
  { label: t('legalDocs.statusActive'), value: LegalDocumentStatus.Active },
  { label: t('legalDocs.statusInactive'), value: LegalDocumentStatus.Inactive },
])

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / pageSize.value)))

async function load() {
  loading.value = true
  try {
    const res = await contentApi.legalDocuments.list({
      Offset: (page.value - 1) * pageSize.value,
      Limit: pageSize.value,
      Type: typeFilter.value === 'all' ? undefined : typeFilter.value,
      Status: statusFilter.value === 'all' ? undefined : statusFilter.value,
    })
    items.value = res.data.items ?? []
    totalCount.value = res.data.totalCount ?? 0
  } catch (err) {
    message.error(messageOf(err, t('legalDocs.msgLoadFailed')))
    items.value = []
    totalCount.value = 0
  } finally {
    loading.value = false
  }
}

watch([typeFilter, statusFilter], () => { page.value = 1; load() })
function changePage(p: number) { page.value = p; load() }
function changePageSize(size: number) { pageSize.value = size; page.value = 1; load() }

function fmtDate(v?: string | null) {
  return v ? new Date(v).toLocaleString(locale.value, { hour12: false }) : '—'
}

// ── 建立 / 編輯草稿 ─────────────────────────────────────────
const modalOpen = ref(false)
const saving = ref(false)
const editing = ref<LegalDocumentDto | null>(null)
const form = reactive({
  type: LegalDocumentType.TermsOfService as LegalDocumentType,
  title: '',
  content: '',
})

// ── 即時預覽：與 Auth（LegalContentHelper）/ portal-web（LegalView）同一套
//    內容慣例——「## 」＝章節標題（自動編號）、「- 」＝列點、其餘為段落 ──
interface PreviewSection {
  n: string
  h: string
  p: string
  list?: string[]
}

function parseSections(content: string): PreviewSection[] {
  const sections: PreviewSection[] = []
  let current: PreviewSection | null = null
  for (const raw of content.replace(/\r\n/g, '\n').split('\n')) {
    const line = raw.trim()
    if (!line) continue
    if (line.startsWith('## ')) {
      current = { n: String(sections.length + 1).padStart(2, '0'), h: line.slice(3).trim(), p: '' }
      sections.push(current)
    } else if (line.startsWith('- ') && current) {
      (current.list ??= []).push(line.slice(2).trim())
    } else if (current) {
      current.p = current.p ? `${current.p}\n${line}` : line
    } else {
      current = { n: String(sections.length + 1).padStart(2, '0'), h: '', p: line }
      sections.push(current)
    }
  }
  return sections
}

const previewSections = computed(() => parseSections(form.content))

function openCreate() {
  editing.value = null
  form.type = LegalDocumentType.TermsOfService
  form.title = ''
  form.content = ''
  modalOpen.value = true
}

async function openEdit(row: LegalDocumentSummaryDto) {
  if (!row.id) return
  try {
    const res = await contentApi.legalDocuments.get(row.id)
    editing.value = res.data
    form.type = res.data.type ?? LegalDocumentType.TermsOfService
    form.title = res.data.title ?? ''
    form.content = res.data.content ?? ''
    modalOpen.value = true
  } catch (err) {
    message.error(messageOf(err, t('legalDocs.msgLoadFailed')))
  }
}

async function save() {
  const title = form.title.trim()
  if (!title) { message.warning(t('legalDocs.valTitleRequired')); return }
  if (!form.content.trim()) { message.warning(t('legalDocs.valContentRequired')); return }

  saving.value = true
  try {
    if (editing.value?.id) {
      await contentApi.legalDocuments.update(editing.value.id, { title, content: form.content })
      message.success(t('legalDocs.msgUpdated'))
    } else {
      await contentApi.legalDocuments.create({ type: form.type, title, content: form.content })
      message.success(t('legalDocs.msgCreated'))
    }
    modalOpen.value = false
    await load()
  } catch (err) {
    message.error(messageOf(err, editing.value ? t('legalDocs.msgUpdateFailed') : t('legalDocs.msgCreateFailed')))
  } finally {
    saving.value = false
  }
}

// ── 檢視內容（任一狀態，唯讀，供歷史版本比對）───────────────
const viewOpen = ref(false)
const viewing = ref<LegalDocumentDto | null>(null)

async function openView(row: LegalDocumentSummaryDto) {
  if (!row.id) return
  try {
    const res = await contentApi.legalDocuments.get(row.id)
    viewing.value = res.data
    viewOpen.value = true
  } catch (err) {
    message.error(messageOf(err, t('legalDocs.msgLoadFailed')))
  }
}

// ── 啟用 / 停用 ─────────────────────────────────────────────
const actingId = ref<string | null>(null)

async function activate(row: LegalDocumentSummaryDto) {
  if (!row.id) return
  actingId.value = row.id
  try {
    await contentApi.legalDocuments.activate(row.id)
    message.success(t('legalDocs.msgActivated'))
    await load()
  } catch (err) {
    message.error(messageOf(err))
  } finally {
    actingId.value = null
  }
}

async function deactivate(row: LegalDocumentSummaryDto) {
  if (!row.id) return
  actingId.value = row.id
  try {
    await contentApi.legalDocuments.deactivate(row.id)
    message.success(t('legalDocs.msgDeactivated'))
    await load()
  } catch (err) {
    message.error(messageOf(err))
  } finally {
    actingId.value = null
  }
}

onMounted(load)
</script>

<template>
  <div :data-screen-label="t('route.legalDocs')">
    <div class="page-head" style="justify-content:flex-end;">
      <n-button type="primary" size="large" @click="openCreate">
        <template #icon><app-icon name="plus" :size="16" /></template>
        {{ t('legalDocs.newDraft') }}
      </n-button>
    </div>

    <n-spin :show="loading">
      <div class="card-pad legal-table-card">
        <div class="list-filter">
          <div class="filter-bar">
            <div class="fb-group">
              <div class="fb-field" style="flex:1 1 150px;">
                <label class="fb-label">{{ t('legalDocs.type') }}</label>
                <n-select v-model:value="typeFilter" :options="typeOptions" />
              </div>
              <div class="fb-field" style="flex:1 1 150px;">
                <label class="fb-label">{{ t('common.status') }}</label>
                <n-select v-model:value="statusFilter" :options="statusOptions" />
              </div>
            </div>
          </div>
        </div>

        <div class="legal-table-wrap">
          <table class="tbl legal-table">
            <thead>
              <tr>
                <th>{{ t('legalDocs.colDocument') }}</th>
                <th class="num">{{ t('legalDocs.colVersion') }}</th>
                <th>{{ t('common.status') }}</th>
                <th class="hide-sm">{{ t('legalDocs.colActivatedAt') }}</th>
                <th class="hide-sm">{{ t('legalDocs.colUpdatedAt') }}</th>
                <th style="width:170px; text-align:right;">{{ t('legalDocs.colActions') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="!loading && !items.length">
                <td colspan="6" style="text-align:center; padding:48px 24px;">
                  <span class="kpi-ic" style="background:var(--c-violet); margin:0 auto 14px;"><app-icon name="file" :size="22" /></span>
                  <div style="font-weight:700; font-size:15px;">{{ t('legalDocs.emptyTitle') }}</div>
                  <div style="font-size:13px; color:var(--text-faint); margin-top:4px;">{{ t('legalDocs.emptyDesc') }}</div>
                </td>
              </tr>
              <tr v-for="row in items" v-else :key="row.id">
                <td>
                  <div class="prod-cell">
                    <span class="doc-badge" :class="row.type === LegalDocumentType.TermsOfService ? 'terms' : 'privacy'">
                      <app-icon :name="typeOf(row.type).icon" :size="16" />
                    </span>
                    <div style="min-width:0;">
                      <div class="pc-title">{{ row.title }}</div>
                      <div class="pc-meta">{{ t(typeOf(row.type).labelKey) }}</div>
                    </div>
                  </div>
                </td>
                <td class="num legal-mono">v{{ row.version }}</td>
                <td>
                  <n-tag :type="statusOf(row.status).type" size="small" round>{{ t(statusOf(row.status).labelKey) }}</n-tag>
                </td>
                <td class="hide-sm"><span class="legal-mono">{{ fmtDate(row.activatedAt) }}</span></td>
                <td class="hide-sm"><span class="legal-mono">{{ fmtDate(row.updatedAt ?? row.createdAt) }}</span></td>
                <td>
                  <div class="row-actions">
                    <button class="ic-act" :title="t('legalDocs.view')" @click="openView(row)"><app-icon name="eye" :size="17" /></button>
                    <button
                      v-if="row.status === LegalDocumentStatus.Draft"
                      class="ic-act" :title="t('common.edit')" @click="openEdit(row)"><app-icon name="edit" :size="17" /></button>
                    <n-popconfirm v-if="row.status !== LegalDocumentStatus.Active" @positive-click="activate(row)">
                      <template #trigger>
                        <button class="ic-act" :title="t('legalDocs.activate')" :disabled="actingId === row.id"><app-icon name="check" :size="17" /></button>
                      </template>
                      {{ t('legalDocs.activateConfirm') }}
                    </n-popconfirm>
                    <n-popconfirm v-else @positive-click="deactivate(row)">
                      <template #trigger>
                        <button class="ic-act danger" :title="t('legalDocs.deactivate')" :disabled="actingId === row.id"><app-icon name="minus" :size="17" /></button>
                      </template>
                      {{ t('legalDocs.deactivateConfirm') }}
                    </n-popconfirm>
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
            :page-size="pageSize"
            @update:page="changePage"
            @update:page-size="changePageSize" />
        </div>
      </div>
    </n-spin>

    <!-- 建立 / 編輯草稿（左編輯、右即時預覽） -->
    <n-modal v-model:show="modalOpen" preset="card" :title="editing ? t('legalDocs.editTitle') : t('legalDocs.createTitle')" style="max-width:1080px" to=".oj-root">
      <div style="display:grid; gap:16px;">
        <div v-if="!editing">
          <label class="field-label">{{ t('legalDocs.type') }}</label>
          <n-select v-model:value="form.type" :options="typeOptions.slice(1)" />
        </div>
        <div>
          <label class="field-label">{{ t('legalDocs.titleLabel') }}</label>
          <n-input v-model:value="form.title" maxlength="200" show-count :placeholder="t('legalDocs.titlePlaceholder')" />
        </div>
        <div class="editor-split">
          <div>
            <label class="field-label">{{ t('legalDocs.contentLabel') }}</label>
            <n-input
              v-model:value="form.content"
              type="textarea"
              :autosize="{ minRows: 16, maxRows: 24 }"
              :placeholder="t('legalDocs.contentPlaceholder')" />
            <div style="font-size:12px; color:var(--text-faint); margin-top:6px;">{{ t('legalDocs.contentHint') }}</div>
          </div>
          <div>
            <label class="field-label">{{ t('legalDocs.previewLabel') }}</label>
            <div class="legal-preview">
              <div v-if="!previewSections.length" class="legal-preview-empty">{{ t('legalDocs.previewEmpty') }}</div>
              <section v-for="s in previewSections" :key="s.n" class="legal-sec">
                <h4 v-if="s.h"><span class="num">{{ s.n }}</span> {{ s.h }}</h4>
                <p style="white-space: pre-wrap;">{{ s.p }}</p>
                <ul v-if="s.list">
                  <li v-for="(item, i) in s.list" :key="i">{{ item }}</li>
                </ul>
              </section>
            </div>
          </div>
        </div>
      </div>
      <template #footer>
        <div style="display:flex; justify-content:flex-end; gap:10px;">
          <n-button :disabled="saving" @click="modalOpen = false">{{ t('common.cancel') }}</n-button>
          <n-button type="primary" :loading="saving" @click="save">{{ editing ? t('legalDocs.save') : t('legalDocs.create') }}</n-button>
        </div>
      </template>
    </n-modal>

    <!-- 檢視版本內容（唯讀，歷史比對） -->
    <n-modal v-model:show="viewOpen" preset="card" style="max-width:720px" to=".oj-root">
      <template #header>
        <div style="display:flex; align-items:center; gap:10px; min-width:0;">
          <span>{{ viewing?.title }}</span>
          <n-tag size="small" round>v{{ viewing?.version }}</n-tag>
          <n-tag :type="statusOf(viewing?.status).type" size="small" round>{{ t(statusOf(viewing?.status).labelKey) }}</n-tag>
        </div>
      </template>
      <div class="legal-content-view">{{ viewing?.content }}</div>
    </n-modal>
  </div>
</template>

<style scoped>
.legal-table-card {
  padding: 0;
  border-radius: 10px;
  overflow: hidden;
}

.list-filter {
  padding: 16px 18px;
  border-bottom: 1.5px solid var(--border);
}

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
  flex: 1 1 320px;
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

.legal-table-wrap {
  overflow-x: auto;
  padding: 8px 8px 4px;
}

.legal-table {
  min-width: 820px;
}

.legal-table thead th {
  font-size: 12.5px;
  padding-top: 12px;
  vertical-align: middle;
}

.legal-table thead th + th {
  border-left: 1.5px solid var(--border);
}

.legal-table tbody td + td {
  border-left: 1.5px solid var(--border);
}

.legal-mono {
  font-family: var(--oj-mono);
  color: var(--text-soft);
}

.doc-badge {
  width: 30px;
  height: 30px;
  border-radius: 10px;
  display: grid;
  place-items: center;
  flex: none;
  color: #fff;
}

.doc-badge.terms { background: linear-gradient(135deg, var(--c-violet), var(--c-pink)); }
.doc-badge.privacy { background: linear-gradient(135deg, var(--c-cyan), var(--c-violet)); }

.history-pager {
  display: flex;
  justify-content: flex-end;
  padding: 12px 8px;
}

/* 內容檢視：保留換行的純文字呈現（## 章節 / - 列點慣例） */
.legal-content-view {
  white-space: pre-wrap;
  font-size: 14px;
  line-height: 1.75;
  color: var(--text-soft);
  max-height: 60vh;
  overflow-y: auto;
}

/* 編輯 modal：左 textarea、右即時預覽，窄螢幕改上下堆疊 */
.editor-split {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 16px;
  align-items: start;
}

@media (max-width: 760px) {
  .editor-split {
    grid-template-columns: 1fr;
  }
}

/* 即時預覽：與 Auth dialog / portal-web 條款頁相同的章節樣式 */
.legal-preview {
  border: 1.5px solid var(--border);
  border-radius: 10px;
  padding: 16px 18px;
  max-height: 560px;
  overflow-y: auto;
  background: var(--surface);
}

.legal-preview-empty {
  color: var(--text-faint);
  font-size: 13px;
  text-align: center;
  padding: 32px 0;
}

.legal-preview .legal-sec {
  margin-bottom: 18px;
}

.legal-preview .legal-sec:last-child {
  margin-bottom: 0;
}

.legal-preview .legal-sec h4 {
  font-size: 14.5px;
  font-weight: 700;
  color: var(--text);
  margin: 0 0 6px;
  display: flex;
  align-items: baseline;
  gap: 8px;
}

.legal-preview .legal-sec h4 .num {
  font-family: var(--oj-mono);
  font-size: 12px;
  color: var(--oj-primary);
  font-weight: 600;
}

.legal-preview .legal-sec p {
  margin: 0;
  font-size: 13.5px;
  line-height: 1.75;
  color: var(--text-soft);
}

.legal-preview .legal-sec ul {
  margin: 8px 0 0;
  padding-left: 20px;
}

.legal-preview .legal-sec li {
  font-size: 13px;
  line-height: 1.7;
  color: var(--text-soft);
  margin-bottom: 4px;
}
</style>
