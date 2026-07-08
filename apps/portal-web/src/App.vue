<script setup lang="ts">
/* App root — Naive UI providers wrap the router outlet so
   message / dialog hooks are available app-wide. */
import type { GlobalThemeOverrides } from 'naive-ui';
import AppCursor from '@/components/AppCursor.vue';
import { useAuthStore } from '@/stores/auth.js';

const themeOverrides: GlobalThemeOverrides = {
  common: {
    primaryColor: '#6151f0',
    primaryColorHover: '#7264f5',
    primaryColorPressed: '#5040d8',
    borderRadius: '12px',
    fontFamily: 'var(--oj-font)',
  },
};

useAuthStore().getUserIdentity();
</script>

<template>
  <n-config-provider :theme-overrides="themeOverrides">
    <n-message-provider>
      <router-view />
      <AppCursor />
    </n-message-provider>
  </n-config-provider>
</template>
