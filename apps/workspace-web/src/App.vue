<script>
import { ref } from 'vue'
import { useDashboardStore } from '@/stores/dashboard'
import { useAuthStore } from '@/stores/auth'
import { JFmt } from '@/utils/format'
import { ME } from '@/data'

const NAV = {
  sell: [
    { view: 'overview', label: '儀表板', icon: 'grid' },
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
    return {
      store,
      authStore,
      tweaksOpen: ref(false),
      drawerOpen: ref(false),
      userMenuOpen: ref(false),
    }
  },
  data() {
    return {
      me: ME,
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
    navSell() { return NAV.sell },
    navBuy() { return NAV.buy },
    mobileNav() {
      return [
        { view: 'overview', label: '總覽', icon: 'grid' },
        { view: 'products', label: '商品', icon: 'box' },
        { view: 'upload', label: '上架', icon: 'plus' },
        { view: 'purchases', label: '已購', icon: 'bag' },
        { view: 'settings', label: '設定', icon: 'gear' },
      ]
    },
  },
  watch: {
    '$route.name': {
      immediate: true,
      handler(name) { if (name) this.store.syncModeToRoute(name) },
    },
  },
  mounted() {
    this._outside = (e) => {
      if (this.userMenuOpen && !(e.target.closest && e.target.closest('.user-menu'))) this.userMenuOpen = false
    }
    document.addEventListener('click', this._outside)
  },
  beforeUnmount() {
    document.removeEventListener('click', this._outside)
  },
  methods: {
    count(key) {
      if (!key) return null
      const map = {
        products: this.store.products.length,
        orders: this.store.paidOrders.length,
        purchases: this.store.purchases.length,
        wishlist: this.store.wishlist.length,
      }
      return map[key]
    },
    initials(name) { return JFmt.initials(name) },
    nav(view) { this.store.go(view); this.drawerOpen = false },
    pickMode(m) { this.store.setMode(m) },
    isActive(view) { return this.$route.name === view },
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
            <button :class="{ on: store.mode === 'sell' }" @click="pickMode('sell')"><j-icon name="rocket" :size="15" /> 賣家</button>
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
              <button class="icon-btn" title="外觀設定" @click="tweaksOpen = !tweaksOpen">
                <j-icon name="sliders" :size="20" />
              </button>
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
                      <span class="avatar" :style="{ background: me.avatar }">{{ initials(authStore.userName ?? me.name) }}</span>
                      <div style="min-width:0;">
                        <div class="um-name">{{ authStore.userName ?? me.name }}</div>
                        <div class="um-handle">{{ authStore.userEmail ?? me.handle }}</div>
                      </div>
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

      <!-- ============ TWEAKS ============ -->
      <div v-show="tweaksOpen" class="tweaks-panel">
        <div class="tweaks-head">
          <span>Tweaks</span>
          <button class="tweaks-x" @click="tweaksOpen = false"><j-icon name="close" :size="16" /></button>
        </div>
        <div class="tweaks-body">
          <div class="tweaks-section">外觀</div>
          <div class="tweaks-row" style="margin-bottom:12px;">
            <span>版面密度</span>
            <div class="seg">
              <button :class="{ on: store.density === 'comfy' }" @click="store.setDensity('comfy')">寬鬆</button>
              <button :class="{ on: store.density === 'compact' }" @click="store.setDensity('compact')">緊湊</button>
            </div>
          </div>
          <div class="tweaks-row">
            <span>標題字型</span>
            <div class="seg">
              <button :class="{ on: store.font === 'sora' }" @click="store.setFont('sora')">Bricolage</button>
              <button :class="{ on: store.font === 'grotesk' }" @click="store.setFont('grotesk')">Unbounded</button>
            </div>
          </div>
        </div>
      </div>

    </div>
  </n-message-provider>
  </n-config-provider>
</template>
