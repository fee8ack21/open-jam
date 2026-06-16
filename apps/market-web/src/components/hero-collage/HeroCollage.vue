<script setup lang="ts">
/* ============================================================
   HeroCollage — static floating product-card collage behind the
   market hero, echoing the auth/register brand panel for a clean,
   modern feel. Purely decorative (aria-hidden, pointer-events off)
   and hidden on narrow screens.

   Subtle, tasteful motion only: a slow ambient bob (CSS) plus an
   eased pointer parallax (each card drifts by a depth factor toward
   the cursor). Both respect prefers-reduced-motion.
   ============================================================ */
import { ref, onMounted, onBeforeUnmount } from 'vue';
import AppIcon from '@/components/app-icon/AppIcon.vue';

interface CollageCard {
  cls: string;   // position + rotation class (fc-1 … fc-4)
  depth: number; // parallax intensity in px (foreground cards drift more)
  grad: string;
  cat: string;
  glyph: string;
  title: string;
  price: string;
  free?: boolean;
  rating: string;
}

const cards: CollageCard[] = [
  { cls: 'fc-1', depth: 14, grad: 'linear-gradient(135deg,#6c4cf1,#ff4d9d)', cat: 'SCORE', glyph: 'note',  title: '午夜爵士鋼琴',   price: '$18', rating: '4.9' },
  { cls: 'fc-2', depth: 10, grad: 'linear-gradient(135deg,#ffc83a,#ff7a2f)', cat: 'EBOOK', glyph: 'book',  title: '插畫家手冊',     price: '免費', free: true, rating: '5.0' },
  { cls: 'fc-3', depth: 16, grad: 'linear-gradient(135deg,#ff7a2f,#ff4d9d)', cat: 'PHOTO', glyph: 'image', title: '霓虹城市夜景',   price: '$24', rating: '4.8' },
  { cls: 'fc-4', depth: 11, grad: 'linear-gradient(135deg,#1fd6c6,#6c4cf1)', cat: 'SCORE', glyph: 'note',  title: 'Lo-Fi 節拍包',   price: '$15', rating: '4.7' },
];

// normalised cursor offset from viewport centre (-1 … 1), eased toward target
const px = ref(0);
const py = ref(0);
let targetX = 0;
let targetY = 0;
let raf = 0;
let reduced = false;

function tick() {
  px.value += (targetX - px.value) * 0.08;
  py.value += (targetY - py.value) * 0.08;
  if (Math.abs(targetX - px.value) > 0.001 || Math.abs(targetY - py.value) > 0.001) {
    raf = requestAnimationFrame(tick);
  } else {
    px.value = targetX;
    py.value = targetY;
    raf = 0;
  }
}

function onPointer(e: PointerEvent) {
  targetX = (e.clientX / window.innerWidth - 0.5) * 2;
  targetY = (e.clientY / window.innerHeight - 0.5) * 2;
  if (!raf) raf = requestAnimationFrame(tick);
}

function parallax(depth: number) {
  return {
    transform: `translate3d(${(px.value * depth).toFixed(1)}px, ${(py.value * depth).toFixed(1)}px, 0)`,
  };
}

onMounted(() => {
  reduced = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false;
  if (!reduced) window.addEventListener('pointermove', onPointer, { passive: true });
});

onBeforeUnmount(() => {
  window.removeEventListener('pointermove', onPointer);
  if (raf) cancelAnimationFrame(raf);
});
</script>

<template>
  <div class="hero-collage" aria-hidden="true">
    <div v-for="c in cards" :key="c.cls" class="fc" :class="c.cls" :style="parallax(c.depth)">
      <div class="fc-float">
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
    </div>
    <span class="c-shape sq" :style="parallax(24)"></span>
    <span class="c-shape dot d1" :style="parallax(16)"></span>
    <span class="c-shape dot d2" :style="parallax(32)"></span>
  </div>
</template>
