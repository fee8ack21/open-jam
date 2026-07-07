<script setup lang="ts">
/* ============================================================
   MarketView — 市場集 hub (home route "/")
   Search-led hero + category / sort / price browse grid.
   Cards deep-link into the storefront detail route.
   ============================================================ */
import { ref, reactive, computed, watch, nextTick, onMounted, onBeforeUnmount } from 'vue';
import { useI18n } from 'vue-i18n';
import { useShopStore } from '@/stores/shop.js';
import { CATEGORIES, type Product } from '@/data/products';
import AppNav from '@/layout/AppNav.vue';
import AppFooter from '@/layout/AppFooter.vue';
import HeroCollage from '@/components/hero-collage/HeroCollage.vue';
import FeaturedCard from '@/components/FeaturedCard.vue';
import OnboardingGuide from '@/components/OnboardingGuide.vue';
import RotatingWord from '@/components/home/RotatingWord.vue';
import TagMarquee from '@/components/home/TagMarquee.vue';
import CategoryShowcase from '@/components/home/CategoryShowcase.vue';
import TrendingChart from '@/components/home/TrendingChart.vue';
import CreatorSpotlight from '@/components/home/CreatorSpotlight.vue';
import CreatorCtaBand from '@/components/home/CreatorCtaBand.vue';
import { useScrollReveal } from '@/composables/useScrollReveal';

const store = useShopStore();
const { t, rt, tm, locale } = useI18n();

// 後端列表已依上架時間 desc：index 越小越新
const orderMap = computed(() => new Map(store.products.map((p, i) => [p.id, i])));

const cats = CATEGORIES;
const keywords = computed(() => (tm('market.hero.keywords') as string[]).map((k) => rt(k)));
const rotatingWords = computed(() => (tm('market.hero.rotatingWords') as string[]).map((w) => rt(w)));

// 跑馬燈標籤：取自實際商品 tags，依出現頻率排序（點擊即搜尋，保證有結果）
const marqueeTags = computed(() => {
  const freq = new Map<string, number>();
  store.products.forEach((p) => p.tags.forEach((tag) => freq.set(tag, (freq.get(tag) ?? 0) + 1)));
  return [...freq.entries()].sort((a, b) => b[1] - a[1]).map(([tag]) => tag).slice(0, 14);
});

// ----- hero 即時統計（count-up 動畫） -----
const statTargets = computed(() => ({
  works: store.products.length,
  creators: new Set(store.products.map((p) => p.storeSlug)).size,
  sales: store.products.reduce((sum, p) => sum + p.sales, 0),
}));
const shownStats = reactive({ works: 0, creators: 0, sales: 0 });
let statRaf = 0;
watch(statTargets, (target) => {
  cancelAnimationFrame(statRaf);
  if (!target.works || window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
    Object.assign(shownStats, target);
    return;
  }
  const from = { ...shownStats };
  const start = performance.now();
  const dur = 1200;
  const step = (now: number) => {
    const k = Math.min(1, (now - start) / dur);
    const e = 1 - Math.pow(1 - k, 3); // ease-out cubic
    shownStats.works = Math.round(from.works + (target.works - from.works) * e);
    shownStats.creators = Math.round(from.creators + (target.creators - from.creators) * e);
    shownStats.sales = Math.round(from.sales + (target.sales - from.sales) * e);
    if (k < 1) statRaf = requestAnimationFrame(step);
  };
  statRaf = requestAnimationFrame(step);
}, { immediate: true });

// ----- scroll reveal：商品載入後新出現的區塊需重新掃描 -----
const { scan: scanReveal } = useScrollReveal();
watch(() => store.products.length, () => nextTick(scanReveal));

