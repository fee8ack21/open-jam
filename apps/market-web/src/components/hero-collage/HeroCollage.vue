<script setup lang="ts">
/* ============================================================
   HeroCollage — static floating product-card collage behind the
   market hero, echoing the auth/register brand panel. Purely
   decorative (aria-hidden, pointer-events off) and hidden on narrow
   screens. Fully static: no pointer parallax, no ambient motion.
   ============================================================ */
import AppIcon from '@/components/app-icon/AppIcon.vue';

interface CollageCard {
  cls: string; // position + rotation class (fc-1 … fc-4)
  grad: string;
  cat: string;
  glyph: string;
  title: string;
  price: string;
  free?: boolean;
  rating: string;
}

const cards: CollageCard[] = [
  { cls: 'fc-1', grad: 'linear-gradient(135deg,#6c4cf1,#ff4d9d)', cat: 'SCORE', glyph: 'note',  title: '午夜爵士鋼琴',   price: '$18', rating: '4.9' },
  { cls: 'fc-2', grad: 'linear-gradient(135deg,#ffc83a,#ff7a2f)', cat: 'EBOOK', glyph: 'book',  title: '插畫家手冊',     price: '免費', free: true, rating: '5.0' },
  { cls: 'fc-3', grad: 'linear-gradient(135deg,#ff7a2f,#ff4d9d)', cat: 'PHOTO', glyph: 'image', title: '霓虹城市夜景',   price: '$24', rating: '4.8' },
  { cls: 'fc-4', grad: 'linear-gradient(135deg,#1fd6c6,#6c4cf1)', cat: 'SCORE', glyph: 'note',  title: 'Lo-Fi 節拍包',   price: '$15', rating: '4.7' },
];
</script>

<template>
  <div class="hero-collage" aria-hidden="true">
    <div v-for="c in cards" :key="c.cls" class="fc" :class="c.cls">
      <div class="fc-card">
        <div class="fc-thumb" :style="{ background: c.grad }">
          <div class="fc-dots"></div>
          <div class="fc-cat">{{ c.cat }}</div>
          <div class="fc-glyph"><app-icon :name="c.glyph" :size="32" :stroke="1.6" /></div>
        </div>
        <div class="fc-body">
          <div class="fc-title">{{ c.title }}</div>
          <div class="fc-foot">
            <span class="fc-price" :class="{ free: c.free }">{{ c.price }}</span>
            <span class="fc-star"><app-icon name="star" :size="11" fill style="color: #f0a92b" /> {{ c.rating }}</span>
          </div>
        </div>
      </div>
    </div>
    <span class="c-shape sq"></span>
    <span class="c-shape dot d1"></span>
    <span class="c-shape dot d2"></span>
  </div>
</template>
