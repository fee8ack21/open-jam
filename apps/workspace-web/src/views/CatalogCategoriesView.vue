<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useMessage } from 'naive-ui'
import { catalogApi } from '@/api'
import type { CatalogCategoryDto } from '@/api/catalog-service'

const ROOT_PARENT = '__root__'
const SLUG_RE = /^[a-z0-9]+(?:-[a-z0-9]+)*$/

interface CategoryRow extends CatalogCategoryDto {
  depth: number
}

// TODO(mock): layout review only. Remove when CatalogCategory data is available from backend seed/admin input.
const MOCK_CATEGORIES: CatalogCategoryDto[] = [
  {
    id: '00000000-0000-0000-0000-000000000201',
    parentId: null,
    name: '音樂與音效',
    slug: 'audio',
    sortOrder: 0,
  },
  {
    id: '00000000-0000-0000-0000-000000000202',
    parentId: '00000000-0000-0000-0000-000000000201',
    name: '背景音樂',
    slug: 'background-music',
    sortOrder: 0,
  },
]

const message = useMessage()
const categories = ref<CatalogCategoryDto[]>([])
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

function messageOf(err: unknown, fallback = '操作失敗，請稍後再試。') {
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
    const data = res.data ?? []
    categories.value = data.length ? data : [...MOCK_CATEGORIES]
  } catch (err) {
    message.error(messageOf(err, '載入商品分類失敗。'))
    categories.value = [...MOCK_CATEGORIES]
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
  return categories.value
    .filter((category) => (activeParentId.value ? category.parentId === activeParentId.value : !category.parentId))
    .slice()
    .sort((a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0) || (a.name ?? '').localeCompare(b.name ?? '', 'zh-Hant'))
    .map((category) => ({ ...category, depth: activeParentId.value ? 1 : 0 }))
})

const currentParent = computed(() => categories.value.find((category) => category.id === activeParentId.value) ?? null)
const isChildList = computed(() => currentParent.value != null)
const emptyText = computed(() => isChildList.value ? '尚未建立子分類' : '尚未建立商品分類')
const emptyHint = computed(() => isChildList.value ? '在此頂層分類底下新增第一個子分類。' : '新增頂層分類後，可再建立子分類。')

const parentOptions = computed(() => {
  const blocked = descendantsOf(editing.value?.id)
  return [
    { label: '頂層分類', value: ROOT_PARENT },
    ...categories.value
      .filter((category) => category.id && !blocked.has(category.id))
      .sort((a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0) || (a.name ?? '').localeCompare(b.name ?? '', 'zh-Hant'))
      .map((category) => ({
        label: `${category.parentId ? '　' : ''}${category.name ?? '未命名分類'}`,
        value: category.id!,
      })),
  ]
})

function showRootCategories() {
  activeParentId.value = null
}

function showChildCategories(category: CatalogCategoryDto) {
  if (!category.id || category.parentId) return
  activeParentId.value = category.id
}

function validateForm() {
  const name = form.name.trim()
  const slug = form.slug.trim().toLowerCase()
  if (!name) return '請輸入分類名稱。'
  if (name.length > 100) return '分類名稱不可超過 100 字。'
  if (!slug) return '請輸入分類代稱。'
  if (slug.length < 3 || slug.length > 100 || !SLUG_RE.test(slug)) return '分類代稱需為 3–100 字小寫英數字與連字號。'
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
      message.success('已更新商品分類。')
    } else {
      await catalogApi.catalogCategories.create(payload)
      message.success('已建立商品分類。')
    }
    modalOpen.value = false
    await load()
  } catch (err) {
    message.error(messageOf(err, editing.value ? '更新商品分類失敗。' : '建立商品分類失敗。'))
  } finally {
    saving.value = false
  }
}

async function remove(category: CatalogCategoryDto) {
  if (!category.id) return
  deletingId.value = category.id
  try {
    await catalogApi.catalogCategories.delete(category.id)
    message.success('已刪除商品分類。')
    await load()
  } catch (err) {
    message.error(messageOf(err, '此分類仍有子分類或商品引用，無法刪除。'))
  } finally {
    deletingId.value = null
  }
}

