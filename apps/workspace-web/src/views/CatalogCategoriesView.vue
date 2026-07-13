<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useMessage } from 'naive-ui'
import { useI18n } from 'vue-i18n'
import { catalogApi } from '@/api'
import type { CatalogCategoryDto } from '@/api/catalog-service'

const { t } = useI18n()

const ROOT_PARENT = '__root__'
const SLUG_RE = /^[a-z0-9]+(?:-[a-z0-9]+)*$/

interface CategoryRow extends CatalogCategoryDto {
  depth: number
}

const message = useMessage()
const categories = ref<CatalogCategoryDto[]>([])
const keyword = ref('')
const appliedKeyword = ref('') // 已套用的關鍵字（按下搜尋才更新）
const pageSize = ref(10)
const page = ref(1)
const loading = ref(false)
const saving = ref(false)
const deletingId = ref<string | null>(null)
const modalOpen = ref(false)
const editing = ref<CatalogCategoryDto | null>(null)
const activeParentId = ref<string | null>(null)
const form = reactive({
  name: '',
  slug: '',
  parentKey: ROOT_PARENT,
  sortOrder: 0,
})

function messageOf(err: unknown, fallback = t('catalogCategories.msgActionFailed')) {
  const response = err as { error?: { detail?: string; title?: string; errors?: Record<string, string[]> } } | null
  const problem = response?.error
  const firstValidation = problem?.errors ? Object.values(problem.errors).flat()[0] : null
  return firstValidation ?? problem?.detail ?? problem?.title ?? fallback
}

function resetForm(category?: CatalogCategoryDto) {
  editing.value = category ?? null
  form.name = category?.name ?? ''
  form.slug = category?.slug ?? ''
  form.parentKey = category?.parentId ?? ROOT_PARENT
  form.sortOrder = category?.sortOrder ?? 0
}

function openCreate(parentId?: string | null) {
  resetForm()
  form.parentKey = parentId ?? ROOT_PARENT
  modalOpen.value = true
}

function openEdit(category: CatalogCategoryDto) {
  resetForm(category)
  modalOpen.value = true
}

async function load() {
  loading.value = true
  try {
    const res = await catalogApi.catalogCategories.list()
    categories.value = res.data ?? []
  } catch (err) {
    message.error(messageOf(err, t('catalogCategories.msgLoadFailed')))
    categories.value = []
  } finally {
    loading.value = false
  }
}

function descendantsOf(id?: string) {
  if (!id) return new Set<string>()
  const blocked = new Set<string>([id])
  let changed = true
  while (changed) {
    changed = false
    for (const category of categories.value) {
      if (category.id && category.parentId && blocked.has(category.parentId) && !blocked.has(category.id)) {
        blocked.add(category.id)
        changed = true
      }
    }
  }
  return blocked
}

const rows = computed<CategoryRow[]>(() => {
  const q = appliedKeyword.value.toLowerCase()
  return categories.value
    .filter((category) => (activeParentId.value ? category.parentId === activeParentId.value : !category.parentId))
    .filter((category) => !q || [category.name, category.slug].some((f) => (f ?? '').toLowerCase().includes(q)))
    .slice()
    .sort((a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0) || (a.name ?? '').localeCompare(b.name ?? '', 'zh-Hant'))
    .map((category) => ({ ...category, depth: activeParentId.value ? 1 : 0 }))
})

// 分頁：分類為前端一次載入，於目前層級的過濾結果上做客戶端分頁
const totalPages = computed(() => Math.max(1, Math.ceil(rows.value.length / pageSize.value)))
const pagedRows = computed(() => {
  const start = (page.value - 1) * pageSize.value
  return rows.value.slice(start, start + pageSize.value)
})
// 資料變動（刪除 / 過濾）後夾住頁碼，避免停在空白頁
watch(totalPages, (n) => { if (page.value > n) page.value = n })

function changePageSize(size: number) { pageSize.value = size; page.value = 1 }

// 按下搜尋：套用目前關鍵字並回到第一頁（分類為前端一次載入，僅過濾目前層級）
function onSearch() {
  appliedKeyword.value = keyword.value.trim()
  page.value = 1
}

