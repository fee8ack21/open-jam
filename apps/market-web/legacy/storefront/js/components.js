/* ============================================================
   June — shared UI components (Vue global-build component objects)
   Registered globally in app.js
   ============================================================ */
(function () {

  // ---------- icon set (simple UI line icons) ----------
  const PATHS = {
    heart: 'M12 20s-7-4.3-9.3-8.2C1.2 9 2.3 5.8 5.3 5.1 7.2 4.6 9 5.4 10 6.8c.5.6.7 1 1 1 .3 0 .5-.4 1-1 1-1.4 2.8-2.2 4.7-1.7 3 .7 4.1 3.9 2.6 6.7C19 15.7 12 20 12 20z',
    cart: 'M2 3h1.4a1 1 0 0 1 .98.8l.42 2.2m0 0l1.5 7.9a2 2 0 0 0 1.96 1.6h8.2a2 2 0 0 0 1.96-1.58l1.4-6.4a1 1 0 0 0-.98-1.22H5.2M10 20.5a1.25 1.25 0 1 1-2.5 0 1.25 1.25 0 0 1 2.5 0zM18.5 20.5a1.25 1.25 0 1 1-2.5 0 1.25 1.25 0 0 1 2.5 0z',
    search: 'M11 19a8 8 0 1 0 0-16 8 8 0 0 0 0 16zM21 21l-4.3-4.3',
    sun: 'M12 17a5 5 0 1 0 0-10 5 5 0 0 0 0 10zM12 1v2M12 21v2M4.2 4.2l1.4 1.4M18.4 18.4l1.4 1.4M1 12h2M21 12h2M4.2 19.8l1.4-1.4M18.4 5.6l1.4-1.4',
    moon: 'M21 12.8A9 9 0 1 1 11.2 3a7 7 0 0 0 9.8 9.8z',
    star: 'M12 3.5l2.6 5.3 5.9.9-4.2 4.1 1 5.8L12 17l-5.3 2.8 1-5.8L3.5 9.7l5.9-.9L12 3.5z',
    check: 'M5 12.5l4.5 4.5L19 7',
    lock: 'M6 11V8a6 6 0 0 1 12 0v3M5 11h14v9H5z',
    download: 'M12 3v12M7 11l5 4 5-4M5 21h14',
    chevron: 'M9 6l6 6-6 6',
    chevronD: 'M6 9l6 6 6-6',
    file: 'M14 3H7a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V8l-5-5zM14 3v5h5',
    shield: 'M12 3l8 3v5c0 5-3.4 8.5-8 10-4.6-1.5-8-5-8-10V6l8-3z',
    close: 'M6 6l12 12M18 6L6 18',
    plus: 'M12 5v14M5 12h14',
    minus: 'M5 12h14',
    sliders: 'M4 6h11M19 6h1M4 12h4M12 12h8M4 18h9M17 18h3M15 4v4M8 10v4M13 16v4',
    trash: 'M4 7h16M9 7V5a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2M6 7l1 13a1 1 0 0 0 1 1h8a1 1 0 0 0 1-1l1-13',
    note: 'M9 18V6l10-2v12M9 13l10-2M9 18a2.5 2.5 0 1 1-5 0 2.5 2.5 0 0 1 5 0zM19 16a2.5 2.5 0 1 1-5 0 2.5 2.5 0 0 1 5 0z',
    image: 'M4 5h16v14H4zM4 15l4-4 4 4 3-3 5 5M9 9.5a1.5 1.5 0 1 1-3 0 1.5 1.5 0 0 1 3 0z',
    book: 'M4 5a2 2 0 0 1 2-2h13v16H6a2 2 0 0 0-2 2zM4 19a2 2 0 0 1 2-2h13',
    user: 'M12 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8zM4 21a8 8 0 0 1 16 0',
    mail: 'M3 5h18v14H3zM3 6l9 7 9-7',
    card: 'M2 6h20v12H2zM2 10h20',
    sparkle: 'M12 3l1.8 5.2L19 10l-5.2 1.8L12 17l-1.8-5.2L5 10l5.2-1.8L12 3z',
    arrowLeft: 'M19 12H5M11 6l-6 6 6 6',
    bag: 'M6 8h12l-1 12H7L6 8zM9 8V6a3 3 0 0 1 6 0v2',
  };
  const FILLED = { heart: true, star: true };

  window.JIcon = {
    name: 'JIcon',
    props: { name: String, size: { type: [Number, String], default: 20 }, fill: Boolean, stroke: { type: Number, default: 1.8 } },
    computed: {
      d() { return PATHS[this.name] || ''; },
      isFill() { return this.fill || (FILLED[this.name] && this.fill); },
    },
    template: `
      <svg :width="size" :height="size" viewBox="0 0 24 24"
           :fill="fill ? 'currentColor' : 'none'"
           :stroke="fill ? 'none' : 'currentColor'"
           :stroke-width="stroke" stroke-linecap="round" stroke-linejoin="round"
           style="display:block; flex:none;">
        <path :d="d"></path>
      </svg>`,
  };

  // ---------- striped placeholder thumbnail ----------
  window.ProductThumb = {
    name: 'ProductThumb',
    components: { JIcon: window.JIcon },
    props: {
      product: Object,
      label: { type: String, default: '' },
      glyphSize: { type: Number, default: 64 },
      showCat: { type: Boolean, default: true },
      hideLabel: { type: Boolean, default: false },
      seed: { type: Number, default: 0 },
    },
    setup() { return { store: window.useStore() }; },
    computed: {
      hue() { return (this.product.hue + this.seed * 22) % 360; },
      dark() { return this.store.state.theme === 'dark'; },
      vars() {
        const h = this.hue;
        const h2 = (h + 42) % 360;
        return {
          '--c1': `hsl(${h} 88% 62%)`,
          '--c2': `hsl(${h2} 90% 54%)`,
          '--c-deep': `hsl(${h} 70% 26%)`,
        };
      },
      catGlyph() {
        const c = window.JUNE_CATEGORIES.find(c => c.id === this.product.cat);
        return c ? c.glyph : 'image';
      },
      catLabel() {
        return ({ music: 'SCORE', photo: 'PHOTO', ebook: 'EBOOK' })[this.product.cat] || '';
      },
      autoLabel() {
        if (this.label) return this.label;
        const f = this.product.files[0];
        return `${this.product.formats[0]} · ${this.product.totalSize}`;
      },
    },
    template: `
      <div class="thumb" :style="vars">
        <div class="thumb-dots"></div>
        <div class="thumb-blob"></div>
        <div v-if="showCat" class="thumb-cat">{{ catLabel }}</div>
        <div class="thumb-glyph"><j-icon :name="catGlyph" :size="glyphSize" :stroke="1.6" /></div>
        <div v-if="!hideLabel" class="thumb-label">{{ autoLabel }}</div>
      </div>`,
  };

  // ---------- star rating ----------
  window.Stars = {
    name: 'Stars',
    components: { JIcon: window.JIcon },
    props: { value: Number, count: Number, size: { type: Number, default: 13 } },
    template: `
      <span class="rating">
        <j-icon name="star" :size="size" fill style="color:#f0a92b" />
        {{ value.toFixed(1) }}
        <span v-if="count" style="opacity:.7">({{ count }})</span>
      </span>`,
  };

  // ---------- product card ----------
  window.ProductCard = {
    name: 'ProductCard',
    components: { ProductThumb: window.ProductThumb, JIcon: window.JIcon, Stars: window.Stars },
    props: { product: Object },
    setup() { return { store: window.useStore() }; },
    computed: {
      fav() { return this.store.getters.isFav(this.product.id); },
      accent() { return `hsl(${this.product.hue} 85% 58%)`; },
      initials() { return this.product.creator.split(' ').map(s => s[0]).slice(0, 2).join(''); },
    },
    methods: {
      open() { this.store.openProduct(this.product.id); },
      toggleFav(e) { e.stopPropagation(); this.store.toggleFav(this.product.id); },
    },
    template: `
      <div class="card" :style="{ '--accent': accent }" @click="open">
        <div class="fav" :class="{ on: fav }" @click="toggleFav" :title="fav ? '已收藏' : '加入最愛'">
          <j-icon name="heart" :size="17" :fill="fav" />
        </div>
        <product-thumb :product="product" />
        <div class="card-body">
          <h3 class="card-title">{{ product.title }}</h3>
          <div class="card-creator">
            <span class="avatar" :style="{ background: product.avatar }">{{ initials }}</span>
            {{ product.creator }}
          </div>
          <div class="tagrow">
            <span v-for="t in product.tags" :key="t" class="chip">{{ t }}</span>
          </div>
          <div class="card-foot">
            <span class="price" :class="{ free: product.price === 0 }">
              {{ product.price === 0 ? '免費' : '$' + product.price }}
            </span>
            <stars :value="product.rating" :count="product.ratingCount" />
          </div>
        </div>
      </div>`,
  };

})();
