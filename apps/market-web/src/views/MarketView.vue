<script>
/* ============================================================
   MarketView — 市場集 hub (home route "/")
   Search-led hero + category / sort / price browse grid.
   Cards deep-link into the storefront detail route.
   ============================================================ */
import { useShopStore } from '@/stores/shop.js';
import { PRODUCTS, CATEGORIES } from '@/data/catalogue.js';

const order = new Map(PRODUCTS.map((p, i) => [p.id, i])); // catalogue order → newest = larger index

export default {
  name: 'MarketView',
  setup() {
    return { store: useShopStore() };
  },
  data() {
    return {
      cats: CATEGORIES,
      keywords: ['爵士', '京都', '鋼琴', 'Notion', '街拍', '免費'],
      search: '',
      category: 'all',
      sort: 'popular',
      priceBand: 'all',
      sortOptions: [
        { v: 'popular', l: '最熱門' },
        { v: 'newest', l: '最新上架' },
        { v: 'rating', l: '評分最高' },
        { v: 'price-asc', l: '價格低 → 高' },
        { v: 'price-desc', l: '價格高 → 低' },
      ],
      priceOptions: [
        { v: 'all', l: '不限' },
        { v: 'free', l: '免費' },
        { v: 'low', l: '$1–15' },
        { v: 'mid', l: '$16–30' },
        { v: 'high', l: '$30+' },
      ],
    };
  },
  computed: {
    results() {
      let list = PRODUCTS.slice();
      const q = this.search.trim().toLowerCase();
      if (q) {
        list = list.filter(
          (p) =>
            p.title.toLowerCase().includes(q) ||
            p.creator.toLowerCase().includes(q) ||
            p.tags.some((t) => t.toLowerCase().includes(q)),
        );
      }
      if (this.category !== 'all') list = list.filter((p) => p.cat === this.category);
      list = list.filter((p) => this.inBand(p.price));
      switch (this.sort) {
        case 'newest':
          list.sort((a, b) => order.get(b.id) - order.get(a.id));
          break;
        case 'rating':
          list.sort((a, b) => b.rating - a.rating);
          break;
        case 'price-asc':
          list.sort((a, b) => a.price - b.price);
          break;
        case 'price-desc':
          list.sort((a, b) => b.price - a.price);
          break;
        default:
          list.sort((a, b) => b.sales - a.sales);
      }
      return list;
    },
    activeCatLabel() {
      if (this.category === 'all') return '全部作品';
      return (CATEGORIES.find((c) => c.id === this.category) || {}).label || '全部作品';
    },
  },
  methods: {
    catColor(id) {
      return { music: 'var(--c-violet)', photo: 'var(--c-pink)', ebook: 'var(--c-cyan)' }[id];
    },
    catCount(id) {
      return id === 'all' ? PRODUCTS.length : PRODUCTS.filter((p) => p.cat === id).length;
    },
    inBand(price) {
      switch (this.priceBand) {
        case 'free':
          return price === 0;
        case 'low':
          return price > 0 && price <= 15;
        case 'mid':
          return price >= 16 && price <= 30;
        case 'high':
          return price > 30;
        default:
          return true;
      }
    },
    scrollToBrowse() {
      const el = document.getElementById('browse');
      if (el) window.scrollTo({ top: el.offsetTop - 80, behavior: 'smooth' });
    },
    pickKeyword(k) {
      this.search = k;
      this.scrollToBrowse();
    },
    reset() {
      this.category = 'all';
      this.sort = 'popular';
      this.priceBand = 'all';
      this.search = '';
    },
    goAdmin() {
      // Placeholder — wire to the creator/admin backend route on migration.
      // e.g. this.$router.push('/admin')
    },
  },
};
</script>