// ----- hero 標題固定單行：超出容器寬度時依比例縮小字級 -----
// 輪播字 slot 已以隱形佔位固定為最寬詞的寬度，量一次即涵蓋所有輪播狀態。
function fitHeroTitle() {
  const el = document.querySelector<HTMLElement>('.mkt-hero-title');
  if (!el) return;
  el.style.fontSize = ''; // 先還原為 CSS clamp 的字級再量測
  const avail = el.clientWidth;
  const need = el.scrollWidth;
  if (need > avail) {
    const cur = parseFloat(getComputedStyle(el).fontSize);
    el.style.fontSize = `${Math.max(16, cur * (avail / need) * 0.97).toFixed(2)}px`;
  }
}
watch(locale, () => nextTick(fitHeroTitle));
const search = ref('');
const category = ref('all');
const sort = ref('popular');
const priceBand = ref('all');
const pageSize = 12;
const visibleCount = ref(12);
const showToTop = ref(false);

const sortOptions = computed(() => [
  { v: 'popular', l: t('market.sort.popular') },
  { v: 'newest', l: t('market.sort.newest') },
  { v: 'rating', l: t('market.sort.rating') },
  { v: 'price-asc', l: t('market.sort.priceAsc') },
  { v: 'price-desc', l: t('market.sort.priceDesc') },
]);
const priceOptions = computed(() => [
  { v: 'all', l: t('market.price.all') },
  { v: 'free', l: t('market.price.free') },
  { v: 'low', l: t('market.price.low') },
  { v: 'mid', l: t('market.price.mid') },
  { v: 'high', l: t('market.price.high') },
]);

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
  let list = store.products.slice();
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
    case 'newest':     list.sort((a, b) => (orderMap.value.get(a.id) ?? 0) - (orderMap.value.get(b.id) ?? 0)); break;
    case 'rating':     list.sort((a, b) => b.rating - a.rating); break;
    case 'price-asc':  list.sort((a, b) => a.price - b.price); break;
    case 'price-desc': list.sort((a, b) => b.price - a.price); break;
    default:           list.sort((a, b) => b.sales - a.sales);
  }
  return list;
});

const activeCatLabel = computed(() => {
  if (category.value === 'all') return t('market.browse.allWorks');
  return CATEGORIES.some((c) => c.id === category.value)
    ? t('category.' + category.value)
    : t('market.browse.allWorks');
});

// active filter chips — surface what's currently narrowing the grid (excludes sort)
const activeFilters = computed(() => {
  const chips: { key: string; label: string; clear: () => void }[] = [];
  const q = search.value.trim();
  if (q) chips.push({ key: 'q', label: `「${q}」`, clear: () => { search.value = ''; } });
  if (category.value !== 'all') chips.push({ key: 'cat', label: activeCatLabel.value, clear: () => { category.value = 'all'; } });
  if (priceBand.value !== 'all') {
    const l = priceOptions.value.find((o) => o.v === priceBand.value)?.l ?? '';
    chips.push({ key: 'price', label: l, clear: () => { priceBand.value = 'all'; } });
  }
  return chips;
});

const visibleResults = computed(() => results.value.slice(0, visibleCount.value));
const hasMore = computed(() => visibleCount.value < results.value.length);

watch(results, () => { visibleCount.value = pageSize; });

// ----- featured carousel (精選作品) -----
// 混合策展：少量「人工精選」(後端 isFeatured) + 其餘以「銷量熱門」自動補滿。
// 人工精選設上限，避免壟斷整個輪播造成不公平；演算法部分讓有銷量的新店也有機會曝光。
const FEATURED_SLOTS = 8;   // 輪播總格數
const MAX_CURATED = 4;      // 人工精選最多佔幾格，其餘保留給熱門
const featured = computed(() => {
  const all = store.products;
  // 人工精選優先，但限量
  const curated = all.filter((p) => p.featured).slice(0, MAX_CURATED);
  const picked = new Set(curated.map((p) => p.id));
  // 其餘格數以銷量由高到低補滿；同銷量維持後端順序（上架時間新→舊），故新品自然靠前
  const hot = all
    .filter((p) => !picked.has(p.id))
    .slice()
    .sort((a, b) => b.sales - a.sales)
    .slice(0, FEATURED_SLOTS - curated.length);
  return [...curated, ...hot];
});
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
const newestIds = computed(() => new Set(store.products.slice(0, 3).map((p) => p.id)));
function badgeFor(p: Product): { label: string; tone: 'hot' | 'new' | 'feat' } | null {
  if (p.featured) return { label: t('market.badge.feat'), tone: 'feat' };
  if (p.sales >= 1500) return { label: t('market.badge.hot'), tone: 'hot' };
  if (newestIds.value.has(p.id)) return { label: t('market.badge.new'), tone: 'new' };
  return null;
}

