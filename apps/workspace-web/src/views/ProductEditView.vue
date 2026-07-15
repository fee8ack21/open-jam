<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { useMessage } from 'naive-ui'
import { useI18n } from 'vue-i18n'
import { storeToRefs } from 'pinia'
import { useDashboardStore } from '@/stores/dashboard'
import { useCatalogStore } from '@/stores/catalog'
import { useCatalogEditStore, type CatalogBasics } from '@/stores/catalogEdit'
import { CatalogStatus, type CatalogVersionDto } from '@/api/catalog-service'

const { t, locale } = useI18n()

const STATUS = {
  [CatalogStatus.Published]: { labelKey: 'catalogStatus.published', type: 'success' as const },
  [CatalogStatus.Draft]:     { labelKey: 'catalogStatus.draft',     type: 'default' as const },
  [CatalogStatus.Archived]:  { labelKey: 'catalogStatus.archived',  type: 'warning' as const },
  [CatalogStatus.Suspended]: { labelKey: 'catalogStatus.suspended', type: 'error' as const },
}

// 與後端 CatalogAssetContentTypes 對齊：封面（Thumbnail）僅接受圖片
const COVER_ACCEPT = 'image/png,image/jpeg,image/gif,image/webp'
const COVER_ALLOWED = ['image/jpeg', 'image/png', 'image/gif', 'image/webp']
const COVER_MAX_BYTES = 5 * 1024 * 1024

const MAX_TAGS = 5
// 與上架精靈相同的色票（後端接受 0–359）
const HUE_OPTIONS = [256, 320, 28, 168, 44, 198, 142, 226]
const HUE_PASTELS = ['#dff5d3', '#e4f6ff', '#ffe3f6', '#fff3c4', '#ede6ff']
function huePastel(h: number) { return HUE_PASTELS[Math.abs(h) % HUE_PASTELS.length] }

const route = useRoute()
const message = useMessage()
const dashboard = useDashboardStore()
const catalogList = useCatalogStore()
const edit = useCatalogEditStore()
const { catalog, versions, loading, saving, busy } = storeToRefs(edit)

const catalogId = computed(() => String(route.params.id ?? ''))

const form = ref<CatalogBasics>({
  name: '', summary: '', description: '', coverHue: HUE_OPTIONS[0], price: 0, categoryId: null, tags: [],
})
const free = ref(false)
const tagDraft = ref('')
const coverInput = ref<HTMLInputElement | null>(null)
// 上傳與 confirm 期間先顯示使用者選的圖，成功後改由後端 URL 接手
const coverPreview = ref('')

// 新版本 modal
const versionModal = ref(false)
const versionForm = ref({ version: '', releaseNote: '' })
// 各版本的檔案輸入（版本 id → input element）
const versionInputs = ref<Record<string, HTMLInputElement | null>>({})

const statusMeta = computed(() =>
  (catalog.value?.status && STATUS[catalog.value.status]) || { labelKey: 'catalogStatus.unknown', type: 'default' as const },
)
// 頂層分類（子分類不在此挑選，與上架精靈一致）；清空選擇即為不指定分類。
const categoryOptions = computed(() =>
  catalogList.categories
    .filter((c) => !c.parentId)
    .slice()
    .sort((a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0))
    .map((c) => ({ label: c.name ?? c.slug ?? '', value: c.id! })),
)
const coverUrl = computed(() => coverPreview.value || catalog.value?.thumbnailUrl || '')
const currentVersionId = computed(() => catalog.value?.currentVersion?.id ?? null)

/** 目前表單與後端資料是否有差異（未變更時儲存鈕維持禁用）。 */
const dirty = computed(() => {
  const c = catalog.value
  if (!c) return false
  const price = free.value ? 0 : form.value.price
  const sameTags =
    form.value.tags.length === (c.tags?.length ?? 0) &&
    form.value.tags.every((tag, i) => tag === c.tags?.[i])
  return (
    form.value.name.trim() !== (c.name ?? '') ||
    form.value.summary.trim() !== (c.summary ?? '') ||
    form.value.description.trim() !== (c.description ?? '') ||
    form.value.coverHue !== (c.coverHue ?? HUE_OPTIONS[0]) ||
    price !== (c.price ?? 0) ||
    (form.value.categoryId ?? null) !== (c.categoryId ?? null) ||
    !sameTags
  )
})
const canSave = computed(() => form.value.name.trim().length >= 1 && dirty.value && !saving.value)

