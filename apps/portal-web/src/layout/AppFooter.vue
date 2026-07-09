<script setup lang="ts">
/* ============================================================
   AppFooter — portal-web 全站頁尾
   品牌標誌 + 連結列 + 版權字樣。
   ============================================================ */
import { useI18n } from 'vue-i18n';
import BrandLogo from '@/components/BrandLogo.vue';
import { env } from '@/environment.js';

const { t } = useI18n();

function goWorkspace() { window.location.href = env.WORKSPACE_PAGE_URL; }
</script>

<template>
  <footer class="mkt-foot">
    <BrandLogo />
    <nav class="mkt-foot-links" :aria-label="t('footer.links')">
      <a href="#" @click.prevent="goWorkspace">{{ t('footer.becomeCreator') }}</a>
      <router-link to="/privacy">{{ t('footer.privacy') }}</router-link>
      <router-link to="/terms">{{ t('footer.terms') }}</router-link>
    </nav>
    <div class="mkt-foot-copy">{{ t('footer.copyright') }}</div>
  </footer>
</template>
