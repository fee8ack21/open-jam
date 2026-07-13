<script setup lang="ts">
/* ============================================================
   NotFoundView — 404（設計稿「404 創作者空間」）
   置中白卡：空木箱插畫 + 404 黃色貼紙 + 回商店 / 逛市集 CTA。
   ============================================================ */
import { computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { useShopStore } from '@/stores/shop';
import { env } from '@/environment';
import AppIcon from '@/components/app-icon';

const store = useShopStore();
const router = useRouter();
const { t } = useI18n();

// 讓「回到商店」CTA 能帶出店名
onMounted(store.loadCatalog);
const storeName = computed(() => store.storefront.storeName || 'Open Jam');

const goList = () => router.push({ name: 'list' });
const goMarket = () => { window.location.href = env.PORTAL_PAGE_URL; };
const goHelp = () => { window.location.href = `${env.PORTAL_PAGE_URL}/faq`; };
</script>

<template>
  <div class="page page-pad nf-page" :data-screen-label="t('notfound.screenLabel')">
    <div class="result-card nf-card">
      <div class="result-deco square" style="left:-18px; top:70px; background:var(--c-lime); --r:12deg; transform:rotate(12deg)"></div>
      <div class="result-deco dot" style="right:-16px; top:160px; background:var(--c-cyan)"></div>
      <div class="result-note"><app-icon name="note" :size="26" /></div>

      <!-- 空木箱插畫（設計稿 empty crate） -->
      <div class="nf-crate" aria-hidden="true">
        <div class="crate-body"></div>
        <div class="crate-lid"></div>
        <div class="crate-q">?</div>
        <div class="crate-ball"></div>
      </div>

      <div class="nf-pill">404 NOT FOUND</div>
      <h1 class="nf-title">{{ t('notfound.title') }}</h1>
      <p class="nf-sub">{{ t('notfound.sub') }}</p>

      <div class="result-cta">
        <button type="button" class="cta-ink block" @click="goList">
          <app-icon name="arrowL" :size="14" /> {{ t('notfound.backToStore', { store: storeName }) }}
        </button>
        <div class="result-cta-row">
          <button type="button" class="cta-line" @click="goMarket">{{ t('notfound.backToMarket') }}</button>
          <button type="button" class="cta-line" style="--hover-c:var(--t-pink)" @click="goHelp">{{ t('notfound.help') }}</button>
        </div>
      </div>

      <div class="result-hand hand-note">{{ t('notfound.handNote') }}</div>
    </div>
  </div>
</template>

<style scoped>
.nf-page {
  flex: 1;
  display: flex;
  flex-direction: column;
  justify-content: center;
  padding-top: 40px;
  padding-bottom: 56px;
}
.nf-card { max-width: 620px; text-align: center; }

/* 空木箱插畫 */
.nf-crate { width: 150px; height: 120px; margin: 0 auto; position: relative; }
.crate-body {
  position: absolute; left: 8px; right: 8px; bottom: 0; height: 76px;
  background: var(--t-pink); border: var(--bw) solid var(--border-strong); border-radius: 12px;
}
.crate-lid {
  position: absolute; left: 0; right: 0; bottom: 58px; height: 22px;
  background: var(--c-pink); border: var(--bw) solid var(--border-strong); border-radius: 8px; transform: rotate(-2deg);
}
.crate-q {
  position: absolute; left: 50%; bottom: 18px; transform: translateX(-50%);
  font-size: 30px; font-weight: 900; font-family: var(--oj-display); color: var(--text);
}
.crate-ball {
  position: absolute; right: -12px; top: 0; width: 26px; height: 26px;
  background: var(--c-yellow); border: var(--bw) solid var(--border-strong); border-radius: 999px;
}

.nf-pill {
  display: inline-block; background: var(--c-yellow); border: var(--bw) solid var(--border-strong); border-radius: 999px;
  font-family: var(--oj-display); font-weight: 700; font-size: 14px; letter-spacing: 2px;
  padding: 4px 16px; margin-top: 22px; transform: rotate(-2deg); white-space: nowrap;
}
.nf-title { font-size: clamp(26px, 4vw, 34px); font-weight: 900; margin: 16px 0 10px; }
.nf-sub { font-weight: 500; font-size: 15px; line-height: 1.9; color: var(--text-soft); margin: 0 0 28px; }
.nf-card .result-cta { margin-top: 0; }
</style>
