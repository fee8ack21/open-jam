<script setup lang="ts">
import { computed, ref } from 'vue'
import { useDashboardStore, type DraftFile } from '@/stores/dashboard'
import { JFmt } from '@/utils/format'
import { CATEGORIES, CAT_DESC, TAGS, ME } from '@/data/products'

const STEPS = [
  { n: 1, k: 'STEP 01', l: '基本資訊' },
  { n: 2, k: 'STEP 02', l: '檔案與內容' },
  { n: 3, k: 'STEP 03', l: '預覽與發佈' },
]

// sample files a creator might "drop in" — cycled on each add
const SAMPLE_FILES = [
  { name: '高解析作品集.zip', type: 'ZIP', bytes: 1.1e9 },
  { name: 'Lightroom 預設組.xmp', type: 'XMP', bytes: 2e6 },
  { name: '使用說明與授權.pdf', type: 'PDF', bytes: 4e6 },
  { name: '預覽縮圖.jpg', type: 'JPG', bytes: 8e5 },
  { name: '原始檔.psd', type: 'PSD', bytes: 320e6 },
]
const TYPE_COLOR = { ZIP: '#6c4cf1', XMP: '#1fd6c6', PDF: '#ff4d9d', JPG: '#ff7a2f', PSD: '#3b7fd4', WAV: '#8b5cf6', MP3: '#16a07a', FIG: '#d8a017' }

const store = useDashboardStore()
const F = JFmt

const tagDraft = ref('')
const dragging = ref(false)
let uploadCount = 0

const step = computed(() => store.wizardStep)
const d = computed(() => store.draft)
const steps = STEPS
const cats = CATEGORIES
const catDesc = CAT_DESC

const suggestedTags = computed(() =>
  (TAGS[d.value.cat] || []).filter(t => !d.value.tags.includes(t)).slice(0, 6),
)
const totalBytes = computed(() => d.value.files.reduce((s, f) => s + (f.bytes || 0), 0))
const totalSize = computed(() => store.fmtBytes(totalBytes.value) || '—')
const previewProduct = computed(() => ({
  cat: d.value.cat, hue: d.value.coverHue,
  title: d.value.title || '你的作品標題會顯示在這裡',
  creator: ME.name, avatar: ME.avatar,
  tags: d.value.tags.length ? d.value.tags : ['標籤'],
  price: d.value.free ? 0 : d.value.price,
  rating: 0,
  formats: d.value.files.length ? [...new Set(d.value.files.map(f => f.type))] : ['格式'],
  totalSize: totalSize.value,
}))
const step1Valid = computed(() => d.value.title.trim().length >= 2)
const step2Valid = computed(() => d.value.files.length > 0)
const hueOptions = [256, 320, 28, 168, 44, 198, 142, 226]

function addTag() { if (tagDraft.value.trim()) { store.addDraftTag(tagDraft.value); tagDraft.value = '' } }
function fakeDrop() {
  const f: DraftFile = { ...SAMPLE_FILES[uploadCount % SAMPLE_FILES.length], progress: 0 }
  uploadCount++
  store.addDraftFile(f)
  const idx = d.value.files.length - 1
  const tick = () => {
    const file = d.value.files[idx]; if (!file) return
    file.progress = Math.min(100, (file.progress || 0) + 18 + Math.random() * 22)
    if (file.progress < 100) setTimeout(tick, 160)
  }
  setTimeout(tick, 150)
}
function typeColor(t: string) { return (TYPE_COLOR as Record<string, string>)[t] || 'var(--c-violet)' }
function goNext() {
  if (step.value === 1 && !step1Valid.value) return
  if (step.value === 2 && !step2Valid.value) return
  store.nextStep()
}
function publish() { store.publishDraft(); store.go('products') }
function saveDraft() {
  // keep draft persisted, mark intent
  store.go('products')
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
              <div v-for="c in cats" :key="c.id" class="cat-card" :class="{ on: d.cat === c.id }"
                   @click="store.patchDraft({ cat: c.id })">
                <span class="cc-ic" :style="{ background: ({music:'var(--c-violet)',photo:'var(--c-pink)',ebook:'var(--c-cyan)'})[c.id] }"><app-icon :name="c.glyph" :size="20" /></span>
                <div>
                  <div class="cc-l">{{ c.label }}</div>
                  <div class="cc-d">{{ catDesc[c.id] }}</div>
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
            <div class="dropzone" :class="{ drag: dragging }"
                 @click="fakeDrop" @dragover.prevent="dragging = true" @dragleave="dragging = false"
                 @drop.prevent="dragging = false; fakeDrop()">
              <div class="dz-ic"><app-icon name="upload" :size="28" :stroke="2.2" /></div>
              <div class="dz-title">拖曳檔案到這裡，或點擊選擇</div>
              <div class="dz-sub">買家付款後即可下載這些檔案</div>
              <div class="dz-hint">（示範用：點擊會加入一個範例檔案）</div>
            </div>

            <div class="upload-list" v-if="d.files.length">
              <div v-for="(f, i) in d.files" :key="i" class="upload-row">
                <span class="file-ic" :style="{ background: typeColor(f.type) }">{{ f.type }}</span>
                <div class="ur-body">
                  <div class="ur-name">{{ f.name }}</div>
                  <div class="ur-meta">{{ store.fmtBytes(f.bytes) }} · {{ (f.progress || 0) >= 100 ? '上傳完成' : '上傳中 ' + Math.round(f.progress || 0) + '%' }}</div>
                  <div class="progress" v-if="(f.progress || 0) < 100"><i :style="{ width: (f.progress || 0) + '%' }"></i></div>
                </div>
                <button class="ic-act danger" @click="store.removeDraftFile(i)"><app-icon name="trash" :size="16" /></button>
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
            <p class="fb-sub">送出後會進入審核，通常 24 小時內完成。你也可以先存成草稿。</p>
            <div style="margin-top:6px;">
              <div class="rev-row"><span class="rev-k">標題</span><span class="rev-v">{{ d.title || '未命名作品' }}</span></div>
              <div class="rev-row"><span class="rev-k">分類</span><span class="rev-v">{{ (cats.find(c=>c.id===d.cat)||{}).label }}</span></div>
              <div class="rev-row"><span class="rev-k">標籤</span><span class="rev-v">{{ d.tags.length ? d.tags.join('、') : '—' }}</span></div>
              <div class="rev-row"><span class="rev-k">定價</span><span class="rev-v">{{ d.free ? '免費' : '$' + d.price }}</span></div>
              <div class="rev-row"><span class="rev-k">檔案</span><span class="rev-v">{{ d.files.length }} 個 · {{ totalSize }}</span></div>
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
            <n-button v-if="step === 3" size="large" @click="saveDraft">存成草稿</n-button>
            <n-button v-if="step < 3" type="primary" size="large" :disabled="(step===1 && !step1Valid) || (step===2 && !step2Valid)" @click="goNext">
              下一步
              <template #icon><app-icon name="arrowRight" :size="17" /></template>
            </n-button>
            <button v-else class="cta-pop" style="font-size:15px; padding:12px 22px;" @click="publish">
              <app-icon name="rocket" :size="17" style="vertical-align:-3px; margin-right:5px;" />送出審核
            </button>
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
