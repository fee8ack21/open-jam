<script setup lang="ts">
import { computed } from 'vue';
import { useShopStore } from '@/stores/shop';
import { CATEGORIES, TAGS, PRODUCTS, type Product } from '@/data/products';
import ProductCard from '@/components/ProductCard.vue';
import AppIcon from '@/components/app-icon';

const store = useShopStore();
const s = store;

type SortKey = 'popular' | 'newest' | 'rating' | 'price-asc' | 'price-desc';
const sortOptions: { label: string; value: SortKey }[] = [
  { label: '最熱門', value: 'popular' },
  { label: '最新上架', value: 'newest' },
  { label: '評分最高', value: 'rating' },
  { label: '價格低 → 高', value: 'price-asc' },
  { label: '價格高 → 低', value: 'price-desc' },
];

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
const catLabel = (id: string) => CATEGORIES.find((c) => c.id === id)?.label ?? id;
const toggleTag = (t: string) => store.toggleTag(t);
const clear = () => store.clearFilters();
const priceLabel = (v: number) => (v === 0 ? '免費' : '$' + v);

// ----- grid badges (熱賣 / 新上架) — adds rhythm to the uniform grid -----
const orderMap = new Map(PRODUCTS.map((p, i) => [p.id, i])); // catalogue order → newest = larger index
const newestIds = new Set(
  PRODUCTS.slice()
    .sort((a, b) => (orderMap.get(b.id) ?? 0) - (orderMap.get(a.id) ?? 0))
    .slice(0, 3)
    .map((p) => p.id),
);
function badgeFor(p: Product): { label: string; tone: 'hot' | 'new' | 'feat' } | null {
  if (p.sales >= 1500) return { label: '熱賣', tone: 'hot' };
  if (newestIds.has(p.id)) return { label: '新上架', tone: 'new' };
  return null;
}

// active filter chips — surface everything currently narrowing the grid (excludes sort)
const activeChips = computed(() => {
  const chips: { key: string; label: string; clear: () => void }[] = [];
  const q = s.search.trim();
  if (q) chips.push({ key: 'q', label: `「${q}」`, clear: () => { s.search = ''; } });
  if (s.category !== 'all') chips.push({ key: 'cat', label: catLabel(s.category), clear: () => setCat('all') });
  for (const t of s.activeTags) chips.push({ key: 'tag-' + t, label: t, clear: () => toggleTag(t) });
  if (s.onlyFree) chips.push({ key: 'free', label: '免費', clear: () => { s.onlyFree = false; } });
  if (s.priceRange[0] !== 0 || s.priceRange[1] !== 40) {
    const hi = s.priceRange[1] >= 40 ? '$40+' : '$' + s.priceRange[1];
    chips.push({ key: 'price', label: `${priceLabel(s.priceRange[0])} – ${hi}`, clear: () => { priceRange.value = [0, 40]; } });
  }
  return chips;
});
</script>

<template>
  <div class="page page-pad" data-screen-label="產品列表頁">
    <section class="hero">
      <div class="hero-shapes">
        <span class="shape s1"></span>
        <span class="shape s2"></span>
        <span class="shape s3"></span>
      </div>
      <p class="hero-eyebrow"><app-icon name="sparkle" :size="14" /> OPEN JAM · 創作者數位市集</p>
      <h1 class="hero-title">把你的<span class="hl hl-lime">創造力</span><br>變成可以<span class="hl hl-pink">販售</span>的作品</h1>
      <p class="hero-sub">樂譜、攝影集、電子書 — 直接向創作者購買，付款後立即下載。</p>
      <div class="hero-cats">
        <span class="cat-pill c-all" :class="{ on: s.category === 'all' }" @click="setCat('all')">
          <span class="dot" style="background:var(--c-violet)"></span>全部作品
        </span>
        <span v-for="c in cats" :key="c.id" class="cat-pill" :class="['c-' + c.id, { on: s.category === c.id }]" @click="setCat(c.id)">
          <span class="dot" :style="{ background: catColor(c.id) }"></span>{{ c.label }}
        </span>
      </div>
    </section>

    <section class="browse">
      <!-- toolbar: result count + sort tabs -->
      <div class="browse-toolbar">
        <span class="browse-count">共 <b>{{ results.length }}</b> 件作品</span>
        <div class="sort-tabs">
          <span class="sort-lab">排序</span>
          <button type="button" v-for="o in sortOptions" :key="o.value" class="sort-tab"
                  :class="{ on: s.sort === o.value }" :aria-pressed="s.sort === o.value" @click="s.sort = o.value">
            {{ o.label }}
          </button>
          <select class="sort-select" v-model="s.sort" aria-label="排序方式">
            <option v-for="o in sortOptions" :key="o.value" :value="o.value">{{ o.label }}</option>
          </select>
        </div>
      </div>

      <div class="browse-body">
        <!-- filter rail -->
        <aside class="browse-side">
          <div class="side-card">
            <p class="side-title"><app-icon name="sparkle" :size="15" /> 篩選</p>

            <div class="side-group">
              <p class="side-label">標籤</p>
              <div class="tag-wrap">
                <span v-for="t in availableTags" :key="t" class="tag-toggle"
                      :class="{ on: s.activeTags.includes(t) }" @click="toggleTag(t)">
                  {{ t }}
                </span>
              </div>
            </div>

            <div class="side-group">
              <p class="side-label">價格區間</p>
              <n-slider v-model:value="priceRange" range :min="0" :max="40" :step="1"
                        :format-tooltip="priceLabel" :marks="{ 0: '免費', 40: '$40+' }" />
              <div class="side-price-vals">
                <span>{{ priceLabel(priceRange[0]) }}</span>
                <span>{{ priceRange[1] >= 40 ? '$40+' : '$' + priceRange[1] }}</span>
              </div>
            </div>

            <div class="side-group">
              <div class="side-row">
                <span>只看免費作品</span>
                <n-switch v-model:value="s.onlyFree" size="small" />
              </div>
            </div>

            <button v-if="activeChips.length" type="button" class="side-reset" @click="clear">清除全部篩選</button>
          </div>
        </aside>

        <!-- results -->
        <div class="browse-main">
          <div v-if="activeChips.length" class="active-chips">
            <span class="active-chips-lab">篩選中</span>
            <button v-for="f in activeChips" :key="f.key" type="button" class="fchip" @click="f.clear()">
              {{ f.label }}
              <span class="fchip-x"><app-icon name="close" :size="13" :stroke="2.4" /></span>
            </button>
            <button type="button" class="fchip-clear" @click="clear">清除全部</button>
          </div>

          <div v-if="results.length" class="grid">
            <product-card v-for="p in results" :key="p.id" :product="p" :badge="badgeFor(p)" />
          </div>
          <div v-else class="empty">
            <app-icon name="search" :size="40" style="margin-bottom:14px; opacity:.5;" />
            <p style="font-size:17px; font-weight:600; color:var(--text-soft);">找不到符合的作品</p>
            <p style="margin-top:6px;">試著放寬篩選條件或清除搜尋關鍵字。</p>
            <n-button style="margin-top:16px" @click="clear">清除篩選</n-button>
          </div>
        </div>
      </div>
    </section>
  </div>
</template>
