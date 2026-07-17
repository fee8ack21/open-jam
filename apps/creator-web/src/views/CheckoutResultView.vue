<script setup lang="ts">
/* ============================================================
   CheckoutResultView — 結帳結果頁（設計稿「結帳成功 / 失敗」）
   置中白卡 + 漂浮貼紙裝飾；成功＝綠勾徽章 + 訂單摘要框、
   取消/失敗＝粉紅叉徽章 + 保留購物車框 + 常見原因。
   ============================================================ */
import { computed, onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { useShopStore } from '@/stores/shop';
import ProductThumb from '@/components/ProductThumb.vue';
import ReviewWidget from '@/components/ReviewWidget.vue';
import AppIcon from '@/components/app-icon';
import { env } from '@/environment';

const store = useShopStore();
const route = useRoute();
const router = useRouter();
const { t, tm, rt } = useI18n();

/** 由路由名稱判定為成功或取消頁（對應 PaymentService 的 SuccessUrl / CancelUrl）。 */
const isSuccess = computed(() => route.name === 'checkout-success');
const restored = ref(false);

const order = computed(() => store.order);
/** 取消付款時仍保留的購物車內容（設計稿「你的購物車・已保留」）。 */
const keptItems = computed(() => store.cartProducts);
/** 付款失敗常見原因（i18n 陣列）。 */
const causes = computed(() => (tm('result.causes') as string[]).map((c) => rt(c)));
const causeDots = ['var(--c-pink)', 'var(--c-yellow)', 'var(--c-cyan)'];

onMounted(() => {
  // 自 Stripe 導回為全頁載入：需重新載入目錄才能顯示保留的購物車內容
  store.loadCatalog();
  if (isSuccess.value) {
    // 自 Stripe 成功頁返回：以暫存訂單還原成功畫面並清空購物車。
    restored.value = store.finishCheckout();
  } else {
    // 取消付款：保留購物車，僅清除待付款暫存。
    store.cancelCheckout();
  }
});

const goList = () => router.push({ name: 'list' });
const goCheckout = () => router.push({ name: 'checkout' });
const goMarket = () => { window.location.href = env.PORTAL_PAGE_URL; };
const goHelp = () => { window.location.href = `${env.PORTAL_PAGE_URL}/faq`; };
/** 前往訂單下載頁（訪客憑訂單 ID 下載，與訂單完成信中的連結相同）。 */
const goOrder = () => {
  if (order.value?.orderId) router.push({ name: 'order', params: { orderId: order.value.orderId } });
};
</script>

<template>
  <div class="page page-pad result-page" :data-screen-label="t('result.screenLabel')">

    <!-- SUCCESS -->
    <div v-if="isSuccess && order" class="result-card">
      <div class="result-deco square" style="left:-18px; top:64px; background:var(--c-yellow); --r:14deg; transform:rotate(14deg)"></div>
      <div class="result-deco dot" style="right:-16px; top:150px; background:var(--c-pink)"></div>
      <div class="result-note"><app-icon name="note" :size="26" /></div>

      <div class="result-badge ok"><app-icon name="check" :size="40" /></div>
      <i18n-t keypath="result.successTitle" tag="h1" class="result-title" scope="global">
        <template #punct><span class="punct">{{ t('result.successPunct') }}</span></template>
      </i18n-t>
      <i18n-t keypath="result.successSub" tag="p" class="result-sub" scope="global">
        <template #email><span class="mono">{{ order.buyer.email }}</span></template>
      </i18n-t>

      <div class="result-box">
        <div class="result-box-head">
          <span class="lab">{{ t('result.orderNumber') }}</span>
          <span class="val">{{ order.id }}</span>
        </div>
        <div v-for="it in order.items" :key="it.id" class="result-box-item">
          <product-thumb :product="it" :glyph-size="26" :show-cat="false" hide-label />
          <div class="result-box-item-main">
            <div class="result-box-item-title">{{ it.title }}</div>
            <div class="result-box-item-meta">{{ it.creator }}・{{ it.formats.join('・') }}・{{ it.totalSize }}</div>
            <review-widget :catalog-id="it.id" :order-id="order.orderId" />
          </div>
          <span class="result-box-item-price">{{ it.price === 0 ? t('common.free') : '$' + it.price }}</span>
        </div>
        <div class="result-box-foot">
          <span class="pay">{{ t('result.payMethod') }}</span>
          <span class="total">{{ t('result.total') }} <b>${{ order.total }}</b></span>
        </div>
      </div>

      <div class="result-cta">
        <button type="button" class="cta-ink block" @click="goOrder">
          <app-icon name="arrowD" :size="15" /> {{ t('result.downloadAll') }}
        </button>
        <div class="result-cta-row">
          <button type="button" class="cta-line" @click="goList">{{ t('result.backToStore') }}</button>
          <button type="button" class="cta-line" style="--hover-c:var(--t-pink)" @click="goMarket">
            {{ t('result.continueExplore') }} <app-icon name="arrow" :size="13" />
          </button>
        </div>
      </div>

      <div class="result-hand hand-note">{{ t('result.handNoteSuccess') }}</div>
    </div>

    <!-- SUCCESS, but nothing to restore (e.g. page revisited / refreshed) -->
    <div v-else-if="isSuccess" class="result-card">
      <div class="result-deco square" style="left:-18px; top:64px; background:var(--c-yellow); --r:14deg; transform:rotate(14deg)"></div>
      <div class="result-note"><app-icon name="note" :size="26" /></div>

      <div class="result-badge ok"><app-icon name="check" :size="40" /></div>
      <h1 class="result-title">{{ t('result.successTitle2') }}</h1>
      <p class="result-sub">{{ t('result.successSub2') }}</p>
      <div class="result-cta">
        <button type="button" class="cta-ink block" @click="goList">{{ t('result.backToStore') }}</button>
      </div>
      <div class="result-hand hand-note">{{ t('result.handNoteSuccess') }}</div>
    </div>

    <!-- CANCELLED — 未完成付款：沒有扣款、購物車保留（設計稿「結帳失敗」） -->
    <div v-else class="result-card">
      <div class="result-deco dot" style="left:-16px; top:80px; background:var(--c-cyan)"></div>
      <div class="result-deco square" style="right:-18px; top:170px; background:var(--c-yellow); --r:-12deg; transform:rotate(-12deg)"></div>

      <div class="result-badge bad"><app-icon name="close" :size="36" /></div>
      <h1 class="result-title">{{ t('result.cancelTitle') }}</h1>
      <i18n-t keypath="result.cancelSub" tag="p" class="result-sub" scope="global">
        <template #keep><b>{{ t('result.cancelKeep') }}</b></template>
      </i18n-t>

      <div v-if="keptItems.length" class="result-box">
        <div class="result-box-head">
          <span class="lab">{{ t('result.cartLabel') }}</span>
          <span class="kept-badge">{{ t('result.cartKept') }}</span>
        </div>
        <div v-for="it in keptItems" :key="it.id" class="result-box-item">
          <product-thumb :product="it" :glyph-size="26" :show-cat="false" hide-label />
          <div class="result-box-item-main">
            <div class="result-box-item-title">{{ it.title }}</div>
            <div class="result-box-item-meta">{{ it.creator }}・{{ it.formats.join('・') }}・{{ it.totalSize }}</div>
          </div>
          <span class="result-box-item-price">{{ it.price === 0 ? t('common.free') : '$' + it.price }}</span>
        </div>
      </div>

      <div class="result-causes">
        <p class="result-causes-title">{{ t('result.causesTitle') }}</p>
        <ul>
          <li v-for="(c, i) in causes" :key="i">
            <span class="li-dot" :style="{ background: causeDots[i % causeDots.length] }"></span>
            <span>{{ c }}</span>
          </li>
        </ul>
      </div>

      <div class="result-cta">
        <button type="button" class="cta-ink block" @click="goCheckout">
          <app-icon name="refresh" :size="15" /> {{ t('result.retryPay') }}
        </button>
        <div class="result-cta-row">
          <button type="button" class="cta-line" @click="goList">{{ t('result.backToStore') }}</button>
          <button type="button" class="cta-line" style="--hover-c:var(--t-pink)" @click="goHelp">{{ t('result.contactUs') }}</button>
        </div>
      </div>

      <div class="result-hand hand-note">{{ t('result.handNoteCancel') }}</div>
    </div>
  </div>
</template>

<style scoped>
/* 結果頁填滿 main（= 100vh - header - footer）並垂直置中：
   短內容不再撐高頁面、不出現捲軸；內容較長（含訂單明細）時
   flex item 仍會長到內容高、正常捲動，不會裁切。 */
.result-page {
  flex: 1;
  display: flex;
  flex-direction: column;
  justify-content: center;
  padding-top: 40px;
  padding-bottom: 56px;
}
.kept-badge {
  background: var(--c-yellow); border: var(--bw) solid var(--border-strong); border-radius: 999px;
  font-weight: 900; font-size: 11px; padding: 1px 10px; white-space: nowrap;
}
</style>
