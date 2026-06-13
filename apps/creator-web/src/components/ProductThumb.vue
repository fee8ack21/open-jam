<script setup lang="ts">
import { computed } from 'vue';
import { CATEGORIES } from '../data/products';
import JIcon from './JIcon.vue';

/** ProductThumb 只需要這些欄位，Product 與購物車商品皆相容。 */
interface ThumbProduct {
  hue: number;
  cat: string;
  formats: string[];
  totalSize: string;
}

const props = withDefaults(defineProps<{
  product: ThumbProduct
  label?: string
  glyphSize?: number
  showCat?: boolean
  hideLabel?: boolean
  seed?: number
}>(), {
  label: '',
  glyphSize: 64,
  showCat: true,
  hideLabel: false,
  seed: 0,
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
const catLabel = computed(() => ({ music: 'SCORE', photo: 'PHOTO', ebook: 'EBOOK' } as Record<string, string>)[props.product.cat] || '');
const autoLabel = computed(() => {
  if (props.label) return props.label;
  return `${props.product.formats[0]} · ${props.product.totalSize}`;
});
</script>

<template>
  <div class="thumb" :style="vars">
    <div class="thumb-dots"></div>
    <div class="thumb-blob"></div>
    <div v-if="showCat" class="thumb-cat">{{ catLabel }}</div>
    <div class="thumb-glyph"><j-icon :name="catGlyph" :size="glyphSize" :stroke="1.6" /></div>
    <div v-if="!hideLabel" class="thumb-label">{{ autoLabel }}</div>
  </div>
</template>
