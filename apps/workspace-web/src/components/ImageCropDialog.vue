<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { Cropper, CircleStencil, RectangleStencil } from 'vue-advanced-cropper'
import 'vue-advanced-cropper/dist/style.css'

const { t } = useI18n()

const props = defineProps<{
  show: boolean
  file: File | null
  kind: 'avatar' | 'banner'
}>()

const emit = defineEmits<{
  'update:show': [boolean]
  cropped: [File]
}>()

const open = computed({
  get: () => props.show,
  set: (v: boolean) => emit('update:show', v),
})

// 裁切輸出上限（超過才縮小，不放大）：avatar 圓形顯示最大 104px、banner 滿版封面
const OUTPUT = {
  avatar: { maxWidth: 512, maxHeight: 512 },
  banner: { maxWidth: 2400, maxHeight: 600 },
} as const

// 輸出格式跟隨原檔 MIME；副檔名對齊輸出格式（GIF 已不在允許清單）
const EXT: Record<string, string> = { 'image/jpeg': 'jpg', 'image/png': 'png', 'image/webp': 'webp' }

const cropperRef = ref<InstanceType<typeof Cropper> | null>(null)
const src = ref('')                                  // 選檔的 object URL
const error = ref<'' | 'load' | 'crop'>('')
const applying = ref(false)

watch(
  () => props.file,
  (file) => {
    if (src.value) URL.revokeObjectURL(src.value)
    error.value = ''
    src.value = file ? URL.createObjectURL(file) : ''
  },
)
onBeforeUnmount(() => {
  if (src.value) URL.revokeObjectURL(src.value)
})

const isAvatar = computed(() => props.kind === 'avatar')
// avatar 用圓形遮罩（店面即為圓形呈現）；banner 固定 4:1 並以 previewClass 疊安全區參考框
const stencilComponent = computed(() => (isAvatar.value ? CircleStencil : RectangleStencil))
const stencilProps = computed(() =>
  isAvatar.value
    ? { aspectRatio: 1 }
    : { aspectRatio: 4, previewClass: 'crop-banner-safe' },
)

/** 預設把裁切框撐到可視範圍最大，使用者只需微調。 */
function defaultSize({
  imageSize,
  visibleArea,
}: {
  imageSize: { width: number; height: number }
  visibleArea?: { width: number; height: number } | null
}) {
  const area = visibleArea ?? imageSize
  return { width: area.width, height: area.height }
}

/** canvas.toBlob 的 Promise 包裝；quality 僅對 jpeg / webp 生效。 */
function toBlob(canvas: HTMLCanvasElement, type: string): Promise<Blob | null> {
  return new Promise((resolve) => canvas.toBlob(resolve, type, 0.92))
}

async function apply() {
  const file = props.file
  const canvas = cropperRef.value?.getResult().canvas
  if (!file || !canvas) return
  applying.value = true
  try {
    const type = EXT[file.type] ? file.type : 'image/png'
    const blob = await toBlob(canvas, type)
    if (!blob) {
      error.value = 'crop'
      return
    }
    const stem = file.name.replace(/\.[^.]+$/, '') || props.kind
    emit('cropped', new File([blob], `${stem}.${EXT[type]}`, { type }))
    open.value = false
  } finally {
    applying.value = false
  }
}
</script>

<template>
  <n-modal v-model:show="open" preset="card"
           :title="isAvatar ? t('cropDialog.titleAvatar') : t('cropDialog.titleBanner')"
           style="max-width:640px;">
    <p class="crop-hint">{{ isAvatar ? t('cropDialog.hintAvatar') : t('cropDialog.hintBanner') }}</p>

    <div v-if="error === 'load'" class="crop-error">{{ t('cropDialog.loadError') }}</div>
    <Cropper v-else ref="cropperRef" class="crop-area" :src="src"
             :stencil-component="stencilComponent" :stencil-props="stencilProps"
             :canvas="OUTPUT[kind]" :default-size="defaultSize"
             @error="error = 'load'" />
    <div v-if="error === 'crop'" class="crop-error">{{ t('cropDialog.cropError') }}</div>

    <div class="crop-actions">
      <n-button size="large" @click="open = false">{{ t('cropDialog.cancel') }}</n-button>
      <n-button type="primary" size="large" strong
                :loading="applying" :disabled="error === 'load'" @click="apply">
        {{ t('cropDialog.apply') }}
      </n-button>
    </div>
  </n-modal>
</template>

<!-- previewClass 掛在 library 內部節點上，需為全域樣式 -->
<style>
/* banner 安全區：手機（2.5:1）裁掉左右剩 62.5% 寬、大螢幕（6:1）裁掉上下剩 66% 高 */
.crop-banner-safe::after {
  content: '';
  position: absolute;
  left: 18.75%;
  right: 18.75%;
  top: 17%;
  bottom: 17%;
  border: 2px dashed rgba(255, 255, 255, 0.9);
  border-radius: 8px;
  pointer-events: none;
}
</style>

<style scoped>
.crop-hint {
  margin: 0 0 12px;
  font-size: 13px;
  color: var(--text-soft);
}

.crop-area {
  max-height: 380px;
  min-height: 200px;
  border-radius: var(--r-md);
  overflow: hidden;
  background: var(--text);
}

.crop-error {
  padding: 24px;
  text-align: center;
  color: var(--c-pink-deep);
  font-size: 13px;
}

.crop-actions {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
  margin-top: 16px;
}
</style>
