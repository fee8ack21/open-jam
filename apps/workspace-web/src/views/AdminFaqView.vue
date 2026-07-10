<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useMessage } from 'naive-ui'
import { useI18n } from 'vue-i18n'
import { contentApi } from '@/api'
import type { FaqCategoryDto, FaqItemDto } from '@/api/content-service'

const { t, locale } = useI18n()
const message = useMessage()

// 主題分類（後台可 CRUD，來自 ContentService）
const categories = ref<FaqCategoryDto[]>([])
async function loadCategories() {
  try {
    const res = await contentApi.faqCategories.list()
    categories.value = res.data ?? []
  } catch {
    categories.value = []
  }
}

function messageOf(err: unknown, fallback = t('faqs.msgActionFailed')) {
  const response = err as { error?: { detail?: string; title?: string; errors?: Record<string, string[]> } } | null
  const problem = response?.error
  const firstValidation = problem?.errors ? Object.values(problem.errors).flat()[0] : null
  return firstValidation ?? problem?.detail ?? problem?.title ?? fallback
}

// ── 列表（伺服器端分頁）────────────────────────────────────
const PAGE_SIZE = 10
const items = ref<FaqItemDto[]>([])
const totalCount = ref(0)
const loading = ref(false)
const page = ref(1)
/** 分類篩選：all | <categoryId> */
const categoryFilter = ref<'all' | string>('all')
/** 發布狀態篩選：all | true | false */
const publishedFilter = ref<'all' | 'true' | 'false'>('all')

// 篩選下拉（含「全部主題」）；表單下拉（僅分類本身）
const categorySelectOptions = computed(() => categories.value.map((c) => ({ label: c.name ?? '', value: c.id! })))
const categoryFilterOptions = computed(() => [
  { label: t('faqs.filterAllCategories'), value: 'all' },
  ...categorySelectOptions.value,
])
const publishedOptions = computed(() => [
  { label: t('faqs.filterAllStatuses'), value: 'all' },
  { label: t('faqs.statusPublished'), value: 'true' },
  { label: t('faqs.statusHidden'), value: 'false' },
])

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / PAGE_SIZE)))

async function load() {
  loading.value = true
  try {
    const res = await contentApi.faqs.list({
      Offset: (page.value - 1) * PAGE_SIZE,
      Limit: PAGE_SIZE,
      CategoryId: categoryFilter.value === 'all' ? undefined : categoryFilter.value,
      IsPublished: publishedFilter.value === 'all' ? undefined : publishedFilter.value === 'true',
    })
    items.value = res.data.items ?? []
    totalCount.value = res.data.totalCount ?? 0
  } catch (err) {
    message.error(messageOf(err, t('faqs.msgLoadFailed')))
    items.value = []
    totalCount.value = 0
  } finally {
    loading.value = false
  }
}

watch([categoryFilter, publishedFilter], () => { page.value = 1; load() })
function changePage(p: number) { page.value = p; load() }

function fmtDate(v?: string | null) {
  return v ? new Date(v).toLocaleString(locale.value, { hour12: false }) : '—'
}

// ── 建立 / 編輯 ─────────────────────────────────────────────
const modalOpen = ref(false)
const saving = ref(false)
const editing = ref<FaqItemDto | null>(null)
const form = reactive({
  categoryId: '',
  question: '',
  answer: '',
  sortOrder: 0,
  isPublished: true,
})

function openCreate() {
  editing.value = null
  form.categoryId = categories.value[0]?.id ?? ''
  form.question = ''
  form.answer = ''
  form.sortOrder = 0
  form.isPublished = true
  modalOpen.value = true
}

function openEdit(row: FaqItemDto) {
  editing.value = row
  form.categoryId = row.categoryId ?? ''
  form.question = row.question ?? ''
  form.answer = row.answer ?? ''
  form.sortOrder = row.sortOrder ?? 0
  form.isPublished = row.isPublished ?? true
  modalOpen.value = true
}

async function save() {
  const question = form.question.trim()
  if (!form.categoryId) { message.warning(t('faqs.valCategoryRequired')); return }
  if (!question) { message.warning(t('faqs.valQuestionRequired')); return }
  if (!form.answer.trim()) { message.warning(t('faqs.valAnswerRequired')); return }

  const payload = {
    categoryId: form.categoryId,
    question,
    answer: form.answer,
    sortOrder: form.sortOrder,
    isPublished: form.isPublished,
  }

  saving.value = true
  try {
    if (editing.value?.id) {
      await contentApi.faqs.update(editing.value.id, payload)
      message.success(t('faqs.msgUpdated'))
    } else {
      await contentApi.faqs.create(payload)
      message.success(t('faqs.msgCreated'))
    }
    modalOpen.value = false
    await load()
  } catch (err) {
    message.error(messageOf(err, editing.value ? t('faqs.msgUpdateFailed') : t('faqs.msgCreateFailed')))
  } finally {
    saving.value = false
  }
}

// ── 刪除 ────────────────────────────────────────────────────
const actingId = ref<string | null>(null)

async function remove(row: FaqItemDto) {
  if (!row.id) return
  actingId.value = row.id
  try {
    await contentApi.faqs.delete(row.id)
    message.success(t('faqs.msgDeleted'))
    // 刪除末頁最後一筆時退回前一頁
    if (items.value.length === 1 && page.value > 1) page.value -= 1
    await load()
  } catch (err) {
    message.error(messageOf(err, t('faqs.msgDeleteFailed')))
  } finally {
    actingId.value = null
  }
}

onMounted(() => { loadCategories(); load() })
</script>

