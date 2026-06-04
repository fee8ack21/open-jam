/* ============================================================
   Open Jam — shop store (Pinia)
   Real defineStore() — drops straight into a Vite SPA.
   View/navigation state has been moved out to vue-router;
   this store now owns catalogue filters, cart, favorites
   and the completed-order record only.
   ============================================================ */
import { defineStore } from 'pinia';
import { PRODUCTS } from '../data/products.js';

const load = (k, fb) => {
  try { const v = localStorage.getItem('openjam.' + k); return v ? JSON.parse(v) : fb; }
  catch (e) { return fb; }
};
const save = (k, v) => { try { localStorage.setItem('openjam.' + k, JSON.stringify(v)); } catch (e) {} };

export const useShopStore = defineStore('shop', {
  state: () => ({
    // theme / display
    theme: load('theme', 'light'),
    font: load('font', 'sora'),

    // catalogue search & filters
    search: '',
    category: 'all',          // 'all' | music | photo | ebook
    activeTags: [],
    priceRange: [0, 40],
    sort: 'popular',          // popular | newest | price-asc | price-desc | rating
    onlyFree: false,

    // user data (persisted)
    favorites: load('favorites', []),
    cart: load('cart', []),   // [{ id, qty }]

    // checkout
    order: null,              // set after successful payment
  }),

  getters: {
    // arg-taking getters return a function
    product: () => (id) => PRODUCTS.find((p) => p.id === id),
    isFav: (state) => (id) => state.favorites.includes(id),
    inCart: (state) => (id) => state.cart.some((c) => c.id === id),

    cartProducts(state) {
      return state.cart
        .map((c) => ({ ...PRODUCTS.find((p) => p.id === c.id), qty: c.qty }))
        .filter((p) => p.id);
    },
    cartCount: (state) => state.cart.reduce((n, c) => n + c.qty, 0),
    subtotal() {
      return this.cartProducts.reduce((s, p) => s + p.price * p.qty, 0);
    },

    filtered(state) {
      let list = PRODUCTS.slice();
      const q = state.search.trim().toLowerCase();
      if (q) list = list.filter((p) =>
        p.title.toLowerCase().includes(q) ||
        p.creator.toLowerCase().includes(q) ||
        p.tags.some((t) => t.toLowerCase().includes(q)));
      if (state.category !== 'all') list = list.filter((p) => p.cat === state.category);
      if (state.activeTags.length) list = list.filter((p) => state.activeTags.every((t) => p.tags.includes(t)));
      if (state.onlyFree) list = list.filter((p) => p.price === 0);
      list = list.filter((p) => p.price >= state.priceRange[0] && p.price <= state.priceRange[1]);
      switch (state.sort) {
        case 'newest': list.reverse(); break;
        case 'price-asc': list.sort((a, b) => a.price - b.price); break;
        case 'price-desc': list.sort((a, b) => b.price - a.price); break;
        case 'rating': list.sort((a, b) => b.rating - a.rating); break;
        default: list.sort((a, b) => b.sales - a.sales);
      }
      return list;
    },
    activeFilterCount(state) {
      let n = state.activeTags.length;
      if (state.category !== 'all') n++;
      if (state.onlyFree) n++;
      if (state.priceRange[0] !== 0 || state.priceRange[1] !== 40) n++;
      return n;
    },
  },

  actions: {
    setTheme(t) { this.theme = t; save('theme', t); },
    toggleTheme() { this.setTheme(this.theme === 'light' ? 'dark' : 'light'); },
    setFont(f) { this.font = f; save('font', f); },

    toggleFav(id) {
      const i = this.favorites.indexOf(id);
      if (i >= 0) this.favorites.splice(i, 1); else this.favorites.push(id);
      save('favorites', this.favorites);
    },

    addToCart(id, qty = 1) {
      const item = this.cart.find((c) => c.id === id);
      if (item) item.qty += qty; else this.cart.push({ id, qty });
      save('cart', this.cart);
    },
    setQty(id, qty) {
      const item = this.cart.find((c) => c.id === id);
      if (item) item.qty = Math.max(1, qty);
      save('cart', this.cart);
    },
    removeFromCart(id) {
      const i = this.cart.findIndex((c) => c.id === id);
      if (i >= 0) this.cart.splice(i, 1);
      save('cart', this.cart);
    },
    clearCart() { this.cart = []; save('cart', []); },

    setCategory(c) { this.category = c; this.activeTags = []; },
    toggleTag(t) {
      const i = this.activeTags.indexOf(t);
      if (i >= 0) this.activeTags.splice(i, 1); else this.activeTags.push(t);
    },
    clearFilters() {
      this.category = 'all'; this.activeTags = []; this.priceRange = [0, 40];
      this.onlyFree = false; this.search = '';
    },

    // checkout
    startCheckout() { this.order = null; },
    completeOrder(buyer) {
      this.order = {
        id: 'JUN-' + Math.random().toString(36).slice(2, 7).toUpperCase(),
        items: this.cartProducts,
        total: this.subtotal,
        buyer,
        date: new Date(),
      };
      this.clearCart();
    },
  },
});