/** 以後端資料重置表單（載入完成 / 儲存成功後對齊）。 */
function syncForm() {
  const c = catalog.value
  if (!c) return
  form.value = {
    name: c.name ?? '',
    summary: c.summary ?? '',
    description: c.description ?? '',
    coverHue: c.coverHue ?? HUE_OPTIONS[0],
    price: c.price ?? 0,
    categoryId: c.categoryId ?? null,
    tags: [...(c.tags ?? [])],
  }
  free.value = (c.price ?? 0) === 0
}
watch(catalog, syncForm)

onMounted(async () => {
  await Promise.all([catalogList.loadCategories(), edit.load(catalogId.value)])
})

onBeforeUnmount(() => {
  if (coverPreview.value) URL.revokeObjectURL(coverPreview.value)
})

function setCoverPreview(file: File | null) {
  if (coverPreview.value) URL.revokeObjectURL(coverPreview.value)
  coverPreview.value = file ? URL.createObjectURL(file) : ''
}

function addTag() {
  const tag = tagDraft.value.trim().toLowerCase()
  tagDraft.value = ''
  if (!tag || form.value.tags.includes(tag) || form.value.tags.length >= MAX_TAGS) return
  form.value.tags.push(tag)
}
function removeTag(tag: string) {
  form.value.tags = form.value.tags.filter((x) => x !== tag)
}

async function onSave() {
  if (!canSave.value) return
  const ok = await edit.saveBasics(catalogId.value, { ...form.value, price: free.value ? 0 : form.value.price })
  if (ok) message.success(t('productEdit.msgSaved'))
  else message.error(edit.error ?? t('productEdit.msgSaveFailed'))
}

function pickCover() { coverInput.value?.click() }

async function onCoverPick(e: Event) {
  const input = e.target as HTMLInputElement
  const file = input.files?.[0]
  input.value = ''
  if (!file) return
  if (!COVER_ALLOWED.includes(file.type)) { message.error(t('productEdit.msgCoverType')); return }
  if (file.size > COVER_MAX_BYTES) { message.error(t('productEdit.msgCoverTooLarge')); return }

  setCoverPreview(file)
  const ok = await edit.uploadCover(catalogId.value, file)
  setCoverPreview(null)
  if (ok) message.success(t('productEdit.msgCoverUpdated'))
  else message.error(edit.error ?? t('productEdit.msgCoverFailed'))
}

async function onCoverRemove() {
  const ok = await edit.removeCover(catalogId.value)
  if (ok) message.success(t('productEdit.msgCoverRemoved'))
  else message.error(edit.error ?? t('productEdit.msgCoverFailed'))
}

function openVersionModal() {
  versionForm.value = { version: '', releaseNote: '' }
  versionModal.value = true
}
async function onCreateVersion() {
  const name = versionForm.value.version.trim()
  if (!name) { message.warning(t('productEdit.msgNeedVersion')); return }
  const ok = await edit.createVersion(catalogId.value, name, versionForm.value.releaseNote)
  if (ok) {
    versionModal.value = false
    message.success(t('productEdit.msgVersionCreated'))
  } else {
    message.error(edit.error ?? t('productEdit.msgVersionFailed'))
  }
}

function pickVersionFile(versionId: string) { versionInputs.value[versionId]?.click() }

async function onVersionFiles(e: Event, versionId: string) {
  const input = e.target as HTMLInputElement
  const files = Array.from(input.files ?? [])
  input.value = ''
  for (const file of files) {
    const ok = await edit.uploadVersionFile(catalogId.value, versionId, file)
    if (!ok) { message.error(edit.error ?? t('productEdit.msgFileFailed', { name: file.name })); return }
  }
  if (files.length) message.success(t('productEdit.msgFilesUploaded', { count: files.length }))
}

async function onDeleteFile(versionId: string, assetId: string) {
  const ok = await edit.deleteVersionFile(catalogId.value, versionId, assetId)
  if (ok) message.success(t('productEdit.msgFileDeleted'))
  else message.error(edit.error ?? t('productEdit.msgFileDeleteFailed'))
}

async function onDownloadFile(versionId: string, assetId: string) {
  const url = await edit.versionFileDownloadUrl(catalogId.value, versionId, assetId)
  if (url) window.open(url, '_blank', 'noopener')
  else message.error(edit.error ?? t('productEdit.msgDownloadFailed'))
}

function fileType(name: string) { return (name.split('.').pop() || 'FILE').toUpperCase().slice(0, 4) }
function versionSize(v: CatalogVersionDto) {
  return dashboard.fmtBytes((v.assets ?? []).reduce((s, a) => s + (a.fileSize ?? 0), 0))
}
function fmtDate(v?: string | null) {
  return v ? new Date(v).toLocaleDateString(locale.value) : '—'
}
</script>

