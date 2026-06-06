/* ============================================================
   Open Jam — shop store (Pinia)
   Ported 1:1 from the prototype's hand-rolled store. Navigation
   state (view / currentId) is gone — Vue Router owns that now.
   Catalogue filters, cart, favorites, and order live here.
   ============================================================ */
import { defineStore } from 'pinia';
import { PRODUCTS } from '@/data/catalogue.js';

const load = (k, fb) => {
  try {
    const v = localStorage.getItem('openjam.shop.' + k);
    return v ? JSON.parse(v) : fb;
  } catch (e) {
    return fb;
  }
};
const save = (k, v) => {
  try {
    localStorage.setItem('openjam.shop.' + k, JSON.stringify(v));
  } catch (e) {
    /* no-op */
  }
};

export const useShopStore = defineStore('shop', {
  state: () => ({
    font: load('font', 'sora'), // 'sora' (Bricolage) | 'grotesk' (Unbounded)
    search: '',

    // catalogue filters
    category: 'all', // 'all' | music | photo | ebook
    activeTags: [],
    priceRange: [0, 40],
    sort: 'popular', // popular | newest | price-asc | price-desc | rating
    onlyFree: false,

    // user data
    favorites: load('favorites', []),
    cart: load('cart', []), // [{ id, qty }]

    // checkout
    order: null, // set after a successful payment
  }),

  getters: {
    product: () => (id) => PRODUCTS.find((p) => p.id === id),

    cartProducts(state) {
      return state.cart
        .map((c) => ({ ...this.product(c.id), qty: c.qty }))
        .filter((p) => p.id);
    },
    cartCount: (state) => state.cart.reduce((n, c) => n + c.qty, 0),
    subtotal() {
      return this.cartProducts.reduce((s, p) => s + p.price * p.qty, 0);
    },

    isFav: (state) => (id) => state.favorites.includes(id),
    inCart: (state) => (id) => state.cart.some((c) => c.id === id),

    filtered(state) {
      let list = PRODUCTS.slice();
      const q = state.search.trim().toLowerCase();
      if (q) {
        list = list.filter(
          (p) =>
            p.title.toLowerCase().includes(q) ||
            p.creator.toLowerCase().includes(q) ||
            p.tags.some((t) => t.toLowerCase().includes(q)),
        );
      }
      if (state.category !== 'all') list = list.filter((p) => p.cat === state.category);
      if (state.activeTags.length) {
        list = list.filter((p) => state.activeTags.every((t) => p.tags.includes(t)));
      }
      if (state.onlyFree) list = list.filter((p) => p.price === 0);
      list = list.filter(
        (p) => p.price >= state.priceRange[0] && p.price <= state.priceRange[1],
      );
      switch (state.sort) {
        case 'newest':
          list.reverse();
          break;
        case 'price-asc':
          list.sort((a, b) => a.price - b.price);
          break;
        case 'price-desc':
          list.sort((a, b) => b.price - a.price);
          break;
        case 'rating':
          list.sort((a, b) => b.rating - a.rating);
          break;
        default:
          list.sort((a, b) => b.sales - a.sales);
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
    setFont(f) {
      this.font = f;
      save('font', f);
    },

    // ---- favorites ----
    toggleFav(id) {
      const i = this.favorites.indexOf(id);
      if (i >= 0) this.favorites.splice(i, 1);
      else this.favorites.push(id);
      save('favorites', this.favorites);
    },

    // ---- cart ----
    addToCart(id, qty = 1) {
      const item = this.cart.find((c) => c.id === id);
      if (item) item.qty += qty;
      else this.cart.push({ id, qty });
      save('cart', this.cart);
    },
    removeFromCart(id) {
      const i = this.cart.findIndex((c) => c.id === id);
      if (i >= 0) this.cart.splice(i, 1);
      save('cart', this.cart);
    },
    clearCart() {
      this.cart = [];
      save('cart', []);
    },

    // ---- filters ----
    setCategory(c) {
      this.category = c;
      this.activeTags = [];
    },
    toggleTag(t) {
      const i = this.activeTags.indexOf(t);
      if (i >= 0) this.activeTags.splice(i, 1);
      else this.activeTags.push(t);
    },
    clearFilters() {
      this.category = 'all';
      this.activeTags = [];
      this.priceRange = [0, 40];
      this.onlyFree = false;
      this.search = '';
    },

    // ---- checkout ----
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
    resetOrder() {
      this.order = null;
    },
  },
});
