<script setup lang="ts">
import { computed, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useShopStore } from '@/stores/shop';
import { CATEGORIES, TAGS, type Product } from '@/data/products';
import ProductCard from '@/components/ProductCard.vue';
import AppIcon from '@/components/app-icon';

const store = useShopStore();
const s = store;
const { t } = useI18n();

onMounted(store.loadCatalog);

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
    <section class="hero">
      <div class="hero-shapes">
        <span class="shape s1"></span>
        <span class="shape s2"></span>
        <span class="shape s3"></span>
      </div>
      <p class="hero-eyebrow"><app-icon name="sparkle" :size="14" /> OPEN JAM · {{ store.storefront.storeName }}</p>
      <i18n-t keypath="list.heroTitle" tag="h1" class="hero-title" scope="global">
        <template #creativity><span class="hl hl-lime">{{ t('list.heroCreativity') }}</span></template>
        <template #sell><span class="hl hl-pink">{{ t('list.heroSell') }}</span></template>
      </i18n-t>
      <p class="hero-sub">{{ t('list.heroSub') }}</p>
      <div class="hero-cats">
        <span class="cat-pill c-all" :class="{ on: s.category === 'all' }" @click="setCat('all')">
          <span class="dot" style="background:var(--c-violet)"></span>{{ t('list.allWorks') }}
        </span>
        <span v-for="c in cats" :key="c.id" class="cat-pill" :class="['c-' + c.id, { on: s.category === c.id }]" @click="setCat(c.id)">
          <span class="dot" :style="{ background: catColor(c.id) }"></span>{{ t('category.' + c.id) }}
        </span>
      </div>
    </section>

    <section class="browse">
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
