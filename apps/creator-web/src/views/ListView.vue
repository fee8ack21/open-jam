<script setup lang="ts">
import { computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { useShopStore } from '@/stores/shop';
import { CATEGORIES, TAGS, type Product } from '@/data/products';
import ProductCard from '@/components/ProductCard.vue';
import ProductThumb from '@/components/ProductThumb.vue';
import RotatingWord from '@/components/RotatingWord.vue';
import AppIcon from '@/components/app-icon';

const store = useShopStore();
const s = store;
const router = useRouter();
const { t, tm, rt } = useI18n();

// fallback banner 標題的輪播關鍵詞（參考 portal-web hero）
const rotatingWords = computed(() => (tm('list.bannerRotating') as string[]).map((w) => rt(w)));

onMounted(store.loadCatalog);

// ----- hero: creator profile stats -----
// 店家於後台設定的橫幅：有設定時渲染為封面條（頭像疊在下緣，與設定頁預覽一致）
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

// ----- 店長精選 spotlight：店長精選（依 featuredOrder 排序）優先，否則以最熱賣作品補位 -----
const spotlight = computed<Product | null>(() => {
  const list = store.products;
  if (!list.length) return null;
  const featured = list
    .filter((p) => p.featured)
    .sort((a, b) => (a.featuredOrder ?? 0) - (b.featuredOrder ?? 0));
  return featured[0] ?? [...list].sort((a, b) => b.sales - a.sales)[0];
});
const goSpotlight = () => {
  if (spotlight.value) router.push({ name: 'product', params: { id: spotlight.value.id } });
};

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
      <!-- 橫幅封面：有設定→顯示圖片；未設定→品牌漸層 + 手繪幾何 fallback。頭像一律疊在下緣 -->
      <div class="hero-cover" :class="{ empty: !bannerUrl }"
           :style="bannerUrl ? { backgroundImage: `url(${bannerUrl})` } : {}"
           :role="bannerUrl ? 'img' : undefined"
           :aria-label="bannerUrl ? t('list.bannerAlt', { store: store.storefront.storeName }) : undefined">
        <div v-if="!bannerUrl" class="hero-cover-copy">
          <p class="hero-cover-eyebrow"><app-icon name="sparkle" :size="14" /> {{ t('list.bannerEyebrow') }}</p>
          <i18n-t keypath="list.bannerTitle" tag="p" class="hero-cover-title" scope="global">
            <template #collect><span class="hl hl-lime">{{ t('list.bannerCollect') }}</span></template>
            <template #rotating><rotating-word :words="rotatingWords" /></template>
          </i18n-t>
        </div>
      </div>

      <div class="hero-band">
        <span class="hero-avatar">
          <img v-if="store.storefront.avatarUrl" :src="store.storefront.avatarUrl" :alt="store.storefront.storeName" />
          <template v-else>{{ avatarInitial }}</template>
        </span>

        <div class="hero-main">
          <h1 class="hero-title">{{ store.storefront.storeName }}</h1>
          <p class="hero-sub">{{ heroDesc }}</p>
          <div class="hero-stats">
            <span class="hero-stat"><b>{{ workCount }}</b> {{ t('list.statWorks') }}</span>
            <span class="hero-stat-sep"></span>
            <span class="hero-stat"><app-icon name="star" :size="16" :stroke="2.2" class="stat-star" /><b>{{ avgRating ?? '—' }}</b> {{ t('list.statRating') }}</span>
            <span class="hero-stat-sep"></span>
            <span class="hero-stat"><b>{{ store.followerCount.toLocaleString() }}</b> {{ t('list.statFollowers') }}</span>
          </div>
        </div>
      </div>
    </section>

    <!-- 店長精選 spotlight -->
    <section v-if="spotlight" class="spotlight">
      <div class="spot-body">
        <span class="spot-badge"><app-icon name="sparkle" :size="13" /> {{ t('spotlight.badge') }}</span>
        <h2 class="spot-title">{{ spotlight.title }}</h2>
        <p v-if="spotlight.blurb" class="spot-blurb">{{ spotlight.blurb }}</p>
        <div class="spot-meta">
          <span class="spot-chip">{{ t('spotlight.sold', { count: spotlight.sales.toLocaleString() }) }}</span>
          <span v-if="spotlight.ratingCount" class="spot-chip">★ {{ spotlight.rating.toFixed(1) }}（{{ spotlight.ratingCount }}）</span>
          <span class="spot-chip">{{ catLabel(spotlight.cat) }}</span>
        </div>
        <div class="spot-actions">
          <button type="button" class="spot-cta" @click="goSpotlight">{{ t('spotlight.cta') }}</button>
          <span class="spot-price" :class="{ free: spotlight.price === 0 }">
            {{ spotlight.price === 0 ? t('common.free') : '$' + spotlight.price }}
          </span>
        </div>
      </div>
      <div class="spot-cover" @click="goSpotlight">
        <product-thumb :product="spotlight" hide-label :glyph-size="72" />
      </div>
    </section>

    <section class="browse">
      <div class="hero-cats">
        <span class="cat-pill c-all" :class="{ on: s.category === 'all' }" @click="setCat('all')">
          <span class="dot" style="background:var(--c-violet)"></span>{{ t('list.allWorks') }}
        </span>
        <span v-for="c in cats" :key="c.id" class="cat-pill" :class="['c-' + c.id, { on: s.category === c.id }]" @click="setCat(c.id)">
          <span class="dot" :style="{ background: catColor(c.id) }"></span>{{ t('category.' + c.id) }}
        </span>
      </div>

      <!-- toolbar: result count + sort tabs -->
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
          <select class="sort-select" v-model="s.sort" :aria-label="t('list.sortAria')">
            <option v-for="o in sortOptions" :key="o.value" :value="o.value">{{ o.label }}</option>
          </select>
        </div>
      </div>

      <div class="browse-body">
        <!-- filter rail -->
        <aside class="browse-side">
          <div class="side-card">
            <p class="side-title"><app-icon name="sparkle" :size="15" /> {{ t('list.filter') }}</p>

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
              <span class="fchip-x"><app-icon name="close" :size="13" :stroke="2.4" /></span>
            </button>
            <button type="button" class="fchip-clear" @click="clear">{{ t('list.clearAllShort') }}</button>
          </div>

          <div v-if="results.length" class="grid">
            <product-card v-for="p in results" :key="p.id" :product="p" :badge="badgeFor(p)" />
          </div>
          <div v-else class="empty">
            <app-icon name="search" :size="40" style="margin-bottom:14px; opacity:.5;" />
            <p style="font-size:17px; font-weight:600; color:var(--text-soft);">{{ t('list.emptyTitle') }}</p>
            <p style="margin-top:6px;">{{ t('list.emptyDesc') }}</p>
            <n-button style="margin-top:16px" @click="clear">{{ t('list.emptyClear') }}</n-button>
          </div>
        </div>
      </div>
    </section>
  </div>
</template>
