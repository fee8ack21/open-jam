<script setup lang="ts">
/* ============================================================
   AppFooter — creator-web 店面全站頁尾（墨黑滿版帶，同 portal-web）
   果醬罐標誌 + 連結列（皆導向 portal-web 市集頁面）+ 版權字樣。
   ============================================================ */
import { useI18n } from 'vue-i18n';
import BrandLogo from '@/components/BrandLogo.vue';
import { env } from '@/environment';

const { t } = useI18n();

const portalUrl = (path = '') => `${env.PORTAL_PAGE_URL}${path}`;

/** 應用程式版本號（Vite build 時由 package.json 的 version 注入），併入頁尾版權列顯示。 */
const appVersion = __APP_VERSION__;
</script>

<template>
  <footer class="foot">
    <brand-logo inverse />
    <nav class="foot-links" :aria-label="t('footer.links')">
      <a :href="portalUrl()">{{ t('footer.marketplace') }}</a>
      <a :href="portalUrl('/about')">{{ t('footer.about') }}</a>
      <a :href="portalUrl('/faq')">{{ t('footer.faq') }}</a>
      <a :href="portalUrl('/privacy')">{{ t('footer.privacy') }}</a>
      <a :href="portalUrl('/terms')">{{ t('footer.terms') }}</a>
    </nav>
    <div class="foot-copy">{{ t('footer.copyright') }} · v{{ appVersion }}</div>
  </footer>
</template>
