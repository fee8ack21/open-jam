<script setup lang="ts">
/* ============================================================
   NotFoundView — 404 頁（router catch-all）
   紫色圓點滿版舞台：貼紙數字 4「旋轉唱片」4 + 雙 CTA +
   手寫註記，上下接全站 nav / footer。
   ============================================================ */
import { useI18n } from 'vue-i18n';
import { useShopStore } from '@/stores/shop.js';
import AppNav from '@/layout/AppNav.vue';
import AppFooter from '@/layout/AppFooter.vue';

const store = useShopStore();
const { t } = useI18n();
</script>

<template>
  <div class="oj-root nf-root" :class="'font-' + store.font" :data-screen-label="t('notFound.screenLabel')">
    <app-nav />

    <main class="nf-main">
      <!-- 漂浮貼紙 -->
      <i class="nf-deco nf-deco-sq" aria-hidden="true"></i>
      <i class="nf-deco nf-deco-dot" aria-hidden="true"></i>
      <span class="nf-deco nf-deco-n1" aria-hidden="true"><app-icon name="note" :size="34" /></span>
      <span class="nf-deco nf-deco-n2" aria-hidden="true"><app-icon name="beats" :size="28" /></span>

      <div class="nf-inner">
        <!-- 404 貼紙數字：中間的 0 是旋轉中的黑膠唱片 -->
        <div class="nf-digits" aria-hidden="true">
          <span class="nf-four nf-four-a">4</span>
          <span class="nf-record">
            <span class="nf-record-ring"></span>
            <span class="nf-record-core"><app-icon name="note" :size="15" /></span>
          </span>
          <span class="nf-four nf-four-b">4</span>
        </div>

        <h1 class="nf-title">{{ t('notFound.title') }}</h1>
        <p class="nf-text">{{ t('notFound.text') }}</p>

        <div class="nf-actions">
          <router-link class="nf-btn nf-btn-home" to="/">
            <app-icon name="arrowL" :size="14" />
            {{ t('notFound.home') }}
          </router-link>
          <router-link class="nf-btn nf-btn-faq" to="/faq">{{ t('notFound.faq') }}</router-link>
        </div>

        <div class="nf-note">{{ t('notFound.note') }}</div>
      </div>
    </main>

    <app-footer />
  </div>
</template>

<style scoped>
.nf-root { display: flex; flex-direction: column; min-height: 100vh; }
.nf-root :deep(.mkt-foot) { margin-top: 0; }

.nf-main {
  position: relative; flex: 1; overflow: hidden;
  display: flex; align-items: center; justify-content: center;
  padding: 64px 32px;
  background: #8a5cf6;
  background-image: radial-gradient(rgba(255, 255, 255, 0.18) 1.5px, transparent 1.5px);
  background-size: 26px 26px;
}

/* 漂浮貼紙 */
.nf-deco { position: absolute; font-style: normal; pointer-events: none; }
.nf-deco-sq { left: 9%; top: 16%; width: 52px; height: 52px; background: var(--c-yellow); border: 2px solid var(--border-strong); border-radius: 16px; transform: rotate(14deg); animation: nf-float 6s ease-in-out infinite; --r: 14deg; }
.nf-deco-dot { right: 11%; top: 22%; width: 44px; height: 44px; background: var(--c-lime); border: 2px solid var(--border-strong); border-radius: 50%; animation: nf-float 7s ease-in-out .6s infinite; --r: 0deg; }
.nf-deco-n1 { left: 14%; bottom: 18%; color: var(--c-yellow); transform: rotate(-10deg); }
.nf-deco-n2 { right: 16%; bottom: 22%; color: var(--c-pink); transform: rotate(12deg); }
@keyframes nf-float {
  0%, 100% { transform: translateY(0) rotate(var(--r, 0deg)); }
  50% { transform: translateY(-10px) rotate(var(--r, 0deg)); }
}

.nf-inner { max-width: 680px; text-align: center; position: relative; z-index: 2; }

/* 404 貼紙數字 */
.nf-digits { display: flex; justify-content: center; gap: 14px; align-items: center; }
.nf-four {
  display: inline-block; padding: 14px 26px; line-height: 1;
  font-family: var(--oj-display); font-weight: 700; font-size: clamp(64px, 10vw, 96px); color: var(--text);
  border: 2px solid var(--border-strong); border-radius: 22px;
  box-shadow: 0 10px 24px rgba(26, 26, 26, 0.25);
}
.nf-four-a { background: var(--c-yellow); transform: rotate(-4deg); }
.nf-four-b { background: var(--c-lime); transform: rotate(3deg); }
/* 中間的 0：旋轉黑膠 */
.nf-record {
  position: relative; width: clamp(96px, 14vw, 128px); aspect-ratio: 1; flex: none;
  display: inline-flex; align-items: center; justify-content: center;
  background: var(--text); border: 2px solid var(--border-strong); border-radius: 999px;
  box-shadow: 0 10px 24px rgba(26, 26, 26, 0.3);
  animation: nf-spin 6s linear infinite;
}
.nf-record-ring { position: absolute; inset: 14px; border: 2px dashed rgba(255, 255, 255, 0.35); border-radius: 999px; }
.nf-record-core {
  width: 40px; height: 40px; display: flex; align-items: center; justify-content: center;
  background: var(--c-pink); border: 2px solid #fff; border-radius: 999px; color: var(--text);
}
@keyframes nf-spin { from { transform: rotate(0deg); } to { transform: rotate(360deg); } }

.nf-title { font-size: clamp(30px, 4.4vw, 42px); font-weight: 900; color: #fff; margin: 34px 0 12px; }
.nf-text { color: #fff; font-weight: 700; font-size: 16px; line-height: 1.9; margin: 0 0 30px; white-space: pre-line; }

.nf-actions { display: flex; gap: 14px; justify-content: center; flex-wrap: wrap; }
.nf-btn {
  display: inline-flex; align-items: center; gap: 8px;
  border: 2px solid var(--border-strong); border-radius: 999px;
  font-family: var(--oj-font); font-weight: 900; font-size: 15px; color: var(--text);
  padding: 14px 30px; text-decoration: none;
  transition: transform .2s var(--ease-pop), box-shadow .2s;
}
.nf-btn-home { background: var(--c-yellow); }
.nf-btn-home:hover { transform: translateY(-3px) rotate(-1deg); box-shadow: 0 10px 22px rgba(26, 26, 26, 0.3); }
.nf-btn-faq { background: #fff; }
.nf-btn-faq:hover { transform: translateY(-3px) rotate(1deg); box-shadow: 0 10px 22px rgba(26, 26, 26, 0.3); }

.nf-note {
  font-family: var(--oj-hand); font-weight: 700; font-size: 26px; color: var(--c-lime);
  margin-top: 26px; transform: rotate(-2deg);
}

@media (prefers-reduced-motion: reduce) {
  .nf-record, .nf-deco-sq, .nf-deco-dot { animation: none; }
}
@media (max-width: 560px) {
  .nf-deco { display: none; }
  .nf-four { padding: 10px 18px; }
  .nf-actions .nf-btn { width: 100%; justify-content: center; }
}
</style>