function catColor(id: string): string {
  const map: Record<string, string> = { music: 'var(--c-violet)', photo: 'var(--c-pink)', ebook: 'var(--c-cyan)' };
  return map[id] ?? '';
}
function catCount(id: string): number { return id === 'all' ? store.products.length : store.products.filter((p) => p.cat === id).length; }
function scrollToBrowse() {
  const el = document.getElementById('browse');
  if (el) window.scrollTo({ top: el.offsetTop - 80, behavior: 'smooth' });
}
function pickKeyword(k: string) { search.value = k; scrollToBrowse(); }
function pickCategory(id: string) { category.value = id; scrollToBrowse(); }
function reset() { category.value = 'all'; sort.value = 'popular'; priceBand.value = 'all'; search.value = ''; }
function onScroll() { showToTop.value = window.scrollY > 300; }
function scrollToTop() { window.scrollTo({ top: 0, behavior: 'smooth' }); }
function loadMore() { visibleCount.value += pageSize; }

function onResize() {
  updateFeatNav();
  fitHeroTitle();
}

onMounted(() => {
  store.loadCatalog();
  window.addEventListener('scroll', onScroll);
  window.addEventListener('resize', onResize);
  updateFeatNav();
  // nextTick：等 RotatingWord 量測完 slot 寬、DOM 更新後再量整行
  nextTick(fitHeroTitle);
  // 顯示字型載入完成後字寬會變，需再量一次（同樣等 slot 寬更新落地）
  document.fonts?.ready.then(() => nextTick(fitHeroTitle));
});
onBeforeUnmount(() => {
  cancelAnimationFrame(statRaf);
  window.removeEventListener('scroll', onScroll);
  window.removeEventListener('resize', onResize);
});
</script>

