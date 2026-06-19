<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useMessage } from 'naive-ui'
import { useDashboardStore } from '@/stores/dashboard'
import { useCatalogStore } from '@/stores/catalog'
import { useStoreApplicationStore } from '@/stores/storeApplication'
import { JFmt } from '@/utils/format'
import { TAGS, ME } from '@/data/products'

const STEPS = [
  { n: 1, k: 'STEP 01', l: '基本資訊' },
  { n: 2, k: 'STEP 02', l: '檔案與內容' },
  { n: 3, k: 'STEP 03', l: '預覽與發佈' },
]

// 後端分類 slug → 前端展示資訊（icon / 顏色 / 描述 / 建議標籤 / 縮圖類別）；
// API 僅提供 name / slug，這些純展示欄位由前端對應，未知 slug 走 fallback。
const CAT_META: Record<string, { color: string; glyph: string; desc: string; tags: string[]; thumb: string }> = {
  music:       { color: 'var(--c-violet)', glyph: 'note',  desc: '樂譜、配樂、分軌音檔', tags: TAGS.music, thumb: 'music' },
  photography: { color: 'var(--c-pink)',   glyph: 'image', desc: '照片集、RAW、預設',   tags: TAGS.photo, thumb: 'photo' },
  ebook:       { color: 'var(--c-cyan)',   glyph: 'book',  desc: '電子書、範本、文件',   tags: TAGS.ebook, thumb: 'ebook' },
}
const CAT_FALLBACK = { color: 'var(--c-violet)', glyph: 'tag', desc: '', tags: [] as string[], thumb: 'photo' }
function catMeta(slug: string) { return CAT_META[slug] ?? CAT_FALLBACK }

const TYPE_COLOR: Record<string, string> = { ZIP: '#6c4cf1', XMP: '#1fd6c6', PDF: '#ff4d9d', JPG: '#ff7a2f', PNG: '#ff7a2f', PSD: '#3b7fd4', WAV: '#8b5cf6', MP3: '#16a07a', FIG: '#d8a017' }

const store = useDashboardStore()
const message = useMessage()
const catalog = useCatalogStore()
const storeApp = useStoreApplicationStore()
const F = JFmt

const tagDraft = ref('')
const dragging = ref(false)
const fileInput = ref<HTMLInputElement | null>(null)
// 真實待上傳檔案（File 物件無法序列化，故不存入 dashboard draft）
const files = ref<File[]>([])

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
      return { slug: c.slug ?? '', label: c.name ?? c.slug ?? '', ...meta, desc: c.description || meta.desc }
    }),
)

const storeId = computed(() => storeApp.stores[0]?.store?.id ?? '')
const suggestedTags = computed(() =>
  catMeta(d.value.cat).tags.filter(t => !d.value.tags.includes(t)).slice(0, 6),
)
const totalBytes = computed(() => files.value.reduce((s, f) => s + f.size, 0))
const totalSize = computed(() => store.fmtBytes(totalBytes.value) || '—')
const fileFormats = computed(() => [...new Set(files.value.map(f => fileType(f.name)))])
const previewProduct = computed(() => ({
  cat: catMeta(d.value.cat).thumb, hue: d.value.coverHue,
  title: d.value.title || '你的作品標題會顯示在這裡',
  creator: ME.name, avatar: ME.avatar,
  tags: d.value.tags.length ? d.value.tags : ['標籤'],
  price: d.value.free ? 0 : d.value.price,
  rating: 0,
  formats: fileFormats.value.length ? fileFormats.value : ['格式'],
  totalSize: totalSize.value,
}))
const step1Valid = computed(() => d.value.title.trim().length >= 2)
const step2Valid = computed(() => files.value.length > 0)
const hueOptions = [256, 320, 28, 168, 44, 198, 142, 226]

