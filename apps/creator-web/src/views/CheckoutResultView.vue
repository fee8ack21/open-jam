<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { useShopStore } from '@/stores/shop';
import ProductThumb from '@/components/ProductThumb.vue';
import ReviewWidget from '@/components/ReviewWidget.vue';
import ResultArt from '@/components/ResultArt.vue';
import AppIcon from '@/components/app-icon';

const store = useShopStore();
const route = useRoute();
const router = useRouter();
const { t } = useI18n();

/** 由路由名稱判定為成功或取消頁（對應 PaymentService 的 SuccessUrl / CancelUrl）。 */
const isSuccess = computed(() => route.name === 'checkout-success');
const restored = ref(false);

const order = computed(() => store.order);

onMounted(() => {
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
/** 前往訂單下載頁（訪客憑訂單 ID 下載，與訂單完成信中的連結相同）。 */
const goOrder = () => {
  if (order.value?.orderId) router.push({ name: 'order', params: { orderId: order.value.orderId } });
};
</script>

<template>
  <div class="page page-pad" :data-screen-label="t('result.screenLabel')">

    <!-- SUCCESS -->
    <div v-if="isSuccess && order" class="success-wrap">
      <span class="result-art"><result-art name="success" /></span>
      <p class="h-eyebrow" style="text-align:center">{{ t('result.successOrder', { id: order.id }) }}</p>
      <h1 class="h-title" style="text-align:center">{{ t('result.successTitle') }}</h1>
      <i18n-t keypath="result.successSub" tag="p" class="h-sub" style="text-align:center" scope="global">
        <template #email><b style="color:var(--text)">{{ order.buyer.email }}</b></template>
      </i18n-t>

      <div class="panel" style="margin-top:30px; text-align:left;">
        <div v-for="it in order.items" :key="it.id" class="cart-item">
          <product-thumb :product="it" :glyph-size="34" :show-cat="false" hide-label />
          <div class="cart-item-body">
            <div class="cart-item-title">{{ it.title }}</div>
            <div class="cart-item-creator">{{ it.creator }} · {{ it.totalSize }}</div>
            <div class="cart-item-foot">
              <span style="font-size:12.5px; color:var(--text-faint); font-family:var(--oj-mono)">{{ it.formats.join(' · ') }}</span>
              <n-button size="small" type="primary" secondary @click="goOrder">
                <template #icon><app-icon name="download" :size="15" /></template>
                {{ t('result.download') }}
              </n-button>
            </div>
            <review-widget :catalog-id="it.id" />
          </div>
        </div>
      </div>

      <div style="display:flex; gap:12px; margin-top:24px; justify-content:center;">
        <n-button size="large" @click="goList">{{ t('result.continueExplore') }}</n-button>
        <n-button size="large" type="primary" @click="goOrder">
          <template #icon><app-icon name="download" :size="18" /></template>
          {{ t('result.downloadAll') }}
        </n-button>
      </div>
    </div>

    <!-- SUCCESS, but nothing to restore (e.g. page revisited / refreshed) -->
    <div v-else-if="isSuccess" class="success-wrap">
      <span class="result-art"><result-art name="success" /></span>
      <h1 class="h-title" style="text-align:center">{{ t('result.successTitle2') }}</h1>
      <p class="h-sub" style="text-align:center">{{ t('result.successSub2') }}</p>
      <div style="display:flex; gap:12px; margin-top:24px; justify-content:center;">
        <n-button size="large" type="primary" @click="goList">{{ t('result.continueExplore') }}</n-button>
      </div>
    </div>

    <!-- CANCELLED — 放棄結帳不是錯誤，收斂為「暫停」語氣，不用錯誤紅 -->
    <div v-else class="success-wrap">
      <span class="result-art"><result-art name="cancel" /></span>
      <h1 class="h-title" style="text-align:center">{{ t('result.cancelTitle') }}</h1>
      <p class="h-sub" style="text-align:center">{{ t('result.cancelSub') }}</p>
      <div style="display:flex; gap:12px; margin-top:24px; justify-content:center;">
        <n-button size="large" @click="goList">{{ t('result.continueExplore') }}</n-button>
        <n-button size="large" type="primary" @click="goCheckout">{{ t('result.backToCheckout') }}</n-button>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* 手繪貼紙插畫（比照 portal-web AboutView 的 LandingArt）：
   奶油面板 + 品牌色小場景，外層補品牌硬陰影。 */
/* 結果頁填滿 main（= 100vh - header - footer）並垂直置中：
   短內容不再撐高頁面、不出現捲軸；內容較長（含訂單明細）時
   flex item 仍會長到內容高、正常捲動，不會裁切。 */
.page.page-pad {
  flex: 1;
  display: flex;
  flex-direction: column;
  justify-content: center;
  padding-top: 32px;
  padding-bottom: 32px;
}
.success-wrap { margin-top: 0; margin-bottom: 0; }
.result-art { display: block; width: 104px; margin: 0 auto 22px; }
</style>
