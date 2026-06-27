<script setup lang="ts">
import { useI18n } from 'vue-i18n';
import { env } from '@/environment';
import AppIcon from '@/components/app-icon';

const { t } = useI18n();
const goMarket = () => { window.location.href = env.MARKET_PAGE_URL; };
</script>

<template>
  <div class="page page-pad" :data-screen-label="t('notfound.screenLabel')">
    <div class="nf">
      <div class="nf-shapes" aria-hidden="true">
        <span class="shape s1"></span>
        <span class="shape s2"></span>
        <span class="shape s3"></span>
      </div>

      <p class="nf-code"><span class="hl hl-lime">4</span><span class="hl hl-pink">0</span><span class="hl hl-cyan">4</span></p>
      <i18n-t keypath="notfound.title" tag="h1" class="nf-title" scope="global">
        <template #market><span class="hl hl-pink">{{ t('notfound.market') }}</span></template>
      </i18n-t>
      <p class="nf-sub">{{ t('notfound.sub') }}</p>

      <div class="nf-actions">
        <n-button type="primary" size="large" strong @click="goMarket">
          <template #icon><app-icon name="arrowLeft" :size="18" /></template>
          {{ t('notfound.backToMarket') }}
        </n-button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.nf {
  position: relative;
  text-align: center;
  padding: 96px 20px 64px;
  overflow: hidden;
}
.nf-shapes { position: absolute; inset: 0; pointer-events: none; z-index: 0; }
.nf-shapes .shape {
  position: absolute; border-radius: 999px; filter: blur(6px); opacity: .5;
}
.nf-shapes .s1 { width: 120px; height: 120px; background: var(--c-violet); top: 8%; left: 12%; }
.nf-shapes .s2 { width: 84px; height: 84px; background: var(--c-pink); top: 22%; right: 14%; }
.nf-shapes .s3 { width: 64px; height: 64px; background: var(--c-cyan); bottom: 14%; left: 24%; }

.nf > :not(.nf-shapes) { position: relative; z-index: 1; }

.nf-code {
  font-family: var(--oj-display);
  font-size: clamp(64px, 14vw, 132px);
  font-weight: 800;
  letter-spacing: -2px;
  line-height: 1;
  margin: 0 0 8px;
}
.nf-code .hl { transform: rotate(-3deg); }
.nf-code .hl-pink { transform: rotate(4deg); }

.nf-title {
  font-family: var(--oj-display);
  font-size: clamp(28px, 4.6vw, 46px);
  font-weight: 800;
  line-height: 1.08;
  letter-spacing: -1px;
  margin: 0;
  color: var(--text);
  text-wrap: balance;
}
.nf-sub {
  color: var(--text-soft);
  margin: 18px auto 0;
  font-size: 16px;
  max-width: 42ch;
  line-height: 1.6;
}
.nf-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  justify-content: center;
  margin-top: 30px;
}
</style>
