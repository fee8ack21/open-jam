<script setup lang="ts">
/* ============================================================
   CreatorCtaBand — 頁尾前的「成為創作者」招募帶。
   主按鈕導向 workspace（開店入口）、次連結導向 /about。
   ============================================================ */
import { useI18n } from 'vue-i18n';
import { env } from '@/environment.js';

const { t } = useI18n();

function goWorkspace() {
  window.location.href = env.WORKSPACE_PAGE_URL;
}
</script>

<template>
  <section class="sec rv">
    <div class="cta-band">
      <span class="cta-shape cta-sq"></span>
      <span class="cta-shape cta-dot"></span>
      <h2 class="cta-title">{{ t('market.ctaBand.title') }}</h2>
      <p class="cta-sub">{{ t('market.ctaBand.sub') }}</p>
      <div class="cta-actions">
        <button type="button" class="cta-main" @click="goWorkspace">
          <app-icon name="sparkle" :size="17" /> {{ t('market.ctaBand.button') }}
        </button>
        <router-link class="cta-alt" to="/about">{{ t('market.ctaBand.more') }}</router-link>
      </div>
    </div>
  </section>
</template>