const currentParent = computed(() => categories.value.find((category) => category.id === activeParentId.value) ?? null)
const isChildList = computed(() => currentParent.value != null)
const emptyText = computed(() => isChildList.value ? t('catalogCategories.emptyChild') : t('catalogCategories.emptyRoot'))
const emptyHint = computed(() => isChildList.value ? t('catalogCategories.emptyChildHint') : t('catalogCategories.emptyRootHint'))

const parentOptions = computed(() => {
  const blocked = descendantsOf(editing.value?.id)
  return [
    { label: t('catalogCategories.rootCategory'), value: ROOT_PARENT },
    ...categories.value
      .filter((category) => category.id && !blocked.has(category.id))
      .sort((a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0) || (a.name ?? '').localeCompare(b.name ?? '', 'zh-Hant'))
      .map((category) => ({
        label: `${category.parentId ? '　' : ''}${category.name ?? t('catalogCategories.unnamed')}`,
        value: category.id!,
      })),
  ]
})

function showRootCategories() {
  activeParentId.value = null
  page.value = 1
}

function showChildCategories(category: CatalogCategoryDto) {
  if (!category.id || category.parentId) return
  activeParentId.value = category.id
  page.value = 1
}

function validateForm() {
  const name = form.name.trim()
  const slug = form.slug.trim().toLowerCase()
  if (!name) return t('catalogCategories.valNameRequired')
  if (name.length > 100) return t('catalogCategories.valNameTooLong')
  if (!slug) return t('catalogCategories.valSlugRequired')
  if (slug.length < 3 || slug.length > 100 || !SLUG_RE.test(slug)) return t('catalogCategories.valSlugInvalid')
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
    parentId: form.parentKey === ROOT_PARENT ? null : form.parentKey,
    sortOrder: Number(form.sortOrder) || 0,
  }

  saving.value = true
  try {
    if (editing.value?.id) {
      await catalogApi.catalogCategories.update(editing.value.id, payload)
      message.success(t('catalogCategories.msgUpdated'))
    } else {
      await catalogApi.catalogCategories.create(payload)
      message.success(t('catalogCategories.msgCreated'))
    }
    modalOpen.value = false
    await load()
  } catch (err) {
    message.error(messageOf(err, editing.value ? t('catalogCategories.msgUpdateFailed') : t('catalogCategories.msgCreateFailed')))
  } finally {
    saving.value = false
  }
}

async function remove(category: CatalogCategoryDto) {
  if (!category.id) return
  deletingId.value = category.id
  try {
    await catalogApi.catalogCategories.delete(category.id)
    message.success(t('catalogCategories.msgDeleted'))
    await load()
  } catch (err) {
    message.error(messageOf(err, t('catalogCategories.msgDeleteBlocked')))
  } finally {
    deletingId.value = null
  }
}

onMounted(load)
</script>

