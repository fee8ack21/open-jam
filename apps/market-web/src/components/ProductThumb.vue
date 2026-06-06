<script setup>
/* ============================================================
   ProductThumb — vibrant gradient + halftone placeholder thumb
   Registered globally as <product-thumb>.
   ============================================================ */
import { computed } from 'vue';
import { CATEGORIES } from '@/data/catalogue.js';

const props = defineProps({
  product: Object,
  label: { type: String, default: '' },
  glyphSize: { type: Number, default: 64 },
  showCat: { type: Boolean, default: true },
  hideLabel: { type: Boolean, default: false },
  seed: { type: Number, default: 0 },
});

const hue = computed(() => (props.product.hue + props.seed * 22) % 360);
const vars = computed(() => {
  const h = hue.value;
  const h2 = (h + 42) % 360;
  return {
    '--c1': `hsl(${h} 88% 62%)`,
    '--c2': `hsl(${h2} 90% 54%)`,
    '--c-deep': `hsl(${h} 70% 26%)`,
  };
});
const catGlyph = computed(() => {
  const c = CATEGORIES.find((c) => c.id === props.product.cat);
  return c ? c.glyph : 'image';
});
const catLabel = computed(() => ({ music: 'SCORE', photo: 'PHOTO', ebook: 'EBOOK' }[props.product.cat] || ''));
const autoLabel = computed(() => props.label || `${props.product.formats[0]} · ${props.product.totalSize}`);
</script>

<template>
  <div class="thumb" :style="vars">
    <div class="thumb-dots"></div>
    <div class="thumb-blob"></div>
    <div v-if="showCat" class="thumb-cat">{{ catLabel }}</div>
    <div class="thumb-glyph">
      <j-icon :name="catGlyph" :size="glyphSize" :stroke="1.6" />
    </div>
    <div v-if="!hideLabel" class="thumb-label">{{ autoLabel }}</div>
  </div>
</template>