onMounted(async () => {
  await catalog.loadCategories()
  // draft 預設 / localStorage 殘留可能不是有效 slug，載入後對齊到第一個分類。
  const slugs = cats.value.map(c => c.slug)
  if (!slugs.includes(d.value.cat) && slugs.length) store.patchDraft({ cat: slugs[0] })
})

function fileType(name: string) { return (name.split('.').pop() || 'FILE').toUpperCase().slice(0, 4) }
function typeColor(t: string) { return TYPE_COLOR[t] || 'var(--c-violet)' }
function addTag() { if (tagDraft.value.trim()) { store.addDraftTag(tagDraft.value); tagDraft.value = '' } }

function pickFiles() { fileInput.value?.click() }
function onFiles(e: Event) {
  const list = (e.target as HTMLInputElement).files
  if (list) files.value.push(...Array.from(list))
  if (fileInput.value) fileInput.value.value = ''
}
function onDrop(e: DragEvent) {
  dragging.value = false
  const list = e.dataTransfer?.files
  if (list) files.value.push(...Array.from(list))
}
function removeFile(i: number) { files.value.splice(i, 1) }

function goNext() {
  if (step.value === 1 && !step1Valid.value) return
  if (step.value === 2 && !step2Valid.value) return
  store.nextStep()
}

async function submit(publish: boolean) {
  if (!storeId.value) { message.error('尚未建立商店，無法上架。'); return }
  if (!step1Valid.value) { message.warning('請填寫作品標題。'); return }
  if (!files.value.length) { message.warning('請至少加入一個可下載檔案。'); return }

  const created = await catalog.createProduct({
    storeId: storeId.value,
    name: d.value.title,
    categorySlug: d.value.cat || null,
    summary: d.value.blurb,
    coverHue: d.value.coverHue,
    price: d.value.free ? 0 : d.value.price,
    tags: d.value.tags,
    files: files.value,
    publish,
  })

  if (created) {
    message.success(publish ? '已送出上架。' : '已存成草稿。')
    files.value = []
    store.resetDraft()
    store.go('products')
  } else {
    message.error(catalog.error ?? '建立商品失敗')
  }
}
</script>

