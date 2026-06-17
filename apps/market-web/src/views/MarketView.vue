<script setup lang="ts">
/* ============================================================
   MarketView — 市場集 hub (home route "/")
   Search-led hero + category / sort / price browse grid.
   Cards deep-link into the storefront detail route.
   ============================================================ */
import { ref, computed, watch, onMounted, onBeforeUnmount } from 'vue';
import { useShopStore } from '@/stores/shop.js';
import { PRODUCTS, CATEGORIES, type Product } from '@/data/products';
import AppNav from '@/layout/AppNav.vue';
import AppFooter from '@/layout/AppFooter.vue';
import HeroCollage from '@/components/hero-collage/HeroCollage.vue';
import FeaturedCard from '@/components/FeaturedCard.vue';
import OnboardingGuide from '@/components/OnboardingGuide.vue';

const orderMap = new Map(PRODUCTS.map((p, i) => [p.id, i])); // catalogue order → newest = larger index

const store = useShopStore();

const cats = CATEGORIES;
const keywords = ['爵士', '京都', '鋼琴', 'Notion', '街拍', '免費'];
const search = ref('');
const category = ref('all');
const sort = ref('popular');
const priceBand = ref('all');
const pageSize = 12;
const visibleCount = ref(12);
const showToTop = ref(false);

const sortOptions = [
  { v: 'popular', l: '最熱門' },
  { v: 'newest', l: '最新上架' },
  { v: 'rating', l: '評分最高' },
  { v: 'price-asc', l: '價格低 → 高' },
  { v: 'price-desc', l: '價格高 → 低' },
];
const priceOptions = [
  { v: 'all', l: '不限' },
  { v: 'free', l: '免費' },
  { v: 'low', l: '$1–15' },
  { v: 'mid', l: '$16–30' },
  { v: 'high', l: '$30+' },
];

function inBand(price: number): boolean {
  switch (priceBand.value) {
    case 'free': return price === 0;
    case 'low':  return price > 0 && price <= 15;
    case 'mid':  return price >= 16 && price <= 30;
    case 'high': return price > 30;
    default:     return true;
  }
}

const results = computed(() => {
  let list = PRODUCTS.slice();
  const q = search.value.trim().toLowerCase();
  if (q) {
    list = list.filter(
      (p) =>
        p.title.toLowerCase().includes(q) ||
        p.creator.toLowerCase().includes(q) ||
        p.tags.some((t) => t.toLowerCase().includes(q)),
    );
  }
  if (category.value !== 'all') list = list.filter((p) => p.cat === category.value);
  list = list.filter((p) => inBand(p.price));
  switch (sort.value) {
    case 'newest':     list.sort((a, b) => (orderMap.get(b.id) ?? 0) - (orderMap.get(a.id) ?? 0)); break;
    case 'rating':     list.sort((a, b) => b.rating - a.rating); break;
    case 'price-asc':  list.sort((a, b) => a.price - b.price); break;
    case 'price-desc': list.sort((a, b) => b.price - a.price); break;
    default:           list.sort((a, b) => b.sales - a.sales);
  }
  return list;
});

const activeCatLabel = computed(() => {
  if (category.value === 'all') return '全部作品';
  return CATEGORIES.find((c) => c.id === category.value)?.label ?? '全部作品';
});

// active filter chips — surface what's currently narrowing the grid (excludes sort)
const activeFilters = computed(() => {
  const chips: { key: string; label: string; clear: () => void }[] = [];
  const q = search.value.trim();
  if (q) chips.push({ key: 'q', label: `「${q}」`, clear: () => { search.value = ''; } });
  if (category.value !== 'all') chips.push({ key: 'cat', label: activeCatLabel.value, clear: () => { category.value = 'all'; } });
  if (priceBand.value !== 'all') {
    const l = priceOptions.find((o) => o.v === priceBand.value)?.l ?? '';
    chips.push({ key: 'price', label: l, clear: () => { priceBand.value = 'all'; } });
  }
  return chips;
});

