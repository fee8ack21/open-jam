<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, reactive, ref } from 'vue'
import { useMessage } from 'naive-ui'
import { useI18n } from 'vue-i18n'
import { useDashboardStore } from '@/stores/dashboard'
import { useCatalogStore } from '@/stores/catalog'
import { useStoreApplicationStore } from '@/stores/storeApplication'
import ImageCropDialog from '@/components/ImageCropDialog.vue'
import { JFmt } from '@/utils/format'
import { TAGS, ME } from '@/data/products'

const { t } = useI18n()

const STEPS = [
  { n: 1, k: 'STEP 01', labelKey: 'upload.step1' },
  { n: 2, k: 'STEP 02', labelKey: 'upload.step2' },
  { n: 3, k: 'STEP 03', labelKey: 'upload.step3' },
]

// 後端分類 slug → 前端展示資訊（icon / 顏色 / 描述鍵 / 建議標籤 / 縮圖類別）；
// API 僅提供 name / slug，這些純展示欄位由前端對應，未知 slug 走 fallback。
const CAT_META: Record<string, { color: string; glyph: string; descKey: string; tags: string[]; thumb: string }> = {
  music:       { color: 'var(--c-violet)', glyph: 'note',  descKey: 'upload.catDescMusic', tags: TAGS.music, thumb: 'music' },
  photography: { color: 'var(--c-pink)',   glyph: 'image', descKey: 'upload.catDescPhoto', tags: TAGS.photo, thumb: 'photo' },
  ebook:       { color: 'var(--c-cyan)',   glyph: 'book',  descKey: 'upload.catDescEbook', tags: TAGS.ebook, thumb: 'ebook' },
}
const CAT_FALLBACK = { color: 'var(--c-violet)', glyph: 'tag', descKey: '', tags: [] as string[], thumb: 'photo' }
function catMeta(slug: string) { return CAT_META[slug] ?? CAT_FALLBACK }

const TYPE_COLOR: Record<string, string> = { ZIP: '#8a5cf6', XMP: '#7dd9ff', PDF: '#d6479b', JPG: '#ff6b35', PNG: '#ff6b35', PSD: '#7dd9ff', WAV: '#8a5cf6', MP3: '#d6479b', FIG: '#ff6b35' }

// 與後端 CatalogAssetContentTypes 對齊：封面（Thumbnail）僅接受圖片（同商品編輯頁）
const COVER_ACCEPT = 'image/png,image/jpeg,image/gif,image/webp'
const COVER_ALLOWED = ['image/jpeg', 'image/png', 'image/gif', 'image/webp']
const COVER_MAX_BYTES = 5 * 1024 * 1024

const store = useDashboardStore()
const message = useMessage()
const catalog = useCatalogStore()
const storeApp = useStoreApplicationStore()
const F = JFmt

const tagDraft = ref('')
const dragging = ref(false)
const fileInput = ref<HTMLInputElement | null>(null)

// 選檔即時上傳：每個項目在加入當下就簽 URL 直傳（不 confirm、不計配額），
// 送出時才對 uploaded 的資產 confirm。File 物件無法序列化，不存入 dashboard draft。
interface UploadEntry {
  key: string
  file: File
  assetId: string | null
  status: 'uploading' | 'uploaded' | 'error'
}
const entries = ref<UploadEntry[]>([])
const uploadedEntries = computed(() => entries.value.filter(e => e.status === 'uploaded'))

// 封面圖（展示型 Thumbnail 資產）：選圖即直傳（不 confirm、不計配額），送出時才 confirm。
// preview 為本地 object URL，未上傳 / 上傳中即可即時預覽，色調封面作為圖片載入前的 fallback。
interface CoverEntry {
  file: File
  assetId: string | null
  status: 'uploading' | 'uploaded' | 'error'
  preview: string
}
const cover = ref<CoverEntry | null>(null)
const coverInput = ref<HTMLInputElement | null>(null)

const anyUploading = computed(() =>
  entries.value.some(e => e.status === 'uploading') || cover.value?.status === 'uploading',
)

