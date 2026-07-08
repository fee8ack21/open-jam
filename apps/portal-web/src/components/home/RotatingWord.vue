<script setup lang="ts">
/* ============================================================
   RotatingWord — hero 標題內的輪播關鍵詞。字詞翻牌輪替、
   色塊隨字換色（詞序對應 TONES）。

   slot 寬度固定（以 em 記、跟著標題字級縮放）：量測各詞在
   基準字級下的自然寬度，取「最寬詞」與「最窄詞 × MAX_SCALE」
   較小者為 slot 寬，再以 transform: scale() 讓每個詞剛好填滿
   ——字少字體大、字多字體小，輪替時標題總寬恆定不跳動。
   transform 不參與排版，縮放不會撐開行高或擠到相鄰文字。

   prefers-reduced-motion 時停在第一個（最泛用的）詞不輪播。
   ============================================================ */
import { computed, onBeforeUnmount, onMounted, ref, watch, nextTick } from 'vue';

const props = defineProps<{ words: string[] }>();

// 詞序固定對應：數位作品→黃、樂譜→紫、攝影集→粉、電子書→青
const TONES = ['yellow', 'violet', 'pink', 'cyan'];
const INTERVAL_MS = 2600;
const MAX_SCALE = 1.45; // 短字放大上限，避免色塊高度過度膨脹

const rootEl = ref<HTMLElement | null>(null);
const idx = ref(0);
const slotEm = ref<number | null>(null);
const scales = ref<number[]>([]);
let timer = 0;

const word = computed(() => props.words[idx.value] ?? '');
const tone = computed(() => TONES[idx.value % TONES.length]);
const rootStyle = computed(() => (slotEm.value ? { width: `${slotEm.value}em` } : undefined));
const pillStyle = computed(() => ({
  transform: `rotate(-1.5deg) scale(${(scales.value[idx.value] ?? 1).toFixed(4)})`,
}));

/** 量測各詞自然寬度，決定固定 slot 寬與各詞的填滿倍率。 */
function measure() {
  const root = rootEl.value;
  if (!root) return;
  const widths = [...root.querySelectorAll<HTMLElement>('.rw-sizer')].map((s) => s.offsetWidth);
  if (!widths.length || widths.some((w) => !w)) return;
  const fontSize = parseFloat(getComputedStyle(root).fontSize);
  const slot = Math.min(Math.max(...widths), Math.min(...widths) * MAX_SCALE);
  slotEm.value = slot / fontSize;
  scales.value = widths.map((w) => slot / w);
}

watch(() => props.words, () => { idx.value = 0; nextTick(measure); });

onMounted(() => {
  measure();
  // 顯示字型載入完成後字寬會變，需重新量測
  document.fonts?.ready.then(measure);
  if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;
  timer = window.setInterval(() => {
    if (props.words.length) idx.value = (idx.value + 1) % props.words.length;
  }, INTERVAL_MS);
});
onBeforeUnmount(() => window.clearInterval(timer));
</script>

<template>
  <span ref="rootEl" class="rw" :style="rootStyle">
    <!-- 隱形佔位：僅供量測各詞自然寬度（含色塊左右 padding） -->
    <span v-for="w in words" :key="w" class="rw-sizer" aria-hidden="true">{{ w }}</span>
    <Transition name="rw-flip">
      <span :key="idx" class="rw-word">
        <span class="rw-pill" :class="'rw-' + tone" :style="pillStyle">{{ word }}</span>
      </span>
    </Transition>
  </span>
</template>