const visibleResults = computed(() => results.value.slice(0, visibleCount.value));
const hasMore = computed(() => visibleCount.value < results.value.length);

watch(results, () => { visibleCount.value = pageSize; });

// ----- featured carousel (精選作品) -----
const featured = computed(() => PRODUCTS.filter((p) => p.featured));
const featTrack = ref<HTMLElement | null>(null);
const canLeft = ref(false);
const canRight = ref(false);
function updateFeatNav() {
  const el = featTrack.value;
  if (!el) return;
  canLeft.value = el.scrollLeft > 4;
  canRight.value = el.scrollLeft + el.clientWidth < el.scrollWidth - 4;
}
function scrollFeat(dir: number) {
  featTrack.value?.scrollBy({ left: dir * featTrack.value.clientWidth * 0.9, behavior: 'smooth' });
}

// drag-to-scroll (mouse only — touch keeps native momentum scrolling)
let dragging = false;
let dragMoved = false;
let dragStartX = 0;
let dragStartScroll = 0;
function onFeatDown(e: PointerEvent) {
  if (e.pointerType !== 'mouse' || e.button !== 0 || !featTrack.value) return;
  dragging = true;
  dragMoved = false;
  dragStartX = e.clientX;
  dragStartScroll = featTrack.value.scrollLeft;
}
function onFeatMove(e: PointerEvent) {
  if (!dragging || !featTrack.value) return;
  const dx = e.clientX - dragStartX;
  if (!dragMoved && Math.abs(dx) > 4) {
    dragMoved = true;
    featTrack.value.setPointerCapture?.(e.pointerId);
    featTrack.value.classList.add('dragging');
  }
  if (dragMoved) featTrack.value.scrollLeft = dragStartScroll - dx;
}
function onFeatUp() {
  if (!dragging) return;
  dragging = false;
  featTrack.value?.classList.remove('dragging');
  updateFeatNav();
}
// swallow the click that follows a drag so cards don't navigate
function onFeatClick(e: MouseEvent) {
  if (dragMoved) { e.preventDefault(); e.stopPropagation(); dragMoved = false; }
}

// ----- grid badges (精選 / 熱賣 / 新上架) — adds rhythm to the otherwise uniform grid -----
const newestIds = new Set(
  PRODUCTS.slice()
    .sort((a, b) => (orderMap.get(b.id) ?? 0) - (orderMap.get(a.id) ?? 0))
    .slice(0, 3)
    .map((p) => p.id),
);
function badgeFor(p: Product): { label: string; tone: 'hot' | 'new' | 'feat' } | null {
  if (p.featured) return { label: '精選', tone: 'feat' };
  if (p.sales >= 1500) return { label: '熱賣', tone: 'hot' };
  if (newestIds.has(p.id)) return { label: '新上架', tone: 'new' };
  return null;
}

function catColor(id: string): string {
  const map: Record<string, string> = { music: 'var(--c-violet)', photo: 'var(--c-pink)', ebook: 'var(--c-cyan)' };
  return map[id] ?? '';
}
function catCount(id: string): number { return id === 'all' ? PRODUCTS.length : PRODUCTS.filter((p) => p.cat === id).length; }
function scrollToBrowse() {
  const el = document.getElementById('browse');
  if (el) window.scrollTo({ top: el.offsetTop - 80, behavior: 'smooth' });
}
function pickKeyword(k: string) { search.value = k; scrollToBrowse(); }
function reset() { category.value = 'all'; sort.value = 'popular'; priceBand.value = 'all'; search.value = ''; }
function onScroll() { showToTop.value = window.scrollY > 300; }
function scrollToTop() { window.scrollTo({ top: 0, behavior: 'smooth' }); }
function loadMore() { visibleCount.value += pageSize; }

