<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, reactive, ref } from 'vue'
import { useMessage } from 'naive-ui'
import { useI18n } from 'vue-i18n'
import { useDashboardStore } from '@/stores/dashboard'
import { useCatalogStore } from '@/stores/catalog'
import { useStoreApplicationStore } from '@/stores/storeApplication'
import { useConnectStatusStore } from '@/stores/connectStatus'
import ImageCropDialog from '@/components/ImageCropDialog.vue'
import { JFmt } from '@/utils/format'
import { TAGS } from '@/data/products'
import { CatalogAssetType } from '@/api/catalog-service'

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
// 預覽媒體（同商品編輯頁）：影片接受 mp4 / mov / avi / webm
const VIDEO_ALLOWED = ['video/mp4', 'video/quicktime', 'video/x-msvideo', 'video/webm']
const VIDEO_MAX_BYTES = 100 * 1024 * 1024
const MEDIA_ACCEPT = `${COVER_ACCEPT},${VIDEO_ALLOWED.join(',')}`
const MAX_PREVIEW_MEDIA = 8
// 付費商品最低定價（與後端 CatalogValidators.MinPaidPrice 對齊，避免低於 Stripe 最低收費）
const MIN_PAID_PRICE = 30

const store = useDashboardStore()
const message = useMessage()
const catalog = useCatalogStore()
const storeApp = useStoreApplicationStore()
const connect = useConnectStatusStore()
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

// 預覽媒體（商品內頁圖庫）：圖片 / 影片選檔即直傳（不 confirm、不計配額），送出時才 confirm；
// YouTube 嵌入不涉檔案，貼上連結當下即在 Draft catalog 建立（移除時同步刪除）。
interface MediaEntry {
  key: string
  kind: 'image' | 'video' | 'youtube'
  file: File | null
  assetId: string | null
  status: 'uploading' | 'uploaded' | 'error'
  /** image / video 為本地 object URL（video 僅供放大檢視播放）；youtube 為 ytimg 縮圖 URL */
  preview: string
  /** YouTube 影片 ID（放大檢視 embed 用，僅 kind = 'youtube'） */
  ytId?: string
}
const mediaEntries = ref<MediaEntry[]>([])
const mediaInput = ref<HTMLInputElement | null>(null)
const uploadedMedia = computed(() => mediaEntries.value.filter(e => e.status === 'uploaded'))

