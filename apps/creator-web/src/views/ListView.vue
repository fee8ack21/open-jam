<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useShopStore } from '@/stores/shop';
import { CATEGORIES, TAGS, type Product } from '@/data/products';
import ProductCard from '@/components/ProductCard.vue';
import FeaturedCard from '@/components/FeaturedCard.vue';
import JamSelect from '@/components/JamSelect.vue';
import AppIcon from '@/components/app-icon';

const store = useShopStore();
const s = store;
const { t } = useI18n();

onMounted(() => {
  store.loadCatalog();
  window.addEventListener('resize', updateFeatNav);
});
onBeforeUnmount(() => window.removeEventListener('resize', updateFeatNav));

// ----- hero: creator profile（設計稿 Shop profile） -----
// 店家於後台上傳的橫幅：有上傳時渲染為封面圖（頭像疊在下緣）；
// 未上傳則以平台預設標語封面補位（靜態，參考 portal-web 首頁 hero）
const bannerUrl = computed(() => store.storefront.bannerUrl);
const heroDesc = computed(() => store.storefront.description || t('list.heroSub'));
const avatarInitial = computed(() => (store.storefront.storeName || 'O').trim().charAt(0));
const workCount = computed(() => store.products.length);
// 全店平均評分：以各商品評分數加權；尚無評分時顯示「—」
const avgRating = computed(() => {
  const rated = store.products.filter((p) => p.ratingCount > 0);
  const n = rated.reduce((sum, p) => sum + p.ratingCount, 0);
  if (!n) return null;
  return (rated.reduce((sum, p) => sum + p.rating * p.ratingCount, 0) / n).toFixed(1);
});