onMounted(() => {
  window.addEventListener('scroll', onScroll);
  window.addEventListener('resize', updateFeatNav);
  updateFeatNav();
});
onBeforeUnmount(() => {
  window.removeEventListener('scroll', onScroll);
  window.removeEventListener('resize', updateFeatNav);
});
</script>

<template>
  <div class="oj-root" :class="'font-' + store.font" data-screen-label="市場集首頁">
    <!-- ============ NAV ============ -->
    <app-nav />

    <main class="page" id="top">
      <!-- ============ HERO ============ -->
      <section class="mkt-hero">
        <hero-collage />
        <div class="mkt-hero-inner">
          <p class="hero-eyebrow"><app-icon name="sparkle" :size="14" /> OPEN JAM · 創作者數位市集</p>
          <h1 class="mkt-hero-title">發現值得<span class="hl hl-lime">收藏</span>的<br>創作者<span class="hl hl-pink">數位作品</span></h1>

          <form class="mkt-search" @submit.prevent="scrollToBrowse">
            <span class="s-ic"><app-icon name="search" :size="22" /></span>
            <input v-model="search" type="text" placeholder="搜尋作品、創作者或標籤…" aria-label="搜尋市集" />
            <button type="submit"><app-icon name="search" :size="17" /> 搜尋</button>
          </form>

          <div class="kw-row">
            <span class="kw-lab">熱門搜尋</span>
            <a v-for="k in keywords" :key="k" class="kw" href="#browse" @click.prevent="pickKeyword(k)">{{ k }}</a>
          </div>
        </div>
      </section>

      <!-- ============ FEATURED ============ -->
      <section v-if="featured.length" class="sec featured" id="featured">
        <div class="feat-head">
          <div class="feat-head-text">
            <p class="browse-eyebrow"><app-icon name="sparkle" :size="13" /> 編輯精選</p>
            <h2 class="browse-title">精選作品</h2>
          </div>
          <div class="feat-nav">
            <button type="button" class="feat-arrow prev" :disabled="!canLeft" @click="scrollFeat(-1)" aria-label="上一個精選">
              <app-icon name="chevron" :size="20" :stroke="2.4" />
            </button>
            <button type="button" class="feat-arrow" :disabled="!canRight" @click="scrollFeat(1)" aria-label="下一個精選">
              <app-icon name="chevron" :size="20" :stroke="2.4" />
            </button>
          </div>
        </div>
        <div
          class="feat-track"
          ref="featTrack"
          @scroll="updateFeatNav"
          @pointerdown="onFeatDown"
          @pointermove="onFeatMove"
          @pointerup="onFeatUp"
          @pointercancel="onFeatUp"
          @click.capture="onFeatClick"
          @dragstart.prevent
        >
          <featured-card v-for="p in featured" :key="p.id" :product="p" />
        </div>
      </section>

      <!-- ============ BROWSE ============ -->
      <section class="sec browse" id="browse">
        <div class="browse-head">
          <p class="browse-eyebrow"><app-icon name="sparkle" :size="13" /> 探索市集</p>
          <h2 class="browse-title">探索所有作品</h2>
          <p class="browse-sub">數千件來自獨立創作者的音樂、攝影與電子書，每一件都能立即下載擁有。</p>
        </div>

        <!-- toolbar: result count + sort -->
        <div class="browse-toolbar">
          <span class="browse-count"><b>{{ activeCatLabel }}</b> · 共 <b>{{ results.length }}</b> 件作品</span>
          <div class="sort-tabs">
            <span class="sort-lab">排序</span>
            <button type="button" v-for="o in sortOptions" :key="o.v" class="sort-tab" :class="{ on: sort === o.v }" :aria-pressed="sort === o.v" @click="sort = o.v">{{ o.l }}</button>
            <select class="m-select sort-select" v-model="sort" aria-label="排序方式">
              <option v-for="o in sortOptions" :key="o.v" :value="o.v">{{ o.l }}</option>
            </select>
          </div>
        </div>

        <div class="browse-body">
          <!-- filter sidebar (Gumroad-style left rail) -->
          <aside class="browse-side">
            <div class="side-card">
              <p class="side-title"><app-icon name="sparkle" :size="15" /> 篩選</p>

              <div class="side-group">
                <p class="side-label">分類</p>
                <div class="side-opts">
                  <button type="button" class="side-opt" :class="{ on: category === 'all' }" @click="category = 'all'">
                    <span class="so-dot" style="background: var(--c-violet)"></span>
                    <span class="so-name">全部作品</span>
                    <span class="so-count">{{ catCount('all') }}</span>
                  </button>
                  <button type="button" v-for="c in cats" :key="c.id" class="side-opt" :class="{ on: category === c.id }" @click="category = c.id">
                    <span class="so-dot" :style="{ background: catColor(c.id) }"></span>
                    <span class="so-name">{{ c.label }}</span>
                    <span class="so-count">{{ catCount(c.id) }}</span>
                  </button>
                </div>
                <select class="m-select side-select" v-model="category" aria-label="作品分類">
                  <option value="all">全部作品（{{ catCount('all') }}）</option>
                  <option v-for="c in cats" :key="c.id" :value="c.id">{{ c.label }}（{{ catCount(c.id) }}）</option>
                </select>
              </div>

              <div class="side-group">
                <p class="side-label">價格</p>
                <div class="side-opts">
                  <button type="button" v-for="o in priceOptions" :key="o.v" class="side-opt" :class="{ on: priceBand === o.v }" @click="priceBand = o.v">
                    <span class="so-name">{{ o.l }}</span>
                  </button>
                </div>
                <select class="m-select side-select" v-model="priceBand" aria-label="價格區間">
                  <option v-for="o in priceOptions" :key="o.v" :value="o.v">{{ o.l }}</option>
                </select>
              </div>

              <button v-if="activeFilters.length" type="button" class="side-reset" @click="reset">清除全部篩選</button>
            </div>
          </aside>

          <!-- results -->
          <div class="browse-main">
            <div v-if="activeFilters.length" class="active-chips">
              <span class="active-chips-lab">篩選中</span>
              <button v-for="f in activeFilters" :key="f.key" type="button" class="fchip" @click="f.clear()">
                {{ f.label }}
                <span class="fchip-x"><app-icon name="close" :size="13" :stroke="2.4" /></span>
              </button>
              <button type="button" class="fchip-clear" @click="reset">清除全部</button>
            </div>

            <div v-if="results.length" class="grid">
              <product-card v-for="p in visibleResults" :key="p.id" :product="p" :badge="badgeFor(p)" />
            </div>
            <div v-if="hasMore" class="load-more-wrap">
              <button class="load-more-btn" @click="loadMore">
                載入更多
                <span class="load-more-count">（還有 {{ results.length - visibleCount }} 件）</span>
              </button>
            </div>
            <div v-if="!results.length" class="empty">
              <app-icon name="search" :size="40" style="margin-bottom: 14px; opacity: 0.5" />
              <p style="font-size: 17px; font-weight: 600; color: var(--text-soft)">找不到符合的作品</p>
              <p style="margin-top: 6px">試著放寬篩選條件或清除搜尋關鍵字。</p>
              <span class="kw" style="display: inline-block; margin-top: 18px; cursor: pointer" @click="reset">清除全部篩選</span>
            </div>
          </div>
        </div>
      </section>

      <!-- ============ FOOTER ============ -->
      <app-footer />
    </main>

    <Transition name="to-top">
      <button v-if="showToTop" class="to-top-btn" @click="scrollToTop" aria-label="回到頂部">
        <app-icon name="chevronU" :size="22" />
      </button>
    </Transition>

    <onboarding-guide />
  </div>
</template>
