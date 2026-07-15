<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useMessage } from 'naive-ui'
import { useI18n } from 'vue-i18n'
import { storeToRefs } from 'pinia'
import { useStoreApplicationStore } from '@/stores/storeApplication'
import { JFmt } from '@/utils/format'

const { t } = useI18n()

// 與後端 RequestAssetUploadUrlRequestValidator 對齊的允許圖片類型
const ACCEPT = 'image/png,image/jpeg,image/gif,image/webp'
const ALLOWED = ['image/jpeg', 'image/png', 'image/gif', 'image/webp']
const MAX_BYTES = 5 * 1024 * 1024   // 單檔上限 5 MB

const message = useMessage()
const storeApp = useStoreApplicationStore()
const { primaryStore, saving, loading } = storeToRefs(storeApp)
const F = JFmt

const avatarInput = ref<HTMLInputElement | null>(null)
const bannerInput = ref<HTMLInputElement | null>(null)

// 名稱 / 描述採本機草稿，載入或儲存後與後端對齊
const form = ref({ storeName: '', description: '' })

// 選檔後的本機預覽（object URL）：上傳與 confirm 期間先顯示使用者選的圖，
// 成功後改由後端 URL 接手、失敗則還原原圖（後端於 confirm 才綁定，失敗不會動到商店）。
const preview = ref<Record<'avatar' | 'banner', string>>({ avatar: '', banner: '' })

function setPreview(kind: 'avatar' | 'banner', file: File | null) {
  const prev = preview.value[kind]
  if (prev) URL.revokeObjectURL(prev)
  preview.value = { ...preview.value, [kind]: file ? URL.createObjectURL(file) : '' }
}

onBeforeUnmount(() => {
  Object.values(preview.value).forEach((url) => url && URL.revokeObjectURL(url))
})

const slug = computed(() => primaryStore.value?.storeSlug ?? '')
const avatarUrl = computed(() => preview.value.avatar || primaryStore.value?.avatarUrl || '')
const bannerUrl = computed(() => preview.value.banner || primaryStore.value?.bannerUrl || '')
const dirty = computed(() =>
  form.value.storeName.trim() !== (primaryStore.value?.storeName ?? '') ||
  form.value.description.trim() !== (primaryStore.value?.description ?? ''),
)
const canSave = computed(() => form.value.storeName.trim().length >= 1 && dirty.value && !saving.value)

function syncForm() {
  form.value = {
    storeName: primaryStore.value?.storeName ?? '',
    description: primaryStore.value?.description ?? '',
  }
}
watch(primaryStore, syncForm, { immediate: true })

onMounted(async () => {
  if (!storeApp.hasStore) await storeApp.load()
  syncForm()
})

function pick(kind: 'avatar' | 'banner') {
  ;(kind === 'avatar' ? avatarInput : bannerInput).value?.click()
}

/** 等圖片載入完成（載入失敗也視為結束，不卡住交接）。 */
function preload(url?: string | null): Promise<void> {
  if (!url) return Promise.resolve()
  return new Promise((resolve) => {
    const img = new Image()
    img.onload = img.onerror = () => resolve()
    img.src = url
  })
}

async function onPick(e: Event, kind: 'avatar' | 'banner') {
  const input = e.target as HTMLInputElement
  const file = input.files?.[0]
  input.value = ''
  if (!file) return
  if (!ALLOWED.includes(file.type)) { message.error(t('storeSettings.msgInvalidType')); return }
  if (file.size > MAX_BYTES) { message.error(t('storeSettings.msgTooLarge')); return }

  setPreview(kind, file)   // 立即預覽，不等後端往返
  const ok = await storeApp.uploadStoreImage(file, kind)

  // 成功：等後端新圖載入完成再撤預覽，避免交接時閃一下空白；失敗：直接撤，還原原本的圖
  if (ok) await preload(kind === 'avatar' ? primaryStore.value?.avatarUrl : primaryStore.value?.bannerUrl)
  setPreview(kind, null)

  if (ok) message.success(kind === 'avatar' ? t('storeSettings.msgAvatarUpdated') : t('storeSettings.msgBannerUpdated'))
  else message.error(storeApp.error ?? t('storeSettings.msgUploadFailed'))
}

async function onSave() {
  if (!canSave.value) return
  const ok = await storeApp.updateStore({
    storeName: form.value.storeName.trim(),
    description: form.value.description.trim(),
  })
  if (ok) message.success(t('storeSettings.msgSaved'))
  else message.error(storeApp.error ?? t('storeSettings.msgSaveFailed'))
}
</script>

