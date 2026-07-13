<script setup lang="ts">
/* ============================================================
   HeroCollage — floating product-card collage behind the market
   hero, echoing the auth/register brand panel. Purely decorative
   (aria-hidden, pointer-events off) and hidden on narrow screens.

   Motion (all gated behind prefers-reduced-motion):
   - pointer parallax: cards/shapes drift toward the cursor at
     per-element depths, smoothed with a rAF lerp loop.
   - scroll parallax: elements rise/sink at different rates while
     scrolling through the hero.
   - ambient bob: gentle out-of-sync float (pure CSS, see market.css).
   The loop only sets CSS vars (--mx/--my/--scroll) on the root; all
   depths + composition live in CSS so transforms compose with the
   static rotation on .fc-card and the bob on .fc-float.
   ============================================================ */
import { computed, onBeforeUnmount, onMounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import AppIcon from '@/components/app-icon/AppIcon.vue';

const { t } = useI18n();

interface CollageCard {
  cls: string; // position + rotation + palette class (fc-1 … fc-4)
  cat: string;
  glyph: string;
  title: string;
  price: string;
  free?: boolean;
  rating: string;
}

const cards = computed<CollageCard[]>(() => [
  { cls: 'fc-1', cat: 'SCORE', glyph: 'note',  title: t('collage.card1'), price: '$18', rating: '4.9' },
  { cls: 'fc-2', cat: 'EBOOK', glyph: 'book',  title: t('collage.card2'), price: t('common.free'), free: true, rating: '5.0' },
  { cls: 'fc-3', cat: 'PHOTO', glyph: 'image', title: t('collage.card3'), price: '$24', rating: '4.8' },
  { cls: 'fc-4', cat: 'BEATS', glyph: 'beats',  title: t('collage.card4'), price: '$15', rating: '4.7' },
]);

/* ---- pointer + scroll parallax driver ---------------------------- */
const root = ref<HTMLElement | null>(null);

let raf = 0;
let tmx = 0, tmy = 0;        // target pointer offset, normalised -1..1
let cmx = 0, cmy = 0;        // current (lerped) pointer offset
let tScroll = 0, cScroll = 0; // target / current scroll position (px)
let cleanup: (() => void) | null = null;

function onPointer(e: PointerEvent) {
  tmx = (e.clientX / window.innerWidth) * 2 - 1;
  tmy = (e.clientY / window.innerHeight) * 2 - 1;
}
function onScroll() {
  tScroll = window.scrollY;
}
function tick() {
  // ease current toward target; pointer trails a little softer than scroll
  cmx += (tmx - cmx) * 0.06;
  cmy += (tmy - cmy) * 0.06;
  cScroll += (tScroll - cScroll) * 0.12;
  const el = root.value;
  if (el) {
    el.style.setProperty('--mx', cmx.toFixed(4));
    el.style.setProperty('--my', cmy.toFixed(4));
    el.style.setProperty('--scroll', `${cScroll.toFixed(1)}px`);
  }
  raf = requestAnimationFrame(tick);
}

onMounted(() => {
  // honour reduced-motion: leave the collage fully static
  if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;
  window.addEventListener('pointermove', onPointer, { passive: true });
  window.addEventListener('scroll', onScroll, { passive: true });
  raf = requestAnimationFrame(tick);
  cleanup = () => {
    cancelAnimationFrame(raf);
    window.removeEventListener('pointermove', onPointer);
    window.removeEventListener('scroll', onScroll);
  };
});
onBeforeUnmount(() => cleanup?.());
</script>

<template>
  <div ref="root" class="hero-collage" aria-hidden="true">
    <div v-for="c in cards" :key="c.cls" class="fc" :class="c.cls">
      <div class="fc-float">
        <div class="fc-card">
          <span class="fc-cat">{{ c.cat }}</span>
          <div class="fc-glyph"><app-icon :name="c.glyph" :size="28" /></div>
          <div class="fc-title">{{ c.title }}</div>
          <div class="fc-foot">
            <span class="fc-price" :class="{ free: c.free }">{{ c.price }}</span>
            <span class="fc-star"><app-icon name="star" :size="12" /> {{ c.rating }}</span>
          </div>
        </div>
      </div>
    </div>
    <span class="c-shape sq"></span>
    <span class="c-shape dot d1"></span>
    <span class="c-shape dot d2"></span>
  </div>
</template>