<template>
  <div class="oj-root" :class="[store.theme, 'font-' + store.font]" data-screen-label="市場集首頁">
    <!-- ============ NAV ============ -->
    <header class="nav">
      <div class="nav-inner">
        <router-link class="brand" to="/">
          <span class="brand-mark">
            <svg width="19" height="19" viewBox="0 0 24 24" fill="none">
              <path d="M15 16.4V4.5c3.7 1 5 3.9 2 6.8" stroke="#fff" stroke-width="2.3" stroke-linecap="round" stroke-linejoin="round" fill="none"></path>
              <ellipse cx="10.4" cy="16.8" rx="4.7" ry="3.5" fill="#fff" transform="rotate(-22 10.4 16.8)"></ellipse>
            </svg>
          </span>
          <span class="brand-name">Open Jam</span>
        </router-link>

        <div class="nav-spacer"></div>

        <div class="nav-actions">
          <!-- 後台入口：實際 SPA 中應導向創作者 / 管理後台路由（如 /admin） -->
          <a class="nav-admin" href="#" title="前往後台" @click.prevent="goAdmin">
            <j-icon name="user" :size="18" /> 前往後台
          </a>
        </div>
      </div>
    </header>

    <main class="page" id="top">
      <!-- ============ HERO ============ -->
      <section class="mkt-hero">
        <div class="hero-shapes">
          <span class="shape s1"></span>
          <span class="shape s2"></span>
          <span class="shape s3"></span>
        </div>
        <div class="mkt-hero-inner">
          <p class="hero-eyebrow"><j-icon name="sparkle" :size="14" /> OPEN JAM · 創作者數位市集</p>
          <h1 class="mkt-hero-title">發現值得<span class="hl hl-lime">收藏</span>的<br>創作者<span class="hl hl-pink">數位作品</span></h1>

          <form class="mkt-search" @submit.prevent="scrollToBrowse">
            <span class="s-ic"><j-icon name="search" :size="22" /></span>
            <input v-model="search" type="text" placeholder="搜尋作品、創作者或標籤…" aria-label="搜尋市集" />
            <button type="submit"><j-icon name="search" :size="17" /> 搜尋</button>
          </form>

          <div class="kw-row">
            <span class="kw-lab">熱門搜尋</span>
            <a v-for="k in keywords" :key="k" class="kw" href="#browse" @click.prevent="pickKeyword(k)">{{ k }}</a>
          </div>
        </div>
      </section>

      <!-- ============ BROWSE ============ -->
      <section class="sec browse" id="browse">
        <!-- category pills (desktop) -->
        <div class="browse-cats">
          <span class="cat-pill c-all" :class="{ on: category === 'all' }" @click="category = 'all'">
            <span class="dot" style="background: var(--c-violet)"></span>全部作品
            <span class="cp-count">{{ catCount('all') }}</span>
          </span>
          <span
            v-for="c in cats"
            :key="c.id"
            class="cat-pill"
            :class="['c-' + c.id, { on: category === c.id }]"
            @click="category = c.id"
          >
            <span class="dot" :style="{ background: catColor(c.id) }"></span>{{ c.label }}
            <span class="cp-count">{{ catCount(c.id) }}</span>
          </span>
        </div>

        <!-- category dropdown (mobile) -->
        <div class="browse-select">
          <select class="m-select" v-model="category" aria-label="作品分類">
            <option value="all">全部作品（{{ catCount('all') }}）</option>
            <option v-for="c in cats" :key="c.id" :value="c.id">{{ c.label }}（{{ catCount(c.id) }}）</option>
          </select>
        </div>

        <!-- sort + price toolbar -->
        <div class="browse-bar">
          <div class="bb-group bb-sort">
            <span class="bb-label">排序</span>
            <div class="bb-pills">
              <span v-for="o in sortOptions" :key="o.v" class="tag-toggle" :class="{ on: sort === o.v }" @click="sort = o.v">{{ o.l }}</span>
            </div>
            <select class="m-select bb-select" v-model="sort" aria-label="排序方式">
              <option v-for="o in sortOptions" :key="o.v" :value="o.v">{{ o.l }}</option>
            </select>
          </div>
          <div class="bb-group bb-price">
            <span class="bb-label">價格</span>
            <div class="bb-pills">
              <span v-for="o in priceOptions" :key="o.v" class="tag-toggle" :class="{ on: priceBand === o.v }" @click="priceBand = o.v">{{ o.l }}</span>
            </div>
            <select class="m-select bb-select" v-model="priceBand" aria-label="價格區間">
              <option v-for="o in priceOptions" :key="o.v" :value="o.v">{{ o.l }}</option>
            </select>
          </div>
        </div>

        <div class="browse-count">
          <span><b>{{ activeCatLabel }}</b> · 共 <b>{{ results.length }}</b> 件作品</span>
        </div>

        <div v-if="results.length" class="grid">
          <mkt-card v-for="p in results" :key="p.id" :product="p" />
        </div>
        <div v-else class="empty">
          <j-icon name="search" :size="40" style="margin-bottom: 14px; opacity: 0.5" />
          <p style="font-size: 17px; font-weight: 600; color: var(--text-soft)">找不到符合的作品</p>
          <p style="margin-top: 6px">試著放寬篩選條件或清除搜尋關鍵字。</p>
          <span class="kw" style="display: inline-block; margin-top: 18px; cursor: pointer" @click="reset">清除全部篩選</span>
        </div>
      </section>

      <!-- ============ FOOTER ============ -->
      <footer class="mkt-foot">
        <div class="brand" style="cursor: default">
          <span class="brand-mark">
            <svg width="19" height="19" viewBox="0 0 24 24" fill="none">
              <path d="M15 16.4V4.5c3.7 1 5 3.9 2 6.8" stroke="#fff" stroke-width="2.3" stroke-linecap="round" stroke-linejoin="round" fill="none"></path>
              <ellipse cx="10.4" cy="16.8" rx="4.7" ry="3.5" fill="#fff" transform="rotate(-22 10.4 16.8)"></ellipse>
            </svg>
          </span>
          <span class="brand-name">Open Jam</span>
        </div>
        <div class="mkt-foot-links"></div>
        <div class="mkt-foot-copy">© 2026 Open Jam · 創作者數位市集</div>
      </footer>
    </main>
  </div>
</template>
