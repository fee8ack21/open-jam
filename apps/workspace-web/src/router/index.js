import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth.js'

const routes = [
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

let userLoaded = false

router.beforeEach(async () => {
  const auth = useAuthStore()
  if (!userLoaded) {
    await auth.getUserIdentity()
    userLoaded = true
  }
  if (!auth.isAuthenticated) {
    auth.login(window.location.href)
    return false
  }
})

export default router
