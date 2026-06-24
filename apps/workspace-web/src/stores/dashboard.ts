/* ============================================================
   Dashboard store (Pinia)
   Mirrors the original prototype store: state / getters / actions
   with the same localStorage persistence. `go()` and `setMode()`
   push routes — the router instance is injected in main.js via a
   Pinia plugin (store.router).
   ============================================================ */
import { defineStore } from 'pinia'
import { MY_PRODUCTS, ORDERS, PURCHASES, WISHLIST, REVENUE, ME } from '@/data/products'
import type { Product, ProductStatus, Order, Purchase, WishlistItem } from '@/data/products'

/** 上架精靈中暫存的檔案。 */
export interface DraftFile {
  name: string
  type: string
  bytes: number
  /** 模擬上傳進度（0–100）。 */
  progress?: number
}

/** 上架精靈草稿。 */
interface DraftState {
  title: string
  cat: string
  tags: string[]
  price: number
  free: boolean
  blurb: string
  files: DraftFile[]
  coverHue: number
}

interface DashboardState {
  mode: string
  font: string
  density: string
  search: string
  products: Product[]
  productFilter: string
  orders: Order[]
  purchases: Purchase[]
  wishlist: WishlistItem[]
  wizardStep: number
  draft: DraftState
}

const KEY = 'openjam.dash.'
const load = <T>(k: string, fb: T): T => {
  try { const v = localStorage.getItem(KEY + k); return v ? (JSON.parse(v) as T) : fb }
  catch { return fb }
}
const save = (k: string, v: unknown) => { try { localStorage.setItem(KEY + k, JSON.stringify(v)) } catch { /* ignore */ } }

// hydrate products with persisted status overrides
const statusOverrides = load<Record<string, ProductStatus>>('statusOverrides', {})

