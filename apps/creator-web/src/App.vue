<script setup lang="ts">
import { computed } from 'vue';
import AppNav from './layout/AppNav.vue';
import AppFooter from './layout/AppFooter.vue';
import { STORE } from './data/store';
import { useAuthStore } from '@/stores/auth';
import { useShopStore } from '@/stores/shop';

// 店面瀏覽器標題：Open Jam · <店名>（待串接後端後改由 API 取得）
document.title = `Open Jam · ${STORE.storeName}`;

// 消費者免註冊，但若已於其他 .openjam.co 子網域登入，靜默讀取 SSO session，
// 供「追蹤創作者」預填信箱、並載入該使用者的商品收藏（wishlist）。
useAuthStore().getUserIdentity().then(() => useShopStore().loadFavorites());

const naiveTheme = computed(() => null);
const overrides = computed(() => ({
  common: {
    primaryColor: '#8a5cf6',
    primaryColorHover: '#a685ff',
    primaryColorPressed: '#7a4ce0',
    primaryColorSuppl: '#a685ff',
    borderRadius: '14px',
    borderRadiusSmall: '10px',
    fontFamily: "'Noto Sans TC', sans-serif",
    fontWeightStrong: '900',
  },
  Button: { fontWeight: '900', borderRadiusMedium: '999px', borderRadiusLarge: '999px', borderRadiusSmall: '999px' },
  Input: {
    borderRadius: '12px',
    border: '2px solid var(--border-strong)',
    borderHover: '2px solid var(--border-strong)',
    borderFocus: '2px solid var(--border-strong)',
    borderError: '2px solid var(--c-pink-deep)',
    borderFocusError: '2px solid var(--c-pink-deep)',
    borderHoverError: '2px solid var(--c-pink-deep)',
    boxShadowFocus: 'none',
    boxShadowFocusError: 'none',
    color: 'var(--surface)',
    colorFocus: 'var(--surface)',
    caretColor: '#8a5cf6',
    heightMedium: '42px',
    heightLarge: '46px',
  },
  InternalSelection: {
    borderRadius: '12px',
    border: '2px solid var(--border-strong)',
    borderHover: '2px solid var(--border-strong)',
    borderActive: '2px solid var(--border-strong)',
    borderFocus: '2px solid var(--border-strong)',
    boxShadowActive: 'none',
    boxShadowFocus: 'none',
    color: 'var(--surface)',
    colorActive: 'var(--surface)',
    heightMedium: '42px',
  },
  Slider: {
    fillColor: '#8a5cf6',
    fillColorHover: '#a685ff',
    handleColor: '#ffffff',
    dotBorderActive: '2px solid #8a5cf6',
  },
  Switch: { railColorActive: '#8a5cf6' },
}));
</script>

<template>
  <n-config-provider :theme="naiveTheme" :theme-overrides="overrides">
  <n-message-provider>
    <div class="oj-root">

      <app-nav />

      <main>
        <router-view />
      </main>

      <app-footer />

    </div>
  </n-message-provider>
  </n-config-provider>
</template>