<template>
  <div class="oj-root" :class="'font-' + store.font" :data-screen-label="t('market.screenLabel')">
    <!-- ============ NAV ============ -->
    <app-nav />

    <main class="page" id="top">
      <!-- ============ HERO ============ -->
      <section class="mkt-hero">
        <hero-collage />
        <div class="mkt-hero-inner">
          <p class="hero-eyebrow"><app-icon name="sparkle" :size="14" /> {{ t('market.hero.eyebrow') }}</p>
          <i18n-t keypath="market.hero.title" tag="h1" class="mkt-hero-title" scope="global">
            <template #collect><span class="hl hl-lime">{{ t('market.hero.collect') }}</span></template>
            <template #rotating><rotating-word :words="rotatingWords" /></template>
          </i18n-t>

          <form class="mkt-search" @submit.prevent="scrollToBrowse">
            <span class="s-ic"><app-icon name="search" :size="22" /></span>
            <input v-model="search" type="text" :placeholder="t('market.hero.searchPlaceholder')" :aria-label="t('market.hero.searchAria')" />
            <button type="submit"><app-icon name="search" :size="17" /> {{ t('common.search') }}</button>
          </form>

          <div class="kw-row">
            <span class="kw-lab">{{ t('market.hero.popularSearch') }}</span>
            <a v-for="k in keywords" :key="k" class="kw" href="#browse" @click.prevent="pickKeyword(k)">{{ k }}</a>
          </div>

          <div v-if="store.products.length" class="hero-stats">
            <span class="hs"><b>{{ shownStats.works.toLocaleString() }}</b>{{ t('market.hero.stats.works') }}</span>
            <span class="hs-dot" aria-hidden="true">·</span>
            <span class="hs"><b>{{ shownStats.creators.toLocaleString() }}</b>{{ t('market.hero.stats.creators') }}</span>
            <template v-if="statTargets.sales > 0">
              <span class="hs-dot" aria-hidden="true">·</span>
              <span class="hs"><b>{{ shownStats.sales.toLocaleString() }}</b>{{ t('market.hero.stats.sales') }}</span>
            </template>
          </div>
        </div>
      </section>

      <!-- ============ TAG MARQUEE ============ -->
      <tag-marquee :items="marqueeTags" @pick="pickKeyword" />

      <!-- ============ CATEGORY SHOWCASE ============ -->
      <category-showcase v-if="store.products.length" :products="store.products" @pick="pickCategory" />

      <!-- ============ FEATURED ============ -->
      <section v-if="featured.length" class="sec featured rv" id="featured">
        <div class="feat-head">
          <div class="feat-head-text">
            <p class="browse-eyebrow"><app-icon name="sparkle" :size="13" /> {{ t('market.featured.eyebrow') }}</p>
            <h2 class="browse-title">{{ t('market.featured.title') }}</h2>
          </div>
          <div class="feat-nav">
            <button type="button" class="feat-arrow prev" :disabled="!canLeft" @click="scrollFeat(-1)" :aria-label="t('market.featured.prevAria')">
              <app-icon name="chevron" :size="20" :stroke="2.4" />
            </button>
            <button type="button" class="feat-arrow" :disabled="!canRight" @click="scrollFeat(1)" :aria-label="t('market.featured.nextAria')">
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

      <!-- ============ PULSE：熱銷排行 + 活躍創作者 ============ -->
      <section v-if="store.products.length" class="sec pulse rv">
        <div class="pulse-grid">
          <trending-chart :products="store.products" />
          <creator-spotlight :products="store.products" />
        </div>
      </section>

      <!-- ============ BROWSE ============ -->
      <section class="sec browse" id="browse">
        <div class="browse-head">
          <p class="browse-eyebrow"><app-icon name="sparkle" :size="13" /> {{ t('market.browse.eyebrow') }}</p>
          <h2 class="browse-title">{{ t('market.browse.title') }}</h2>
          <p class="browse-sub">{{ t('market.browse.sub') }}</p>
        </div>

        <!-- toolbar: result count + sort -->
        <div class="browse-toolbar">
          <i18n-t keypath="market.browse.count" tag="span" class="browse-count" scope="global">
            <template #cat><b>{{ activeCatLabel }}</b></template>
            <template #count><b>{{ results.length }}</b></template>
          </i18n-t>
          <div class="sort-tabs">
            <span class="sort-lab">{{ t('market.browse.sortLabel') }}</span>
            <button type="button" v-for="o in sortOptions" :key="o.v" class="sort-tab" :class="{ on: sort === o.v }" :aria-pressed="sort === o.v" @click="sort = o.v">{{ o.l }}</button>
            <select class="m-select sort-select" v-model="sort" :aria-label="t('market.browse.sortAria')">
              <option v-for="o in sortOptions" :key="o.v" :value="o.v">{{ o.l }}</option>
            </select>
          </div>
        </div>

        <div class="browse-body">
          <!-- filter sidebar (Gumroad-style left rail) -->
          <aside class="browse-side">
            <div class="side-card">
              <p class="side-title"><app-icon name="sparkle" :size="15" /> {{ t('market.browse.filter') }}</p>

              <div class="side-group">
                <p class="side-label">{{ t('market.browse.categoryGroup') }}</p>
                <div class="side-opts">
                  <button type="button" class="side-opt" :class="{ on: category === 'all' }" @click="category = 'all'">
                    <span class="so-dot" style="background: var(--c-violet)"></span>
                    <span class="so-name">{{ t('market.browse.allWorks') }}</span>
                    <span class="so-count">{{ catCount('all') }}</span>
                  </button>
                  <button type="button" v-for="c in cats" :key="c.id" class="side-opt" :class="{ on: category === c.id }" @click="category = c.id">
                    <span class="so-dot" :style="{ background: catColor(c.id) }"></span>
                    <span class="so-name">{{ t('category.' + c.id) }}</span>
                    <span class="so-count">{{ catCount(c.id) }}</span>
                  </button>
                </div>
                <select class="m-select side-select" v-model="category" :aria-label="t('market.browse.categoryAria')">
                  <option value="all">{{ t('market.browse.optionCount', { label: t('market.browse.allWorks'), count: catCount('all') }) }}</option>
                  <option v-for="c in cats" :key="c.id" :value="c.id">{{ t('market.browse.optionCount', { label: t('category.' + c.id), count: catCount(c.id) }) }}</option>
                </select>
              </div>

              <div class="side-group">
                <p class="side-label">{{ t('market.browse.priceGroup') }}</p>
                <div class="side-opts">
                  <button type="button" v-for="o in priceOptions" :key="o.v" class="side-opt" :class="{ on: priceBand === o.v }" @click="priceBand = o.v">
                    <span class="so-name">{{ o.l }}</span>
                  </button>
                </div>
                <select class="m-select side-select" v-model="priceBand" :aria-label="t('market.browse.priceAria')">
                  <option v-for="o in priceOptions" :key="o.v" :value="o.v">{{ o.l }}</option>
                </select>
              </div>

              <button v-if="activeFilters.length" type="button" class="side-reset" @click="reset">{{ t('market.browse.clearAll') }}</button>
            </div>
          </aside>

          <!-- results -->
          <div class="browse-main">
            <div v-if="activeFilters.length" class="active-chips">
              <span class="active-chips-lab">{{ t('market.browse.filtering') }}</span>
              <button v-for="f in activeFilters" :key="f.key" type="button" class="fchip" @click="f.clear()">
                {{ f.label }}
                <span class="fchip-x"><app-icon name="close" :size="13" :stroke="2.4" /></span>
              </button>
              <button type="button" class="fchip-clear" @click="reset">{{ t('market.browse.clearAllShort') }}</button>
            </div>

            <div v-if="results.length" class="grid">
              <product-card v-for="p in visibleResults" :key="p.id" :product="p" :badge="badgeFor(p)" />
            </div>
            <div v-if="hasMore" class="load-more-wrap">
              <button class="load-more-btn" @click="loadMore">
                {{ t('market.browse.loadMore') }}
                <span class="load-more-count">{{ t('market.browse.loadMoreCount', { count: results.length - visibleCount }) }}</span>
              </button>
            </div>
            <div v-if="!results.length" class="empty">
              <app-icon name="search" :size="40" style="margin-bottom: 14px; opacity: 0.5" />
              <p style="font-size: 17px; font-weight: 600; color: var(--text-soft)">{{ t('market.browse.emptyTitle') }}</p>
              <p style="margin-top: 6px">{{ t('market.browse.emptyDesc') }}</p>
              <span class="kw" style="display: inline-block; margin-top: 18px; cursor: pointer" @click="reset">{{ t('market.browse.clearAll') }}</span>
            </div>
          </div>
        </div>
      </section>

      <!-- ============ CREATOR CTA ============ -->
      <creator-cta-band />

      <!-- ============ FOOTER ============ -->
      <app-footer />
    </main>

    <Transition name="to-top">
      <button v-if="showToTop" class="to-top-btn" @click="scrollToTop" :aria-label="t('market.toTop')">
        <app-icon name="chevronU" :size="22" />
      </button>
    </Transition>

    <onboarding-guide />
  </div>
</template>
