/* ============================================================
   June — store
   Structured to mirror a Pinia store (state / getters / actions)
   so it maps 1:1 to defineStore() when you move to Vite.
   In production:  export const useShopStore = defineStore('shop', { ... })
   ============================================================ */
(function () {
  const { reactive, computed } = Vue;

  const load = (k, fb) => {
    try { const v = localStorage.getItem('june.' + k); return v ? JSON.parse(v) : fb; }
    catch (e) { return fb; }
  };
  const save = (k, v) => { try { localStorage.setItem('june.' + k, JSON.stringify(v)); } catch (e) {} };

  // ---- state ----
  const state = reactive({
    // ui
    view: 'list',            // 'list' | 'detail' | 'checkout'
    currentId: null,
    theme: load('theme', 'light'),
    font: load('font', 'sora'),
    search: '',

    // catalogue filters
    category: 'all',         // 'all' | music | photo | ebook
    activeTags: [],
    priceRange: [0, 40],
    sort: 'popular',         // popular | newest | price-asc | price-desc | rating
    onlyFree: false,

    // user data
    favorites: load('favorites', []),
    cart: load('cart', []),  // [{ id, qty }]

    // checkout
    order: null,             // set after successful payment
  });

  // ---- getters ----
  const getters = {
    product: (id) => window.JUNE_PRODUCTS.find(p => p.id === id),
    get current() { return getters.product(state.currentId); },
    get cartProducts() {
      return state.cart.map(c => ({ ...getters.product(c.id), qty: c.qty })).filter(p => p.id);
    },
    get cartCount() { return state.cart.reduce((n, c) => n + c.qty, 0); },
    get subtotal() {
      return getters.cartProducts.reduce((s, p) => s + p.price * p.qty, 0);
    },
    isFav: (id) => state.favorites.includes(id),
    inCart: (id) => state.cart.some(c => c.id === id),

    get filtered() {
      let list = window.JUNE_PRODUCTS.slice();
      const q = state.search.trim().toLowerCase();
      if (q) list = list.filter(p =>
        p.title.toLowerCase().includes(q) ||
        p.creator.toLowerCase().includes(q) ||
        p.tags.some(t => t.toLowerCase().includes(q)));
      if (state.category !== 'all') list = list.filter(p => p.cat === state.category);
      if (state.activeTags.length) list = list.filter(p => state.activeTags.every(t => p.tags.includes(t)));
      if (state.onlyFree) list = list.filter(p => p.price === 0);
      list = list.filter(p => p.price >= state.priceRange[0] && p.price <= state.priceRange[1]);
      switch (state.sort) {
        case 'newest': list.reverse(); break;
        case 'price-asc': list.sort((a, b) => a.price - b.price); break;
        case 'price-desc': list.sort((a, b) => b.price - a.price); break;
        case 'rating': list.sort((a, b) => b.rating - a.rating); break;
        default: list.sort((a, b) => b.sales - a.sales);
      }
      return list;
    },
    get activeFilterCount() {
      let n = state.activeTags.length;
      if (state.category !== 'all') n++;
      if (state.onlyFree) n++;
      if (state.priceRange[0] !== 0 || state.priceRange[1] !== 40) n++;
      return n;
    },
  };

  // ---- actions ----
  const actions = {
    goList() { state.view = 'list'; window.scrollTo({ top: 0 }); },
    openProduct(id) { state.currentId = id; state.view = 'detail'; window.scrollTo({ top: 0 }); },
    goCheckout() { state.view = 'checkout'; state.order = null; window.scrollTo({ top: 0 }); },

    setTheme(t) { state.theme = t; save('theme', t); },
    toggleTheme() { actions.setTheme(state.theme === 'light' ? 'dark' : 'light'); },
    setFont(f) { state.font = f; save('font', f); },

    toggleFav(id) {
      const i = state.favorites.indexOf(id);
      if (i >= 0) state.favorites.splice(i, 1); else state.favorites.push(id);
      save('favorites', state.favorites);
    },

    addToCart(id, qty = 1) {
      const item = state.cart.find(c => c.id === id);
      if (item) item.qty += qty; else state.cart.push({ id, qty });
      save('cart', state.cart);
    },
    setQty(id, qty) {
      const item = state.cart.find(c => c.id === id);
      if (item) item.qty = Math.max(1, qty);
      save('cart', state.cart);
    },
    removeFromCart(id) {
      const i = state.cart.findIndex(c => c.id === id);
      if (i >= 0) state.cart.splice(i, 1);
      save('cart', state.cart);
    },
    clearCart() { state.cart = []; save('cart', []); },

    setCategory(c) { state.category = c; state.activeTags = []; },
    toggleTag(t) {
      const i = state.activeTags.indexOf(t);
      if (i >= 0) state.activeTags.splice(i, 1); else state.activeTags.push(t);
    },
    clearFilters() {
      state.category = 'all'; state.activeTags = []; state.priceRange = [0, 40];
      state.onlyFree = false; state.search = '';
    },

    completeOrder(buyer) {
      state.order = {
        id: 'JUN-' + Math.random().toString(36).slice(2, 7).toUpperCase(),
        items: getters.cartProducts,
        total: getters.subtotal,
        buyer,
        date: new Date(),
      };
      actions.clearCart();
    },
  };

  // singleton — mirrors useStore()
  let instance = null;
  window.useStore = function () {
    if (!instance) instance = { state, getters, ...actions };
    return instance;
  };
})();
