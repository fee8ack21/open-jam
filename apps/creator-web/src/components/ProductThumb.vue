<script setup lang="ts">
/* ============================================================
   ProductThumb — 便條淡彩 + 圖紋佔位縮圖（果醬罐設計稿，
   同 portal-web）：由商品 hue 決定固定的 pastel / 紋理組合，
   同一商品在任何列表位置外觀一致。有實際縮圖時直接顯示。
   ============================================================ */
import { computed } from 'vue';
import { CATEGORIES } from '@/data/products';
import AppIcon from './app-icon';

/** ProductThumb 只需要這些欄位，Product 與購物車商品皆相容。 */
interface ThumbProduct {
  hue: number;
  cat: string;
  formats: string[];
  totalSize: string;
  /** 實際商品縮圖；有值時取代程式產生的佔位縮圖。 */
  image?: string;
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
  glyphSize: 42,
  showCat: true,
  hideLabel: false,
  seed: 0,
});

const PASTELS = ['#dff5d3', '#e4f6ff', '#ffe3f6', '#fff3c4', '#ede6ff'];
const PATTERNS: { image: string; size: string }[] = [
  { image: 'radial-gradient(rgba(26,26,26,0.08) 1.5px, transparent 1.5px)', size: '18px 18px' },
  { image: 'repeating-linear-gradient(45deg, transparent, transparent 12px, rgba(26,26,26,0.07) 12px, rgba(26,26,26,0.07) 14px)', size: 'auto' },
  { image: 'repeating-linear-gradient(-45deg, transparent, transparent 12px, rgba(26,26,26,0.07) 12px, rgba(26,26,26,0.07) 14px)', size: 'auto' },
  { image: 'repeating-linear-gradient(0deg, transparent, transparent 12px, rgba(26,26,26,0.07) 12px, rgba(26,26,26,0.07) 14px)', size: 'auto' },
  { image: 'repeating-linear-gradient(90deg, transparent, transparent 12px, rgba(26,26,26,0.07) 12px, rgba(26,26,26,0.07) 14px)', size: 'auto' },
];
const vars = computed(() => {
  const n = Math.abs(props.product.hue + props.seed * 22);
  const pattern = PATTERNS[Math.floor(n / 60) % PATTERNS.length];
  return {
    backgroundColor: PASTELS[n % PASTELS.length],
    backgroundImage: pattern.image,
    backgroundSize: pattern.size,
  };
});
const catGlyph = computed(() => {
  const c = CATEGORIES.find((c) => c.id === props.product.cat);
  return c ? c.glyph : 'image';
});
const catLabel = computed((): string => {
  const map: Record<string, string> = { music: 'SCORE', photo: 'PHOTO', ebook: 'EBOOK' };
  return map[props.product.cat] ?? '';
});
// 後端商品列表不提供檔案格式 / 總大小（formats 為空、totalSize 為 '—' 佔位），
// 缺值時整段略過，避免顯示 "undefined · —"。
const autoLabel = computed(() =>
  props.label || [props.product.formats[0], props.product.totalSize]
    .filter((part) => part && part !== '—')
    .join(' · '));
</script>

<template>
  <div class="thumb" :class="{ 'has-image': !!product.image }" :style="product.image ? undefined : vars">
    <template v-if="product.image">
      <img class="thumb-img" :src="product.image" alt="" loading="lazy" />
    </template>
    <template v-else>
      <div v-if="showCat" class="thumb-cat">{{ catLabel }}</div>
      <div class="thumb-glyph">
        <app-icon :name="catGlyph" :size="glyphSize" />
      </div>
      <div v-if="!hideLabel && autoLabel" class="thumb-label">{{ autoLabel }}</div>
    </template>
  </div>
</template>

<style scoped>
.thumb-img {
  position: absolute;
  inset: 0;
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
}
</style>
