<script setup lang="ts">
/* ============================================================
   Lightbox — 商品頁圖片放大檢視（Teleport 至 body 的全螢幕 overlay）。
   受控元件：父層以 v-if 掛載、v-model:index 同步目前張數；
   鍵盤 ←/→ 切換、Esc / 點背景關閉、手機水平 swipe 換張，
   掛載期間鎖住 body 捲動。
   ============================================================ */
import { onBeforeUnmount, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import AppIcon from './app-icon';

const props = defineProps<{
  /** 圖片公開讀取 URL 清單（僅圖片，不含影片 / YouTube 項目）。 */
  images: string[];
  /** 目前顯示的圖片索引。 */
  index: number;
}>();
const emit = defineEmits<{ 'update:index': [i: number]; close: [] }>();
const { t } = useI18n();

const prev = () => { if (props.index > 0) emit('update:index', props.index - 1); };
const next = () => { if (props.index < props.images.length - 1) emit('update:index', props.index + 1); };

function onKey(e: KeyboardEvent) {
  if (e.key === 'Escape') emit('close');
  else if (e.key === 'ArrowLeft') prev();
  else if (e.key === 'ArrowRight') next();
}

/* 手機 swipe：水平位移達閾值且大於垂直位移才換張，避免誤判捲動手勢 */
const SWIPE_MIN = 40;
let touchX = 0;
let touchY = 0;
const onTouchStart = (e: TouchEvent) => { touchX = e.touches[0].clientX; touchY = e.touches[0].clientY; };
function onTouchEnd(e: TouchEvent) {
  const dx = e.changedTouches[0].clientX - touchX;
  const dy = e.changedTouches[0].clientY - touchY;
  if (Math.abs(dx) < SWIPE_MIN || Math.abs(dx) < Math.abs(dy)) return;
  if (dx > 0) prev(); else next();
}

let bodyOverflow = '';
onMounted(() => {
  window.addEventListener('keydown', onKey);
  bodyOverflow = document.body.style.overflow;
  document.body.style.overflow = 'hidden';
});
onBeforeUnmount(() => {
  window.removeEventListener('keydown', onKey);
  document.body.style.overflow = bodyOverflow;
});
</script>

<template>
  <teleport to="body">
    <div class="lightbox" role="dialog" aria-modal="true"
         @click.self="emit('close')" @touchstart.passive="onTouchStart" @touchend.passive="onTouchEnd">
      <img class="lb-img" :src="images[index]" alt="" />
      <button type="button" class="lb-btn lb-close" :aria-label="t('lightbox.close')" @click="emit('close')">
        <app-icon name="close" :size="20" />
      </button>
      <template v-if="images.length > 1">
        <button type="button" class="lb-btn lb-prev" :disabled="index === 0"
                :aria-label="t('lightbox.prev')" @click="prev">
          <app-icon name="arrowL" :size="20" />
        </button>
        <button type="button" class="lb-btn lb-next" :disabled="index === images.length - 1"
                :aria-label="t('lightbox.next')" @click="next">
          <app-icon name="arrow" :size="20" />
        </button>
        <div class="lb-count">{{ index + 1 }} / {{ images.length }}</div>
      </template>
    </div>
  </teleport>
</template>

<style scoped>
.lightbox {
  position: fixed; inset: 0; z-index: 1000;
  display: flex; align-items: center; justify-content: center;
  padding: clamp(16px, 5vw, 56px);
  background: rgba(26, 26, 26, .92);
  animation: lb-fade .18s ease-out;
}
@keyframes lb-fade { from { opacity: 0; } to { opacity: 1; } }

.lb-img {
  max-width: 100%; max-height: 100%; object-fit: contain;
  border: var(--bw) solid var(--border-strong); border-radius: var(--r-md);
  background: #1a1a1a;
  box-shadow: 0 24px 60px rgba(0, 0, 0, .5);
}

/* 圓形墨線按鈕（同 .icon-btn 語言，置於深色 overlay 上） */
.lb-btn {
  position: absolute; width: 44px; height: 44px; border-radius: 999px;
  display: grid; place-items: center; cursor: pointer;
  color: var(--text); background: var(--surface); border: var(--bw) solid var(--border-strong);
  transition: background .15s, transform .2s var(--ease-pop), opacity .15s;
}
.lb-btn:hover:not(:disabled) { background: var(--c-yellow); }
.lb-btn:disabled { opacity: .35; cursor: default; }
.lb-close { top: 20px; right: 20px; }
.lb-close:hover { transform: translateY(-2px); }
.lb-prev,
.lb-next { top: 50%; transform: translateY(-50%); }
.lb-prev { left: 20px; }
.lb-next { right: 20px; }
.lb-prev:hover:not(:disabled),
.lb-next:hover:not(:disabled) { transform: translateY(-50%) scale(1.08); }

.lb-count {
  position: absolute; bottom: 20px; left: 50%; transform: translateX(-50%);
  padding: 5px 14px; border-radius: 999px;
  font-family: var(--oj-display); font-weight: 700; font-size: 13px; color: var(--text);
  background: var(--surface); border: var(--bw) solid var(--border-strong);
}
</style>
