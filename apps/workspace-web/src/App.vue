<script lang="ts">
import { ref } from 'vue'
import { useDashboardStore } from '@/stores/dashboard'
import { useAuthStore } from '@/stores/auth'
import { useStoreApplicationStore } from '@/stores/storeApplication'
import { ME } from '@/data'

const NAV = {
  sell: [
    { view: 'overview', label: '儀表板', icon: 'grid' },
    { view: 'open-store', label: '開店', icon: 'rocket' },
    { view: 'products', label: '商品管理', icon: 'box', countKey: 'products' },
    { view: 'upload', label: '上架新作品', icon: 'upload' },
    { view: 'orders', label: '訂單管理', icon: 'receipt', countKey: 'orders' },
  ],
  buy: [
    { view: 'purchases', label: '購買紀錄', icon: 'bag', countKey: 'purchases' },
    { view: 'wishlist', label: 'Wishlist', icon: 'heart', countKey: 'wishlist' },
  ],
}

export default {
  name: 'App',
  setup() {
    const store = useDashboardStore()
    const authStore = useAuthStore()
    const storeAppStore = useStoreApplicationStore()
    return {
      store,
      authStore,
      storeAppStore,
      drawerOpen: ref(false),
      userMenuOpen: ref(false),
    }
  },
  data() {
    return {
      me: ME,
      _outside: null as ((e: MouseEvent) => void) | null,
      overrides: {
        common: {
          primaryColor: '#5639d6', primaryColorHover: '#7a63ee',
          primaryColorPressed: '#4a30bd', primaryColorSuppl: '#7a63ee',
          borderRadius: '12px', borderRadiusSmall: '9px',
          fontFamily: "'Space Grotesk', 'Noto Sans TC', sans-serif",
          fontWeightStrong: '700',
        },
        Button: { fontWeight: '700' },
        Input: { borderRadius: '12px', heightMedium: '42px', heightLarge: '46px', caretColor: '#6c4cf1' },
        InternalSelection: { borderRadius: '12px', heightMedium: '42px' },
        Switch: { railColorActive: '#5639d6' },
        Popover: { borderRadius: '14px', padding: '6px' },
      },
    }
  },
  computed: {
    s() { return this.store },
    rootClass() {
      return ['light', 'dash-theme', 'font-' + this.store.font, this.store.density === 'compact' ? 'is-compact' : '']
    },
    pageTitle() { return this.$route.meta.title || '' },
    /** 用戶選單顯示的信箱；access token 僅提供 email，無名稱可用。 */
    accountEmail() { return this.authStore.userEmail ?? this.me.email },
    /** 頭像字母：信箱首字母大寫。 */
    avatarText() { return (this.accountEmail.charAt(0) || '?').toUpperCase() },
    /** 是否為一般使用者：唯一擁有賣家/上架流程的角色。 */
    canSell() { return this.authStore.isUser },
    /** 是否已開店：未開店前賣家選單只露出「開店」。 */
    hasStore() { return this.storeAppStore.hasStore },
    navSell() {
      // 尚未開店：賣家選單只顯示「開店」，開店成功後才顯示其餘項目
      if (!this.hasStore) return NAV.sell.filter(it => it.view === 'open-store')
      return NAV.sell
    },
    navBuy() { return NAV.buy },
    mobileNav() {
      const buy = { view: 'purchases', label: '已購', icon: 'bag' }
      const settings = { view: 'settings', label: '設定', icon: 'gear' }
      if (!this.canSell) {
        return [buy, { view: 'wishlist', label: '收藏', icon: 'heart' }, settings]
      }
      if (!this.hasStore) {
        return [{ view: 'open-store', label: '開店', icon: 'rocket' }, buy, settings]
      }
      return [
        { view: 'overview', label: '總覽', icon: 'grid' },
        { view: 'products', label: '商品', icon: 'box' },
        { view: 'upload', label: '上架', icon: 'plus' },
        buy,
        settings,
      ]
    },
  },
  watch: {
    '$route.name': {
      immediate: true,
      handler(name) { if (name) this.store.syncModeToRoute(name) },
    },
    // 非賣家角色不應停留在賣家模式
    canSell: {
      immediate: true,
      handler(can) { if (!can && this.store.mode === 'sell') this.store.setMode('buy') },
    },
  },
  mounted() {
    this._outside = (e: MouseEvent) => {
      const t = e.target as Element | null
      if (this.userMenuOpen && !(t && t.closest && t.closest('.user-menu'))) this.userMenuOpen = false
    }
    document.addEventListener('click', this._outside)
  },
  beforeUnmount() {
    if (this._outside) document.removeEventListener('click', this._outside)
  },
  methods: {
    count(key?: string) {
      if (!key) return null
      const map: Record<string, number> = {
        products: this.store.products.length,
        orders: this.store.paidOrders.length,
        purchases: this.store.purchases.length,
        wishlist: this.store.wishlist.length,
      }
      return map[key]
    },
    nav(view: string) { this.store.go(view); this.drawerOpen = false },
    pickMode(m: string) { this.store.setMode(m) },
    isActive(view: string) { return this.$route.name === view },
  },
}
</script>