<template>
  <div :data-screen-label="t('route.productEdit')">
    <div class="page-head" style="margin-bottom:24px; justify-content:flex-start;">
      <button class="link-btn" style="font-size:14px;" @click="dashboard.go('products')">
        <app-icon name="arrowLeft" :size="16" /> {{ t('productEdit.backToProducts') }}
      </button>
    </div>

    <n-spin :show="loading">
      <!-- 載入失敗（不存在 / 非本人商品）：不留空白頁 -->
      <div v-if="!loading && !catalog" class="card-pad">
        <div class="empty-box">
          <div class="eb-ic"><app-icon name="box" :size="30" /></div>
          <div class="eb-t">{{ t('productEdit.notFoundTitle') }}</div>
          <div class="eb-hand">{{ edit.error ?? t('productEdit.notFoundDesc') }}</div>
          <div style="margin-top:20px;">
            <n-button @click="dashboard.go('products')">{{ t('productEdit.backToProducts') }}</n-button>
          </div>
        </div>
      </div>

      <div v-else-if="catalog" class="dash-grid" style="gap:18px;">
        <!-- 標題列：商品名稱 + 狀態 -->
        <div class="card-pad set-card">
          <div class="pe-head">
            <div style="min-width:0;">
              <h2 class="pe-title">{{ catalog.name }}</h2>
              <div class="pe-slug">{{ catalog.slug }}</div>
            </div>
            <n-tag :type="statusMeta.type" size="small" round>{{ t(statusMeta.labelKey) }}</n-tag>
          </div>
        </div>

        <!-- 基本資料 -->
        <div class="card-pad set-card">
          <div class="set-grid">
            <div class="sg-k">
              <div class="sgk-t">{{ t('productEdit.basicsTitle') }}</div>
              <div class="sgk-d">{{ t('productEdit.basicsDesc') }}</div>
            </div>
            <div>
              <div class="field">
                <label class="field-label">{{ t('productEdit.nameLabel') }}</label>
                <n-input v-model:value="form.name" size="large" maxlength="200" show-count
                         :placeholder="t('productEdit.namePlaceholder')" />
              </div>

              <div class="field">
                <label class="field-label">{{ t('productEdit.summaryLabel') }}</label>
                <n-input v-model:value="form.summary" type="textarea" size="large"
                         :autosize="{ minRows: 2, maxRows: 3 }" maxlength="200" show-count
                         :placeholder="t('productEdit.summaryPlaceholder')" />
              </div>

              <div class="field">
                <label class="field-label">{{ t('productEdit.descriptionLabel') }}</label>
                <n-input v-model:value="form.description" type="textarea" size="large"
                         :autosize="{ minRows: 4, maxRows: 12 }"
                         :placeholder="t('productEdit.descriptionPlaceholder')" />
                <div class="field-hint">{{ t('productEdit.descriptionHint') }}</div>
              </div>

              <div class="field">
                <label class="field-label">{{ t('productEdit.categoryLabel') }}</label>
                <n-select v-model:value="form.categoryId" :options="categoryOptions" size="large"
                          clearable :placeholder="t('productEdit.noCategory')" />
              </div>

              <div class="field">
                <label class="field-label">{{ t('productEdit.tagsLabel') }}</label>
                <div class="chip-input">
                  <span v-for="tag in form.tags" :key="tag" class="chip-rm">
                    {{ tag }}
                    <button @click="removeTag(tag)"><app-icon name="close" :size="13" /></button>
                  </span>
                  <input v-model="tagDraft" :disabled="form.tags.length >= MAX_TAGS"
                         :placeholder="t('productEdit.tagsPlaceholder')" @keydown.enter.prevent="addTag" />
                </div>
                <div class="field-hint">{{ t('productEdit.tagsHint', { max: MAX_TAGS }) }}</div>
              </div>

              <div class="field">
                <label class="field-label">{{ t('productEdit.priceLabel') }}</label>
                <div class="price-input">
                  <n-input-number v-model:value="form.price" :disabled="free" :min="0" :step="1"
                                  size="large" style="width:180px;">
                    <template #prefix>$</template>
                  </n-input-number>
                  <button class="free-toggle" :class="{ on: free }" @click="free = !free">
                    <app-icon :name="free ? 'check' : 'tag'" :size="16" /> {{ t('productEdit.freeToggle') }}
                  </button>
                </div>
              </div>

              <div class="field">
                <label class="field-label">{{ t('productEdit.coverHueLabel') }}</label>
                <div style="display:flex; gap:10px; flex-wrap:wrap;">
                  <button v-for="h in HUE_OPTIONS" :key="h" class="hue-dot"
                          :class="{ on: form.coverHue === h }"
                          :style="{ background: huePastel(h) }"
                          @click="form.coverHue = h"></button>
                </div>
                <div class="field-hint">{{ t('productEdit.coverHueHint') }}</div>
              </div>

              <div style="display:flex; justify-content:flex-end; margin-top:16px;">
                <n-button type="primary" size="large" strong :disabled="!canSave" :loading="saving" @click="onSave">
                  {{ t('common.save') }}
                </n-button>
              </div>
            </div>
          </div>
        </div>

        <!-- 封面圖 -->
        <div class="card-pad set-card">
          <div class="set-grid">
            <div class="sg-k">
              <div class="sgk-t">{{ t('productEdit.coverTitle') }}</div>
              <div class="sgk-d">{{ t('productEdit.coverDesc') }}</div>
            </div>
            <div>
              <input ref="coverInput" type="file" :accept="COVER_ACCEPT" style="display:none" @change="onCoverPick" />
              <div class="cover-row">
                <div class="cover-box" :class="{ empty: !coverUrl }"
                     :style="coverUrl ? { backgroundImage: `url(${coverUrl})` } : { background: huePastel(form.coverHue) }"
                     @click="pickCover">
                  <div v-if="!coverUrl" class="cover-empty">
                    <app-icon name="image" :size="24" />
                    <span>{{ t('productEdit.coverEmpty') }}</span>
                  </div>
                </div>
                <div class="cover-actions">
                  <n-button size="medium" :loading="busy" :disabled="busy" @click="pickCover">
                    <template #icon><app-icon name="upload" :size="16" /></template>
                    {{ catalog.thumbnailUrl ? t('productEdit.coverReplace') : t('productEdit.coverUpload') }}
                  </n-button>
                  <n-button v-if="catalog.thumbnailUrl" size="medium" quaternary :disabled="busy" @click="onCoverRemove">
                    {{ t('productEdit.coverRemove') }}
                  </n-button>
                  <div class="field-hint">{{ t('productEdit.coverHint') }}</div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- 版本與可下載檔 -->
        <div class="card-pad set-card">
          <div class="set-grid">
            <div class="sg-k">
              <div class="sgk-t">{{ t('productEdit.versionsTitle') }}</div>
              <div class="sgk-d">{{ t('productEdit.versionsDesc') }}</div>
            </div>
            <div>
              <div style="display:flex; justify-content:flex-end; margin-bottom:14px;">
                <n-button size="medium" :disabled="busy" @click="openVersionModal">
                  <template #icon><app-icon name="plus" :size="16" /></template>
                  {{ t('productEdit.newVersion') }}
                </n-button>
              </div>

              <div v-if="!versions.length" class="empty-box" style="padding:32px 16px;">
                <div class="eb-t">{{ t('productEdit.noVersions') }}</div>
                <div class="eb-hand">{{ t('productEdit.noVersionsHint') }}</div>
              </div>

              <div v-for="v in versions" :key="v.id" class="ver-block">
                <div class="ver-head">
                  <div style="min-width:0;">
                    <div class="ver-name">
                      {{ v.version }}
                      <span v-if="v.id === currentVersionId" class="ver-current">{{ t('productEdit.currentVersion') }}</span>
                    </div>
                    <div class="ver-meta">
                      {{ t('productEdit.versionMeta', { count: v.assets?.length ?? 0, size: versionSize(v), date: fmtDate(v.createdAt) }) }}
                    </div>
                    <div v-if="v.releaseNote" class="ver-note">{{ v.releaseNote }}</div>
                  </div>
                  <input :ref="el => (versionInputs[v.id!] = el as HTMLInputElement)" type="file" multiple
                         style="display:none" @change="e => onVersionFiles(e, v.id!)" />
                  <n-button size="small" :disabled="busy" @click="pickVersionFile(v.id!)">
                    <template #icon><app-icon name="upload" :size="14" /></template>
                    {{ t('productEdit.addFiles') }}
                  </n-button>
                </div>

                <div v-if="!v.assets?.length" class="ver-nofile">{{ t('productEdit.noFiles') }}</div>
                <div v-else class="upload-list" style="margin-top:12px;">
                  <div v-for="a in v.assets" :key="a.id" class="upload-row">
                    <span class="file-ic" style="background:var(--c-violet);">{{ fileType(a.fileName ?? '') }}</span>
                    <div class="ur-body">
                      <div class="ur-name">{{ a.fileName }}</div>
                      <div class="ur-meta">{{ dashboard.fmtBytes(a.fileSize ?? 0) }}</div>
                    </div>
                    <button class="ic-act" :title="t('productEdit.downloadFile')" :disabled="busy"
                            @click="onDownloadFile(v.id!, a.id!)">
                      <app-icon name="download" :size="16" />
                    </button>
                    <n-popconfirm @positive-click="onDeleteFile(v.id!, a.id!)">
                      <template #trigger>
                        <button class="ic-act" :title="t('common.delete')" :disabled="busy">
                          <app-icon name="trash" :size="16" />
                        </button>
                      </template>
                      {{ t('productEdit.deleteFileConfirm') }}
                    </n-popconfirm>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </n-spin>

    <!-- 建立新版本 -->
    <n-modal v-model:show="versionModal" preset="card" style="max-width:460px;" :title="t('productEdit.newVersion')">
      <div class="field">
        <label class="field-label">{{ t('productEdit.versionLabel') }}</label>
        <n-input v-model:value="versionForm.version" size="large" maxlength="50"
                 :placeholder="t('productEdit.versionPlaceholder')" />
      </div>
      <div class="field">
        <label class="field-label">{{ t('productEdit.releaseNoteLabel') }}</label>
        <n-input v-model:value="versionForm.releaseNote" type="textarea" size="large"
                 :autosize="{ minRows: 2, maxRows: 5 }" :placeholder="t('productEdit.releaseNotePlaceholder')" />
      </div>
      <div class="field-hint" style="margin-top:12px;">{{ t('productEdit.newVersionHint') }}</div>
      <template #footer>
        <div style="display:flex; justify-content:flex-end; gap:10px;">
          <n-button @click="versionModal = false">{{ t('common.cancel') }}</n-button>
          <n-button type="primary" :loading="busy" @click="onCreateVersion">{{ t('common.confirm') }}</n-button>
        </div>
      </template>
    </n-modal>
  </div>
