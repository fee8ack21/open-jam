<script setup lang="ts">
/* ============================================================
   RotatingWord — hero 標題內的輪播關鍵詞。字詞翻牌輪替、
   色塊隨字換色（詞序對應 TONES）。prefers-reduced-motion 時
   停在第一個（最泛用的）詞不輪播。
   ============================================================ */
import { computed, onBeforeUnmount, onMounted, ref } from 'vue';

const props = defineProps<{ words: string[] }>();

// 詞序固定對應：數位作品→黃、樂譜→紫、攝影集→粉、電子書→青
const TONES = ['yellow', 'violet', 'pink', 'cyan'];
const INTERVAL_MS = 2600;

const idx = ref(0);
let timer = 0;

const word = computed(() => props.words[idx.value] ?? '');
const tone = computed(() => TONES[idx.value % TONES.length]);

onMounted(() => {
  if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;
  timer = window.setInterval(() => {
    if (props.words.length) idx.value = (idx.value + 1) % props.words.length;
  }, INTERVAL_MS);
});
onBeforeUnmount(() => window.clearInterval(timer));
</script>

<template>
  <span class="rw">
    <Transition name="rw-flip">
      <span :key="idx" class="rw-word" :class="'rw-' + tone">{{ word }}</span>
    </Transition>
  </span>
</template>
