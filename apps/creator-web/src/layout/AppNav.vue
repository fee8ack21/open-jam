<script setup lang="ts">
import { computed, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useShopStore } from '@/stores/shop';
import AppIcon from '@/components/app-icon';

const store = useShopStore();
const route = useRoute();
const router = useRouter();

const followEmail = ref('');
const subscribed = ref(false);
const mobileSearchOpen = ref(false);
const mobileFollowOpen = ref(false);

const cartCount = computed(() => store.cartCount);
// 404 等頁面只顯示 Logo，隱藏搜尋／購物車／追蹤等互動欄位
const minimal = computed(() => route.name === 'not-found');

const subscribe = () => {
  const v = followEmail.value.trim();
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v)) return;
  subscribed.value = true;
  mobileFollowOpen.value = false;
};
const toggleSearch = () => { mobileSearchOpen.value = !mobileSearchOpen.value; mobileFollowOpen.value = false; };
const toggleFollow = () => { mobileFollowOpen.value = !mobileFollowOpen.value; mobileSearchOpen.value = false; };

const goList = () => { if (route.name !== 'list') router.push({ name: 'list' }); };
const goCheckout = () => router.push({ name: 'checkout' });
const onSearch = (v: string) => {
  store.search = v;
  if (route.name !== 'list') router.push({ name: 'list' });
};
</script>

<template>
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

      <div v-if="!minimal" class="nav-search" :class="{ 'is-open': mobileSearchOpen }">
        <div class="search-box">
          <span class="follow-icon"><app-icon name="search" :size="17" /></span>
          <input class="search-input" type="text" :value="store.search"
                 @input="onSearch(($event.target as HTMLInputElement).value)"
                 placeholder="搜尋作品、創作者或標籤…" aria-label="搜尋" />
          <button v-if="store.search" class="search-clear" type="button"
                  @click="onSearch('')" aria-label="清除"><app-icon name="close" :size="15" /></button>
        </div>
      </div>

      <div class="nav-spacer"></div>

      <div v-if="!minimal" class="nav-actions">
        <div class="icon-btn nav-icon-toggle" :class="{ active: mobileSearchOpen }"
             @click="toggleSearch" title="搜尋">
          <app-icon name="search" :size="20" />
        </div>

        <div class="icon-btn" @click="goCheckout" title="購物車">
          <app-icon name="cart" :size="20" />
          <span v-if="cartCount" class="cart-badge">{{ cartCount }}</span>
        </div>

        <div v-if="!subscribed" class="icon-btn nav-icon-toggle" :class="{ active: mobileFollowOpen }"
             @click="toggleFollow" title="追蹤創作者">
          <app-icon name="mail" :size="20" />
        </div>
      </div>

      <div v-if="!minimal" class="nav-follow" :class="{ 'is-open': mobileFollowOpen }">
        <form v-if="!subscribed" class="follow-form" @submit.prevent="subscribe">
          <span class="follow-icon"><app-icon name="mail" :size="16" /></span>
          <input class="follow-input" type="email" v-model="followEmail"
                 placeholder="輸入信箱，追蹤創作者" aria-label="訂閱信箱" />
          <button class="follow-btn" type="submit">追蹤</button>
        </form>
        <div v-else class="follow-done">
          <app-icon name="check" :size="16" /> 已追蹤
        </div>
      </div>
    </div>
  </header>
</template>
