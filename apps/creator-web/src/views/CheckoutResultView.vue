<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useShopStore } from '@/stores/shop';
import ProductThumb from '@/components/ProductThumb.vue';
import AppIcon from '@/components/app-icon';

const store = useShopStore();
const route = useRoute();
const router = useRouter();

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
</script>

<template>
  <div class="page page-pad" data-screen-label="結帳結果頁">

    <!-- SUCCESS -->
    <div v-if="isSuccess && order" class="success-wrap">
      <div class="success-ring"><app-icon name="check" :size="44" :stroke="2.4" /></div>
      <p class="h-eyebrow" style="text-align:center">訂單 {{ order.id }}</p>
      <h1 class="h-title" style="text-align:center">購買完成</h1>
      <p class="h-sub" style="text-align:center">
        收據與下載連結已寄至 <b style="color:var(--text)">{{ order.buyer.email }}</b>
      </p>

      <div class="panel" style="margin-top:30px; text-align:left;">
        <div v-for="it in order.items" :key="it.id" class="cart-item">
          <product-thumb :product="it" :glyph-size="34" :show-cat="false" hide-label />
          <div class="cart-item-body">
            <div class="cart-item-title">{{ it.title }}</div>
            <div class="cart-item-creator">{{ it.creator }} · {{ it.totalSize }}</div>
            <div class="cart-item-foot">
              <span style="font-size:12.5px; color:var(--text-faint); font-family:var(--oj-mono)">{{ it.formats.join(' · ') }}</span>
              <n-button size="small" type="primary" secondary>
                <template #icon><app-icon name="download" :size="15" /></template>
                下載
              </n-button>
            </div>
          </div>
        </div>
      </div>

      <div style="display:flex; gap:12px; margin-top:24px; justify-content:center;">
        <n-button size="large" @click="goList">繼續探索</n-button>
        <n-button size="large" type="primary">
          <template #icon><app-icon name="download" :size="18" /></template>
          下載全部
        </n-button>
      </div>
    </div>

    <!-- SUCCESS, but nothing to restore (e.g. page revisited / refreshed) -->
    <div v-else-if="isSuccess" class="success-wrap">
      <div class="success-ring"><app-icon name="check" :size="44" :stroke="2.4" /></div>
      <h1 class="h-title" style="text-align:center">付款完成</h1>
      <p class="h-sub" style="text-align:center">收據與下載連結已寄至您的信箱。</p>
      <div style="display:flex; gap:12px; margin-top:24px; justify-content:center;">
        <n-button size="large" type="primary" @click="goList">繼續探索</n-button>
      </div>
    </div>

    <!-- CANCELLED -->
    <div v-else class="success-wrap">
      <div class="success-ring cancel"><app-icon name="close" :size="40" :stroke="2.4" /></div>
      <h1 class="h-title" style="text-align:center">付款已取消</h1>
      <p class="h-sub" style="text-align:center">您的購物車仍保留著，隨時可以回來完成結帳。</p>
      <div style="display:flex; gap:12px; margin-top:24px; justify-content:center;">
        <n-button size="large" @click="goList">繼續探索</n-button>
        <n-button size="large" type="primary" @click="goCheckout">回到結帳</n-button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.success-ring.cancel {
  color: var(--err, #e5484d);
  background: color-mix(in srgb, var(--err, #e5484d) 12%, transparent);
}
</style>