onMounted(load)
</script>

<template>
  <div data-screen-label="商品分類">
    <div class="page-head">
      <div>
        <p class="h-eyebrow">平台管理</p>
        <h1 class="h-title">{{ isChildList ? currentParent?.name : '商品分類' }}</h1>
        <p class="h-sub">{{ isChildList ? '管理此頂層分類底下的子分類' : `共 ${categories.length} 個平台分類，用於市集瀏覽與商品歸類` }}</p>
      </div>
      <n-button type="primary" size="large" @click="openCreate(activeParentId)">
        <template #icon><app-icon name="plus" :size="16" /></template>
        {{ isChildList ? '新增子分類' : '新增分類' }}
      </n-button>
    </div>

    <div class="category-breadcrumb">
      <button :class="{ on: !isChildList }" @click="showRootCategories">商品分類</button>
      <template v-if="isChildList">
        <span>/</span>
        <strong>{{ currentParent?.name }}</strong>
      </template>
    </div>

    <n-spin :show="loading">
      <div class="card-pad category-table-card" style="padding:8px 8px 4px;">
        <table class="tbl category-table">
          <thead>
            <tr>
              <th>分類</th>
              <th class="hide-sm">Slug</th>
              <th class="num hide-sm">排序</th>
              <th style="width:170px; text-align:right;">操作</th>
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
            <tr v-for="category in rows" v-else :key="category.id">
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
                    <div class="pc-meta">{{ category.parentId ? '子分類' : '頂層分類' }}</div>
                  </div>
                </div>
              </td>
              <td class="hide-sm" style="font-family:var(--oj-mono); color:var(--text-soft);">{{ category.slug }}</td>
              <td class="num hide-sm">{{ category.sortOrder ?? 0 }}</td>
              <td>
                <div class="row-actions">
                  <button class="ic-act" title="編輯" @click="openEdit(category)"><app-icon name="edit" :size="17" /></button>
                  <n-popconfirm @positive-click="remove(category)">
                    <template #trigger>
                      <button class="ic-act danger" title="刪除" :disabled="deletingId === category.id"><app-icon name="trash" :size="17" /></button>
                    </template>
                    刪除後無法復原；若仍有子分類或商品引用，後端會拒絕刪除。
                  </n-popconfirm>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </n-spin>

    <n-modal v-model:show="modalOpen" preset="card" :title="editing ? '編輯分類' : '新增分類'" style="max-width:520px" to=".oj-root">
      <div style="display:grid; gap:16px;">
        <div>
          <label class="field-label">分類名稱</label>
          <n-input v-model:value="form.name" maxlength="100" show-count placeholder="例：音樂與音效" />
        </div>
        <div>
          <label class="field-label">分類代稱</label>
          <n-input v-model:value="form.slug" placeholder="例：audio" />
          <div style="font-size:12px; color:var(--text-faint); margin-top:6px;">僅允許小寫英數字與連字號，且全平台唯一。</div>
        </div>
        <div>
          <label class="field-label">上層分類</label>
          <n-select v-model:value="form.parentKey" :options="parentOptions" />
        </div>
        <div>
          <label class="field-label">同層排序</label>
          <n-input-number v-model:value="form.sortOrder" style="width:100%;" :min="0" :step="1" />
        </div>
      </div>
      <template #footer>
        <div style="display:flex; justify-content:flex-end; gap:10px;">
          <n-button :disabled="saving" @click="modalOpen = false">取消</n-button>
          <n-button type="primary" :loading="saving" @click="save">{{ editing ? '儲存變更' : '建立分類' }}</n-button>
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
  border-radius: 10px;
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
  color: var(--c-pink);
}

.category-table thead th + th {
  border-left: 1.5px solid var(--border);
}

.category-table tbody td + td {
  border-left: 1.5px solid var(--border);
}
</style>
