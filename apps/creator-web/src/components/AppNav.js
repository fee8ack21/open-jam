/* ============================================================
   Open Jam — AppNav: sticky top navigation
   brand · search · cart · follow-the-creator subscription
   collapses search + follow into icon toggles on mobile
   ============================================================ */
import { ref } from 'vue';
import { useShopStore } from '../stores/shop.js';
import JIcon from './JIcon.js';

export default {
  name: 'AppNav',
  components: { JIcon },
  setup() {
    const store = useShopStore();

    // follow-the-creator email subscription
    const followEmail = ref('');
    const subscribed = ref(false);

    // mobile: collapse search & follow into icon toggles
    const mobileSearchOpen = ref(false);
    const mobileFollowOpen = ref(false);

    const subscribe = () => {
      const v = followEmail.value.trim();
      if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v)) return;
      subscribed.value = true;
      mobileFollowOpen.value = false;
    };
    const toggleSearch = () => { mobileSearchOpen.value = !mobileSearchOpen.value; mobileFollowOpen.value = false; };
    const toggleFollow = () => { mobileFollowOpen.value = !mobileFollowOpen.value; mobileSearchOpen.value = false; };

    return { store, followEmail, subscribed, subscribe, mobileSearchOpen, mobileFollowOpen, toggleSearch, toggleFollow };
  },
  computed: {
    cartCount() { return this.store.cartCount; },
  },
  methods: {
    goList() { if (this.$route.name !== 'list') this.$router.push({ name: 'list' }); },
    goCheckout() { this.$router.push({ name: 'checkout' }); },
    onSearch(v) {
      this.store.search = v;
      if (this.$route.name !== 'list') this.$router.push({ name: 'list' });
    },
  },
  template: `
    <header class="nav">
      <div class="nav-inner">
        <div class="brand" @click="goList">
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
            <input class="search-input" type="text" :value="store.search"
                   @input="onSearch($event.target.value)"
                   placeholder="搜尋作品、創作者或標籤…" aria-label="搜尋" />
            <button v-if="store.search" class="search-clear" type="button"
                    @click="onSearch('')" aria-label="清除"><j-icon name="close" :size="15" /></button>
          </div>
        </div>

        <div class="nav-spacer"></div>

        <div class="nav-actions">
          <div class="icon-btn nav-icon-toggle" :class="{ active: mobileSearchOpen }"
               @click="toggleSearch" title="搜尋">
            <j-icon name="search" :size="20" />
          </div>

          <div class="icon-btn" @click="goCheckout" title="購物車">
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
    </header>`,
};