</template>

<style scoped>
.set-card { border-radius: var(--r-lg); }

.pe-head { display: flex; align-items: center; justify-content: space-between; gap: 14px; }
.pe-title { font-family: var(--oj-font); font-weight: 900; font-size: 20px; margin: 0; }
.pe-slug { font-family: var(--oj-mono); font-size: 12px; color: var(--text-faint); margin-top: 4px; }

.field-hint { font-size: 12px; color: var(--text-faint); margin-top: 8px; line-height: 1.5; }

.hue-dot {
  width: 42px; height: 42px; border-radius: 12px; cursor: pointer;
  border: var(--bw) solid var(--border-strong); transition: all .15s;
}
.hue-dot.on { box-shadow: var(--ink-drop-sm); transform: rotate(-3deg); }

/* 封面 */
.cover-row { display: flex; align-items: flex-start; gap: 18px; flex-wrap: wrap; }
.cover-box {
  width: 220px; height: 150px; flex: none; cursor: pointer;
  border-radius: var(--r-md); border: var(--bw) solid var(--border-strong);
  background-size: cover; background-position: center;
  box-shadow: var(--ink-drop-sm); display: grid; place-items: center;
  transition: filter .15s;
}
.cover-box:hover { filter: brightness(.94); }
.cover-box.empty { border-style: dashed; box-shadow: none; }
.cover-empty { display: grid; justify-items: center; gap: 8px; color: var(--text-faint); font-size: 12.5px; font-weight: 700; }
.cover-actions { display: flex; flex-direction: column; align-items: flex-start; gap: 10px; }

/* 版本 */
.ver-block {
  padding: 16px; border-radius: var(--r-md);
  border: var(--bw) solid var(--border-strong); background: var(--surface);
}
.ver-block + .ver-block { margin-top: 14px; }
.ver-head { display: flex; align-items: flex-start; justify-content: space-between; gap: 14px; }
.ver-name { font-family: var(--oj-font); font-weight: 900; font-size: 15px; display: flex; align-items: center; gap: 8px; }
.ver-current {
  font-family: var(--oj-display); font-size: 10.5px; font-weight: 700; letter-spacing: .06em;
  padding: 2px 8px; border-radius: var(--r-pill); background: var(--c-lime);
  border: var(--bw) solid var(--border-strong);
}
.ver-meta { font-family: var(--oj-display); font-size: 11.5px; color: var(--text-faint); margin-top: 4px; }
.ver-note { font-size: 13px; color: var(--text-soft); margin-top: 8px; line-height: 1.5; white-space: pre-wrap; }
.ver-nofile {
  margin-top: 12px; padding: 16px; text-align: center; border-radius: var(--r-sm);
  border: var(--bw) dashed var(--border); color: var(--text-faint); font-size: 12.5px; font-weight: 700;
}
</style>
