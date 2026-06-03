<script>
import { CATEGORIES } from '@/data'

export default {
  name: 'ProductThumb',
  props: {
    product: Object,
    label: { type: String, default: '' },
    glyphSize: { type: Number, default: 64 },
    showCat: { type: Boolean, default: true },
    hideLabel: { type: Boolean, default: false },
    seed: { type: Number, default: 0 },
  },
  computed: {
    hue() { return (this.product.hue + this.seed * 22) % 360 },
    vars() {
      const h = this.hue, h2 = (h + 42) % 360
      return { '--c1': `hsl(${h} 88% 62%)`, '--c2': `hsl(${h2} 90% 54%)` }
    },
    catGlyph() {
      const c = CATEGORIES.find(c => c.id === this.product.cat)
      return c ? c.glyph : 'image'
    },
    catLabel() { return ({ music: 'SCORE', photo: 'PHOTO', ebook: 'EBOOK' })[this.product.cat] || '' },
    autoLabel() {
      if (this.label) return this.label
      return `${this.product.formats[0]} · ${this.product.totalSize}`
    },
  },
}
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
