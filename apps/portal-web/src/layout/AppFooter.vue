<script setup lang="ts">
/* ============================================================
   AppFooter — portal-web 全站頁尾
   品牌標誌 + 連結列 + 版權字樣。
   ============================================================ */
import { useI18n } from 'vue-i18n';
import BrandLogo from '@/components/BrandLogo.vue';
import { env } from '@/environment.js';

const { t } = useI18n();

/** 應用程式版本號（Vite build 時由 package.json 的 version 注入），併入頁尾版權列顯示。 */
const appVersion = __APP_VERSION__;

function goWorkspace() { window.location.href = env.WORKSPACE_PAGE_URL; }
</script>

<template>
  <footer class="mkt-foot">
    <BrandLogo />
    <nav class="mkt-foot-links" :aria-label="t('footer.links')">
      <a href="#" @click.prevent="goWorkspace">{{ t('footer.becomeCreator') }}</a>
      <router-link to="/about">{{ t('footer.about') }}</router-link>
      <router-link to="/faq">{{ t('footer.faq') }}</router-link>
      <router-link to="/privacy">{{ t('footer.privacy') }}</router-link>
      <router-link to="/terms">{{ t('footer.terms') }}</router-link>
    </nav>
    <div class="mkt-foot-copy">{{ t('footer.copyright') }} · v{{ appVersion }}</div>
  </footer>
</template>
