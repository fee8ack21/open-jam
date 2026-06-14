/* ============================================================
   Open Jam — shop store (Pinia)
   Real defineStore() — drops straight into a Vite SPA.
   View/navigation state has been moved out to vue-router;
   this store now owns catalogue filters, cart, favorites
   and the completed-order record only.
   ============================================================ */
import { defineStore } from 'pinia';
import { PRODUCTS, type Product } from '../data/products';

type Theme = 'light' | 'dark';
type SortKey = 'popular' | 'newest' | 'price-asc' | 'price-desc' | 'rating';

/** 購物車項目。 */
interface CartItem {
  id: string;
  qty: number;
}

/** 購物車內展開後的商品（含數量）。 */
type CartProduct = Product & { qty: number };

/** 購買人資訊。 */
interface Buyer {
  name: string;
  email: string;
}

/** 完成的訂單紀錄。 */
interface Order {
  id: string;
  items: CartProduct[];
  total: number;
  buyer: Buyer;
  date: Date;
}

interface ShopState {
  theme: Theme;
  search: string;
  category: string;
  activeTags: string[];
  priceRange: [number, number];
  sort: SortKey;
  onlyFree: boolean;
  favorites: string[];
  cart: CartItem[];
  order: Order | null;
}

const load = <T>(k: string, fb: T): T => {
  try { const v = localStorage.getItem('openjam.' + k); return v ? JSON.parse(v) as T : fb; }
  catch (e) { return fb; }
};
const save = (k: string, v: unknown) => { try { localStorage.setItem('openjam.' + k, JSON.stringify(v)); } catch (e) {} };

export const useShopStore = defineStore('shop', {
  state: (): ShopState => ({
    // theme / display
    theme: load<Theme>('theme', 'light'),

    // catalogue search & filters
    search: '',
    category: 'all',          // 'all' | music | photo | ebook
    activeTags: [],
    priceRange: [0, 40],
    sort: 'popular',          // popular | newest | price-asc | price-desc | rating
    onlyFree: false,

    // user data (persisted)
    favorites: load<string[]>('favorites', []),
    cart: load<CartItem[]>('cart', []),   // [{ id, qty }]

    // checkout
    order: null,              // set after successful payment
  }),

  getters: {
    // arg-taking getters return a function
    product: () => (id: string) => PRODUCTS.find((p) => p.id === id),
    isFav: (state) => (id: string) => state.favorites.includes(id),
    inCart: (state) => (id: string) => state.cart.some((c) => c.id === id),

    cartProducts(state): CartProduct[] {
      return state.cart
        .map((c) => ({ ...PRODUCTS.find((p) => p.id === c.id), qty: c.qty }))
        .filter((p): p is CartProduct => Boolean(p.id));
    },
    cartCount: (state) => state.cart.reduce((n, c) => n + c.qty, 0),
    subtotal(): number {
      return this.cartProducts.reduce((s, p) => s + p.price * p.qty, 0);
    },

    filtered(state): Product[] {
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
    setTheme(t: Theme) { this.theme = t; save('theme', t); },
    toggleTheme() { this.setTheme(this.theme === 'light' ? 'dark' : 'light'); },

    toggleFav(id: string) {
      const i = this.favorites.indexOf(id);
      if (i >= 0) this.favorites.splice(i, 1); else this.favorites.push(id);
      save('favorites', this.favorites);
    },

    addToCart(id: string, qty = 1) {
      const item = this.cart.find((c) => c.id === id);
      if (item) item.qty += qty; else this.cart.push({ id, qty });
      save('cart', this.cart);
    },
    setQty(id: string, qty: number) {
      const item = this.cart.find((c) => c.id === id);
      if (item) item.qty = Math.max(1, qty);
      save('cart', this.cart);
    },
    removeFromCart(id: string) {
      const i = this.cart.findIndex((c) => c.id === id);
      if (i >= 0) this.cart.splice(i, 1);
      save('cart', this.cart);
    },
    clearCart() { this.cart = []; save('cart', []); },

    setCategory(c: string) { this.category = c; this.activeTags = []; },
    toggleTag(t: string) {
      const i = this.activeTags.indexOf(t);
      if (i >= 0) this.activeTags.splice(i, 1); else this.activeTags.push(t);
    },
    clearFilters() {
      this.category = 'all'; this.activeTags = []; this.priceRange = [0, 40];
      this.onlyFree = false; this.search = '';
    },

    // checkout
    startCheckout() { this.order = null; },
    completeOrder(buyer: Buyer) {
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
