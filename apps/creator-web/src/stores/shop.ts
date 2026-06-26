/* ============================================================
   Open Jam — shop store (Pinia)
   Setup store（Composition API）—— state 以 ref、getter 以 computed、
   action 為一般函式。View/navigation state 已移至 vue-router；
   此 store 只擁有商品篩選、購物車、收藏與已完成訂單紀錄。
   ============================================================ */
import { computed, ref } from 'vue';
import { defineStore } from 'pinia';
import { PRODUCTS, type Product } from '@/data/products';
import { STORE, type Store } from '@/data/store';
import { catalogApi, orderApi, paymentApi, storeApi } from '@/api';
import { useAuthStore } from '@/stores/auth';
import { env } from '@/environment';
import { categoryKeyResolver, toProduct, toStore, hueColor, type StoreInfo } from '@/data/mapCatalog';
import type { CatalogCategoryDto } from '@/api/catalog-service';

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

/**
 * 結帳後、導向 Stripe 前暫存的訂單資訊。
 * Stripe 付款頁會整頁離開 SPA，回到成功頁時記憶體狀態已遺失，
 * 故以 localStorage 暫存，供成功頁還原訂單摘要並清空購物車。
 */
interface PendingOrder {
  orderId: string;
  orderNumber: string;
  buyer: Buyer;
  items: CartProduct[];
  total: number;
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

  // catalogue（由 CatalogService 載入；後端尚未可用時退回示範資料）
  const products = ref<Product[]>([]);
  const storefront = ref<Store>(STORE);
  const categories = ref<CatalogCategoryDto[]>([]);
  const loading = ref(false);
  const loaded = ref(false);

  // catalogue search & filters
  const search = ref('');
  const category = ref('all');          // 'all' | music | photo | ebook
  const activeTags = ref<string[]>([]);
  const priceRange = ref<[number, number]>([0, 40]);
  const sort = ref<SortKey>('popular'); // popular | newest | price-asc | price-desc | rating
  const onlyFree = ref(false);

  // user data
  // 收藏（wishlist）為登入使用者所有，存於 CatalogService；以 API 為來源、不落地 localStorage
  const favorites = ref<string[]>([]);
  const cart = ref<CartItem[]>(load<CartItem[]>('cart', []));   // [{ id, qty }]

  // checkout
  const order = ref<Order | null>(null);  // set after successful payment
  const pendingOrder = ref<PendingOrder | null>(load<PendingOrder | null>('pendingOrder', null));
  const checkingOut = ref(false);

  // 追蹤創作者（依信箱訂閱本店面更新）
  const following = ref(false);

  // ── getters ────────────────────────────────────────────
  // arg-taking getters return a function
  const product = computed(() => (id: string) => products.value.find((p) => p.id === id));
  const isFav = computed(() => (id: string) => favorites.value.includes(id));
  const inCart = computed(() => (id: string) => cart.value.some((c) => c.id === id));

  const cartProducts = computed<CartProduct[]>(() =>
    cart.value
      .map((c) => ({ ...products.value.find((p) => p.id === c.id), qty: c.qty }))
      .filter((p): p is CartProduct => Boolean(p.id)));
  const cartCount = computed(() => cart.value.reduce((n, c) => n + c.qty, 0));
  const subtotal = computed(() => cartProducts.value.reduce((s, p) => s + p.price * p.qty, 0));

