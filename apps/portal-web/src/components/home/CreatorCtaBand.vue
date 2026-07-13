<script setup lang="ts">
/* ============================================================
   CreatorCtaBand — 頁尾前的「成為創作者」招募帶。
   主按鈕導向 workspace（開店入口）。
   ============================================================ */
import { useI18n } from 'vue-i18n';
import { env } from '@/environment.js';

const { t } = useI18n();

function goWorkspace() {
  window.location.href = env.WORKSPACE_PAGE_URL;
}
</script>

<template>
  <section class="sec cta-sec rv">
    <div class="cta-band">
      <span class="cta-shape cta-sq"></span>
      <span class="cta-shape cta-dot"></span>
      <span class="cta-shape cta-note-sq"><app-icon name="note" :size="30" /></span>
      <span class="cta-shape cta-note-dot"><app-icon name="beats" :size="24" /></span>
      <i18n-t keypath="market.ctaBand.title" tag="h2" class="cta-title" scope="global">
        <template #hl><span class="cta-hl">{{ t('market.ctaBand.titleHl') }}</span></template>
      </i18n-t>
      <p class="cta-sub">{{ t('market.ctaBand.sub') }}</p>
      <div class="cta-actions">
        <button type="button" class="cta-main" @click="goWorkspace">
          <app-icon name="note" :size="16" /> {{ t('market.ctaBand.button') }}
        </button>
      </div>
      <div class="cta-note">{{ t('market.ctaBand.note') }}</div>
    </div>
  </section>
</template>
