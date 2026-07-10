<script setup lang="ts">
/* ============================================================
   AppFooter — creator-web 店面全站頁尾（內容參考 portal-web）
   品牌標誌 + 連結列（皆導向 portal-web 市集頁面）+ 版權字樣。
   ============================================================ */
import { useI18n } from 'vue-i18n';
import { env } from '@/environment';

const { t } = useI18n();

const portalUrl = (path = '') => `${env.PORTAL_PAGE_URL}${path}`;
</script>

<template>
  <footer class="foot">
    <a class="brand" :href="portalUrl()" aria-label="Open Jam">
      <span class="brand-mark" aria-hidden="true">
        <svg width="19" height="19" viewBox="0 0 24 24" fill="none">
          <path d="M15 16.4V4.5c3.7 1 5 3.9 2 6.8" stroke="#fff" stroke-width="2.3" stroke-linecap="round" stroke-linejoin="round" fill="none"></path>
          <ellipse cx="10.4" cy="16.8" rx="4.7" ry="3.5" fill="#fff" transform="rotate(-22 10.4 16.8)"></ellipse>
        </svg>
      </span>
      <span class="brand-name">Open Jam</span>
    </a>
    <nav class="foot-links" :aria-label="t('footer.links')">
      <a :href="portalUrl()">{{ t('footer.marketplace') }}</a>
      <a :href="portalUrl('/about')">{{ t('footer.about') }}</a>
      <a :href="portalUrl('/faq')">{{ t('footer.faq') }}</a>
      <a :href="portalUrl('/privacy')">{{ t('footer.privacy') }}</a>
      <a :href="portalUrl('/terms')">{{ t('footer.terms') }}</a>
    </nav>
    <div class="foot-copy">{{ t('footer.copyright') }}</div>
  </footer>
</template>
