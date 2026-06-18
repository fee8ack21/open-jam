<script setup lang="ts">
import { computed } from 'vue';
import AppNav from './layout/AppNav.vue';
import { STORE } from './data/store';

// 店面瀏覽器標題：Open Jam · <店名>（待串接後端後改由 API 取得）
document.title = `Open Jam · ${STORE.storeName}`;

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
</script>

<template>
  <n-config-provider :theme="naiveTheme" :theme-overrides="overrides">
  <n-message-provider>
    <div class="oj-root light">

      <app-nav />

      <main>
        <router-view />
      </main>

    </div>
  </n-message-provider>
  </n-config-provider>
</template>