const anyUploading = computed(() =>
  entries.value.some(e => e.status === 'uploading')
  || cover.value?.status === 'uploading'
  || mediaEntries.value.some(e => e.status === 'uploading'),
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
// 付費商品上架前主動擋下（收款未就緒）；查詢失敗不擋，交由後端上架閘門回 422。
// 僅擋「直接上架」，存成草稿不受限。
const payoutBlocked = computed(() => !d.value.free && connect.chargesEnabled === false)
const suggestedTags = computed(() =>
  catMeta(d.value.cat).tags.filter(t => !d.value.tags.includes(t)).slice(0, 6),
)
const totalBytes = computed(() => entries.value.reduce((s, e) => s + e.file.size, 0))
const totalSize = computed(() => store.fmtBytes(totalBytes.value) || '—')
const fileFormats = computed(() => [...new Set(entries.value.map(e => fileType(e.file.name)))])
const previewProduct = computed(() => ({
  cat: catMeta(d.value.cat).thumb, hue: d.value.coverHue,
  title: d.value.title || t('upload.previewTitlePlaceholder'),
  // 店家資訊套用實際商店（router guard 已先載入開店資料）
  creator: storeApp.primaryStore?.storeName ?? '',
  avatarUrl: storeApp.primaryStore?.avatarUrl ?? '',
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
  // 每次開啟精靈都從乾淨狀態開始：清空 step-1 草稿欄位（含步驟回到 1）與
  // Draft 上傳關聯。檔案本就隨元件卸載清空，中途放棄的舊草稿留在後端為 Draft，
  // 殘留的欄位文字若掛到新草稿上反而不一致。
  store.resetDraft()
  catalog.resetDraftUpload()
  // 收款狀態供付費商品上架前主動擋下（router guard 已先載入開店資料）
  if (storeId.value) connect.load(storeId.value)
  await catalog.loadCategories()
  // draft 預設 cat 不是有效 slug，載入後對齊到第一個分類。
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

// ── 預覽媒體 ────────────────────────────────────────────────
function pickMediaFiles() { mediaInput.value?.click() }

async function startMediaUpload(entry: MediaEntry) {
  if (!entry.file) return
  entry.status = 'uploading'
  const assetId = await catalog.uploadDraftPreviewMedia(draftMeta(), entry.file)
  if (assetId) {
    entry.assetId = assetId
    entry.status = 'uploaded'
  } else {
    entry.status = 'error'
    message.error(catalog.error ?? t('upload.msgUploadFailed', { name: entry.file.name }))
  }
}

function onMediaPick(e: Event) {
  const input = e.target as HTMLInputElement
  const files = Array.from(input.files ?? [])
  input.value = ''
  if (!files.length) return
  if (!storeId.value) { message.error(t('upload.msgNoStore')); return }
  const room = MAX_PREVIEW_MEDIA - mediaEntries.value.length
  if (files.length > room) { message.warning(t('upload.msgMediaLimit', { max: MAX_PREVIEW_MEDIA })); return }
  for (const file of files) {
    if (VIDEO_ALLOWED.includes(file.type)) {
      if (file.size > VIDEO_MAX_BYTES) { message.error(t('upload.msgVideoTooLarge')); return }
    } else if (COVER_ALLOWED.includes(file.type)) {
      if (file.size > COVER_MAX_BYTES) { message.error(t('upload.msgCoverTooLarge')); return }
    } else {
      message.error(t('upload.msgMediaType')); return
    }
  }
  for (const file of files) {
    const isVideo = VIDEO_ALLOWED.includes(file.type)
    const entry = reactive<MediaEntry>({
      key: `${Date.now()}-${Math.random().toString(36).slice(2)}`,
      kind: isVideo ? 'video' : 'image',
      file,
      assetId: null,
      status: 'uploading',
      preview: URL.createObjectURL(file),
    })
    mediaEntries.value.push(entry)
    void startMediaUpload(entry)
  }
}

// 點擊媒體磚放大檢視（大圖 / 影片播放 / YouTube 嵌入）
const mediaViewer = ref<MediaEntry | null>(null)
const ytEmbedUrl = (id: string) => `https://www.youtube-nocookie.com/embed/${id}`

// YouTube 嵌入：貼上連結即在 Draft catalog 建立
const youtubeModal = ref(false)
const youtubeUrl = ref('')
const youtubeAdding = ref(false)

function ytIdFromUrl(url: string) {
  const m = /[?&]v=([A-Za-z0-9_-]{11})/.exec(url)
    ?? /youtu\.be\/([A-Za-z0-9_-]{11})/.exec(url)
    ?? /\/(?:shorts|embed|live)\/([A-Za-z0-9_-]{11})/.exec(url)
  return m ? m[1] : null
}

function openYoutubeModal() {
  if (!storeId.value) { message.error(t('upload.msgNoStore')); return }
  youtubeUrl.value = ''
  youtubeModal.value = true
}

async function onAddYoutube() {
  const url = youtubeUrl.value.trim()
  if (!url) { message.warning(t('upload.msgNeedYoutubeUrl')); return }
  if (mediaEntries.value.length >= MAX_PREVIEW_MEDIA) { message.warning(t('upload.msgMediaLimit', { max: MAX_PREVIEW_MEDIA })); return }
  youtubeAdding.value = true
  const assetId = await catalog.addDraftExternalVideo(draftMeta(), url)
  youtubeAdding.value = false
  if (!assetId) { message.error(catalog.error ?? t('upload.msgYoutubeFailed')); return }
  const id = ytIdFromUrl(url)
  mediaEntries.value.push(reactive<MediaEntry>({
    key: `${Date.now()}-${Math.random().toString(36).slice(2)}`,
    kind: 'youtube',
    file: null,
    assetId,
    status: 'uploaded',
    preview: id ? `https://i.ytimg.com/vi/${id}/hqdefault.jpg` : '',
    ytId: id ?? undefined,
  }))
  youtubeModal.value = false
}

/** 前移 / 後移預覽媒體（本地排序，送出時以最終順序呼叫 reorder）。 */
function moveMedia(i: number, dir: -1 | 1) {
  const j = i + dir
  if (j < 0 || j >= mediaEntries.value.length) return
  const arr = mediaEntries.value
  ;[arr[i], arr[j]] = [arr[j], arr[i]]
}

// 移除預覽媒體：檔案僅本地移除（未 confirm 不計配額）；YouTube 已建立於後端，需同步刪除。
async function removeMedia(entry: MediaEntry) {
  if (entry.kind === 'youtube' && entry.assetId) {
    const ok = await catalog.deleteDraftAsset(entry.assetId)
    if (!ok) { message.error(catalog.error ?? t('upload.msgMediaFailed')); return }
  }
  if (entry.kind !== 'youtube' && entry.preview) URL.revokeObjectURL(entry.preview)
  const i = mediaEntries.value.indexOf(entry)
  if (i >= 0) mediaEntries.value.splice(i, 1)
}

/** 釋放預覽媒體的本地 object URL 並清空清單。 */
function clearMediaEntries() {
  for (const m of mediaEntries.value)
    if (m.kind !== 'youtube' && m.preview) URL.revokeObjectURL(m.preview)
  mediaEntries.value = []
}

onBeforeUnmount(() => {
  if (cover.value?.preview) URL.revokeObjectURL(cover.value.preview)
  clearMediaEntries()
})

function goNext() {
  if (step.value === 1 && !step1Valid.value) return
  if (step.value === 2 && !step2Valid.value) return
  store.nextStep()
}

async function submit(publish: boolean) {
  if (!storeId.value) { message.error(t('upload.msgNoStore')); return }
  if (publish && payoutBlocked.value) { message.warning(t('common.payoutRequired')); return }
  if (!step1Valid.value) { message.warning(t('upload.msgNeedTitle')); return }
  // 清空定價欄位會落回 0，繞過 :min 夾制；付費商品於送出前再擋一次。
  if (!d.value.free && (d.value.price ?? 0) < MIN_PAID_PRICE) { message.warning(t('upload.minPriceHint', { min: MIN_PAID_PRICE })); return }
  if (anyUploading.value) { message.warning(t('upload.msgUploadInProgress')); return }
  if (cover.value?.status === 'error') { message.warning(t('upload.msgCoverNotReady')); return }
  if (mediaEntries.value.some(e => e.status === 'error')) { message.warning(t('upload.msgMediaNotReady')); return }
  const assetIds = uploadedEntries.value.map(e => e.assetId!).filter(Boolean)
  if (!assetIds.length) { message.warning(t('upload.msgNeedFile')); return }

  // YouTube 嵌入建立當下已完成，僅上傳的圖片 / 影片需在送出時 confirm。
  const previewMedia = uploadedMedia.value
    .filter(e => e.kind !== 'youtube' && e.assetId)
    .map(e => ({
      assetId: e.assetId!,
      type: e.kind === 'video' ? CatalogAssetType.PreviewVideo : CatalogAssetType.Screenshot,
    }))

  // 使用者於精靈中排定的最終順序（含 YouTube 嵌入）。
  const previewOrder = uploadedMedia.value.map(e => e.assetId!).filter(Boolean)

  const created = await catalog.finalizeDraft(
    draftMeta(), assetIds, publish, cover.value?.assetId ?? null, previewMedia, previewOrder)

  if (created) {
    message.success(publish ? t('upload.msgPublished') : t('upload.msgDrafted'))
    entries.value = []
    removeCover()
    clearMediaEntries()
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
                              :disabled="d.free" :min="d.free ? 0 : MIN_PAID_PRICE" :max="999" :step="1" size="large" style="width:180px;">
                <template #prefix>$</template>
              </n-input-number>
              <button class="free-toggle" :class="{ on: d.free }"
                      @click="store.patchDraft(d.free ? { free: false, price: Math.max(d.price, MIN_PAID_PRICE) } : { free: true })">
                <app-icon :name="d.free ? 'check' : 'tag'" :size="16" :stroke="2.2" /> {{ t('upload.freeToggle') }}
              </button>
              <span style="font-size:12.5px; color:var(--text-faint); font-family:var(--oj-mono);">{{ t('upload.platformFee') }}</span>
            </div>
            <div v-if="!d.free" style="margin-top:8px; font-size:12.5px; color:var(--text-faint); font-family:var(--oj-mono);">{{ t('upload.minPriceHint', { min: MIN_PAID_PRICE }) }}</div>
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
            <h4 class="fb-title">{{ t('upload.mediaLabel') }}</h4>
            <p class="fb-sub">{{ t('upload.mediaSub') }}</p>
            <input ref="mediaInput" type="file" :accept="MEDIA_ACCEPT" multiple style="display:none" @change="onMediaPick" />
            <div class="shot-grid">
              <div v-for="(m, i) in mediaEntries" :key="m.key" class="shot-box shot-clickable"
                   :class="{ 'shot-media': m.kind !== 'image', 'shot-error': m.status === 'error' }"
                   :style="m.kind !== 'video' && m.preview ? { backgroundImage: `url(${m.preview})` } : undefined"
                   @click="mediaViewer = m">
                <span v-if="m.kind !== 'image'" class="shot-play"><app-icon name="play" :size="15" /></span>
                <span v-if="m.kind === 'video'" class="shot-name">{{ m.file?.name }}</span>
                <span v-if="m.status === 'uploading'" class="shot-veil"><span class="ur-spin"></span></span>
                <span class="shot-moves">
                  <button class="ic-act" :title="t('upload.mediaMoveForward')" :disabled="i === 0"
                          @click.stop="moveMedia(i, -1)"><app-icon name="chevronL" :size="14" /></button>
                  <button class="ic-act" :title="t('upload.mediaMoveBackward')" :disabled="i === mediaEntries.length - 1"
                          @click.stop="moveMedia(i, 1)"><app-icon name="chevronR" :size="14" /></button>
                </span>
                <button v-if="m.status === 'error'" class="ic-act shot-retry" :title="t('upload.retryUpload')" @click.stop="startMediaUpload(m)">
                  <app-icon name="refresh" :size="15" />
                </button>
                <button class="ic-act danger shot-del" :disabled="m.status === 'uploading'" @click.stop="removeMedia(m)">
                  <app-icon name="trash" :size="15" />
                </button>
              </div>
              <button v-if="mediaEntries.length < MAX_PREVIEW_MEDIA" class="shot-box shot-add" @click="pickMediaFiles">
                <app-icon name="plus" :size="20" :stroke="2.2" />
                <span>{{ t('upload.mediaAdd') }}</span>
              </button>
              <button v-if="mediaEntries.length < MAX_PREVIEW_MEDIA" class="shot-box shot-add" @click="openYoutubeModal">
                <app-icon name="play" :size="20" :stroke="2.2" />
                <span>{{ t('upload.mediaAddYoutube') }}</span>
              </button>
            </div>
            <p class="fb-sub" style="margin-top:10px; margin-bottom:0;">{{ t('upload.mediaHint', { max: MAX_PREVIEW_MEDIA }) }}</p>
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
              <div class="rev-row"><span class="rev-k">{{ t('upload.revMedia') }}</span><span class="rev-v">{{ uploadedMedia.length ? t('upload.revMediaValue', { count: uploadedMedia.length }) : '—' }}</span></div>
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
            <n-tooltip v-else :disabled="!payoutBlocked" style="max-width:260px;">
              <template #trigger>
                <n-button type="primary" size="large" :loading="catalog.creating" :disabled="catalog.creating || anyUploading || payoutBlocked" @click="submit(true)">
                  <template #icon><app-icon name="rocket" :size="17" /></template>
                  {{ t('upload.publish') }}
                </n-button>
              </template>
              {{ t('common.payoutRequired') }}
            </n-tooltip>
          </div>
        </div>
      </div>

      <!-- 預覽媒體放大檢視 -->
      <n-modal :show="!!mediaViewer" preset="card" style="max-width:720px;"
               :title="mediaViewer?.kind === 'video' ? (mediaViewer?.file?.name ?? '') : t('upload.mediaLabel')"
               @update:show="(v: boolean) => { if (!v) mediaViewer = null }">
        <template v-if="mediaViewer">
          <img v-if="mediaViewer.kind === 'image'" class="media-viewer" :src="mediaViewer.preview" alt="" />
          <video v-else-if="mediaViewer.kind === 'video'" class="media-viewer" :src="mediaViewer.preview"
                 controls autoplay preload="metadata"></video>
          <iframe v-else-if="mediaViewer.ytId" class="media-viewer media-viewer-frame"
                  :src="ytEmbedUrl(mediaViewer.ytId)"
                  title="YouTube video player" frameborder="0" allowfullscreen
                  allow="accelerometer; clipboard-write; encrypted-media; gyroscope; picture-in-picture"></iframe>
        </template>
      </n-modal>

      <!-- 加入 YouTube 連結（同商品編輯頁） -->
      <n-modal v-model:show="youtubeModal" preset="card" style="max-width:460px;" :title="t('upload.youtubeModalTitle')">
        <div class="field">
          <label class="field-label">{{ t('upload.youtubeUrlLabel') }}</label>
          <n-input v-model:value="youtubeUrl" size="large" maxlength="500"
                   :placeholder="t('upload.youtubeUrlPlaceholder')" @keydown.enter="onAddYoutube" />
        </div>
        <div class="field-hint" style="margin-top:12px;">{{ t('upload.youtubeHint') }}</div>
        <template #footer>
          <div style="display:flex; justify-content:flex-end; gap:10px;">
            <n-button @click="youtubeModal = false">{{ t('common.cancel') }}</n-button>
            <n-button type="primary" :loading="youtubeAdding" @click="onAddYoutube">{{ t('common.confirm') }}</n-button>
          </div>
        </template>
      </n-modal>

      <!-- ============ RIGHT: live preview ============ -->
      <div class="preview-rail">
        <div class="pr-label"><app-icon name="eye" :size="14" /> {{ t('upload.previewLabel') }}</div>
        <div class="preview-card">
          <product-thumb :product="previewProduct" :image="cover?.preview ?? ''" />
          <div class="card-body">
            <h3 class="card-title">{{ previewProduct.title }}</h3>
            <div class="card-creator">
              <span class="avatar"
                    :style="previewProduct.avatarUrl
                      ? { backgroundImage: `url(${previewProduct.avatarUrl})`, backgroundSize: 'cover', backgroundPosition: 'center' }
                      : { background: '#8a5cf6' }">
                <template v-if="!previewProduct.avatarUrl">{{ F.initials(previewProduct.creator) }}</template>
              </span>
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

/* 預覽媒體格線（與商品編輯頁一致）：4:3 磚 + 右上刪除鍵 + 新增磚 */
.shot-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(140px, 1fr));
  gap: 12px;
}
.shot-box {
  position: relative;
  aspect-ratio: 4 / 3;
  border-radius: var(--r-md);
  border: var(--bw) solid var(--border-strong);
  background-size: cover;
  background-position: center;
  box-shadow: var(--ink-drop-sm);
}
.shot-del {
  position: absolute;
  top: 6px;
  right: 6px;
  background: var(--bg);
}
.shot-retry {
  position: absolute;
  top: 6px;
  right: 40px;
  background: var(--bg);
}
.shot-add {
  display: grid;
  place-items: center;
  align-content: center;
  gap: 6px;
  cursor: pointer;
  border-style: dashed;
  box-shadow: none;
  background: var(--surface);
  color: var(--text-faint);
  font-size: 12.5px;
  font-weight: 700;
  transition: filter .15s;
}
.shot-add:hover:not(:disabled) { filter: brightness(.96); }
.shot-add:disabled { cursor: not-allowed; opacity: .6; }
/* 影片 / YouTube 磚：深色底 + 中央播放徽章（YouTube 另以縮圖為背景） */
.shot-media { background-color: #1a1a1a; }
.shot-play {
  position: absolute; top: 50%; left: 50%; transform: translate(-50%, -50%);
  width: 32px; height: 32px; border-radius: 999px; display: grid; place-items: center;
  background: rgba(255, 255, 255, .92); border: var(--bw) solid var(--border-strong);
  color: var(--text); pointer-events: none;
}
.shot-name {
  position: absolute; left: 6px; right: 6px; bottom: 6px;
  font-family: var(--oj-mono); font-size: 10.5px; color: #fff;
  white-space: nowrap; overflow: hidden; text-overflow: ellipsis;
  pointer-events: none;
}
/* 左下移動鈕（前移 / 後移）；影片磚檔名讓位給移動鈕 */
.shot-moves {
  position: absolute;
  left: 6px;
  bottom: 6px;
  display: inline-flex;
  gap: 4px;
}
.shot-moves .ic-act { background: var(--bg); }
.shot-media .shot-name { left: 70px; }
/* 點擊放大檢視 */
.shot-clickable { cursor: zoom-in; }
.media-viewer { display: block; width: 100%; max-height: 70vh; object-fit: contain; border-radius: var(--r-md); background: #1a1a1a; }
img.media-viewer { background: transparent; }
.media-viewer-frame { aspect-ratio: 16 / 9; border: 0; }

/* 上傳中遮罩與失敗框線 */
.shot-veil {
  position: absolute; inset: 0; display: grid; place-items: center;
  background: rgba(255, 255, 255, .55); border-radius: inherit;
}
.shot-veil .ur-spin {
  width: 18px; height: 18px; border-radius: 50%; display: inline-block;
  border: 3px solid var(--border-strong); border-top-color: transparent;
  animation: cover-spin .7s linear infinite;
}
.shot-error { border-color: var(--c-pink-deep); }
</style>