<template>
  <div :data-screen-label="t('route.catalogCategories')">
    <div class="page-head" style="justify-content:flex-end;">
      <n-button type="primary" size="large" @click="openCreate(activeParentId)">
        <template #icon><app-icon name="plus" :size="16" /></template>
        {{ isChildList ? t('catalogCategories.newChild') : t('catalogCategories.newCategory') }}
      </n-button>
    </div>

    <div class="category-breadcrumb">
      <button :class="{ on: !isChildList }" @click="showRootCategories">{{ t('route.catalogCategories') }}</button>
      <template v-if="isChildList">
        <span>/</span>
        <strong>{{ currentParent?.name }}</strong>
      </template>
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
                  :placeholder="t('catalogCategories.searchPlaceholder')"
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
              <th>{{ t('catalogCategories.colCategory') }}</th>
              <th class="hide-sm">Slug</th>
              <th class="num hide-sm">{{ t('catalogCategories.colSort') }}</th>
              <th style="width:170px; text-align:right;">{{ t('catalogCategories.colActions') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="!loading && !rows.length">
              <td colspan="4" style="text-align:center; padding:48px 24px;">
                <span class="kpi-ic" style="background:var(--c-violet); margin:0 auto 14px;"><app-icon name="tag" :size="22" /></span>
                <div style="font-weight:700; font-size:15px;">{{ emptyText }}</div>
                <div style="font-size:13px; color:var(--text-faint); margin-top:4px;">{{ emptyHint }}</div>
              </td>
            </tr>
            <tr v-for="category in pagedRows" v-else :key="category.id">
              <td>
                <div style="display:flex; align-items:center; gap:11px; min-width:0;" :style="{ paddingLeft: `${category.depth * 22}px` }">
                  <span class="rank" :class="category.depth === 0 ? 'r1' : category.depth === 1 ? 'r2' : 'rx'">
                    {{ category.depth + 1 }}
                  </span>
                  <div style="min-width:0;">
                    <button
                      v-if="!category.parentId"
                      class="category-name-button"
                      @click="showChildCategories(category)">
                      {{ category.name }}
                    </button>
                    <div v-else class="pc-title">{{ category.name }}</div>
                    <div class="pc-meta">{{ category.parentId ? t('catalogCategories.childCategory') : t('catalogCategories.rootCategory') }}</div>
                  </div>
                </div>
              </td>
              <td class="hide-sm" style="font-family:var(--oj-mono); color:var(--text-soft);">{{ category.slug }}</td>
              <td class="num hide-sm">{{ category.sortOrder ?? 0 }}</td>
              <td>
                <div class="row-actions">
                  <button class="ic-act" :title="t('common.edit')" @click="openEdit(category)"><app-icon name="edit" :size="17" /></button>
                  <button
                    v-if="category.isSystem"
                    class="ic-act danger"
                    :title="t('catalogCategories.systemDeleteBlocked')"
                    disabled>
                    <app-icon name="trash" :size="17" />
                  </button>
                  <n-popconfirm v-else @positive-click="remove(category)">
                    <template #trigger>
                      <button class="ic-act danger" :title="t('common.delete')" :disabled="deletingId === category.id"><app-icon name="trash" :size="17" /></button>
                    </template>
                    {{ t('catalogCategories.deleteConfirm') }}
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

    <n-modal v-model:show="modalOpen" preset="card" :title="editing ? t('catalogCategories.editTitle') : t('catalogCategories.createTitle')" style="max-width:520px" to=".oj-root">
      <div style="display:grid; gap:16px;">
        <div>
          <label class="field-label">{{ t('catalogCategories.nameLabel') }}</label>
          <n-input v-model:value="form.name" maxlength="100" show-count :placeholder="t('catalogCategories.namePlaceholder')" />
        </div>
        <div>
          <label class="field-label">{{ t('catalogCategories.slugLabel') }}</label>
          <n-input v-model:value="form.slug" :placeholder="t('catalogCategories.slugPlaceholder')" />
          <div style="font-size:12px; color:var(--text-faint); margin-top:6px;">{{ t('catalogCategories.slugHint') }}</div>
        </div>
        <div>
          <label class="field-label">{{ t('catalogCategories.parentLabel') }}</label>
          <n-select v-model:value="form.parentKey" :options="parentOptions" />
        </div>
        <div>
          <label class="field-label">{{ t('catalogCategories.sortLabel') }}</label>
          <n-input-number v-model:value="form.sortOrder" style="width:100%;" :min="0" :step="1" />
        </div>
      </div>
      <template #footer>
        <div style="display:flex; justify-content:flex-end; gap:10px;">
          <n-button :disabled="saving" @click="modalOpen = false">{{ t('common.cancel') }}</n-button>
          <n-button type="primary" :loading="saving" @click="save">{{ editing ? t('catalogCategories.save') : t('catalogCategories.create') }}</n-button>
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
  border-bottom: 2px solid var(--border-strong);
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

.category-breadcrumb {
  display: flex;
  align-items: center;
  gap: 8px;
  margin: -10px 0 16px;
  color: var(--text-faint);
  font-size: 13px;
  font-weight: 600;
}

.category-breadcrumb button {
  border: 0;
  background: transparent;
  color: var(--oj-primary);
  cursor: pointer;
  font: inherit;
  padding: 0;
}

.category-breadcrumb button.on {
  color: var(--text-faint);
  cursor: default;
}

.category-breadcrumb strong {
  color: var(--text);
}

.category-name-button {
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

.category-name-button:hover {
  color: var(--c-pink-deep);
}


</style>
