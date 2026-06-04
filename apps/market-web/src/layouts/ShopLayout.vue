<script>
/* ============================================================
   ShopLayout — storefront shell for /shop routes
   Sticky nav (search · cart · follow), tweaks panel, and the
   nested <router-view> for list / detail / checkout.
   ============================================================ */
import { ref, computed, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useShopStore } from '@/stores/shop.js';

export default {
  name: 'ShopLayout',
  setup() {
    const store = useShopStore();
    const route = useRoute();
    const router = useRouter();

    const tweaksOpen = ref(false);
    const followEmail = ref('');
    const subscribed = ref(false);
    const mobileSearchOpen = ref(false);
    const mobileFollowOpen = ref(false);

    const cartCount = computed(() => store.cartCount);
    const fontClass = computed(() => 'font-' + store.font);

    const onSearch = (v) => {
      store.search = v;
      if (route.name !== 'shop-list') router.push('/shop');
    };
    const goHome = () => router.push('/');
    const goCheckout = () => router.push('/shop/checkout');

    const subscribe = () => {
      const v = followEmail.value.trim();
      if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v)) return;
      subscribed.value = true;
      mobileFollowOpen.value = false;
    };
    const toggleSearch = () => {
      mobileSearchOpen.value = !mobileSearchOpen.value;
      mobileFollowOpen.value = false;
    };
    const toggleFollow = () => {
      mobileFollowOpen.value = !mobileFollowOpen.value;
      mobileSearchOpen.value = false;
    };
    const dismissTweaks = () => {
      tweaksOpen.value = false;
      window.parent.postMessage({ type: '__edit_mode_dismissed' }, '*');
    };

    onMounted(() => {
      // ---- hydrate filters from deep-link query params ----
      const q = route.query;
      if (q.category && ['music', 'photo', 'ebook'].includes(q.category)) store.setCategory(q.category);
      if (q.tag) store.toggleTag(q.tag);
      if (q.search) store.search = q.search;
      if (q.free === '1' || q.free === 'true') store.onlyFree = true;
      if (q.sort) store.sort = q.sort;

      // ---- host edit-mode (tweaks) bridge ----
      window.addEventListener('message', (e) => {
        const t = e && e.data && e.data.type;
        if (t === '__activate_edit_mode') tweaksOpen.value = true;
        else if (t === '__deactivate_edit_mode') tweaksOpen.value = false;
      });
      window.parent.postMessage({ type: '__edit_mode_available' }, '*');
    });

    return {
      store, tweaksOpen, dismissTweaks,
      followEmail, subscribed, subscribe,
      mobileSearchOpen, mobileFollowOpen, toggleSearch, toggleFollow,
      cartCount, fontClass, onSearch, goHome, goCheckout,
    };
  },
};
</script>

<template>
  <div class="oj-root" :class="[store.theme, fontClass]">
    <!-- ============ NAV ============ -->
    <header class="nav">
      <div class="nav-inner">
        <div class="brand" @click="goHome">
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
            <input
              class="search-input"
              type="text"
              :value="store.search"
              @input="onSearch($event.target.value)"
              placeholder="搜尋作品、創作者或標籤…"
              aria-label="搜尋"
            />
            <button v-if="store.search" class="search-clear" type="button" @click="onSearch('')" aria-label="清除">
              <j-icon name="close" :size="15" />
            </button>
          </div>
        </div>

        <div class="nav-spacer"></div>

        <div class="nav-actions">
          <div class="icon-btn nav-icon-toggle" :class="{ active: mobileSearchOpen }" @click="toggleSearch" title="搜尋">
            <j-icon name="search" :size="20" />
          </div>

          <div class="icon-btn" @click="goCheckout" title="購物車">
            <j-icon name="cart" :size="20" />
            <span v-if="cartCount" class="cart-badge">{{ cartCount }}</span>
          </div>

          <div
            v-if="!subscribed"
            class="icon-btn nav-icon-toggle"
            :class="{ active: mobileFollowOpen }"
            @click="toggleFollow"
            title="追蹤創作者"
          >
            <j-icon name="mail" :size="20" />
          </div>
        </div>

        <div class="nav-follow" :class="{ 'is-open': mobileFollowOpen }">
          <form v-if="!subscribed" class="follow-form" @submit.prevent="subscribe">
            <span class="follow-icon"><j-icon name="mail" :size="16" /></span>
            <input class="follow-input" type="email" v-model="followEmail" placeholder="輸入信箱，追蹤創作者" aria-label="訂閱信箱" />
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
      <router-view />
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
            <button :class="{ on: store.font === 'sora' }" @click="store.setFont('sora')">Bricolage</button>
            <button :class="{ on: store.font === 'grotesk' }" @click="store.setFont('grotesk')">Unbounded</button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
