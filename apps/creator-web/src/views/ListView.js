/* ============================================================
   Open Jam — ListView (route: /)
   sort · tag filter · price range · favorites · card grid
   ============================================================ */
import { useShopStore } from '../stores/shop.js';
import { CATEGORIES, TAGS, PRODUCTS } from '../data/products.js';
import ProductCard from '../components/ProductCard.js';
import JIcon from '../components/JIcon.js';

export default {
  name: 'ListView',
  components: { ProductCard, JIcon },
  setup() { return { store: useShopStore() }; },
  data() {
    return {
      sortOptions: [
        { label: '最熱門', value: 'popular' },
        { label: '最新上架', value: 'newest' },
        { label: '評分最高', value: 'rating' },
        { label: '價格：低 → 高', value: 'price-asc' },
        { label: '價格：高 → 低', value: 'price-desc' },
      ],
    };
  },
  computed: {
    s() { return this.store; },
    cats() { return CATEGORIES; },
    results() { return this.store.filtered; },
    total() { return PRODUCTS.length; },
    availableTags() {
      if (this.store.category !== 'all') return TAGS[this.store.category];
      return [...new Set(Object.values(TAGS).flat())];
    },
    filterCount() { return this.store.activeFilterCount; },
    priceRange: {
      get() { return this.store.priceRange; },
      set(v) { this.store.priceRange = v; },
    },
  },
  methods: {
    setCat(c) { this.store.setCategory(c); },
    catColor(id) { return ({ music: 'var(--c-violet)', photo: 'var(--c-pink)', ebook: 'var(--c-cyan)' })[id]; },
    toggleTag(t) { this.store.toggleTag(t); },
    clear() { this.store.clearFilters(); },
    priceLabel(v) { return v === 0 ? '免費' : '$' + v; },
  },
  template: `
    <div class="page page-pad" data-screen-label="產品列表頁">
      <section class="hero">
        <div class="hero-shapes">
          <span class="shape s1"></span>
          <span class="shape s2"></span>
          <span class="shape s3"></span>
        </div>
        <p class="hero-eyebrow"><j-icon name="sparkle" :size="14" /> OPEN JAM · 創作者數位市集</p>
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

      <div class="list-layout">
        <!-- ---------- filters ---------- -->
        <aside class="filters">
          <div class="filter-block">
            <p class="filter-title">分類</p>
            <div class="tag-wrap" style="flex-direction:column; gap:6px; align-items:stretch;">
              <div class="tag-toggle" style="justify-content:space-between;"
                   :class="{ on: s.category === 'all' }" @click="setCat('all')">
                <span>全部作品</span>
                <span style="opacity:.7; font-size:12px;">{{ total }}</span>
              </div>
              <div v-for="c in cats" :key="c.id" class="tag-toggle" style="justify-content:space-between;"
                   :class="{ on: s.category === c.id }" @click="setCat(c.id)">
                <span>{{ c.label }}</span>
              </div>
            </div>
          </div>

          <div class="filter-block">
            <p class="filter-title">標籤</p>
            <div class="tag-wrap">
              <span v-for="t in availableTags" :key="t" class="tag-toggle"
                    :class="{ on: s.activeTags.includes(t) }" @click="toggleTag(t)">
                {{ t }}
              </span>
            </div>
          </div>

          <div class="filter-block">
            <p class="filter-title">價格區間</p>
            <n-slider v-model:value="priceRange" range :min="0" :max="40" :step="1"
                      :format-tooltip="priceLabel" :marks="{0:'免費',40:'$40+'}" />
            <div style="display:flex; justify-content:space-between; margin-top:18px; font-size:13px; color:var(--text-soft);">
              <span>{{ priceLabel(priceRange[0]) }}</span>
              <span>{{ priceRange[1] >= 40 ? '$40+' : '$' + priceRange[1] }}</span>
            </div>
          </div>

          <div class="filter-block">
            <div style="display:flex; align-items:center; justify-content:space-between;">
              <span style="font-size:14px; color:var(--text-soft);">只看免費作品</span>
              <n-switch v-model:value="s.onlyFree" size="small" />
            </div>
          </div>

          <div class="filter-block" v-if="filterCount">
            <n-button quaternary block size="small" @click="clear">
              <template #icon><j-icon name="close" :size="15" /></template>
              清除全部篩選（{{ filterCount }}）
            </n-button>
          </div>
        </aside>

        <!-- ---------- results ---------- -->
        <div>
          <div class="result-bar">
            <span class="result-count">共 <b>{{ results.length }}</b> 件作品</span>
            <div style="display:flex; align-items:center; gap:10px;">
              <span style="font-size:13px; color:var(--text-faint);">排序</span>
              <n-select v-model:value="s.sort" :options="sortOptions" size="medium" style="width:172px" to=".oj-root" />
            </div>
          </div>

          <div v-if="s.activeTags.length" class="active-filters">
            <span v-for="t in s.activeTags" :key="t" class="tag-toggle on" @click="toggleTag(t)" style="cursor:pointer;">
              {{ t }} <j-icon name="close" :size="13" />
            </span>
          </div>

          <div v-if="results.length" class="grid">
            <product-card v-for="p in results" :key="p.id" :product="p" />
          </div>
          <div v-else class="empty">
            <j-icon name="search" :size="40" style="margin-bottom:14px; opacity:.5;" />
            <p style="font-size:17px; font-weight:600; color:var(--text-soft);">找不到符合的作品</p>
            <p style="margin-top:6px;">試著放寬篩選條件或清除搜尋關鍵字。</p>
            <n-button style="margin-top:16px" @click="clear">清除篩選</n-button>
          </div>
        </div>
      </div>
    </div>`,
};
