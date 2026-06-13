<script lang="ts">
import { defineComponent, type PropType } from 'vue';
import { useShopStore } from '../stores/shop';
import { CATEGORIES } from '../data/products';
import JIcon from './JIcon.vue';

/** ProductThumb 只需要這些欄位，Product 與購物車商品皆相容。 */
interface ThumbProduct {
  hue: number;
  cat: string;
  formats: string[];
  totalSize: string;
}

export default defineComponent({
  name: 'ProductThumb',
  components: { JIcon },
  props: {
    product: { type: Object as PropType<ThumbProduct>, required: true },
    label: { type: String, default: '' },
    glyphSize: { type: Number, default: 64 },
    showCat: { type: Boolean, default: true },
    hideLabel: { type: Boolean, default: false },
    seed: { type: Number, default: 0 },
  },
  setup() { return { store: useShopStore() }; },
  computed: {
    hue() { return (this.product.hue + this.seed * 22) % 360; },
    dark() { return this.store.theme === 'dark'; },
    vars() {
      const h = this.hue;
      const h2 = (h + 42) % 360;
      return {
        '--c1': `hsl(${h} 88% 62%)`,
        '--c2': `hsl(${h2} 90% 54%)`,
        '--c-deep': `hsl(${h} 70% 26%)`,
      };
    },
    catGlyph() {
      const c = CATEGORIES.find((c) => c.id === this.product.cat);
      return c ? c.glyph : 'image';
    },
    catLabel() {
      return ({ music: 'SCORE', photo: 'PHOTO', ebook: 'EBOOK' } as Record<string, string>)[this.product.cat] || '';
    },
    autoLabel() {
      if (this.label) return this.label;
      return `${this.product.formats[0]} · ${this.product.totalSize}`;
    },
  },
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
