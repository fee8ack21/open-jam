<script setup>
/* App root — Naive UI providers wrap the router outlet so
   message / dialog hooks are available app-wide. */
import { useAuthStore } from '@/stores/auth.js';

const themeOverrides = {
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
    </n-message-provider>
  </n-config-provider>
</template>
