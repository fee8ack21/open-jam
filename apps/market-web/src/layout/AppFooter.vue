<script setup lang="ts">
/* ============================================================
   AppFooter — market-web 全站頁尾
   品牌標誌 + 連結列 + 版權字樣。
   ============================================================ */
import { useI18n } from 'vue-i18n';
import { env } from '@/environment.js';

const { t } = useI18n();

function goWorkspace() { window.location.href = env.WORKSPACE_PAGE_URL; }
</script>

<template>
  <footer class="mkt-foot">
    <router-link class="brand" to="/">
      <span class="brand-mark">
        <svg width="19" height="19" viewBox="0 0 24 24" fill="none">
          <path d="M15 16.4V4.5c3.7 1 5 3.9 2 6.8" stroke="#fff" stroke-width="2.3" stroke-linecap="round" stroke-linejoin="round" fill="none"></path>
          <ellipse cx="10.4" cy="16.8" rx="4.7" ry="3.5" fill="#fff" transform="rotate(-22 10.4 16.8)"></ellipse>
        </svg>
      </span>
      <span class="brand-name">Open Jam</span>
    </router-link>
    <nav class="mkt-foot-links" :aria-label="t('footer.links')">
      <router-link to="/about">{{ t('footer.about') }}</router-link>
      <a href="#" @click.prevent="goWorkspace">{{ t('footer.becomeCreator') }}</a>
      <router-link to="/privacy">{{ t('footer.privacy') }}</router-link>
      <router-link to="/terms">{{ t('footer.terms') }}</router-link>
    </nav>
    <div class="mkt-foot-copy">{{ t('footer.copyright') }}</div>
  </footer>
</template>
