<script setup lang="ts">
import { computed, watch } from 'vue'
import { useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useDashboardStore } from '@/stores/dashboard'
import { useAuthStore } from '@/stores/auth'
import { useStoreApplicationStore } from '@/stores/storeApplication'
import { useStoreReviewStore } from '@/stores/storeReview'
import { useStoreListStore } from '@/stores/storeList'
import { useMemberListStore } from '@/stores/memberList'
import { useWishlistStore } from '@/stores/wishlist'

/** 賣家 / 買家兩組導覽項目；labelKey 對應 i18n route.*，countKey 對應 store 中的數量。 */
const NAV = {
  sell: [
    { view: 'overview', labelKey: 'route.overview', icon: 'grid' },
    { view: 'open-store', labelKey: 'route.openStore', icon: 'rocket' },
    { view: 'products', labelKey: 'route.products', icon: 'box', countKey: 'products' },
    { view: 'upload', labelKey: 'route.upload', icon: 'upload' },
    { view: 'orders', labelKey: 'route.orders', icon: 'receipt' },
    { view: 'announcements', labelKey: 'route.announcements', icon: 'bell' },
    { view: 'payouts', labelKey: 'route.payouts', icon: 'wallet' },
    { view: 'store-settings', labelKey: 'route.storeSettings', icon: 'gear' },
  ],
  buy: [
    { view: 'purchases', labelKey: 'route.purchases', icon: 'bag' },
    { view: 'wishlist', labelKey: 'route.wishlist', icon: 'heart', countKey: 'wishlist' },
  ],
}

/** open：行動版抽屜是否展開（由父層控制）。 */
defineProps<{ open: boolean }>()
/** navigate：選單項目被點擊後通知父層收合抽屜。 */
const emit = defineEmits<{ navigate: [] }>()

const route = useRoute()
const { t } = useI18n()
const store = useDashboardStore()
const authStore = useAuthStore()
const storeAppStore = useStoreApplicationStore()
const reviewStore = useStoreReviewStore()
const storeListStore = useStoreListStore()
const memberListStore = useMemberListStore()
const wishlistStore = useWishlistStore()

/** 已有可用身份時才呈現選單；登出卸載 user 後到導頁前保持空白，避免閃現錯誤角色項目。 */
const isReady = computed(() => authStore.isReady && authStore.isAuthenticated)

// 使用者可用後便宜地取一次收藏數（單一請求），讓側欄願望清單徽章即時正確。
watch(isReady, (ready) => { if (ready) wishlistStore.loadCount() }, { immediate: true })
/** 是否為一般使用者：唯一擁有賣家/上架流程的角色。 */
const canSell = computed(() => authStore.isUser)
/** 是否為系統管理員：顯示店家審核後台，不顯示買家/賣家分頁。 */
const isAdmin = computed(() => authStore.isAdmin)
/** 是否已開店：未開店前賣家選單只露出「開店」。 */
const hasStore = computed(() => storeAppStore.hasStore)
const navSell = computed(() => {
  // 尚未開店：賣家選單只顯示「開店」，開店成功後才顯示其餘項目
  if (!hasStore.value) return NAV.sell.filter(it => it.view === 'open-store')
  return NAV.sell
})
const navBuy = computed(() => NAV.buy)

function count(key?: string) {
  if (!key) return null
  const map: Record<string, number> = {
    products: store.products.length,
    orders: store.paidOrders.length,
    wishlist: wishlistStore.count,
  }
  return map[key]
}
function nav(view: string) { store.go(view); emit('navigate') }
function pickMode(m: string) { store.setMode(m) }
// 內頁（無自身選單項）由所屬選單項代表，停留其上時仍維持高亮
const NAV_PARENT: Record<string, string> = { 'product-edit': 'products' }
function isActive(view: string) {
  const name = route.name as string | undefined
  return name === view || (!!name && NAV_PARENT[name] === view)
}

/** 應用程式版本號（Vite build 時由 package.json 的 version 注入），顯示於選單下方。 */
const appVersion = __APP_VERSION__

</script>