const step = computed(() => store.wizardStep)
const d = computed(() => store.draft)
const steps = STEPS

// 頂層商品分類由 CatalogService API 取得（slug + name），子分類不在上架卡片呈現。
const cats = computed(() =>
  catalog.categories
    .filter(c => !c.parentId)
    .slice()
    .sort((a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0))
    .map(c => {
      const meta = catMeta(c.slug ?? '')
      // 敘述優先採用後端分類的 description，未設定時退回前端對應文案。
      const fallbackDesc = meta.descKey ? t(meta.descKey) : ''
      return { slug: c.slug ?? '', label: c.name ?? c.slug ?? '', ...meta, desc: c.description || fallbackDesc }
    }),
)

const storeId = computed(() => storeApp.stores[0]?.store?.id ?? '')
const suggestedTags = computed(() =>
  catMeta(d.value.cat).tags.filter(t => !d.value.tags.includes(t)).slice(0, 6),
)
const totalBytes = computed(() => entries.value.reduce((s, e) => s + e.file.size, 0))
const totalSize = computed(() => store.fmtBytes(totalBytes.value) || '—')
const fileFormats = computed(() => [...new Set(entries.value.map(e => fileType(e.file.name)))])
const previewProduct = computed(() => ({
  cat: catMeta(d.value.cat).thumb, hue: d.value.coverHue,
  title: d.value.title || t('upload.previewTitlePlaceholder'),
  creator: ME.name, avatar: ME.avatar,
  tags: d.value.tags.length ? d.value.tags : [t('upload.tagPlaceholder')],
  price: d.value.free ? 0 : d.value.price,
  rating: 0,
  formats: fileFormats.value.length ? fileFormats.value : [t('upload.formatPlaceholder')],
  totalSize: totalSize.value,
}))
const step1Valid = computed(() => d.value.title.trim().length >= 2)
// 至少一個檔案成功上傳才可離開 step 2（上傳中 / 失敗的不算）。
const step2Valid = computed(() => uploadedEntries.value.length > 0)
const hueOptions = [256, 320, 28, 168, 44, 198, 142, 226]
// 色票預覽：與 ProductThumb 相同的 hue → 便條淡彩對應
const HUE_PASTELS = ['#dff5d3', '#e4f6ff', '#ffe3f6', '#fff3c4', '#ede6ff']
function huePastel(h: number) { return HUE_PASTELS[Math.abs(h) % HUE_PASTELS.length] }

onMounted(async () => {
  // 每次開啟精靈都從乾淨的 Draft 上傳狀態開始（未送出的舊草稿留在後端為 Draft）。
  catalog.resetDraftUpload()
  await catalog.loadCategories()
  // draft 預設 / localStorage 殘留可能不是有效 slug，載入後對齊到第一個分類。
  const slugs = cats.value.map(c => c.slug)
  if (!slugs.includes(d.value.cat) && slugs.length) store.patchDraft({ cat: slugs[0] })
})

function fileType(name: string) { return (name.split('.').pop() || 'FILE').toUpperCase().slice(0, 4) }
function typeColor(t: string) { return TYPE_COLOR[t] || 'var(--c-violet)' }
function addTag() { if (tagDraft.value.trim()) { store.addDraftTag(tagDraft.value); tagDraft.value = '' } }

// step-1 欄位快照，供建立 Draft catalog 與送出時同步。
function draftMeta() {
  return {
    storeId: storeId.value,
    name: d.value.title,
    categorySlug: d.value.cat || null,
    summary: d.value.blurb,
    coverHue: d.value.coverHue,
    price: d.value.free ? 0 : d.value.price,
    tags: d.value.tags,
  }
}

async function startUpload(entry: UploadEntry) {
  entry.status = 'uploading'
  const assetId = await catalog.uploadDraftAsset(draftMeta(), entry.file)
  if (assetId) {
    entry.assetId = assetId
    entry.status = 'uploaded'
  } else {
    entry.status = 'error'
    message.error(catalog.error ?? t('upload.msgUploadFailed', { name: entry.file.name }))
  }
}

