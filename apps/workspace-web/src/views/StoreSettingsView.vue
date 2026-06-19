<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useMessage } from 'naive-ui'
import { storeToRefs } from 'pinia'
import { useStoreApplicationStore } from '@/stores/storeApplication'
import { JFmt } from '@/utils/format'

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
  if (!ALLOWED.includes(file.type)) { message.error('僅支援 JPG / PNG / GIF / WebP 圖片。'); return }
  if (file.size > MAX_BYTES) { message.error('圖片大小不可超過 5 MB。'); return }

  const ok = await storeApp.uploadStoreImage(file, kind)
  if (ok) message.success(kind === 'avatar' ? '已更新頭像。' : '已更新橫幅。')
  else message.error(storeApp.error ?? '上傳失敗')
}

async function onSave() {
  if (!canSave.value) return
  const ok = await storeApp.updateStore({
    storeName: form.value.storeName.trim(),
    description: form.value.description.trim(),
  })
  if (ok) message.success('商店資料已更新。')
  else message.error(storeApp.error ?? '更新失敗')
}
</script>

<template>
  <div data-screen-label="商店設定">
    <div class="page-head" style="margin-bottom:22px;">
      <div>
        <p class="h-eyebrow">賣家工作室</p>
        <h1 class="h-title">商店設定</h1>
      </div>
    </div>

    <n-spin :show="loading">
      <div class="dash-grid" style="gap:18px;">
        <!-- 隱藏檔案輸入 -->
        <input ref="avatarInput" type="file" :accept="ACCEPT" style="display:none" @change="e => onPick(e, 'avatar')" />
        <input ref="bannerInput" type="file" :accept="ACCEPT" style="display:none" @change="e => onPick(e, 'banner')" />

        <!-- 店面外觀：橫幅 + 頭像即時預覽 -->
        <div class="card-pad set-card">
          <div class="set-grid">
            <div class="sg-k">
              <div class="sgk-t">店面外觀</div>
              <div class="sgk-d">橫幅與頭像會顯示在你的商店頁。點擊圖片即可更換，支援 JPG / PNG / GIF / WebP，單檔 5 MB 以內。</div>
            </div>
            <div>
              <div class="banner" :class="{ empty: !bannerUrl }"
                   :style="bannerUrl ? { backgroundImage: `url(${bannerUrl})` } : {}"
                   @click="pick('banner')">
                <div class="img-hint"><app-icon name="image" :size="18" /> 更換橫幅</div>
              </div>

              <div class="identity">
                <div class="st-avatar" :class="{ empty: !avatarUrl }"
                     :style="avatarUrl ? { backgroundImage: `url(${avatarUrl})` } : {}"
                     @click="pick('avatar')">
                  <span v-if="!avatarUrl" class="st-avatar-initials">{{ F.initials(form.storeName || '店') }}</span>
                  <div class="st-avatar-cam"><app-icon name="upload" :size="15" :stroke="2.2" /></div>
                </div>
                <div class="identity-meta">
                  <div class="store-name">{{ form.storeName || '未命名商店' }}</div>
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
              <div class="sgk-t">商店資料</div>
              <div class="sgk-d">商店名稱與描述會顯示給買家；子網域於開店時決定，無法變更。</div>
            </div>
            <div>
              <div class="field">
                <label class="field-label">商店名稱</label>
                <n-input v-model:value="form.storeName" size="large" maxlength="100" show-count
                         placeholder="例如：小明的數位商店" />
              </div>
              <div class="field">
                <label class="field-label">子網域</label>
                <n-input :value="slug" size="large" disabled>
                  <template #suffix><span style="color:var(--text-faint)">.openjam.co</span></template>
                </n-input>
              </div>
              <div class="field">
                <label class="field-label">商店描述</label>
                <n-input v-model:value="form.description" type="textarea" size="large"
                         :autosize="{ minRows: 3, maxRows: 6 }" maxlength="500" show-count
                         placeholder="向買家介紹你的商店、作品風格與特色…" />
              </div>

              <div style="display:flex; justify-content:flex-end; margin-top:16px;">
                <n-button type="primary" size="large" strong
                          :disabled="!canSave" :loading="saving" @click="onSave">
                  儲存變更
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
