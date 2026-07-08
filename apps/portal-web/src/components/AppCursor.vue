<script setup lang="ts">
/* ============================================================
   AppCursor — 情境式自訂游標（參考 Begonia Design 手法）
   小圓點即時跟隨、外圈延遲跟隨營造慣性；依 hover 對象變形：
   - 可點擊元素：外圈放大聚焦
   - 作品卡 [data-cursor="play"]：外圈填滿品牌色並亮出 ▶
     （「播放這件作品」的音樂隱喻）
   - 文字輸入：隱藏自訂游標，還原原生 I-beam
   僅在 hover + fine pointer 且未要求減少動態的裝置啟用；
   啟用時以 html.oj-cursor-on 隱藏原生游標（樣式在 base.css）。
   ============================================================ */
import { onBeforeUnmount, onMounted, ref } from 'vue';
import gsap from 'gsap';

type CursorMode = 'default' | 'link' | 'play' | 'text';

const visible = ref(false);
const down = ref(false);
const mode = ref<CursorMode>('default');
const dotEl = ref<HTMLElement | null>(null);
const ringEl = ref<HTMLElement | null>(null);

const INTERACTIVE =
  'a, button, [role="button"], [role="menuitem"], [role="option"], label, select, summary';
// 僅文字輸入還原原生 I-beam；checkbox / radio 等按鈕型 input 仍走 link 放大
const TEXTY =
  'textarea, [contenteditable="true"], input:not([type="checkbox"]):not([type="radio"]):not([type="button"]):not([type="submit"]):not([type="range"]):not([type="file"]):not([type="color"])';

function resolveMode(target: EventTarget | null): CursorMode {
  if (!(target instanceof Element)) return 'default';
  if (target.closest('[data-cursor="play"]')) return 'play';
  if (target.closest(TEXTY)) return 'text';
  if (target.closest(INTERACTIVE)) return 'link';
  return 'default';
}

let teardown: (() => void) | undefined;

function attach() {
  if (teardown || !dotEl.value || !ringEl.value) return;
  document.documentElement.classList.add('oj-cursor-on');

  const dotX = gsap.quickTo(dotEl.value, 'x', { duration: 0.13, ease: 'power2.out' });
  const dotY = gsap.quickTo(dotEl.value, 'y', { duration: 0.13, ease: 'power2.out' });
  const ringX = gsap.quickTo(ringEl.value, 'x', { duration: 0.45, ease: 'power3.out' });
  const ringY = gsap.quickTo(ringEl.value, 'y', { duration: 0.45, ease: 'power3.out' });

  const onMove = (e: PointerEvent) => {
    visible.value = true;
    dotX(e.clientX);
    dotY(e.clientY);
    ringX(e.clientX);
    ringY(e.clientY);
  };
  const onOver = (e: MouseEvent) => {
    mode.value = resolveMode(e.target);
  };
  const onDown = () => {
    down.value = true;
  };
  const onUp = () => {
    down.value = false;
  };
  const hide = () => {
    visible.value = false;
  };

  window.addEventListener('pointermove', onMove, { passive: true });
  document.addEventListener('mouseover', onOver, true);
  window.addEventListener('pointerdown', onDown, true);
  window.addEventListener('pointerup', onUp, true);
  document.documentElement.addEventListener('mouseleave', hide);
  window.addEventListener('blur', hide);

  teardown = () => {
    window.removeEventListener('pointermove', onMove);
    document.removeEventListener('mouseover', onOver, true);
    window.removeEventListener('pointerdown', onDown, true);
    window.removeEventListener('pointerup', onUp, true);
    document.documentElement.removeEventListener('mouseleave', hide);
    window.removeEventListener('blur', hide);
    document.documentElement.classList.remove('oj-cursor-on');
    visible.value = false;
    teardown = undefined;
  };
}

const mqPointer = window.matchMedia('(hover: hover) and (pointer: fine)');
const mqReduce = window.matchMedia('(prefers-reduced-motion: reduce)');

function sync() {
  if (mqPointer.matches && !mqReduce.matches) attach();
  else teardown?.();
}

onMounted(() => {
  mqPointer.addEventListener('change', sync);
  mqReduce.addEventListener('change', sync);
  sync();
});
onBeforeUnmount(() => {
  mqPointer.removeEventListener('change', sync);
  mqReduce.removeEventListener('change', sync);
  teardown?.();
});
</script>

<template>
  <div
    class="oj-cursor"
    :class="['is-' + mode, { 'is-visible': visible, 'is-down': down }]"
    aria-hidden="true"
  >
    <div ref="ringEl" class="oj-cursor-move">
      <span class="oj-cursor-ring"><span class="oj-cursor-tri"></span></span>
    </div>
    <div ref="dotEl" class="oj-cursor-move">
      <span class="oj-cursor-dot"></span>
    </div>
  </div>
</template>