<template>
  <n-config-provider :theme="null" :theme-overrides="overrides">
  <n-message-provider>
    <div class="oj-root" :class="rootClass">
      <div class="dash-shell">

        <!-- ============ SIDEBAR / MOBILE DRAWER ============ -->
        <aside class="side" :class="{ 'drawer-open': drawerOpen }">
          <div class="side-brand" @click="nav('overview')">
            <span class="brand-mark">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
                <path d="M15 16.4V4.5c3.7 1 5 3.9 2 6.8" stroke="#fff" stroke-width="2.3" stroke-linecap="round" stroke-linejoin="round"></path>
                <ellipse cx="10.4" cy="16.8" rx="4.7" ry="3.5" fill="#fff" transform="rotate(-22 10.4 16.8)"></ellipse>
              </svg>
            </span>
            <span class="brand-name">Open Jam<small>Creator Studio</small></span>
          </div>

          <div class="mode-switch">
            <button v-if="canSell" :class="{ on: store.mode === 'sell' }" @click="pickMode('sell')"><j-icon name="rocket" :size="15" /> 賣家</button>
            <button :class="{ on: store.mode === 'buy' }" @click="pickMode('buy')"><j-icon name="bag" :size="15" /> 買家</button>
          </div>

          <nav style="flex:1; overflow-y:auto;">
            <template v-if="store.mode === 'sell'">
              <div class="nav-group">
                <div class="nav-label">賣家工作室</div>
                <div v-for="it in navSell" :key="it.view" class="nav-item" :class="{ on: isActive(it.view) }" @click="nav(it.view)">
                  <span class="nav-ic"><j-icon :name="it.icon" :size="19" /></span>
                  <span>{{ it.label }}</span>
                  <span v-if="count(it.countKey) != null" class="nav-count">{{ count(it.countKey) }}</span>
                </div>
              </div>
            </template>
            <template v-else>
              <div class="nav-group">
                <div class="nav-label">我的收藏庫</div>
                <div v-for="it in navBuy" :key="it.view" class="nav-item" :class="{ on: isActive(it.view) }" @click="nav(it.view)">
                  <span class="nav-ic"><j-icon :name="it.icon" :size="19" /></span>
                  <span>{{ it.label }}</span>
                  <span v-if="count(it.countKey) != null" class="nav-count">{{ count(it.countKey) }}</span>
                </div>
              </div>
            </template>
          </nav>
        </aside>

        <!-- mobile drawer scrim -->
        <div class="drawer-scrim" :class="{ show: drawerOpen }" @click="drawerOpen = false"></div>

        <!-- ============ MAIN ============ -->
        <div class="dash-main">
          <header class="topbar">
            <button class="mobile-menu icon-btn" @click="drawerOpen = true" title="開啟選單">
              <j-icon name="menu" :size="22" />
            </button>
            <h2 class="tb-title">{{ pageTitle }}</h2>

            <div class="tb-spacer"></div>

            <div class="tb-actions">
              <div class="icon-btn" title="通知">
                <j-icon name="bell" :size="20" />
                <span class="cart-badge">3</span>
              </div>
              <div class="user-menu">
                <button class="icon-btn user-trigger" :class="{ on: userMenuOpen }" @click="userMenuOpen = !userMenuOpen" title="選單">
                  <j-icon name="menu" :size="20" />
                </button>
                <transition name="um">
                  <div v-if="userMenuOpen" class="user-pop">
                    <div class="um-head">
                      <span class="avatar" :style="{ background: me.avatar }">{{ avatarText }}</span>
                      <div class="um-email" :title="accountEmail">{{ accountEmail }}</div>
                    </div>
                    <div class="um-sep"></div>
                    <button class="um-item" @click="nav('settings'); userMenuOpen = false"><j-icon name="gear" :size="17" /> 帳號設定</button>
                    <button class="um-item danger" @click="authStore.logout()"><j-icon name="logout" :size="17" /> 登出</button>
                  </div>
                </transition>
              </div>
            </div>
          </header>

          <main class="dash-body">
            <router-view />
          </main>
        </div>

      </div>

      <!-- ============ MOBILE BOTTOM NAV ============ -->
      <nav class="bottom-nav">
        <button v-for="it in mobileNav" :key="it.view" class="bn-item" :class="{ on: isActive(it.view) }"
                @click="nav(it.view)">
          <j-icon :name="it.icon" :size="21" />
          {{ it.label }}
        </button>
      </nav>

    </div>
  </n-message-provider>
  </n-config-provider>
</template>
