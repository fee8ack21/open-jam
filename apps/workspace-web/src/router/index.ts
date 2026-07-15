import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useStoreApplicationStore } from '@/stores/storeApplication'

const routes: RouteRecordRaw[] = [
  // 預設進入買家「購買紀錄」頁
  { path: '/', redirect: '/purchases' },
  { path: '/overview', name: 'overview', component: () => import('@/views/OverviewView.vue'), meta: { titleKey: 'route.overview' } },
  { path: '/admin', name: 'admin-overview', component: () => import('@/views/AdminOverviewView.vue'), meta: { titleKey: 'route.adminOverview' } },
  { path: '/review', name: 'review', component: () => import('@/views/ReviewView.vue'), meta: { titleKey: 'route.review' } },
  { path: '/review-history', name: 'review-history', component: () => import('@/views/ReviewHistoryView.vue'), meta: { titleKey: 'route.reviewHistory' } },
  { path: '/stores', name: 'stores', component: () => import('@/views/StoresView.vue'), meta: { titleKey: 'route.stores' } },
  { path: '/stores/:id/products', name: 'store-products', component: () => import('@/views/StoreProductsView.vue'), meta: { titleKey: 'route.storeProducts' } },
  { path: '/members', name: 'members', component: () => import('@/views/AdminMembersView.vue'), meta: { titleKey: 'route.members' } },
  { path: '/admin-products', name: 'admin-products', component: () => import('@/views/AdminProductsView.vue'), meta: { titleKey: 'route.adminProducts' } },
  { path: '/catalog-categories', name: 'catalog-categories', component: () => import('@/views/CatalogCategoriesView.vue'), meta: { titleKey: 'route.catalogCategories' } },
  { path: '/admin-orders', name: 'admin-orders', component: () => import('@/views/AdminOrdersView.vue'), meta: { titleKey: 'route.adminOrders' } },
  { path: '/resource-usage', name: 'resource-usage', component: () => import('@/views/AdminUsageView.vue'), meta: { titleKey: 'route.resourceUsage' } },
  { path: '/legal-documents', name: 'legal-documents', component: () => import('@/views/AdminLegalView.vue'), meta: { titleKey: 'route.legalDocs' } },
  { path: '/faqs', name: 'faqs', component: () => import('@/views/AdminFaqView.vue'), meta: { titleKey: 'route.faqs' } },
  { path: '/faq-categories', name: 'faq-categories', component: () => import('@/views/FaqCategoriesView.vue'), meta: { titleKey: 'route.faqCategories' } },
  { path: '/audit-log', name: 'audit-log', component: () => import('@/views/AuditLogView.vue'), meta: { titleKey: 'route.auditLog' } },
  { path: '/open-store', name: 'open-store', component: () => import('@/views/OpenStoreView.vue'), meta: { titleKey: 'route.openStore' } },
  { path: '/products', name: 'products', component: () => import('@/views/ProductsView.vue'), meta: { titleKey: 'route.products' } },
  { path: '/products/:id/edit', name: 'product-edit', component: () => import('@/views/ProductEditView.vue'), meta: { titleKey: 'route.productEdit' } },
  { path: '/upload', name: 'upload', component: () => import('@/views/UploadView.vue'), meta: { titleKey: 'route.upload' } },
  { path: '/orders', name: 'orders', component: () => import('@/views/OrdersView.vue'), meta: { titleKey: 'route.orders' } },
  { path: '/announcements', name: 'announcements', component: () => import('@/views/AnnouncementsView.vue'), meta: { titleKey: 'route.announcements' } },
  { path: '/store-settings', name: 'store-settings', component: () => import('@/views/StoreSettingsView.vue'), meta: { titleKey: 'route.storeSettings' } },
  { path: '/payouts', name: 'payouts', component: () => import('@/views/PayoutSettingsView.vue'), meta: { titleKey: 'route.payouts' } },
  { path: '/purchases', name: 'purchases', component: () => import('@/views/PurchasesView.vue'), meta: { titleKey: 'route.purchases' } },
  { path: '/wishlist', name: 'wishlist', component: () => import('@/views/WishlistView.vue'), meta: { titleKey: 'route.wishlist' } },
  { path: '/:pathMatch(.*)*', redirect: '/purchases' },
]

export const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
  scrollBehavior() { return { top: 0 } },
})

// 賣家（上架）流程相關路由：僅 role === "User" 可進入
const SELL_ROUTES = ['overview', 'open-store', 'products', 'product-edit', 'upload', 'orders', 'announcements', 'store-settings', 'payouts']
// 需要先開店才能操作的路由：尚未開店時一律導回「開店」
const REQUIRE_STORE_ROUTES = ['overview', 'products', 'product-edit', 'upload', 'orders', 'announcements', 'store-settings', 'payouts']
// 平台管理員專屬路由：僅 role === "Admin" 可進入
const ADMIN_ROUTES = ['admin-overview', 'review', 'review-history', 'stores', 'store-products', 'members', 'admin-products', 'catalog-categories', 'admin-orders', 'resource-usage', 'legal-documents', 'faqs', 'faq-categories', 'audit-log']

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

  const name = to.name as string | undefined

  // 系統管理員：不走買家/賣家流程，預設進入平台儀表板
  if (auth.isAdmin) {
    if (name && ADMIN_ROUTES.includes(name)) return
    return { name: 'admin-overview' }
  }

  // 非管理員不可進入管理員專屬路由
  if (name && ADMIN_ROUTES.includes(name)) {
    return { name: 'purchases' }
  }

  // 載入「我的開店申請／商店」狀態（僅需一次，後續由 store 反應式更新）
  const storeApp = useStoreApplicationStore()
  if (!storeStateLoaded) {
    await storeApp.load()
    storeStateLoaded = true
  }

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