function addFiles(list: File[]) {
  if (!storeId.value) { message.error(t('upload.msgNoStore')); return }
  for (const file of list) {
    const entry = reactive<UploadEntry>({ key: `${Date.now()}-${Math.random().toString(36).slice(2)}`, file, assetId: null, status: 'uploading' })
    entries.value.push(entry)
    void startUpload(entry)
  }
}

function pickFiles() { fileInput.value?.click() }
function onFiles(e: Event) {
  const list = (e.target as HTMLInputElement).files
  if (list) addFiles(Array.from(list))
  if (fileInput.value) fileInput.value.value = ''
}
function onDrop(e: DragEvent) {
  dragging.value = false
  const list = e.dataTransfer?.files
  if (list) addFiles(Array.from(list))
}
// 移除：僅從清單移除。已上傳但未 confirm 的檔案不計配額，逾期由 StorageService 清理。
function removeFile(entry: UploadEntry) {
  const i = entries.value.indexOf(entry)
  if (i >= 0) entries.value.splice(i, 1)
}

function pickCover() { coverInput.value?.click() }

async function startCoverUpload(entry: CoverEntry) {
  entry.status = 'uploading'
  const assetId = await catalog.uploadDraftCover(draftMeta(), entry.file)
  if (assetId) {
    entry.assetId = assetId
    entry.status = 'uploaded'
  } else {
    entry.status = 'error'
    message.error(catalog.error ?? t('upload.msgUploadFailed', { name: entry.file.name }))
  }
}

// 裁切視窗：選檔通過驗證後先進裁切，「套用」才進上傳（同商店外觀設定）
const cropShow = ref(false)
const cropFile = ref<File | null>(null)

function onCoverPick(e: Event) {
  const input = e.target as HTMLInputElement
  const file = input.files?.[0]
  input.value = ''
  if (!file) return
  if (!storeId.value) { message.error(t('upload.msgNoStore')); return }
  if (!COVER_ALLOWED.includes(file.type)) { message.error(t('upload.msgCoverType')); return }
  if (file.size > COVER_MAX_BYTES) { message.error(t('upload.msgCoverTooLarge')); return }

  // GIF 經 canvas 裁切會失去動畫，維持原檔直傳
  if (file.type === 'image/gif') { acceptCover(file); return }
  cropFile.value = file
  cropShow.value = true
}

function onCropped(file: File) {
  // 裁切輸出理論上只會更小，仍防呆再驗一次上限
  if (file.size > COVER_MAX_BYTES) { message.error(t('upload.msgCoverTooLarge')); return }
  acceptCover(file)
}

function acceptCover(file: File) {
  removeCover()
  const entry = reactive<CoverEntry>({ file, assetId: null, status: 'uploading', preview: URL.createObjectURL(file) })
  cover.value = entry
  void startCoverUpload(entry)
}

// 移除封面圖：釋放本地預覽並清空。已直傳但未 confirm 的檔案不計配額，逾期由 StorageService 清理。
function removeCover() {
  if (cover.value?.preview) URL.revokeObjectURL(cover.value.preview)
  cover.value = null
}

onBeforeUnmount(() => {
  if (cover.value?.preview) URL.revokeObjectURL(cover.value.preview)
})

function goNext() {
  if (step.value === 1 && !step1Valid.value) return
  if (step.value === 2 && !step2Valid.value) return
  store.nextStep()
}

async function submit(publish: boolean) {
  if (!storeId.value) { message.error(t('upload.msgNoStore')); return }
  if (!step1Valid.value) { message.warning(t('upload.msgNeedTitle')); return }
  if (anyUploading.value) { message.warning(t('upload.msgUploadInProgress')); return }
  if (cover.value?.status === 'error') { message.warning(t('upload.msgCoverNotReady')); return }
  const assetIds = uploadedEntries.value.map(e => e.assetId!).filter(Boolean)
  if (!assetIds.length) { message.warning(t('upload.msgNeedFile')); return }

  const created = await catalog.finalizeDraft(draftMeta(), assetIds, publish, cover.value?.assetId ?? null)

  if (created) {
    message.success(publish ? t('upload.msgPublished') : t('upload.msgDrafted'))
    entries.value = []
    removeCover()
    store.resetDraft()
    store.go('products')
  } else {
    message.error(catalog.error ?? t('upload.msgCreateFailed'))
  }
}
</script>