<template>
  <aside class="side" :class="{ 'drawer-open': open }">
    <!-- 果醬罐 logo（v3 設計稿，hover 罐蓋彈開、果醬噴濺、Jam 抹上果醬印，同 portal-web BrandLogo） -->
    <div class="side-brand" @click="nav('overview')">
      <svg class="brand-jar" width="34" height="38" viewBox="0 0 48 54" aria-hidden="true">
        <g class="oj-splash">
          <path d="M17 8 c-1.5 -5 1 -8.5 3.5 -9.5 c-0.5 3 2 4 1.5 7 c-0.4 2.4 -2.5 3.5 -5 2.5 z" fill="#FF90E8" stroke="#1A1A1A" stroke-width="1.8" />
          <circle cx="12" cy="1" r="2.6" fill="#D6479B" stroke="#1A1A1A" stroke-width="1.6" />
          <circle cx="27" cy="-4" r="3.2" fill="#FF90E8" stroke="#1A1A1A" stroke-width="1.8" />
          <circle cx="34" cy="-8" r="1.9" fill="#D6479B" stroke="#1A1A1A" stroke-width="1.5" />
          <circle cx="21" cy="-9" r="1.6" fill="#FF90E8" stroke="#1A1A1A" stroke-width="1.5" />
        </g>
        <rect x="7" y="12" width="34" height="38" rx="11" fill="#FF90E8" stroke="#1A1A1A" stroke-width="2.5"></rect>
        <path d="M12 13 h24 v3 c-2.5 3.5 -5.5 -1 -8.5 2.5 c-3 3.5 -6 -2 -9 1 c-2.8 2.8 -5.5 -0.5 -6.5 -1.5 z" fill="#D6479B" stroke="#1A1A1A" stroke-width="2"></path>
        <rect x="12" y="25" width="24" height="16" rx="5" fill="#FFFFFF" stroke="#1A1A1A" stroke-width="2"></rect>
        <text x="24" y="37.5" text-anchor="middle" font-family="'Space Grotesk','Noto Sans TC',sans-serif" font-weight="700" font-size="12.5" fill="#1A1A1A">OJ</text>
        <g class="oj-lid"><rect x="10" y="3" width="28" height="9" rx="4" fill="#FFDE00" stroke="#1A1A1A" stroke-width="2.5"></rect></g>
      </svg>
      <span class="brand-name">
        Open
        <span class="brand-word">
          <svg class="oj-smear" viewBox="0 0 78 34" preserveAspectRatio="none" style="position: absolute; left: -8px; right: -9px; top: -3px; bottom: -4px; width: calc(100% + 17px); height: calc(100% + 7px)">
            <path d="M6 16 c-2 -4 0 -8 4 -8 c3 0 4 -3 8 -3 c5 0 6 -4 11 -3 c4 1 7 -1 12 0 c4 1 8 -2 12 0 c4 2 8 0 11 3 c3 2 8 1 9 5 c1 3 -3 4 -1 7 c2 3 -1 6 -5 6 c-3 0 -5 3 -9 2 c-4 -1 -6 2 -10 1 c-4 -1 -8 2 -12 0 c-3 -2 -8 1 -11 -1 c-3 -2 -7 0 -9 -3 c-2 -2 -6 -1 -7 -4 c-1 -2 1 -4 -3 -2 z" fill="#FF90E8" />
            <circle cx="73" cy="7" r="2.4" fill="#FF90E8" />
            <circle cx="4" cy="28" r="1.8" fill="#FF90E8" />
            <circle cx="70" cy="29" r="1.6" fill="#FF90E8" />
          </svg>
          <span>Jam</span>
        </span>
        <small>Workspace</small>
      </span>
    </div>

    <div v-if="isReady && !isAdmin" class="mode-switch">
      <button v-if="canSell" :class="{ on: store.mode === 'sell' }" @click="pickMode('sell')"><app-icon name="rocket" :size="15" /> <span class="ms-label">{{ t('sidebar.modeSeller') }}</span></button>
      <button :class="{ on: store.mode === 'buy' }" @click="pickMode('buy')"><app-icon name="bag" :size="15" /> <span class="ms-label">{{ t('sidebar.modeBuyer') }}</span></button>
    </div>

    <nav style="flex:1; overflow-y:auto; overflow-x:hidden;">
      <!-- 身份載入完成前保持空白，載入後才依角色呈現選單 -->
      <template v-if="!isReady" />
      <template v-else-if="isAdmin">
        <div class="nav-group">
          <div class="nav-item" :class="{ on: isActive('admin-overview') }" @click="nav('admin-overview')">
            <span class="nav-ic"><app-icon name="grid" :size="19" /></span>
            <span>{{ t('route.adminOverview') }}</span>
          </div>
          <div class="nav-item" :class="{ on: isActive('review') }" @click="nav('review')">
            <span class="nav-ic"><app-icon name="shield" :size="19" /></span>
            <span>{{ t('route.review') }}</span>
            <span v-if="reviewStore.pendingCount" class="nav-count">{{ reviewStore.pendingCount }}</span>
          </div>
          <div class="nav-item" :class="{ on: isActive('review-history') }" @click="nav('review-history')">
            <span class="nav-ic"><app-icon name="receipt" :size="19" /></span>
            <span>{{ t('route.reviewHistory') }}</span>
          </div>
          <div class="nav-item" :class="{ on: isActive('stores') }" @click="nav('stores')">
            <span class="nav-ic"><app-icon name="home" :size="19" /></span>
            <span>{{ t('route.stores') }}</span>
            <span v-if="storeListStore.activeCount" class="nav-count">{{ storeListStore.activeCount }}</span>
          </div>
          <div class="nav-item" :class="{ on: isActive('members') }" @click="nav('members')">
            <span class="nav-ic"><app-icon name="users" :size="19" /></span>
            <span>{{ t('route.members') }}</span>
            <span v-if="memberListStore.activeCount" class="nav-count">{{ memberListStore.activeCount }}</span>
          </div>
          <div class="nav-item" :class="{ on: isActive('admin-products') }" @click="nav('admin-products')">
            <span class="nav-ic"><app-icon name="box" :size="19" /></span>
            <span>{{ t('route.adminProducts') }}</span>
          </div>
          <div class="nav-item" :class="{ on: isActive('catalog-categories') }" @click="nav('catalog-categories')">
            <span class="nav-ic"><app-icon name="tag" :size="19" /></span>
            <span>{{ t('route.catalogCategories') }}</span>
          </div>
          <div class="nav-item" :class="{ on: isActive('admin-orders') }" @click="nav('admin-orders')">
            <span class="nav-ic"><app-icon name="receipt" :size="19" /></span>
            <span>{{ t('route.adminOrders') }}</span>
          </div>
          <div class="nav-item" :class="{ on: isActive('resource-usage') }" @click="nav('resource-usage')">
            <span class="nav-ic"><app-icon name="layers" :size="19" /></span>
            <span>{{ t('route.resourceUsage') }}</span>
          </div>
          <div class="nav-item" :class="{ on: isActive('legal-documents') }" @click="nav('legal-documents')">
            <span class="nav-ic"><app-icon name="file" :size="19" /></span>
            <span>{{ t('route.legalDocs') }}</span>
          </div>
          <div class="nav-item" :class="{ on: isActive('faqs') }" @click="nav('faqs')">
            <span class="nav-ic"><app-icon name="book" :size="19" /></span>
            <span>{{ t('route.faqs') }}</span>
          </div>
          <div class="nav-item" :class="{ on: isActive('faq-categories') }" @click="nav('faq-categories')">
            <span class="nav-ic"><app-icon name="tag" :size="19" /></span>
            <span>{{ t('route.faqCategories') }}</span>
          </div>
          <div class="nav-item" :class="{ on: isActive('audit-log') }" @click="nav('audit-log')">
            <span class="nav-ic"><app-icon name="note" :size="19" /></span>
            <span>{{ t('route.auditLog') }}</span>
          </div>
        </div>
      </template>
      <template v-else-if="store.mode === 'sell'">
        <div class="nav-group">
          <div v-for="it in navSell" :key="it.view" class="nav-item" :class="{ on: isActive(it.view) }" @click="nav(it.view)">
            <span class="nav-ic"><app-icon :name="it.icon" :size="19" /></span>
            <span>{{ t(it.labelKey) }}</span>
            <span v-if="count(it.countKey) != null" class="nav-count">{{ count(it.countKey) }}</span>
          </div>
        </div>
      </template>
      <template v-else>
        <div class="nav-group">
          <div v-for="it in navBuy" :key="it.view" class="nav-item" :class="{ on: isActive(it.view) }" @click="nav(it.view)">
            <span class="nav-ic"><app-icon :name="it.icon" :size="19" /></span>
            <span>{{ t(it.labelKey) }}</span>
            <span v-if="count(it.countKey) != null" class="nav-count">{{ count(it.countKey) }}</span>
          </div>
        </div>
      </template>
    </nav>

    <div class="side-version">v{{ appVersion }}</div>
  </aside>
</template>