// ----- 店長精選 carousel（樣式與互動同 portal-web 精選作品） -----
// 店長精選（依 featuredOrder 排序）優先，剩餘格數以銷量熱門補滿：
// 補入的卡片標「熱門」誠實區隔，不讓熱門商品冒充店長精選。
const FEATURED_SLOTS = 8;   // 輪播總格數
const featured = computed<Product[]>(() => {
  const all = store.products;
  const curated = all
    .filter((p) => p.featured)
    .sort((a, b) => (a.featuredOrder ?? 0) - (b.featuredOrder ?? 0))
    .slice(0, FEATURED_SLOTS);
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
// 商品載入後 track 才渲染，等 DOM 更新再量箭頭可用狀態
watch(featured, () => nextTick(updateFeatNav));

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

type SortKey = 'popular' | 'newest' | 'rating' | 'price-asc' | 'price-desc';
const sortOptions = computed<{ label: string; value: SortKey }[]>(() => [
  { label: t('list.sort.popular'), value: 'popular' },
  { label: t('list.sort.newest'), value: 'newest' },
  { label: t('list.sort.rating'), value: 'rating' },
  { label: t('list.sort.priceAsc'), value: 'price-asc' },
  { label: t('list.sort.priceDesc'), value: 'price-desc' },
]);

const cats = CATEGORIES;
const results = computed(() => store.filtered);
const availableTags = computed(() => {
  if (store.category !== 'all') return TAGS[store.category];
  return [...new Set(Object.values(TAGS).flat())];
});
const priceRange = computed<[number, number]>({
  get() { return store.priceRange; },
  set(v: number | [number, number]) { store.priceRange = (Array.isArray(v) ? v : [v, v]) as [number, number]; },
});

const setCat = (c: string) => store.setCategory(c);
const catColor = (id: string) => ({ music: 'var(--c-violet)', photo: 'var(--c-pink)', ebook: 'var(--c-cyan)' } as Record<string, string>)[id];
const catLabel = (id: string) => (CATEGORIES.some((c) => c.id === id) ? t('category.' + id) : id);
const toggleTag = (tag: string) => store.toggleTag(tag);
const clear = () => store.clearFilters();
const priceLabel = (v: number) => (v === 0 ? t('common.free') : '$' + v);

// ----- grid badges (熱賣 / 新上架) — adds rhythm to the uniform grid -----
// 後端列表已依上架時間 desc，取前 3 筆視為「新上架」
const newestIds = computed(() => new Set(store.products.slice(0, 3).map((p) => p.id)));
function badgeFor(p: Product): { label: string; tone: 'hot' | 'new' | 'feat' } | null {
  if (p.sales >= 1500) return { label: t('list.badge.hot'), tone: 'hot' };
  if (newestIds.value.has(p.id)) return { label: t('list.badge.new'), tone: 'new' };
  return null;
}

// active filter chips — surface everything currently narrowing the grid (excludes sort)
const activeChips = computed(() => {
  const chips: { key: string; label: string; clear: () => void }[] = [];
  const q = s.search.trim();
  if (q) chips.push({ key: 'q', label: `「${q}」`, clear: () => { s.search = ''; } });
  if (s.category !== 'all') chips.push({ key: 'cat', label: catLabel(s.category), clear: () => setCat('all') });
  for (const tag of s.activeTags) chips.push({ key: 'tag-' + tag, label: tag, clear: () => toggleTag(tag) });
  if (s.onlyFree) chips.push({ key: 'free', label: t('common.free'), clear: () => { s.onlyFree = false; } });
  if (s.priceRange[0] !== 0 || s.priceRange[1] !== 40) {
    const hi = s.priceRange[1] >= 40 ? '$40+' : '$' + s.priceRange[1];
    chips.push({ key: 'price', label: `${priceLabel(s.priceRange[0])} – ${hi}`, clear: () => { priceRange.value = [0, 40]; } });
  }
  return chips;
});
</script>

<template>
  <div class="page page-pad" :data-screen-label="t('list.screenLabel')">
    <!-- 創作者 Hero：主角是創作者，不是平台 -->
    <section class="hero">
      <!-- 店家自訂橫幅：後台有上傳顯示封面圖；未上傳退回平台預設標語封面（同 portal-web 首頁 hero 語彙） -->
      <div class="hero-cover" :class="{ 'is-default': !bannerUrl }"
           :style="bannerUrl ? { backgroundImage: `url(${bannerUrl})` } : undefined"
           :role="bannerUrl ? 'img' : undefined"
           :aria-label="bannerUrl ? t('list.bannerAlt', { store: store.storefront.storeName }) : undefined">
        <div v-if="!bannerUrl" class="hero-cover-copy">
          <p class="hero-cover-eyebrow"><app-icon name="sparkle" :size="13" /> {{ t('list.bannerEyebrow') }}</p>
          <i18n-t keypath="list.bannerTitle" tag="p" class="hero-cover-title" scope="global">
            <template #collect><span class="hl hl-lime">{{ t('list.bannerCollect') }}</span></template>
            <template #works><span class="hl hl-yellow">{{ t('list.bannerWorks') }}</span></template>
          </i18n-t>
        </div>
      </div>

      <div class="hero-band">
        <span class="hero-avatar">
          <img v-if="store.storefront.avatarUrl" :src="store.storefront.avatarUrl" :alt="store.storefront.storeName" />
          <template v-else>{{ avatarInitial }}</template>
        </span>

        <div class="hero-main">
          <h1 class="hero-title"><span class="hl hl-lime">{{ store.storefront.storeName }}</span></h1>
          <p class="hero-sub">{{ heroDesc }}</p>
          <div class="hero-stats">
            <span class="hero-stat"><b>{{ workCount }}</b> {{ t('list.statWorks') }}</span>
            <span class="hero-stat-sep"></span>
            <span class="hero-stat"><app-icon name="star" :size="14" class="stat-star" /><b>{{ avgRating ?? '—' }}</b> {{ t('list.statRating') }}</span>
          </div>
        </div>
      </div>
    </section>

    <!-- 店長精選 carousel（白色滿版帶，樣式同 portal-web 精選作品） -->
    <section v-if="featured.length" class="featured">
      <div class="feat-head">
        <div class="feat-head-text">
          <p class="browse-eyebrow"><app-icon name="sparkle" :size="12" /> {{ t('featured.eyebrow') }}</p>
          <h2 class="browse-title">{{ t('featured.title') }} <span class="hand-note hand-pink">{{ t('featured.note') }}</span></h2>
        </div>
        <div class="feat-nav">
          <button type="button" class="feat-arrow prev" :disabled="!canLeft" @click="scrollFeat(-1)" :aria-label="t('featured.prevAria')">
            <app-icon name="arrowL" :size="20" />
          </button>
          <button type="button" class="feat-arrow" :disabled="!canRight" @click="scrollFeat(1)" :aria-label="t('featured.nextAria')">
            <app-icon name="arrow" :size="20" />
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

    <section class="browse">
      <div class="hero-cats">
        <span class="cat-pill c-all" :class="{ on: s.category === 'all' }" @click="setCat('all')">
          <span class="dot" style="background:var(--text)"></span>{{ t('list.allWorks') }}
        </span>
        <span v-for="c in cats" :key="c.id" class="cat-pill" :class="['c-' + c.id, { on: s.category === c.id }]" @click="setCat(c.id)">
          <span class="dot" :style="{ background: catColor(c.id) }"></span>{{ t('category.' + c.id) }}
        </span>
      </div>

      <!-- toolbar: result count + sort pills -->
      <div class="browse-toolbar">
        <i18n-t keypath="list.count" tag="span" class="browse-count" scope="global">
          <template #count><b>{{ results.length }}</b></template>
        </i18n-t>
        <div class="sort-tabs">
          <span class="sort-lab">{{ t('list.sortLabel') }}</span>
          <button type="button" v-for="o in sortOptions" :key="o.value" class="sort-tab"
                  :class="{ on: s.sort === o.value }" :aria-pressed="s.sort === o.value" @click="s.sort = o.value">
            {{ o.label }}
          </button>
          <jam-select class="sort-select" v-model="s.sort" :options="sortOptions"
                      :aria-label="t('list.sortAria')" />
        </div>
      </div>

      <div class="browse-body">
        <!-- filter rail -->
        <aside class="browse-side">
          <div class="side-card">
            <p class="side-title"><app-icon name="funnel" :size="15" /> {{ t('list.filter') }}</p>

            <div class="side-group">
              <p class="side-label">{{ t('list.tagsLabel') }}</p>
              <div class="tag-wrap">
                <span v-for="tag in availableTags" :key="tag" class="tag-toggle"
                      :class="{ on: s.activeTags.includes(tag) }" @click="toggleTag(tag)">
                  {{ tag }}
                </span>
              </div>
            </div>

            <div class="side-group">
              <p class="side-label">{{ t('list.priceRangeLabel') }}</p>
              <n-slider v-model:value="priceRange" range :min="0" :max="40" :step="1"
                        :format-tooltip="priceLabel" :marks="{ 0: t('common.free'), 40: t('list.priceMax') }" />
              <div class="side-price-vals">
                <span>{{ priceLabel(priceRange[0]) }}</span>
                <span>{{ priceRange[1] >= 40 ? t('list.priceMax') : '$' + priceRange[1] }}</span>
              </div>
            </div>

            <div class="side-group">
              <div class="side-row">
                <span>{{ t('list.onlyFree') }}</span>
                <n-switch v-model:value="s.onlyFree" size="small" />
              </div>
            </div>

            <button v-if="activeChips.length" type="button" class="side-reset" @click="clear">{{ t('list.clearAll') }}</button>
          </div>
        </aside>

        <!-- results -->
        <div class="browse-main">
          <div v-if="activeChips.length" class="active-chips">
            <span class="active-chips-lab">{{ t('list.filtering') }}</span>
            <button v-for="f in activeChips" :key="f.key" type="button" class="fchip" @click="f.clear()">
              {{ f.label }}
              <span class="fchip-x"><app-icon name="close" :size="13" /></span>
            </button>
            <button type="button" class="fchip-clear" @click="clear">{{ t('list.clearAllShort') }}</button>
          </div>

          <div v-if="results.length" class="grid">
            <product-card v-for="p in results" :key="p.id" :product="p" :badge="badgeFor(p)" />
          </div>
          <div v-else class="no-results">
            <app-icon name="search" :size="36" style="margin:0 auto;" />
            <p class="no-results-title">{{ t('list.emptyTitle') }}</p>
            <p class="no-results-desc">{{ t('list.emptyDesc') }}</p>
            <button type="button" class="cta-line" style="margin-top:16px" @click="clear">{{ t('list.emptyClear') }}</button>
          </div>
        </div>
      </div>
    </section>
  </div>
</template>

<style scoped>
/* 無符合結果卡（設計稿 no results） */
.no-results {
  border: var(--bw) solid var(--border-strong); border-radius: var(--r-lg); background: var(--surface);
  padding: 48px; text-align: center; box-shadow: var(--pop-2);
}
.no-results-title { font-weight: 900; font-size: 18px; margin: 10px 0 0; }
.no-results-desc { font-weight: 500; font-size: 14px; margin: 6px 0 0; color: var(--text-soft); }
</style>