<template>
  <div :data-screen-label="t('route.faqs')">
    <div class="page-head" style="justify-content:flex-end;">
      <n-button type="primary" size="large" @click="openCreate">
        <template #icon><app-icon name="plus" :size="16" /></template>
        {{ t('faqs.newItem') }}
      </n-button>
    </div>

    <n-spin :show="loading">
      <div class="card-pad faq-table-card">
        <div class="list-filter">
          <div class="filter-bar">
            <div class="fb-group">
              <div class="fb-field" style="flex:1 1 150px;">
                <label class="fb-label">{{ t('faqs.category') }}</label>
                <n-select v-model:value="categoryFilter" :options="categoryFilterOptions" />
              </div>
              <div class="fb-field" style="flex:1 1 150px;">
                <label class="fb-label">{{ t('faqs.colStatus') }}</label>
                <n-select v-model:value="publishedFilter" :options="publishedOptions" />
              </div>
            </div>
          </div>
        </div>

        <div class="faq-table-wrap">
          <table class="tbl faq-table">
            <thead>
              <tr>
                <th class="hide-sm">{{ t('faqs.colCategory') }}</th>
                <th>{{ t('faqs.colQuestion') }}</th>
                <th class="num">{{ t('faqs.colSort') }}</th>
                <th>{{ t('faqs.colStatus') }}</th>
                <th style="width:120px; text-align:right;">{{ t('faqs.colActions') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="!loading && !items.length">
                <td colspan="5" style="text-align:center; padding:48px 24px;">
                  <span class="kpi-ic" style="background:var(--c-violet); margin:0 auto 14px;"><app-icon name="book" :size="22" /></span>
                  <div style="font-weight:700; font-size:15px;">{{ t('faqs.emptyTitle') }}</div>
                  <div style="font-size:13px; color:var(--text-faint); margin-top:4px;">{{ t('faqs.emptyDesc') }}</div>
                </td>
              </tr>
              <tr v-for="row in items" v-else :key="row.id">
                <td class="hide-sm">
                  <n-tag size="small" round>{{ row.categoryName }}</n-tag>
                </td>
                <td>
                  <div class="pc-title faq-q-cell">{{ row.question }}</div>
                  <div class="pc-meta">{{ t('faqs.colUpdatedAt') }} {{ fmtDate(row.updatedAt ?? row.createdAt) }}</div>
                </td>
                <td class="num faq-mono">{{ row.sortOrder }}</td>
                <td>
                  <n-tag :type="row.isPublished ? 'success' : 'default'" size="small" round>
                    {{ t(row.isPublished ? 'faqs.statusPublished' : 'faqs.statusHidden') }}
                  </n-tag>
                </td>
                <td>
                  <div class="row-actions">
                    <button class="ic-act" :title="t('faqs.edit')" @click="openEdit(row)"><app-icon name="edit" :size="17" /></button>
                    <n-popconfirm @positive-click="remove(row)">
                      <template #trigger>
                        <button class="ic-act danger" :title="t('faqs.del')" :disabled="actingId === row.id"><app-icon name="trash" :size="17" /></button>
                      </template>
                      {{ t('faqs.deleteConfirm') }}
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

    <!-- 建立 / 編輯 -->
    <n-modal v-model:show="modalOpen" preset="card" :title="editing ? t('faqs.editTitle') : t('faqs.createTitle')" style="max-width:640px" to=".oj-root">
      <div style="display:grid; gap:16px;">
        <div>
          <label class="field-label">{{ t('faqs.category') }}</label>
          <n-select v-model:value="form.categoryId" :options="categorySelectOptions" :placeholder="t('faqs.categoryPlaceholder')" />
        </div>
        <div>
          <label class="field-label">{{ t('faqs.questionLabel') }}</label>
          <n-input v-model:value="form.question" maxlength="500" show-count :placeholder="t('faqs.questionPlaceholder')" />
        </div>
        <div>
          <label class="field-label">{{ t('faqs.answerLabel') }}</label>
          <n-input
            v-model:value="form.answer"
            type="textarea"
            :autosize="{ minRows: 6, maxRows: 16 }"
            :placeholder="t('faqs.answerPlaceholder')" />
        </div>
        <div style="display:flex; gap:16px; align-items:flex-end; flex-wrap:wrap;">
          <div style="flex:1 1 160px;">
            <label class="field-label">{{ t('faqs.sortLabel') }}</label>
            <n-input-number v-model:value="form.sortOrder" :min="0" style="width:100%;" />
          </div>
          <div style="flex:0 0 auto; display:flex; align-items:center; gap:8px; padding-bottom:6px;">
            <n-switch v-model:value="form.isPublished" />
            <span style="font-size:13.5px;">{{ t('faqs.publishedLabel') }}</span>
          </div>
        </div>
      </div>
      <template #footer>
        <div style="display:flex; justify-content:flex-end; gap:10px;">
          <n-button :disabled="saving" @click="modalOpen = false">{{ t('common.cancel') }}</n-button>
          <n-button type="primary" :loading="saving" @click="save">{{ editing ? t('faqs.save') : t('faqs.create') }}</n-button>
        </div>
      </template>
    </n-modal>
  </div>
</template>

<style scoped>
.faq-table-card {
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

.faq-table-wrap {
  overflow-x: auto;
  padding: 8px 8px 4px;
}

.faq-table {
  min-width: 720px;
}

.faq-table thead th {
  font-size: 12.5px;
  padding-top: 12px;
  vertical-align: middle;
}

.faq-table thead th + th {
  border-left: 1.5px solid var(--border);
}

.faq-table tbody td + td {
  border-left: 1.5px solid var(--border);
}

.faq-q-cell {
  white-space: normal;
  line-height: 1.5;
}

.faq-mono {
  font-family: var(--oj-mono);
  color: var(--text-soft);
}

.history-pager {
  display: flex;
  justify-content: flex-end;
  padding: 12px 8px;
}
</style>
