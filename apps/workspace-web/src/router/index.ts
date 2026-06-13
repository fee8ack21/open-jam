import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useStoreApplicationStore } from '@/stores/storeApplication'

const routes: RouteRecordRaw[] = [
  { path: '/', name: 'overview', component: () => import('@/views/OverviewView.vue'), meta: { title: '儀表板' } },
  { path: '/open-store', name: 'open-store', component: () => import('@/views/OpenStoreView.vue'), meta: { title: '開店' } },
  { path: '/products', name: 'products', component: () => import('@/views/ProductsView.vue'), meta: { title: '商品管理' } },
  { path: '/upload', name: 'upload', component: () => import('@/views/UploadView.vue'), meta: { title: '上架新作品' } },
  { path: '/orders', name: 'orders', component: () => import('@/views/OrdersView.vue'), meta: { title: '訂單管理' } },
  { path: '/purchases', name: 'purchases', component: () => import('@/views/PurchasesView.vue'), meta: { title: '購買紀錄' } },
  { path: '/wishlist', name: 'wishlist', component: () => import('@/views/WishlistView.vue'), meta: { title: 'Wishlist' } },
  { path: '/settings', name: 'settings', component: () => import('@/views/SettingsView.vue'), meta: { title: '設定' } },
  { path: '/:pathMatch(.*)*', redirect: '/' },
]

export const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
  scrollBehavior() { return { top: 0 } },
})

// 賣家（上架）流程相關路由：僅 role === "User" 可進入
const SELL_ROUTES = ['overview', 'open-store', 'products', 'upload', 'orders']
// 需要先開店才能操作的路由：尚未開店時一律導回「開店」
const REQUIRE_STORE_ROUTES = ['overview', 'products', 'upload', 'orders']

let userLoaded = false
let storeStateLoaded = false

router.beforeEach(async (to) => {
  const auth = useAuthStore()
  if (!userLoaded) {
    await auth.getUserIdentity()
    userLoaded = true
  }
  if (!auth.isAuthenticated) {
    auth.login(window.location.href)
    return false
  }

  // 載入「我的開店申請／商店」狀態（僅需一次，後續由 store 反應式更新）
  const storeApp = useStoreApplicationStore()
  if (!storeStateLoaded) {
    await storeApp.load()
    storeStateLoaded = true
  }

  const name = to.name as string | undefined

  // 非一般使用者沒有賣家流程，導向買家頁
  if (name && SELL_ROUTES.includes(name) && !auth.isUser) {
    return { name: 'purchases' }
  }
  // 尚未開店時，賣家操作頁一律導回「開店」
  if (name && REQUIRE_STORE_ROUTES.includes(name) && !storeApp.hasStore) {
    return { name: 'open-store' }
  }
})

export default router