<template>
  <div data-screen-label="上架精靈">
    <div class="page-head" style="margin-bottom:24px;">
      <div>
        <p class="h-eyebrow">賣家工作室</p>
        <h1 class="h-title">上架新作品</h1>
      </div>
      <button class="link-btn" style="font-size:14px;" @click="store.go('products')"><app-icon name="arrowLeft" :size="16" /> 回到商品管理</button>
    </div>

    <!-- stepper -->
    <div class="stepper">
      <template v-for="(st, i) in steps" :key="st.n">
        <div class="step" :class="{ on: step === st.n, done: step > st.n }" @click="step > st.n && store.setStep(st.n)">
          <div class="step-bub"><app-icon v-if="step > st.n" name="check" :size="18" :stroke="2.6" /><span v-else>{{ st.n }}</span></div>
          <div class="step-txt"><div class="st-k">{{ st.k }}</div><div class="st-l">{{ st.l }}</div></div>
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
            <h4 class="fb-title">作品標題</h4>
            <p class="fb-sub">清楚描述買家會得到什麼，最吸睛的關鍵字放前面。</p>
            <n-input :value="d.title" @update:value="(v: string) => store.patchDraft({ title: v })"
                     placeholder="例如：城市清晨・極簡建築攝影集 40 張" size="large" maxlength="60" show-count />
          </div>

          <div class="form-block">
            <h4 class="fb-title">作品分類</h4>
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
            <h4 class="fb-title">標籤</h4>
            <p class="fb-sub">最多 5 個，幫助買家在商城找到你。</p>
            <div class="chip-input">
              <span v-for="t in d.tags" :key="t" class="chip-rm">
                {{ t }}
                <button @click="store.removeDraftTag(t)"><app-icon name="close" :size="13" :stroke="2.4" /></button>
              </span>
              <input v-model="tagDraft" :disabled="d.tags.length >= 5" placeholder="輸入後按 Enter…"
                     @keydown.enter.prevent="addTag" />
            </div>
            <div class="tagrow" style="margin-top:12px;" v-if="suggestedTags.length && d.tags.length < 5">
              <button v-for="t in suggestedTags" :key="t" class="tag-sug" @click="store.addDraftTag(t)">+ {{ t }}</button>
            </div>
          </div>

          <div class="form-block">
            <h4 class="fb-title">定價</h4>
            <div class="price-input">
              <n-input-number :value="d.price" @update:value="(v: number | null) => store.patchDraft({ price: v || 0 })"
                              :disabled="d.free" :min="0" :max="999" :step="1" size="large" style="width:180px;">
                <template #prefix>$</template>
              </n-input-number>
              <button class="free-toggle" :class="{ on: d.free }" @click="store.patchDraft({ free: !d.free })">
                <app-icon :name="d.free ? 'check' : 'tag'" :size="16" :stroke="2.2" /> 免費提供
              </button>
              <span style="font-size:12.5px; color:var(--text-faint); font-family:var(--oj-mono);">平台抽成 3%</span>
            </div>
          </div>

          <div class="form-block">
            <h4 class="fb-title">一句話簡介</h4>
            <n-input :value="d.blurb" @update:value="(v: string) => store.patchDraft({ blurb: v })"
                     type="textarea" placeholder="用一句話打動買家…" :autosize="{ minRows: 2, maxRows: 4 }" maxlength="80" show-count />
          </div>
        </template>

        <!-- STEP 2 -->
        <template v-else-if="step === 2">
          <div class="form-block">
            <h4 class="fb-title">上傳檔案</h4>
            <p class="fb-sub">支援 ZIP、PDF、JPG/PNG、WAV/MP3、PSD 等格式，單檔最大 5 GB。</p>
            <input ref="fileInput" type="file" multiple style="display:none" @change="onFiles" />
            <div class="dropzone" :class="{ drag: dragging }"
                 @click="pickFiles" @dragover.prevent="dragging = true" @dragleave="dragging = false"
                 @drop.prevent="onDrop">
              <div class="dz-ic"><app-icon name="upload" :size="28" :stroke="2.2" /></div>
              <div class="dz-title">拖曳檔案到這裡，或點擊選擇</div>
              <div class="dz-sub">買家付款後即可下載這些檔案</div>
            </div>

            <div class="upload-list" v-if="files.length">
              <div v-for="(f, i) in files" :key="i" class="upload-row">
                <span class="file-ic" :style="{ background: typeColor(fileType(f.name)) }">{{ fileType(f.name) }}</span>
                <div class="ur-body">
                  <div class="ur-name">{{ f.name }}</div>
                  <div class="ur-meta">{{ store.fmtBytes(f.size) }}</div>
                </div>
                <button class="ic-act danger" @click="removeFile(i)"><app-icon name="trash" :size="16" /></button>
              </div>
            </div>
          </div>

          <div class="form-block">
            <h4 class="fb-title">封面色調</h4>
            <p class="fb-sub">作品縮圖會用這個色調生成漸層封面。</p>
            <div style="display:flex; gap:10px; flex-wrap:wrap;">
              <button v-for="h in hueOptions" :key="h" @click="store.patchDraft({ coverHue: h })"
                      :style="{ width:'42px', height:'42px', borderRadius:'12px', cursor:'pointer',
                                background:'linear-gradient(135deg, hsl('+h+' 88% 62%), hsl('+((h+42)%360)+' 90% 54%))',
                                border: d.coverHue===h ? '2.5px solid var(--text)' : '2.5px solid transparent',
                                boxShadow: d.coverHue===h ? '3px 3px 0 var(--text)' : 'none', transition:'all .14s' }"></button>
            </div>
          </div>
        </template>

        <!-- STEP 3 -->
        <template v-else>
          <div class="form-block">
            <h4 class="fb-title">發佈前確認</h4>
            <p class="fb-sub">送出後即建立商品並上架；你也可以先存成草稿稍後再上架。</p>
            <div style="margin-top:6px;">
              <div class="rev-row"><span class="rev-k">標題</span><span class="rev-v">{{ d.title || '未命名作品' }}</span></div>
              <div class="rev-row"><span class="rev-k">分類</span><span class="rev-v">{{ cats.find(c => c.slug === d.cat)?.label || '—' }}</span></div>
              <div class="rev-row"><span class="rev-k">標籤</span><span class="rev-v">{{ d.tags.length ? d.tags.join('、') : '—' }}</span></div>
              <div class="rev-row"><span class="rev-k">定價</span><span class="rev-v">{{ d.free ? '免費' : '$' + d.price }}</span></div>
              <div class="rev-row"><span class="rev-k">檔案</span><span class="rev-v">{{ files.length }} 個 · {{ totalSize }}</span></div>
              <div class="rev-row"><span class="rev-k">簡介</span><span class="rev-v" style="max-width:280px;">{{ d.blurb || '—' }}</span></div>
            </div>
          </div>
          <div style="display:flex; align-items:center; gap:10px; padding:14px 16px; border-radius:var(--r-md); background:var(--oj-primary-wash); color:var(--oj-primary); font-size:13.5px; font-weight:600;">
            <app-icon name="shield" :size="18" /> 你保留作品 100% 著作權，平台僅收取 3% 交易手續費。
          </div>
        </template>

        <!-- footer nav -->
        <div class="wizard-foot">
          <button class="free-toggle" style="border-color:var(--border)" @click="step > 1 ? store.prevStep() : store.go('products')">
            <app-icon name="arrowLeft" :size="16" /> {{ step > 1 ? '上一步' : '取消' }}
          </button>
          <div style="display:flex; gap:10px; align-items:center;">
            <n-button v-if="step === 3" size="large" :disabled="catalog.creating" @click="submit(false)">存成草稿</n-button>
            <n-button v-if="step < 3" type="primary" size="large" :disabled="(step===1 && !step1Valid) || (step===2 && !step2Valid)" @click="goNext">
              下一步
              <template #icon><app-icon name="arrowRight" :size="17" /></template>
            </n-button>
            <n-button v-else type="primary" size="large" :loading="catalog.creating" :disabled="catalog.creating" @click="submit(true)">
              <template #icon><app-icon name="rocket" :size="17" /></template>
              送出上架
            </n-button>
          </div>
        </div>
      </div>

      <!-- ============ RIGHT: live preview ============ -->
      <div class="preview-rail">
        <div class="pr-label"><app-icon name="eye" :size="14" /> 商城預覽</div>
        <div class="preview-card">
          <product-thumb :product="previewProduct" />
          <div class="card-body">
            <h3 class="card-title">{{ previewProduct.title }}</h3>
            <div class="card-creator">
              <span class="avatar" :style="{ background: previewProduct.avatar }">{{ F.initials(previewProduct.creator) }}</span>
              {{ previewProduct.creator }}
            </div>
            <div class="tagrow">
              <span v-for="t in previewProduct.tags" :key="t" class="chip">{{ t }}</span>
            </div>
            <div class="card-foot">
              <span class="price" :class="{ free: previewProduct.price === 0 }">{{ previewProduct.price === 0 ? '免費' : '$' + previewProduct.price }}</span>
              <span style="font-family:var(--oj-mono); font-size:11.5px; color:var(--text-faint);">{{ totalSize }}</span>
            </div>
          </div>
        </div>
        <p style="font-size:12px; color:var(--text-faint); margin-top:14px; line-height:1.6; font-family:var(--oj-font);">
          這是買家在商城看到的卡片樣式，會即時隨你的編輯更新。
        </p>
      </div>
    </div>
  </div>
</template>
