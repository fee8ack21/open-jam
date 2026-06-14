/* ============================================================
   Open Jam — shop store (Pinia)
   Setup store（Composition API）—— state 以 ref、getter 以 computed、
   action 為一般函式。View/navigation state 已移至 vue-router；
   此 store 只擁有商品篩選、購物車、收藏與已完成訂單紀錄。
   ============================================================ */
import { computed, ref } from 'vue';
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

const load = <T>(k: string, fb: T): T => {
  try { const v = localStorage.getItem('openjam.' + k); return v ? JSON.parse(v) as T : fb; }
  catch (e) { return fb; }
};
const save = (k: string, v: unknown) => { try { localStorage.setItem('openjam.' + k, JSON.stringify(v)); } catch (e) {} };

export const useShopStore = defineStore('shop', () => {
  // ── state ──────────────────────────────────────────────
  // theme / display
  const theme = ref<Theme>(load<Theme>('theme', 'light'));

  // catalogue search & filters
  const search = ref('');
  const category = ref('all');          // 'all' | music | photo | ebook
  const activeTags = ref<string[]>([]);
  const priceRange = ref<[number, number]>([0, 40]);
  const sort = ref<SortKey>('popular'); // popular | newest | price-asc | price-desc | rating
  const onlyFree = ref(false);

  // user data (persisted)
  const favorites = ref<string[]>(load<string[]>('favorites', []));
  const cart = ref<CartItem[]>(load<CartItem[]>('cart', []));   // [{ id, qty }]

  // checkout
  const order = ref<Order | null>(null);  // set after successful payment

  // ── getters ────────────────────────────────────────────
  // arg-taking getters return a function
  const product = computed(() => (id: string) => PRODUCTS.find((p) => p.id === id));
  const isFav = computed(() => (id: string) => favorites.value.includes(id));
  const inCart = computed(() => (id: string) => cart.value.some((c) => c.id === id));

  const cartProducts = computed<CartProduct[]>(() =>
    cart.value
      .map((c) => ({ ...PRODUCTS.find((p) => p.id === c.id), qty: c.qty }))
      .filter((p): p is CartProduct => Boolean(p.id)));
  const cartCount = computed(() => cart.value.reduce((n, c) => n + c.qty, 0));
  const subtotal = computed(() => cartProducts.value.reduce((s, p) => s + p.price * p.qty, 0));

  const filtered = computed<Product[]>(() => {
    let list = PRODUCTS.slice();
    const q = search.value.trim().toLowerCase();
    if (q) list = list.filter((p) =>
      p.title.toLowerCase().includes(q) ||
      p.creator.toLowerCase().includes(q) ||
      p.tags.some((t) => t.toLowerCase().includes(q)));
    if (category.value !== 'all') list = list.filter((p) => p.cat === category.value);
    if (activeTags.value.length) list = list.filter((p) => activeTags.value.every((t) => p.tags.includes(t)));
    if (onlyFree.value) list = list.filter((p) => p.price === 0);
    list = list.filter((p) => p.price >= priceRange.value[0] && p.price <= priceRange.value[1]);
    switch (sort.value) {
      case 'newest': list.reverse(); break;
      case 'price-asc': list.sort((a, b) => a.price - b.price); break;
      case 'price-desc': list.sort((a, b) => b.price - a.price); break;
      case 'rating': list.sort((a, b) => b.rating - a.rating); break;
      default: list.sort((a, b) => b.sales - a.sales);
    }
    return list;
  });
  const activeFilterCount = computed(() => {
    let n = activeTags.value.length;
    if (category.value !== 'all') n++;
    if (onlyFree.value) n++;
    if (priceRange.value[0] !== 0 || priceRange.value[1] !== 40) n++;
    return n;
  });

  // ── actions ────────────────────────────────────────────
  function setTheme(t: Theme) { theme.value = t; save('theme', t); }
  function toggleTheme() { setTheme(theme.value === 'light' ? 'dark' : 'light'); }

  function toggleFav(id: string) {
    const i = favorites.value.indexOf(id);
    if (i >= 0) favorites.value.splice(i, 1); else favorites.value.push(id);
    save('favorites', favorites.value);
  }

  function addToCart(id: string, qty = 1) {
    const item = cart.value.find((c) => c.id === id);
    if (item) item.qty += qty; else cart.value.push({ id, qty });
    save('cart', cart.value);
  }
  function setQty(id: string, qty: number) {
    const item = cart.value.find((c) => c.id === id);
    if (item) item.qty = Math.max(1, qty);
    save('cart', cart.value);
  }
  function removeFromCart(id: string) {
    const i = cart.value.findIndex((c) => c.id === id);
    if (i >= 0) cart.value.splice(i, 1);
    save('cart', cart.value);
  }
  function clearCart() { cart.value = []; save('cart', []); }

  function setCategory(c: string) { category.value = c; activeTags.value = []; }
  function toggleTag(t: string) {
    const i = activeTags.value.indexOf(t);
    if (i >= 0) activeTags.value.splice(i, 1); else activeTags.value.push(t);
  }
  function clearFilters() {
    category.value = 'all'; activeTags.value = []; priceRange.value = [0, 40];
    onlyFree.value = false; search.value = '';
  }

  // checkout
  function startCheckout() { order.value = null; }
  function completeOrder(buyer: Buyer) {
    order.value = {
      id: 'JUN-' + Math.random().toString(36).slice(2, 7).toUpperCase(),
      items: cartProducts.value,
      total: subtotal.value,
      buyer,
      date: new Date(),
    };
    clearCart();
  }

  return {
    // state
    theme, search, category, activeTags, priceRange, sort, onlyFree, favorites, cart, order,
    // getters
    product, isFav, inCart, cartProducts, cartCount, subtotal, filtered, activeFilterCount,
    // actions
    setTheme, toggleTheme, toggleFav, addToCart, setQty, removeFromCart, clearCart,
    setCategory, toggleTag, clearFilters, startCheckout, completeOrder,
  };
});