<template>
  <div :data-screen-label="t('route.upload')">
    <div class="page-head" style="margin-bottom:24px; justify-content:flex-start;">
      <button class="link-btn" style="font-size:14px;" @click="store.go('products')"><app-icon name="arrowLeft" :size="16" /> {{ t('upload.backToProducts') }}</button>
    </div>

    <!-- stepper -->
    <div class="stepper">
      <template v-for="(st, i) in steps" :key="st.n">
        <div class="step" :class="{ on: step === st.n, done: step > st.n }" @click="step > st.n && store.setStep(st.n)">
          <div class="step-bub"><app-icon v-if="step > st.n" name="check" :size="18" :stroke="2.6" /><span v-else>{{ st.n }}</span></div>
          <div class="step-txt"><div class="st-k">{{ st.k }}</div><div class="st-l">{{ t(st.labelKey) }}</div></div>
        </div>
        <div v-if="i < steps.length - 1" class="step-link" :class="{ done: step > st.n }"></div>
      </template>
    </div>

    <div class="wizard-grid">
      <!-- ============ LEFT: form ============ -->
      <div class="card-pad">

        <!-- STEP 1 -->
        <template v-if="step === 1">
          <div class="form-block">
            <h4 class="fb-title">{{ t('upload.titleLabel') }}</h4>
            <p class="fb-sub">{{ t('upload.titleSub') }}</p>
            <n-input :value="d.title" @update:value="(v: string) => store.patchDraft({ title: v })"
                     :placeholder="t('upload.titlePlaceholder')" size="large" maxlength="60" show-count />
          </div>

          <div class="form-block">
            <h4 class="fb-title">{{ t('upload.categoryLabel') }}</h4>
            <div class="cat-cards">
              <div v-for="c in cats" :key="c.slug" class="cat-card" :class="{ on: d.cat === c.slug }"
                   @click="store.patchDraft({ cat: c.slug })">
                <span class="cc-ic" :style="{ background: c.color }"><app-icon :name="c.glyph" :size="20" /></span>
                <div>
                  <div class="cc-l">{{ c.label }}</div>
                  <div class="cc-d">{{ c.desc }}</div>
                </div>
              </div>
            </div>
          </div>

          <div class="form-block">
            <h4 class="fb-title">{{ t('upload.tagsLabel') }}</h4>
            <p class="fb-sub">{{ t('upload.tagsSub') }}</p>
            <div class="chip-input">
              <span v-for="tag in d.tags" :key="tag" class="chip-rm">
                {{ tag }}
                <button @click="store.removeDraftTag(tag)"><app-icon name="close" :size="13" :stroke="2.4" /></button>
              </span>
              <input v-model="tagDraft" :disabled="d.tags.length >= 5" :placeholder="t('upload.tagsInputPlaceholder')"
                     @keydown.enter.prevent="addTag" />
            </div>
            <div class="tagrow" style="margin-top:12px;" v-if="suggestedTags.length && d.tags.length < 5">
              <button v-for="tag in suggestedTags" :key="tag" class="tag-sug" @click="store.addDraftTag(tag)">+ {{ tag }}</button>
            </div>
          </div>

          <div class="form-block">
            <h4 class="fb-title">{{ t('upload.priceLabel') }}</h4>
            <div class="price-input">
              <n-input-number :value="d.price" @update:value="(v: number | null) => store.patchDraft({ price: v || 0 })"
                              :disabled="d.free" :min="0" :max="999" :step="1" size="large" style="width:180px;">
                <template #prefix>$</template>
              </n-input-number>
              <button class="free-toggle" :class="{ on: d.free }" @click="store.patchDraft({ free: !d.free })">
                <app-icon :name="d.free ? 'check' : 'tag'" :size="16" :stroke="2.2" /> {{ t('upload.freeToggle') }}
              </button>
              <span style="font-size:12.5px; color:var(--text-faint); font-family:var(--oj-mono);">{{ t('upload.platformFee') }}</span>
            </div>
          </div>

          <div class="form-block">
            <h4 class="fb-title">{{ t('upload.blurbLabel') }}</h4>
            <n-input :value="d.blurb" @update:value="(v: string) => store.patchDraft({ blurb: v })"
                     type="textarea" :placeholder="t('upload.blurbPlaceholder')" :autosize="{ minRows: 2, maxRows: 4 }" maxlength="80" show-count />
          </div>
        </template>

        <!-- STEP 2 -->
        <template v-else-if="step === 2">
          <div class="form-block">
            <h4 class="fb-title">{{ t('upload.uploadLabel') }}</h4>
            <p class="fb-sub">{{ t('upload.uploadSub') }}</p>
            <input ref="fileInput" type="file" multiple style="display:none" @change="onFiles" />
            <div class="dropzone" :class="{ drag: dragging }"
                 @click="pickFiles" @dragover.prevent="dragging = true" @dragleave="dragging = false"
                 @drop.prevent="onDrop">
              <div class="dz-ic"><app-icon name="upload" :size="28" :stroke="2.2" /></div>
              <div class="dz-title">{{ t('upload.dropzoneTitle') }}</div>
              <div class="dz-sub">{{ t('upload.dropzoneSub') }}</div>
            </div>

            <div class="upload-list" v-if="entries.length">
              <div v-for="e in entries" :key="e.key" class="upload-row" :class="'st-' + e.status">
                <span class="file-ic" :style="{ background: typeColor(fileType(e.file.name)) }">{{ fileType(e.file.name) }}</span>
                <div class="ur-body">
                  <div class="ur-name">{{ e.file.name }}</div>
                  <div class="ur-meta">
                    {{ store.fmtBytes(e.file.size) }}
                    <span class="ur-status" :class="e.status">
                      <template v-if="e.status === 'uploading'"><span class="ur-spin"></span> {{ t('upload.statusUploading') }}</template>
                      <template v-else-if="e.status === 'uploaded'"><app-icon name="check" :size="12" :stroke="2.8" /> {{ t('upload.statusUploaded') }}</template>
                      <template v-else>{{ t('upload.statusError') }}</template>
                    </span>
                  </div>
                </div>
                <button v-if="e.status === 'error'" class="ic-act" :title="t('upload.retryUpload')" @click="startUpload(e)"><app-icon name="refresh" :size="16" /></button>
                <button class="ic-act danger" :disabled="e.status === 'uploading'" @click="removeFile(e)"><app-icon name="trash" :size="16" /></button>
              </div>
            </div>
          </div>

          <div class="form-block">
            <h4 class="fb-title">{{ t('upload.coverImgLabel') }}</h4>
            <p class="fb-sub">{{ t('upload.coverImgSub') }}</p>
            <input ref="coverInput" type="file" :accept="COVER_ACCEPT" style="display:none" @change="onCoverPick" />
            <!-- 選檔後先裁切（4:3 + 中央正方形安全區），套用才上傳；GIF 不裁切 -->
            <ImageCropDialog v-model:show="cropShow" :file="cropFile" kind="cover" @cropped="onCropped" />
            <div class="cover-row">
              <div class="cover-box" :class="{ 'is-empty': !cover }"
                   :style="cover ? { backgroundImage: `url(${cover.preview})`, backgroundColor: huePastel(d.coverHue) } : { background: huePastel(d.coverHue) }"
                   @click="pickCover">
                <div v-if="!cover" class="cover-empty">
                  <app-icon name="image" :size="22" />
                  <span>{{ t('upload.coverEmpty') }}</span>
                </div>
              </div>
              <div class="cover-actions">
                <n-button size="medium" :loading="cover?.status === 'uploading'" :disabled="cover?.status === 'uploading'" @click="pickCover">
                  <template #icon><app-icon name="image" :size="16" /></template>
                  {{ cover ? t('upload.coverReplace') : t('upload.coverUpload') }}
                </n-button>
                <n-button v-if="cover?.status === 'error'" size="medium" @click="startCoverUpload(cover!)">
                  <template #icon><app-icon name="refresh" :size="16" /></template>
                  {{ t('upload.retryUpload') }}
                </n-button>
                <n-button v-if="cover" size="medium" quaternary :disabled="cover.status === 'uploading'" @click="removeCover">
                  {{ t('upload.coverRemove') }}
                </n-button>
                <span v-if="cover" class="ur-status" :class="cover.status" style="font-size:12px;">
                  <template v-if="cover.status === 'uploading'"><span class="ur-spin"></span> {{ t('upload.statusUploading') }}</template>
                  <template v-else-if="cover.status === 'uploaded'"><app-icon name="check" :size="12" :stroke="2.8" /> {{ t('upload.statusUploaded') }}</template>
                  <template v-else>{{ t('upload.statusError') }}</template>
                </span>
              </div>
            </div>
          </div>

          <div class="form-block">
            <h4 class="fb-title">{{ t('upload.coverHueLabel') }}</h4>
            <p class="fb-sub">{{ t('upload.coverHueSub') }}</p>
            <div style="display:flex; gap:10px; flex-wrap:wrap;">
              <button v-for="h in hueOptions" :key="h" @click="store.patchDraft({ coverHue: h })"
                      :style="{ width:'42px', height:'42px', borderRadius:'12px', cursor:'pointer',
                                background: huePastel(h),
                                border: 'var(--bw) solid var(--border-strong)',
                                boxShadow: d.coverHue===h ? 'var(--ink-drop-sm)' : 'none',
                                transform: d.coverHue===h ? 'rotate(-3deg)' : 'none', transition:'all .15s' }"></button>
            </div>
          </div>
        </template>

        <!-- STEP 3 -->
        <template v-else>
          <div class="form-block">
            <h4 class="fb-title">{{ t('upload.confirmTitle') }}</h4>
            <p class="fb-sub">{{ t('upload.confirmSub') }}</p>
            <div style="margin-top:6px;">
              <div class="rev-row"><span class="rev-k">{{ t('upload.revTitle') }}</span><span class="rev-v">{{ d.title || t('upload.untitled') }}</span></div>
              <div class="rev-row"><span class="rev-k">{{ t('upload.revCategory') }}</span><span class="rev-v">{{ cats.find(c => c.slug === d.cat)?.label || '—' }}</span></div>
              <div class="rev-row"><span class="rev-k">{{ t('upload.revTags') }}</span><span class="rev-v">{{ d.tags.length ? d.tags.join('、') : '—' }}</span></div>
              <div class="rev-row"><span class="rev-k">{{ t('upload.revPrice') }}</span><span class="rev-v">{{ d.free ? t('common.free') : '$' + d.price }}</span></div>
              <div class="rev-row"><span class="rev-k">{{ t('upload.revFiles') }}</span><span class="rev-v">{{ t('upload.revFilesValue', { count: uploadedEntries.length, size: totalSize }) }}</span></div>
              <div class="rev-row"><span class="rev-k">{{ t('upload.revCover') }}</span><span class="rev-v">{{ cover?.status === 'uploaded' ? cover.file.name : t('upload.revCoverNone') }}</span></div>
              <div class="rev-row"><span class="rev-k">{{ t('upload.revBlurb') }}</span><span class="rev-v" style="max-width:280px;">{{ d.blurb || '—' }}</span></div>
            </div>
          </div>
          <div style="display:flex; align-items:center; gap:10px; padding:14px 16px; border-radius:var(--r-md); background:var(--t-violet); border:var(--bw) solid var(--border-strong); color:var(--text); font-size:13.5px; font-weight:700;">
            <app-icon name="shield" :size="18" /> {{ t('upload.copyrightNote') }}
          </div>
        </template>

        <!-- footer nav -->
        <div class="wizard-foot">
          <button class="free-toggle" @click="step > 1 ? store.prevStep() : store.go('products')">
            <app-icon name="arrowLeft" :size="16" /> {{ step > 1 ? t('upload.prevStep') : t('upload.cancel') }}
          </button>
          <div style="display:flex; gap:10px; align-items:center;">
            <n-button v-if="step === 3" size="large" :disabled="catalog.creating || anyUploading" @click="submit(false)">{{ t('upload.saveDraft') }}</n-button>
            <n-button v-if="step < 3" type="primary" size="large" :disabled="(step===1 && !step1Valid) || (step===2 && !step2Valid)" @click="goNext">
              {{ t('upload.nextStep') }}
              <template #icon><app-icon name="arrowRight" :size="17" /></template>
            </n-button>
            <n-button v-else type="primary" size="large" :loading="catalog.creating" :disabled="catalog.creating || anyUploading" @click="submit(true)">
              <template #icon><app-icon name="rocket" :size="17" /></template>
              {{ t('upload.publish') }}
            </n-button>
          </div>
        </div>
      </div>

      <!-- ============ RIGHT: live preview ============ -->
      <div class="preview-rail">
        <div class="pr-label"><app-icon name="eye" :size="14" /> {{ t('upload.previewLabel') }}</div>
        <div class="preview-card">
          <product-thumb :product="previewProduct" :image="cover?.preview ?? ''" />
          <div class="card-body">
            <h3 class="card-title">{{ previewProduct.title }}</h3>
            <div class="card-creator">
              <span class="avatar" :style="{ background: previewProduct.avatar }">{{ F.initials(previewProduct.creator) }}</span>
              {{ previewProduct.creator }}
            </div>
            <div class="tagrow">
              <span v-for="tag in previewProduct.tags" :key="tag" class="chip">{{ tag }}</span>
            </div>
            <div class="card-foot">
              <span class="price" :class="{ free: previewProduct.price === 0 }">{{ previewProduct.price === 0 ? t('common.free') : '$' + previewProduct.price }}</span>
              <span style="font-family:var(--oj-mono); font-size:11.5px; color:var(--text-faint);">{{ totalSize }}</span>
            </div>
          </div>
        </div>
        <p style="font-size:12px; color:var(--text-faint); margin-top:14px; line-height:1.6; font-family:var(--oj-font);">
          {{ t('upload.previewNote') }}
        </p>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* 封面圖（與商品編輯頁一致） */
