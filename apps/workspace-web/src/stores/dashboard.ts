/* ============================================================
   Dashboard store (Pinia)
   後台外框共用狀態：買家 / 賣家模式切換、路由跳轉 go()、
   上架精靈草稿（localStorage 持久化）。`go()` 與 `setMode()`
   push 路由——router 實例由 main.ts 的 Pinia plugin 注入（store.router）。
   ============================================================ */
import { defineStore } from 'pinia'

/** 上架精靈草稿。 */
interface DraftState {
  title: string
  cat: string
  tags: string[]
  price: number
  free: boolean
  blurb: string
  coverHue: number
}

interface DashboardState {
  mode: string
  wizardStep: number
  draft: DraftState
}

const KEY = 'openjam.dash.'
const load = <T>(k: string, fb: T): T => {
  try { const v = localStorage.getItem(KEY + k); return v ? (JSON.parse(v) as T) : fb }
  catch { return fb }
}
const save = (k: string, v: unknown) => { try { localStorage.setItem(KEY + k, JSON.stringify(v)) } catch { /* ignore */ } }

export const useDashboardStore = defineStore('dashboard', {
  state: (): DashboardState => ({
    mode: load('mode', 'buy'),           // sell | buy（預設買家）

    // upload wizard draft
    wizardStep: 1,
    draft: load<DraftState>('draft', {
      // 預設定價對齊付費商品最低金額（後端 CatalogValidators.MinPaidPrice = 30）
      title: '', cat: 'photo', tags: [], price: 30, free: false,
      blurb: '', coverHue: 256,
    }),
  }),

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
      if (['purchases', 'wishlist'].includes(name)) { this.mode = 'buy'; save('mode', 'buy') }
      else if (['overview', 'open-store', 'products', 'product-edit', 'upload', 'orders'].includes(name)) { this.mode = 'sell'; save('mode', 'sell') }
    },

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
    resetDraft() {
      this.draft = { title: '', cat: 'photo', tags: [], price: 30, free: false, blurb: '', coverHue: 256 }
      this.wizardStep = 1; save('draft', this.draft)
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
