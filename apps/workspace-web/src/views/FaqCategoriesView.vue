<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useMessage } from 'naive-ui'
import { useI18n } from 'vue-i18n'
import { contentApi } from '@/api'
import type { FaqCategoryDto } from '@/api/content-service'

const { t } = useI18n()

const SLUG_RE = /^[a-z0-9]+(?:-[a-z0-9]+)*$/

const message = useMessage()
const categories = ref<FaqCategoryDto[]>([])
const keyword = ref('')
const appliedKeyword = ref('') // 已套用的關鍵字（按下搜尋才更新）
const pageSize = ref(10)
const page = ref(1)
const loading = ref(false)
const saving = ref(false)
const deletingId = ref<string | null>(null)
const modalOpen = ref(false)
const editing = ref<FaqCategoryDto | null>(null)
const form = reactive({
  name: '',
  slug: '',
  description: '',
  sortOrder: 0,
})

function messageOf(err: unknown, fallback = t('faqCategories.msgActionFailed')) {
  const response = err as { error?: { detail?: string; title?: string; errors?: Record<string, string[]> } } | null
  const problem = response?.error
  const firstValidation = problem?.errors ? Object.values(problem.errors).flat()[0] : null
  return firstValidation ?? problem?.detail ?? problem?.title ?? fallback
}

function resetForm(category?: FaqCategoryDto) {
  editing.value = category ?? null
  form.name = category?.name ?? ''
  form.slug = category?.slug ?? ''
  form.description = category?.description ?? ''
  form.sortOrder = category?.sortOrder ?? 0
}

function openCreate() {
  resetForm()
  modalOpen.value = true
}

function openEdit(category: FaqCategoryDto) {
  resetForm(category)
  modalOpen.value = true
}

async function load() {
  loading.value = true
  try {
    const res = await contentApi.faqCategories.list()
    categories.value = res.data ?? []
  } catch (err) {
    message.error(messageOf(err, t('faqCategories.msgLoadFailed')))
    categories.value = []
  } finally {
    loading.value = false
  }
}

const rows = computed<FaqCategoryDto[]>(() => {
  const q = appliedKeyword.value.toLowerCase()
  return categories.value
    .filter((category) => !q || [category.name, category.slug].some((f) => (f ?? '').toLowerCase().includes(q)))
    .slice()
    .sort((a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0) || (a.name ?? '').localeCompare(b.name ?? '', 'zh-Hant'))
})

// 分頁：分類為前端一次載入，於過濾結果上做客戶端分頁
const totalPages = computed(() => Math.max(1, Math.ceil(rows.value.length / pageSize.value)))
const pagedRows = computed(() => {
  const start = (page.value - 1) * pageSize.value
  return rows.value.slice(start, start + pageSize.value)
})
// 資料變動（刪除 / 過濾）後夾住頁碼，避免停在空白頁
watch(totalPages, (n) => { if (page.value > n) page.value = n })

function changePageSize(size: number) { pageSize.value = size; page.value = 1 }

// 按下搜尋：套用目前關鍵字並回到第一頁
function onSearch() {
  appliedKeyword.value = keyword.value.trim()
  page.value = 1
}

function validateForm() {
  const name = form.name.trim()
  const slug = form.slug.trim().toLowerCase()
  if (!name) return t('faqCategories.valNameRequired')
  if (name.length > 100) return t('faqCategories.valNameTooLong')
  if (!slug) return t('faqCategories.valSlugRequired')
  if (slug.length < 3 || slug.length > 100 || !SLUG_RE.test(slug)) return t('faqCategories.valSlugInvalid')
  return null
}

async function save() {
  const validation = validateForm()
  if (validation) {
    message.warning(validation)
    return
  }

  const payload = {
    name: form.name.trim(),
    slug: form.slug.trim().toLowerCase(),
    description: form.description.trim() || null,
    sortOrder: Number(form.sortOrder) || 0,
  }

  saving.value = true
  try {
    if (editing.value?.id) {
      await contentApi.faqCategories.update(editing.value.id, payload)
      message.success(t('faqCategories.msgUpdated'))
    } else {
      await contentApi.faqCategories.create(payload)
      message.success(t('faqCategories.msgCreated'))
    }
    modalOpen.value = false
    await load()
  } catch (err) {
    message.error(messageOf(err, editing.value ? t('faqCategories.msgUpdateFailed') : t('faqCategories.msgCreateFailed')))
  } finally {
    saving.value = false
  }
}

async function remove(category: FaqCategoryDto) {
  if (!category.id) return
  deletingId.value = category.id
  try {
    await contentApi.faqCategories.delete(category.id)
    message.success(t('faqCategories.msgDeleted'))
    await load()
  } catch (err) {
    message.error(messageOf(err, t('faqCategories.msgDeleteBlocked')))
  } finally {
    deletingId.value = null
  }
}

onMounted(load)
</script>