.cover-row { display: flex; align-items: flex-start; gap: 18px; flex-wrap: wrap; }
.cover-box {
  width: 220px; height: 150px; flex: none; cursor: pointer;
  border-radius: var(--r-md); border: var(--bw) solid var(--border-strong);
  background-size: cover; background-position: center;
  box-shadow: var(--ink-drop-sm); display: grid; place-items: center;
  transition: filter .15s;
}
.cover-box:hover { filter: brightness(.94); }
/* 命名 is-empty 避免撞 base.css 全域 .empty（padding 80px 會撐高固定尺寸方塊） */
.cover-box.is-empty { border-style: dashed; box-shadow: none; }
.cover-empty { display: grid; justify-items: center; gap: 8px; color: var(--text-faint); font-size: 12.5px; font-weight: 700; }
.cover-actions { display: flex; flex-direction: column; align-items: flex-start; gap: 10px; }
.cover-actions .ur-status { display: inline-flex; align-items: center; gap: 4px; font-weight: 800; }
.cover-actions .ur-status.uploading { color: var(--text-faint); }
.cover-actions .ur-status.uploaded { color: var(--c-green, #3aa657); }
.cover-actions .ur-status.error { color: var(--c-pink-deep); }
.cover-actions .ur-spin {
  width: 11px; height: 11px; border-radius: 50%; display: inline-block;
  border: 2px solid var(--border-strong); border-top-color: transparent;
  animation: cover-spin .7s linear infinite;
}
@keyframes cover-spin { to { transform: rotate(360deg); } }
</style>