export const useDashboardStore = defineStore('dashboard', {
  state: (): DashboardState => ({
    mode: load('mode', 'buy'),           // sell | buy（預設買家）
    font: load('font', 'sora'),          // sora | grotesk
    density: load('density', 'comfy'),   // comfy | compact
    search: '',

    products: MY_PRODUCTS.map(p => ({ ...p, status: statusOverrides[p.id] || p.status })),
    productFilter: 'all',                // all | live | draft | off | review
    orders: ORDERS.slice(),
    purchases: PURCHASES.slice(),
    wishlist: WISHLIST.slice(),

    // upload wizard draft
    wizardStep: 1,
    draft: load<DraftState>('draft', {
      title: '', cat: 'photo', tags: [], price: 12, free: false,
      blurb: '', files: [], coverHue: 256,
    }),
  }),

  getters: {
    product: (state) => (id: string) => state.products.find(p => p.id === id),
    liveProducts: (state) => state.products.filter(p => p.status === 'live'),
    filteredProducts: (state) => {
      let list = state.products.slice()
      if (state.productFilter !== 'all') list = list.filter(p => p.status === state.productFilter)
      const q = state.search.trim().toLowerCase()
      if (q) list = list.filter(p => p.title.toLowerCase().includes(q) || p.tags.some(t => t.toLowerCase().includes(q)))
      return list
    },
    statusCount: (state) => (s: string) => state.products.filter(p => p.status === s).length,
    // KPIs
    totalRevenue: (state) => state.products.reduce((s, p) => s + (p.revenue || 0), 0),
    monthRevenue: () => { const m = REVENUE.monthly; return m[m.length - 1].value },
    prevMonthRevenue: () => { const m = REVENUE.monthly; return m[m.length - 2].value },
    monthDelta(): number {
      const a = this.monthRevenue, b = this.prevMonthRevenue
      return b ? Math.round(((a - b) / b) * 100) : 0
    },
    totalSales: (state) => state.products.reduce((s, p) => s + (p.sales || 0), 0),
    totalViews: (state) => state.products.reduce((s, p) => s + (p.views || 0), 0),
    pendingPayout(): number { return Math.round(this.monthRevenue * 0.7) },
    topProducts: (state) => state.products.slice().sort((a, b) => b.revenue - a.revenue).slice(0, 5),
    recentOrders: (state) => state.orders.slice(0, 6),
    paidOrders: (state) => state.orders.filter(o => o.status === 'paid'),
    convRate(): string {
      const v = this.totalViews
      return v ? ((this.totalSales / v) * 100).toFixed(1) : '0'
    },
  },

  actions: {
    go(view: string) {
      if (this.router && this.router.currentRoute.value.name !== view) {
        this.router.push({ name: view })
      }
      window.scrollTo({ top: 0 })
    },
    setMode(m: string) {
      this.mode = m; save('mode', m)
      // 切換模式一律進入該模式的第一個 menu item：
      //   買家 → 購買紀錄；賣家 → 儀表板（未開店時由 router guard 導向「開店」）
      this.go(m === 'buy' ? 'purchases' : 'overview')
    },
    syncModeToRoute(name: string) {
      if (['purchases', 'my-orders', 'wishlist'].includes(name)) { this.mode = 'buy'; save('mode', 'buy') }
      else if (['overview', 'open-store', 'products', 'upload', 'orders'].includes(name)) { this.mode = 'sell'; save('mode', 'sell') }
    },
    setFont(f: string) { this.font = f; save('font', f) },
    setDensity(d: string) { this.density = d; save('density', d) },
    setProductFilter(f: string) { this.productFilter = f },

    setStatus(id: string, status: ProductStatus) {
      const p = this.product(id); if (!p) return
      p.status = status
      statusOverrides[id] = status; save('statusOverrides', statusOverrides)
    },
    togglePublish(id: string) {
      const p = this.product(id); if (!p) return
      this.setStatus(id, p.status === 'live' ? 'off' : 'live')
    },

    // wishlist
    removeWish(id: string) { const i = this.wishlist.findIndex(w => w.id === id); if (i >= 0) this.wishlist.splice(i, 1) },

    // wizard
    setStep(n: number) { this.wizardStep = Math.min(3, Math.max(1, n)) },
    nextStep() { this.setStep(this.wizardStep + 1) },
    prevStep() { this.setStep(this.wizardStep - 1) },
    patchDraft(patch: Partial<DraftState>) { Object.assign(this.draft, patch); save('draft', this.draft) },
    addDraftTag(t: string) {
      t = (t || '').trim(); if (!t || this.draft.tags.includes(t)) return
      this.draft.tags.push(t); save('draft', this.draft)
    },
    removeDraftTag(t: string) {
      const i = this.draft.tags.indexOf(t); if (i >= 0) this.draft.tags.splice(i, 1); save('draft', this.draft)
    },
    addDraftFile(f: DraftFile) { this.draft.files.push(f); save('draft', this.draft) },
    removeDraftFile(i: number) { this.draft.files.splice(i, 1); save('draft', this.draft) },
    resetDraft() {
      this.draft = { title: '', cat: 'photo', tags: [], price: 12, free: false, blurb: '', files: [], coverHue: 256 }
      this.wizardStep = 1; save('draft', this.draft)
    },
    publishDraft() {
      const d = this.draft
      const id = 'm' + Math.random().toString(36).slice(2, 6)
      const totalBytes = d.files.reduce((s, f) => s + (f.bytes || 0), 0)
      this.products.unshift({
        id, cat: d.cat, hue: d.coverHue, status: 'review',
        title: d.title || '未命名作品', creator: ME.name, handle: ME.handle, avatar: ME.avatar,
        price: d.free ? 0 : d.price, rating: 0, ratingCount: 0, sales: 0, revenue: 0, views: 0,
        tags: d.tags.slice(), formats: [...new Set(d.files.map(f => f.type))].slice(0, 3),
        totalSize: this.fmtBytes(totalBytes),
        created: new Date().toISOString().slice(0, 10), updated: new Date().toISOString().slice(0, 10),
        contents: d.files.map(f => ({ name: f.name, type: f.type, size: this.fmtBytes(f.bytes) })),
        previews: 0,
      })
      this.resetDraft()
    },
    fmtBytes(b: number) {
      if (!b) return '—'
      if (b >= 1e9) return (b / 1e9).toFixed(1) + ' GB'
      if (b >= 1e6) return (b / 1e6).toFixed(0) + ' MB'
      if (b >= 1e3) return (b / 1e3).toFixed(0) + ' KB'
      return b + ' B'
    },
  },
})
