/* ============================================================
   Dashboard store (Pinia)
   Mirrors the original prototype store: state / getters / actions
   with the same localStorage persistence. `go()` and `setMode()`
   push routes — the router instance is injected in main.js via a
   Pinia plugin (store.router).
   ============================================================ */
import { defineStore } from 'pinia'
import { MY_PRODUCTS, ORDERS, PURCHASES, WISHLIST, REVENUE, ME } from '@/data'

const KEY = 'openjam.dash.'
const load = (k, fb) => {
  try { const v = localStorage.getItem(KEY + k); return v ? JSON.parse(v) : fb }
  catch (e) { return fb }
}
const save = (k, v) => { try { localStorage.setItem(KEY + k, JSON.stringify(v)) } catch (e) {} }

// hydrate products with persisted status overrides
const statusOverrides = load('statusOverrides', {})

export const useDashboardStore = defineStore('dashboard', {
  state: () => ({
    mode: load('mode', 'sell'),          // sell | buy
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
    draft: load('draft', {
      title: '', cat: 'photo', tags: [], price: 12, free: false,
      blurb: '', files: [], coverHue: 256,
    }),
  }),

  getters: {
    product: (state) => (id) => state.products.find(p => p.id === id),
    liveProducts: (state) => state.products.filter(p => p.status === 'live'),
    filteredProducts: (state) => {
      let list = state.products.slice()
      if (state.productFilter !== 'all') list = list.filter(p => p.status === state.productFilter)
      const q = state.search.trim().toLowerCase()
      if (q) list = list.filter(p => p.title.toLowerCase().includes(q) || p.tags.some(t => t.toLowerCase().includes(q)))
      return list
    },
    statusCount: (state) => (s) => state.products.filter(p => p.status === s).length,
    // KPIs
    totalRevenue: (state) => state.products.reduce((s, p) => s + (p.revenue || 0), 0),
    monthRevenue: () => { const m = REVENUE.monthly; return m[m.length - 1].value },
    prevMonthRevenue: () => { const m = REVENUE.monthly; return m[m.length - 2].value },
    monthDelta() {
      const a = this.monthRevenue, b = this.prevMonthRevenue
      return b ? Math.round(((a - b) / b) * 100) : 0
    },
    totalSales: (state) => state.products.reduce((s, p) => s + (p.sales || 0), 0),
    totalViews: (state) => state.products.reduce((s, p) => s + (p.views || 0), 0),
    pendingPayout() { return Math.round(this.monthRevenue * 0.7) },
    topProducts: (state) => state.products.slice().sort((a, b) => b.revenue - a.revenue).slice(0, 5),
    recentOrders: (state) => state.orders.slice(0, 6),
    paidOrders: (state) => state.orders.filter(o => o.status === 'paid'),
    convRate() {
      const v = this.totalViews
      return v ? ((this.totalSales / v) * 100).toFixed(1) : '0'
    },
  },

  actions: {
    go(view) {
      if (this.router && this.router.currentRoute.value.name !== view) {
        this.router.push({ name: view })
      }
      window.scrollTo({ top: 0 })
    },
    setMode(m) {
      this.mode = m; save('mode', m)
      // jump to a sensible default view for the mode
      if (m === 'buy' && ['overview', 'products', 'upload', 'orders'].includes(this.router?.currentRoute.value.name)) this.go('purchases')
      if (m === 'sell' && ['purchases', 'wishlist'].includes(this.router?.currentRoute.value.name)) this.go('overview')
    },
    syncModeToRoute(name) {
      if (['purchases', 'wishlist'].includes(name)) { this.mode = 'buy'; save('mode', 'buy') }
      else if (['overview', 'products', 'upload', 'orders'].includes(name)) { this.mode = 'sell'; save('mode', 'sell') }
    },
    setFont(f) { this.font = f; save('font', f) },
    setDensity(d) { this.density = d; save('density', d) },
    setProductFilter(f) { this.productFilter = f },

    setStatus(id, status) {
      const p = this.product(id); if (!p) return
      p.status = status
      statusOverrides[id] = status; save('statusOverrides', statusOverrides)
    },
    togglePublish(id) {
      const p = this.product(id); if (!p) return
      this.setStatus(id, p.status === 'live' ? 'off' : 'live')
    },

    // wishlist
    removeWish(id) { const i = this.wishlist.findIndex(w => w.id === id); if (i >= 0) this.wishlist.splice(i, 1) },

    // wizard
    setStep(n) { this.wizardStep = Math.min(3, Math.max(1, n)) },
    nextStep() { this.setStep(this.wizardStep + 1) },
    prevStep() { this.setStep(this.wizardStep - 1) },
    patchDraft(patch) { Object.assign(this.draft, patch); save('draft', this.draft) },
    addDraftTag(t) {
      t = (t || '').trim(); if (!t || this.draft.tags.includes(t)) return
      this.draft.tags.push(t); save('draft', this.draft)
    },
    removeDraftTag(t) {
      const i = this.draft.tags.indexOf(t); if (i >= 0) this.draft.tags.splice(i, 1); save('draft', this.draft)
    },
    addDraftFile(f) { this.draft.files.push(f); save('draft', this.draft) },
    removeDraftFile(i) { this.draft.files.splice(i, 1); save('draft', this.draft) },
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
    fmtBytes(b) {
      if (!b) return '—'
      if (b >= 1e9) return (b / 1e9).toFixed(1) + ' GB'
      if (b >= 1e6) return (b / 1e6).toFixed(0) + ' MB'
      if (b >= 1e3) return (b / 1e3).toFixed(0) + ' KB'
      return b + ' B'
    },
  },
})