<template>
  <div :data-screen-label="t('route.faqCategories')">
    <div class="page-head" style="justify-content:flex-end;">
      <n-button type="primary" size="large" @click="openCreate">
        <template #icon><app-icon name="plus" :size="16" /></template>
        {{ t('faqCategories.newCategory') }}
      </n-button>
    </div>

    <n-spin :show="loading">
      <!-- 篩選列與分類表格合併為單一卡片：篩選在上、整寬分隔線、表格在下 -->
      <div class="card-pad category-table-card">
        <div class="list-filter">
          <div class="filter-bar">
            <div class="fb-group">
              <div class="fb-field fb-keyword">
                <label class="fb-label">{{ t('common.keyword') }}</label>
                <n-input
                  v-model:value="keyword"
                  clearable
                  :placeholder="t('faqCategories.searchPlaceholder')"
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

        <div class="category-table-wrap">
        <table class="tbl category-table">
          <thead>
            <tr>
              <th>{{ t('faqCategories.colCategory') }}</th>
              <th class="hide-sm">Slug</th>
              <th class="num hide-sm">{{ t('faqCategories.colSort') }}</th>
              <th style="width:120px; text-align:right;">{{ t('faqCategories.colActions') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="!loading && !rows.length">
              <td colspan="4" style="text-align:center; padding:48px 24px;">
                <span class="kpi-ic" style="background:var(--c-violet); margin:0 auto 14px;"><app-icon name="tag" :size="22" /></span>
                <div style="font-weight:700; font-size:15px;">{{ t('faqCategories.emptyTitle') }}</div>
                <div style="font-size:13px; color:var(--text-faint); margin-top:4px;">{{ t('faqCategories.emptyDesc') }}</div>
              </td>
            </tr>
            <tr v-for="category in pagedRows" v-else :key="category.id">
              <td>
                <div style="min-width:0;">
                  <div class="pc-title">{{ category.name }}</div>
                  <div v-if="category.description" class="pc-meta">{{ category.description }}</div>
                </div>
              </td>
              <td class="hide-sm" style="font-family:var(--oj-mono); color:var(--text-soft);">{{ category.slug }}</td>
              <td class="num hide-sm">{{ category.sortOrder ?? 0 }}</td>
              <td>
                <div class="row-actions">
                  <button class="ic-act" :title="t('common.edit')" @click="openEdit(category)"><app-icon name="edit" :size="17" /></button>
                  <n-popconfirm @positive-click="remove(category)">
                    <template #trigger>
                      <button class="ic-act danger" :title="t('common.delete')" :disabled="deletingId === category.id"><app-icon name="trash" :size="17" /></button>
                    </template>
                    {{ t('faqCategories.deleteConfirm') }}
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
            @update:page="(p: number) => (page = p)"
            @update:page-size="changePageSize" />
        </div>
      </div>
    </n-spin>

    <n-modal v-model:show="modalOpen" preset="card" :title="editing ? t('faqCategories.editTitle') : t('faqCategories.createTitle')" style="max-width:520px" to=".oj-root">
      <div style="display:grid; gap:16px;">
        <div>
          <label class="field-label">{{ t('faqCategories.nameLabel') }}</label>
          <n-input v-model:value="form.name" maxlength="100" show-count :placeholder="t('faqCategories.namePlaceholder')" />
        </div>
        <div>
          <label class="field-label">{{ t('faqCategories.slugLabel') }}</label>
          <n-input v-model:value="form.slug" :placeholder="t('faqCategories.slugPlaceholder')" />
          <div style="font-size:12px; color:var(--text-faint); margin-top:6px;">{{ t('faqCategories.slugHint') }}</div>
        </div>
        <div>
          <label class="field-label">{{ t('faqCategories.descLabel') }}</label>
          <n-input v-model:value="form.description" maxlength="200" show-count :placeholder="t('faqCategories.descPlaceholder')" />
        </div>
        <div>
          <label class="field-label">{{ t('faqCategories.sortLabel') }}</label>
          <n-input-number v-model:value="form.sortOrder" style="width:100%;" :min="0" :step="1" />
        </div>
      </div>
      <template #footer>
        <div style="display:flex; justify-content:flex-end; gap:10px;">
          <n-button :disabled="saving" @click="modalOpen = false">{{ t('common.cancel') }}</n-button>
          <n-button type="primary" :loading="saving" @click="save">{{ editing ? t('faqCategories.save') : t('faqCategories.create') }}</n-button>
        </div>
      </template>
    </n-modal>
  </div>
</template>

<style scoped>
.category-table thead th {
  font-size: 12.5px;
  padding-top: 12px;
  vertical-align: middle;
}

.category-table-card {
  padding: 0;
  border-radius: var(--r-lg);
  overflow: hidden;
}

/* 篩選區段：卡片頂部，底部整寬分隔線與表格分開 */
.list-filter {
  padding: 18px 20px;
  border-bottom: var(--bw) solid var(--border-strong);
  background: var(--bg);
}



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
  flex: 1 1 auto;
  min-width: 0;
}

/* 欄位：標籤在上、控制項在下 */
.fb-field {
  display: flex;
  flex-direction: column;
  gap: 6px;
  min-width: 0;
}

/* 關鍵字欄位滿版，撐滿按鈕以外的剩餘寬度 */
.fb-keyword {
  flex: 1 1 auto;
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

.category-table-wrap {
  overflow-x: auto;
  padding: 0 10px;
}

.history-pager {
  display: flex;
  justify-content: flex-end;
  padding: 14px 20px;
}


</style>