<template>
  <div :data-screen-label="t('route.storeSettings')">

    <n-spin :show="loading">
      <div class="dash-grid" style="gap:18px;">
        <!-- 隱藏檔案輸入 -->
        <input ref="avatarInput" type="file" :accept="ACCEPT" style="display:none" @change="e => onPick(e, 'avatar')" />
        <input ref="bannerInput" type="file" :accept="ACCEPT" style="display:none" @change="e => onPick(e, 'banner')" />

        <!-- 店面外觀：橫幅 + 頭像即時預覽 -->
        <div class="card-pad set-card">
          <div class="set-grid">
            <div class="sg-k">
              <div class="sgk-t">{{ t('storeSettings.appearanceTitle') }}</div>
              <div class="sgk-d">{{ t('storeSettings.appearanceDesc') }}</div>
            </div>
            <div>
              <div class="banner" :class="{ empty: !bannerUrl }"
                   :style="bannerUrl ? { backgroundImage: `url(${bannerUrl})` } : {}"
                   @click="pick('banner')">
                <div class="img-hint"><app-icon name="image" :size="18" /> {{ t('storeSettings.changeBanner') }}</div>
              </div>

              <div class="identity">
                <div class="st-avatar" :class="{ empty: !avatarUrl }"
                     :style="avatarUrl ? { backgroundImage: `url(${avatarUrl})` } : {}"
                     @click="pick('avatar')">
                  <span v-if="!avatarUrl" class="st-avatar-initials">{{ F.initials(form.storeName || t('storeSettings.storeInitial')) }}</span>
                  <div class="st-avatar-cam"><app-icon name="upload" :size="15" :stroke="2.2" /></div>
                </div>
                <div class="identity-meta">
                  <div class="store-name">{{ form.storeName || t('storeSettings.unnamedStore') }}</div>
                  <div class="store-slug">{{ slug }}.openjam.co</div>
                </div>
                <n-spin v-if="saving" :size="18" />
              </div>
            </div>
          </div>
        </div>

        <!-- 基本資料：名稱（可改）/ 子網域（唯讀）/ 描述 -->
        <div class="card-pad set-card">
          <div class="set-grid">
            <div class="sg-k">
              <div class="sgk-t">{{ t('storeSettings.infoTitle') }}</div>
              <div class="sgk-d">{{ t('storeSettings.infoDesc') }}</div>
            </div>
            <div>
              <div class="field">
                <label class="field-label">{{ t('storeSettings.storeName') }}</label>
                <n-input v-model:value="form.storeName" size="large" maxlength="100" show-count
                         :placeholder="t('storeSettings.storeNamePlaceholder')" />
              </div>
              <div class="field">
                <label class="field-label">{{ t('storeSettings.subdomain') }}</label>
                <n-input :value="slug" size="large" disabled>
                  <template #suffix><span style="color:var(--text-faint)">.openjam.co</span></template>
                </n-input>
              </div>
              <div class="field">
                <label class="field-label">{{ t('storeSettings.description') }}</label>
                <n-input v-model:value="form.description" type="textarea" size="large"
                         :autosize="{ minRows: 3, maxRows: 6 }" maxlength="500" show-count
                         :placeholder="t('storeSettings.descriptionPlaceholder')" />
              </div>

              <div style="display:flex; justify-content:flex-end; margin-top:16px;">
                <n-button type="primary" size="large" strong
                          :disabled="!canSave" :loading="saving" @click="onSave">
                  {{ t('storeSettings.save') }}
                </n-button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </n-spin>
  </div>
</template>

<style scoped>
.set-card { border-radius: var(--r-lg); }

/* 橫幅 */
.banner {
  position: relative;
  height: 180px;
  border-radius: var(--r-md);
  background-size: cover;
  background-position: center;
  background-color: var(--t-violet);
  border: var(--bw) solid var(--border-strong);
  cursor: pointer;
  overflow: hidden;
  transition: filter .15s;
}
.banner.empty {
  background-color: var(--t-violet);
  background-image: radial-gradient(rgba(26,26,26,0.08) 1.5px, transparent 1.5px);
  background-size: 18px 18px;
  border-style: dashed;
}
.banner:hover { filter: brightness(.94); }
.img-hint {
  position: absolute;
  right: 12px; bottom: 12px;
  display: inline-flex; align-items: center; gap: 6px;
  padding: 6px 12px;
  border-radius: var(--r-pill);
  background: var(--text);
  color: var(--c-yellow);
  font-size: 12.5px; font-weight: 900;
}

/* 頭像 + 名稱列 */
.identity {
  display: flex;
  align-items: center;
  gap: 14px;
  margin-top: -34px;
  padding: 0 4px;
}
.st-avatar {
  position: relative;
  box-sizing: border-box;
  width: 84px; height: 84px;
  min-height: 84px; max-height: 84px;  /* 硬性固定高度，避免在 flex 列中被撐高（aspect-ratio 交互 bug） */
  flex: 0 0 auto;          /* 固定尺寸的 flex 子項：永不被列高拉伸 */
  align-self: center;       /* 與名稱垂直置中，不受容器 align 影響 */
  cursor: pointer;
  border-radius: 50%;
  background-size: cover;
  background-position: center;
  border: var(--bw) solid var(--border-strong);
  box-shadow: var(--pop-1);
  display: grid; place-items: center;
  background-color: var(--c-violet);
  transition: filter .15s;
}
.st-avatar:hover { filter: brightness(.93); }
.st-avatar-initials { color: #fff; font-weight: 700; font-size: 26px; font-family: var(--oj-display); }
.st-avatar-cam {
  position: absolute;
  right: -2px; bottom: -2px;
  width: 28px; height: 28px;
  border-radius: 50%;
  background: var(--c-yellow);
  color: var(--text);
  display: grid; place-items: center;
  border: var(--bw) solid var(--border-strong);
}
.identity-meta { flex: 1; padding-top: 30px; }
.store-name { font-family: var(--oj-font); font-weight: 900; font-size: 17px; }
.store-slug { font-family: var(--oj-mono); font-size: 12px; color: var(--text-faint); margin-top: 2px; }
</style>