  const filtered = computed<Product[]>(() => {
    let list = products.value.slice();
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
  /** 由子網域推導店面 slug；本機開發無子網域時用 env fallback。 */
  function resolveSlug(): string {
    const parts = window.location.hostname.split('.');
    if (parts.length >= 3 && !['www', 'localhost'].includes(parts[0])) return parts[0];
    return env.STORE_SLUG;
  }

  function storeInfo(): StoreInfo {
    return {
      creator: storefront.value.storeName,
      handle: '@' + storefront.value.storeSlug,
      avatar: hueColor(214),
    };
  }

  /** 載入店面資訊與其已上架商品（CatalogService 公開端點）。 */
  async function loadCatalog() {
    if (loaded.value || loading.value) return;
    loading.value = true;
    try {
      const storeRes = await storeApi.stores.get(resolveSlug());
      storefront.value = toStore(storeRes.data);

      const [catRes, listRes] = await Promise.all([
        catalogApi.catalogCategories.list(),
        catalogApi.catalogs.list({ StoreId: storefront.value.id, Offset: 0, Limit: 100 }),
      ]);
      categories.value = catRes.data ?? [];
      const catKeyOf = categoryKeyResolver(categories.value);
      const info = storeInfo();
      products.value = (listRes.data.items ?? []).map((p) => toProduct(p, catKeyOf(p.categoryId), info));
      loaded.value = true;
    } catch {
      // 後端尚未可用：退回示範資料，店面不致空白
      if (!products.value.length) products.value = PRODUCTS.slice();
    } finally {
      loading.value = false;
    }
  }

  /** 記錄一次商品瀏覽（詳情頁進入時呼叫，fire-and-forget）。 */
  async function recordView(id: string) {
    const p = products.value.find((x) => x.id === id);
    if (p) p.views = (p.views ?? 0) + 1; // 樂觀更新本地顯示
    try {
      await catalogApi.catalogs.incrementView(id);
    } catch {
      // 瀏覽計數失敗不影響瀏覽體驗
    }
  }

  /** 載入單一商品完整資訊（含描述 / 標籤），合併進列表。 */
  async function loadProduct(id: string) {
    try {
      const res = await catalogApi.catalogs.get(id);
      const catKeyOf = categoryKeyResolver(categories.value);
      const mapped = toProduct(res.data, catKeyOf(res.data.categoryId), storeInfo());
      const i = products.value.findIndex((p) => p.id === id);
      if (i >= 0) products.value[i] = mapped;
      else products.value.push(mapped);
    } catch {
      // 保留列表既有資料
    }
  }

  function setTheme(t: Theme) { theme.value = t; save('theme', t); }
  function toggleTheme() { setTheme(theme.value === 'light' ? 'dark' : 'light'); }

  /** 載入目前登入使用者的收藏清單；未登入則清空。 */
  async function loadFavorites() {
    const auth = useAuthStore();
    if (!auth.isAuthenticated) {
      favorites.value = [];
      return;
    }
    try {
      const res = await catalogApi.catalogFavorites.listMine();
      favorites.value = res.data.catalogIds ?? [];
    } catch {
      // 取不到時保留現狀，不致誤清
    }
  }

  /**
   * 收藏 / 取消收藏商品。
   * 未登入 → 導向登入頁，登入成功後由 OIDC 導回目前商品頁（不自動收藏）。
   * 已登入 → 樂觀更新並呼叫 CatalogService 新增 / 移除收藏；失敗則還原。
   */
  async function toggleFav(id: string) {
    const auth = useAuthStore();
    if (!auth.isAuthenticated) {
      auth.login();   // 預設 state 為目前網址，登入後導回本商品頁
      return;
    }

    const wasFav = favorites.value.includes(id);
    // 樂觀更新（愛心立即反映）
    if (wasFav) favorites.value = favorites.value.filter((f) => f !== id);
    else favorites.value = [...favorites.value, id];

    try {
      if (wasFav) await catalogApi.catalogFavorites.remove(id);
      else await catalogApi.catalogFavorites.add(id);
    } catch {
      // 還原樂觀更新
      if (wasFav) favorites.value = [...favorites.value, id];
      else favorites.value = favorites.value.filter((f) => f !== id);
    }
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

  /** 以信箱追蹤目前店面（StoreService POST /v1/stores/{id}/follow，已追蹤為 no-op）。 */
  async function followStore(email: string) {
    // 確保已載入真實店面 id（瀏覽時通常已載入，guard 後重呼為低成本）
    await loadCatalog();
    await storeApi.storeFollowers.follow(storefront.value.id, { email });
    following.value = true;
  }

  // checkout
  function startCheckout() { order.value = null; }

  /**
   * 結帳：於 OrderService 建立訂單，再向 PaymentService 取得 Stripe Checkout Session，
   * 回傳應導向的 Stripe 付款頁 URL（由呼叫端 `window.location.href` 跳轉填寫信用卡資訊）。
   * 金額一律以最低貨幣單位（cents）送出；訂單總額以後端回傳為準，確保與實際扣款一致。
   */
  async function checkout(buyer: Buyer): Promise<string> {
    if (!cartProducts.value.length) throw new Error('購物車是空的');
    checkingOut.value = true;
    try {
      // 下單需商品版本 ID；列表載入的精簡資料未含版本，故補載缺漏項目的商品詳情。
      const missing = cartProducts.value.filter((p) => !p.versionId).map((p) => p.id);
      await Promise.all(missing.map((id) => loadProduct(id)));

      const lines = cartProducts.value;
      if (lines.some((p) => !p.versionId)) {
        throw new Error('部分商品尚無可購買的版本，請稍後再試');
      }

      const currency = (lines[0].currency || 'usd').toLowerCase();
      const items = lines.map((p) => ({
        catalogId: p.id,
        catalogVersionId: p.versionId as string,
        catalogName: p.title,
        unitPrice: Math.round(p.price * 100 * p.qty),
      }));

      const orderRes = await orderApi.orders.create({
        storeId: storefront.value.id,
        buyerEmail: buyer.email,
        currency,
        items,
      });
      const created = orderRes.data;

      const productName = lines.length === 1
        ? lines[0].title
        : `${lines[0].title} 等 ${lines.length} 件商品`;
      const amount = created.totalAmount ?? Math.round(subtotal.value * 100);

      const sessionRes = await paymentApi.payments.createCheckoutSession({
        orderId: created.id as string,
        email: buyer.email,
        amount,
        currency,
        productName,
      });

      pendingOrder.value = {
        orderId: created.id as string,
        orderNumber: created.orderNumber ?? '',
        buyer,
        items: lines,
        total: subtotal.value,
      };
      save('pendingOrder', pendingOrder.value);

      const url = sessionRes.data.url;
      if (!url) throw new Error('無法取得付款頁面網址');
      return url;
    } finally {
      checkingOut.value = false;
    }
  }

  /**
   * 由 Stripe 成功頁回到本站時呼叫：以暫存的訂單摘要組出完成畫面、清空購物車與暫存。
   * 回傳 false 表示沒有可還原的訂單（例如直接造訪成功頁）。
   */
  function finishCheckout(): boolean {
    const p = pendingOrder.value;
    if (!p) return false;
    order.value = {
      id: p.orderNumber || p.orderId,
      items: p.items,
      total: p.total,
      buyer: p.buyer,
      date: new Date(),
    };
    clearCart();
    pendingOrder.value = null;
    save('pendingOrder', null);
    return true;
  }

  /** 取消結帳（自 Stripe 取消頁返回）：保留購物車，僅清除暫存的待付款訂單。 */
  function cancelCheckout() {
    pendingOrder.value = null;
    save('pendingOrder', null);
  }

  return {
    // state
    theme, search, category, activeTags, priceRange, sort, onlyFree, favorites, cart, order, following,
    products, storefront, categories, loading, loaded, pendingOrder, checkingOut,
    // getters
    product, isFav, inCart, cartProducts, cartCount, subtotal, filtered, activeFilterCount,
    // actions
    setTheme, toggleTheme, toggleFav, addToCart, setQty, removeFromCart, clearCart,
    setCategory, toggleTag, clearFilters, startCheckout, checkout, finishCheckout, cancelCheckout, followStore,
    loadCatalog, loadProduct, loadFavorites, recordView,
  };
});
