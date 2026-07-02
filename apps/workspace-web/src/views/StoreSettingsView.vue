<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
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

const slug = computed(() => primaryStore.value?.storeSlug ?? '')
const avatarUrl = computed(() => primaryStore.value?.avatarUrl ?? '')
const bannerUrl = computed(() => primaryStore.value?.bannerUrl ?? '')
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

async function onPick(e: Event, kind: 'avatar' | 'banner') {
  const input = e.target as HTMLInputElement
  const file = input.files?.[0]
  input.value = ''
  if (!file) return
  if (!ALLOWED.includes(file.type)) { message.error(t('storeSettings.msgInvalidType')); return }
  if (file.size > MAX_BYTES) { message.error(t('storeSettings.msgTooLarge')); return }

  const ok = await storeApp.uploadStoreImage(file, kind)
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
.set-card { border-radius: 10px; }

/* 橫幅 */
.banner {
  position: relative;
  height: 180px;
  border-radius: var(--r-md);
  background-size: cover;
  background-position: center;
  background-color: var(--oj-primary-wash, #efeaff);
  border: 1.5px solid var(--border);
  cursor: pointer;
  overflow: hidden;
  transition: filter .15s;
}
.banner.empty {
  background-image: linear-gradient(135deg, hsl(256 88% 64%), hsl(298 90% 56%));
  border-style: dashed;
}
.banner:hover { filter: brightness(.94); }
.img-hint {
  position: absolute;
  right: 12px; bottom: 12px;
  display: inline-flex; align-items: center; gap: 6px;
  padding: 6px 12px;
  border-radius: 999px;
  background: rgba(0, 0, 0, .55);
  color: #fff;
  font-size: 12.5px; font-weight: 600;
  backdrop-filter: blur(2px);
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
  flex: 0 0 auto;          /* 固定尺寸的 flex 子項：永不被列高拉伸 */
  align-self: center;       /* 與名稱垂直置中，不受容器 align 影響 */
  cursor: pointer;
  aspect-ratio: 1 / 1;
  border-radius: 50%;
  background-size: cover;
  background-position: center;
  border: 3px solid var(--surface);
  box-shadow: var(--shadow);
  display: grid; place-items: center;
  background-color: var(--c-violet, #6c4cf1);
  transition: filter .15s;
}
.st-avatar:hover { filter: brightness(.93); }
.st-avatar-initials { color: #fff; font-weight: 800; font-size: 26px; font-family: var(--oj-display); }
.st-avatar-cam {
  position: absolute;
  right: -2px; bottom: -2px;
  width: 28px; height: 28px;
  border-radius: 50%;
  background: var(--oj-primary, #5639d6);
  color: #fff;
  display: grid; place-items: center;
  border: 2.5px solid var(--surface);
}
.identity-meta { flex: 1; padding-top: 30px; }
.store-name { font-family: var(--oj-display); font-weight: 700; font-size: 17px; }
.store-slug { font-family: var(--oj-mono); font-size: 12px; color: var(--text-faint); margin-top: 2px; }
</style>
