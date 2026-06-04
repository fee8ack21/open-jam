/* ============================================================
   June — root app: nav, theme provider, routing, tweaks panel
   ============================================================ */
(function () {
  const { computed, onMounted, ref } = Vue;

  const App = {
    name: 'App',
    components: {
      JIcon: window.JIcon,
      ListView: window.ListView,
      DetailView: window.DetailView,
      CheckoutView: window.CheckoutView,
    },
    setup() {
      const store = window.useStore();
      const tweaksOpen = ref(false);

      const naiveTheme = computed(() => null);
      const overrides = computed(() => ({
        common: {
          primaryColor: '#6c4cf1',
          primaryColorHover: '#8a72ff',
          primaryColorPressed: '#5638d8',
          primaryColorSuppl: '#8a72ff',
          borderRadius: '13px',
          borderRadiusSmall: '9px',
          fontFamily: "'Space Grotesk', 'Noto Sans TC', sans-serif",
          fontWeightStrong: '700',
        },
        Button: { fontWeight: '700' },
        Input: {
          borderRadius: '12px',
          border: '1.5px solid var(--border-strong)',
          borderHover: '1.5px solid var(--c-violet)',
          borderFocus: '1.5px solid var(--c-violet)',
          borderError: '1.5px solid var(--c-pink)',
          borderFocusError: '1.5px solid var(--c-pink)',
          borderHoverError: '1.5px solid var(--c-pink)',
          boxShadowFocus: 'none',
          boxShadowFocusError: 'none',
          color: 'var(--surface)',
          colorFocus: 'var(--surface)',
          caretColor: '#6c4cf1',
          heightMedium: '42px',
          heightLarge: '46px',
        },
        InternalSelection: {
          borderRadius: '12px',
          border: '1.5px solid var(--border-strong)',
          borderHover: '1.5px solid var(--c-violet)',
          borderActive: '1.5px solid var(--c-violet)',
          borderFocus: '1.5px solid var(--c-violet)',
          boxShadowActive: 'none',
          boxShadowFocus: 'none',
          color: 'var(--surface)',
          colorActive: 'var(--surface)',
          heightMedium: '42px',
        },
        Slider: {
          fillColor: '#6c4cf1',
          fillColorHover: '#8a72ff',
          handleColor: '#ffffff',
          dotBorderActive: '1.5px solid #6c4cf1',
        },
        Switch: { railColorActive: '#6c4cf1' },
      }));

      onMounted(() => {
        // ---- deep-link entry from the marketplace hub ----
        try {
          const params = new URLSearchParams(location.search);
          const cat = params.get('category');
          const q = params.get('search');
          const creator = params.get('creator');
          const tag = params.get('tag');
          const free = params.get('free');
          const sort = params.get('sort');
          const pid = params.get('product');
          if (cat && ['music', 'photo', 'ebook'].includes(cat)) store.setCategory(cat);
          if (tag) store.toggleTag(tag);
          if (q) store.state.search = q;
          if (creator) {
            const c = window.JUNE_PRODUCTS.find(p => p.handle === creator);
            store.state.search = c ? c.creator : creator;
          }
          if (free === '1' || free === 'true') store.state.onlyFree = true;
          if (sort) store.state.sort = sort;
          if (pid && window.JUNE_PRODUCTS.some(p => p.id === pid)) store.openProduct(pid);
        } catch (e) { /* no-op */ }

        window.addEventListener('message', (e) => {
          const t = e && e.data && e.data.type;
          if (t === '__activate_edit_mode') tweaksOpen.value = true;
          else if (t === '__deactivate_edit_mode') tweaksOpen.value = false;
        });
        window.parent.postMessage({ type: '__edit_mode_available' }, '*');
      });

      const dismissTweaks = () => {
        tweaksOpen.value = false;
        window.parent.postMessage({ type: '__edit_mode_dismissed' }, '*');
      };

      const currentView = computed(() => ({
        list: 'list-view', detail: 'detail-view', checkout: 'checkout-view',
      })[store.state.view]);

      // ---- follow-the-creator email subscription ----
      const followEmail = ref('');
      const subscribed = ref(false);
      const subscribe = () => {
        const v = followEmail.value.trim();
        if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v)) return;
        subscribed.value = true;
        mobileFollowOpen.value = false;
      };

      // ---- mobile: collapse search & follow into icon toggles ----
      const mobileSearchOpen = ref(false);
      const mobileFollowOpen = ref(false);
      const toggleSearch = () => { mobileSearchOpen.value = !mobileSearchOpen.value; mobileFollowOpen.value = false; };
      const toggleFollow = () => { mobileFollowOpen.value = !mobileFollowOpen.value; mobileSearchOpen.value = false; };

      return { store, naiveTheme, overrides, currentView, tweaksOpen, dismissTweaks,
               followEmail, subscribed, subscribe,
               mobileSearchOpen, mobileFollowOpen, toggleSearch, toggleFollow };
    },
    computed: {
      s() { return this.store.state; },
      cartCount() { return this.store.getters.cartCount; },
      fontClass() { return 'font-' + this.s.font; },
    },
    methods: {
      onSearch(v) {
        this.s.search = v;
        if (this.s.view !== 'list') this.store.goList();
      },
    },
    template: `
      <n-config-provider :theme="naiveTheme" :theme-overrides="overrides">
      <n-message-provider>
        <div class="june-root" :class="['light', fontClass]">

          <!-- ============ NAV ============ -->
          <header class="nav">
            <div class="nav-inner">
              <div class="brand" @click="store.goList()">
                <span class="brand-mark">
                  <svg width="19" height="19" viewBox="0 0 24 24" fill="none">
                    <path d="M15 16.4V4.5c3.7 1 5 3.9 2 6.8" stroke="#fff" stroke-width="2.3" stroke-linecap="round" stroke-linejoin="round" fill="none"></path>
                    <ellipse cx="10.4" cy="16.8" rx="4.7" ry="3.5" fill="#fff" transform="rotate(-22 10.4 16.8)"></ellipse>
                    
                  </svg>
                </span>
                <span class="brand-name">Open Jam</span>
              </div>

              <div class="nav-search" :class="{ 'is-open': mobileSearchOpen }">
                <div class="search-box">
                  <span class="follow-icon"><j-icon name="search" :size="17" /></span>
                  <input class="search-input" type="text" :value="s.search"
                         @input="onSearch($event.target.value)"
                         placeholder="搜尋作品、創作者或標籤…" aria-label="搜尋" />
                  <button v-if="s.search" class="search-clear" type="button"
                          @click="onSearch('')" aria-label="清除"><j-icon name="close" :size="15" /></button>
                </div>
              </div>

              <div class="nav-spacer"></div>

              <div class="nav-actions">
                <div class="icon-btn nav-icon-toggle" :class="{ active: mobileSearchOpen }"
                     @click="toggleSearch" title="搜尋">
                  <j-icon name="search" :size="20" />
                </div>

                <div class="icon-btn" @click="store.goCheckout()" title="購物車">
                  <j-icon name="cart" :size="20" />
                  <span v-if="cartCount" class="cart-badge">{{ cartCount }}</span>
                </div>

                <div v-if="!subscribed" class="icon-btn nav-icon-toggle" :class="{ active: mobileFollowOpen }"
                     @click="toggleFollow" title="追蹤創作者">
                  <j-icon name="mail" :size="20" />
                </div>
              </div>

              <div class="nav-follow" :class="{ 'is-open': mobileFollowOpen }">
                <form v-if="!subscribed" class="follow-form" @submit.prevent="subscribe">
                  <span class="follow-icon"><j-icon name="mail" :size="16" /></span>
                  <input class="follow-input" type="email" v-model="followEmail"
                         placeholder="輸入信箱，追蹤創作者" aria-label="訂閱信箱" />
                  <button class="follow-btn" type="submit">追蹤</button>
                </form>
                <div v-else class="follow-done">
                  <j-icon name="check" :size="16" /> 已追蹤
                </div>
              </div>
            </div>
          </header>

          <!-- ============ ROUTED VIEW ============ -->
          <main>
            <component :is="currentView" />
          </main>

          <!-- ============ TWEAKS PANEL ============ -->
          <div v-show="tweaksOpen" class="tweaks-panel">
            <div class="tweaks-head">
              <span>Tweaks</span>
              <button class="tweaks-x" @click="dismissTweaks"><j-icon name="close" :size="16" /></button>
            </div>
            <div class="tweaks-body">
              <div class="tweaks-section">展示字體</div>
              <div class="tweaks-row">
                <span>標題字型</span>
                <div class="seg">
                  <button :class="{ on: s.font === 'sora' }" @click="store.setFont('sora')">Bricolage</button>
                  <button :class="{ on: s.font === 'grotesk' }" @click="store.setFont('grotesk')">Unbounded</button>
                </div>
              </div>
            </div>
          </div>

        </div>
      </n-message-provider>
      </n-config-provider>`,
  };

  const app = Vue.createApp(App);
  app.use(naive);
  // register shared components globally
  app.component('j-icon', window.JIcon);
  app.component('product-thumb', window.ProductThumb);
  app.component('stars', window.Stars);
  app.component('product-card', window.ProductCard);
  app.component('list-view', window.ListView);
  app.component('detail-view', window.DetailView);
  app.component('checkout-view', window.CheckoutView);
  app.mount('#app');
})();
