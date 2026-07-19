<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue';
import { useRouter } from 'vue-router';
import { useMessage, type FormInst, type FormRules } from 'naive-ui';
import { useI18n } from 'vue-i18n';
import { useShopStore } from '@/stores/shop';
import { useAuthStore } from '@/stores/auth';
import { orderApi } from '@/api';
import ProductThumb from '@/components/ProductThumb.vue';
import AppIcon from '@/components/app-icon';

const store = useShopStore();
const auth = useAuthStore();
const router = useRouter();
const message = useMessage();
const { t } = useI18n();

const form = ref<FormInst | null>(null);
const processing = ref(false);
const model = reactive({ name: '', email: '' });

// 已登入則以登入信箱預填（仍可手動改動）；使用者一旦動過欄位就不再覆蓋。
// 登出（或換帳號的過渡期間）清掉先前預填的信箱，避免殘留舊帳號。
const emailEdited = ref(false);
watch(
  () => auth.userEmail,
  (email) => {
    if (emailEdited.value) return;
    model.email = email ?? '';
  },
  { immediate: true },
);

const rules = computed<FormRules>(() => ({
  name: { required: true, message: t('checkout.rules.nameRequired'), trigger: ['blur', 'input'] },
  email: [
    { required: true, message: t('checkout.rules.emailRequired'), trigger: ['blur', 'input'] },
    { validator: (_, v: string) => /^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(v), message: t('checkout.rules.emailInvalid'), trigger: ['blur'] },
  ],
}));

const items = computed(() => store.cartProducts);
const subtotal = computed(() => store.subtotal);
const total = computed(() => subtotal.value);

// 已購買預檢：登入買家後端以帳號 ID 判定已購買（與結帳信箱無關），送單必吃 409。
// 進頁時逐項查詢購買紀錄，命中者改顯示「前往下載」（連向該筆完成訂單的下載頁）並擋下送單。
// 訪客無從預檢（信箱未驗證），維持送單後由後端 409 訊息擋下。
const purchasedOrders = ref<Record<string, string | null>>({}); // catalogId → 完成訂單 ID（null = 已查、未購買）
const purchasedOf = (id: string): string | null => purchasedOrders.value[id] ?? null;
const hasPurchasedItems = computed(() => items.value.some((it) => purchasedOf(it.id)));

watch(
  [() => items.value.map((it) => it.id).join(','), () => auth.isAuthenticated],
  async () => {
    if (!auth.isAuthenticated) return;
    const unchecked = items.value.filter((it) => !(it.id in purchasedOrders.value));
    await Promise.all(unchecked.map(async (it) => {
      try {
        const res = await orderApi.orders.hasPurchased(it.id);
        purchasedOrders.value[it.id] = res.data.purchased ? (res.data.orderId ?? null) : null;
      } catch {
        // 預檢失敗不擋結帳（僅屬 UX 提前提示），重複購買仍由後端 409 把關
        purchasedOrders.value[it.id] = null;
      }
    }));
  },
  { immediate: true },
);

const goOrder = (orderId: string) => router.push({ name: 'order', params: { orderId } });

onMounted(() => {
  // 直接以網址進入結帳頁時，購物車項目需先載入商品目錄才能顯示
  store.loadCatalog();
  store.startCheckout();
});

const goList = () => router.push({ name: 'list' });
const openProduct = (id: string) => router.push({ name: 'product', params: { id } });

/** 建立訂單 + Stripe Checkout Session，導向 Stripe 安全頁面填寫信用卡資訊。 */
const pay = async () => {
  if (hasPurchasedItems.value) {
    message.error(t('checkout.ownedHint'));
    return;
  }
  try {
    await form.value!.validate();
  } catch { return; }
  processing.value = true;
  try {
    const url = await store.checkout({ name: model.name, email: model.email });
    if (url) {
      // 整頁導向 Stripe 託管的付款頁（卡號 / 有效期限 / CVC 一律在 Stripe 上輸入）。
      window.location.href = url;
      return;
    }
    // 免費訂單：後端跳過金流、訂單已完成，直接進結帳成功頁（沿用 Stripe 導回的還原流程）。
    router.push({ name: 'checkout-success' });
  } catch (e) {
    processing.value = false;
    message.error(errorText(e));
  }
};

/** 從錯誤取出可讀訊息：優先用後端 Problem Details 的 detail（如「商店尚未完成收款設定」），否則退回通用文案。 */
const errorText = (e: unknown): string => {
  if (e && typeof e === 'object' && 'detail' in e && typeof (e as { detail: unknown }).detail === 'string')
    return (e as { detail: string }).detail;
  if (e instanceof Error) return e.message;
  return t('checkout.msgError');
};
</script>

<template>
  <div class="page page-pad" :data-screen-label="t('checkout.screenLabel')">

    <!-- EMPTY -->
    <div v-if="!items.length" class="empty-card">
      <app-icon name="cart" :size="40" style="margin:0 auto;" />
      <p class="empty-title">{{ t('checkout.emptyTitle') }}</p>
      <p class="empty-desc">{{ t('checkout.emptyDesc') }}</p>
      <button type="button" class="cta-ink" style="margin-top:18px" @click="goList">
        <app-icon name="arrowL" :size="14" /> {{ t('checkout.emptyAction') }}
      </button>
    </div>

    <!-- CHECKOUT -->
    <template v-else>
      <div class="breadcrumb">
        <a @click="goList">{{ t('common.explore') }}</a>
        <span class="sep">›</span>
        <span>{{ t('checkout.breadcrumb') }}</span>
      </div>
      <h1 class="h-title" style="margin-bottom:26px">{{ t('checkout.title') }}</h1>

      <n-form ref="form" :model="model" :rules="rules" :show-require-mark="false">
        <div class="checkout-grid">
          <!-- left column -->
          <div>
            <!-- cart -->
            <div class="panel">
              <div class="panel-head">
                <span class="step-num">1</span>
                <h3>{{ t('checkout.step1', { count: items.length }) }}</h3>
              </div>
              <div v-for="it in items" :key="it.id" class="cart-item">
                <product-thumb :product="it" :glyph-size="30" :show-cat="false" hide-label style="cursor:pointer;"
                               @click="openProduct(it.id)" />
                <div class="cart-item-body">
                  <div class="cart-item-title">
                    {{ it.title }}
                    <span v-if="purchasedOf(it.id)" class="owned-tag">
                      <app-icon name="check" :size="12" /> {{ t('checkout.owned') }}
                    </span>
                  </div>
                  <div class="cart-item-creator">{{ it.creator }}・{{ it.formats.join('・') }}</div>
                  <div class="cart-item-foot">
                    <button class="link-btn" @click="store.removeFromCart(it.id)">
                      <app-icon name="trash" :size="14" /> {{ t('checkout.remove') }}
                    </button>
                    <button v-if="purchasedOf(it.id)" type="button" class="link-btn owned-download"
                            @click="goOrder(purchasedOf(it.id)!)">
                      <app-icon name="download" :size="14" /> {{ t('checkout.goDownload') }}
                    </button>
                    <span v-else class="price">{{ it.price === 0 ? t('common.free') : '$' + it.price }}</span>
                  </div>
                </div>
              </div>
            </div>

            <!-- buyer info -->
            <div class="panel">
              <div class="panel-head">
                <span class="step-num">2</span>
                <h3>{{ t('checkout.step2') }}</h3>
              </div>
              <div class="field-grid two">
                <n-form-item :label="t('checkout.name')" path="name">
                  <n-input v-model:value="model.name" :placeholder="t('checkout.namePlaceholder')" size="large" />
                </n-form-item>
                <n-form-item :label="t('checkout.email')" path="email">
                  <n-input v-model:value="model.email" placeholder="you@example.com" size="large"
                           @input="emailEdited = true" />
                </n-form-item>
              </div>
              <div style="display:flex; align-items:center; gap:6px; color:var(--text-soft); font-size:12.5px; font-weight:700; margin-top:-4px;">
                <app-icon name="mail" :size="14" /> {{ t('checkout.emailNote') }}
              </div>
            </div>

            <!-- payment -->
            <div class="panel">
              <div class="panel-head">
                <span class="step-num">3</span>
                <h3>{{ t('checkout.step3') }}</h3>
              </div>

              <div class="stripe-note">
                <div class="stripe-note-icon"><app-icon name="lock" :size="20" /></div>
                <div>
                  <div class="stripe-note-title">{{ t('checkout.stripeTitle') }}</div>
                  <p class="stripe-note-sub">{{ t('checkout.stripeSub') }}</p>
                </div>
              </div>
            </div>
          </div>

          <!-- right column: summary -->
          <div class="summary">
            <div class="panel">
              <h3 style="margin:0 0 18px; font-size:18px; font-weight:900;">{{ t('checkout.summary') }}</h3>
              <div class="sum-row"><span>{{ t('checkout.subtotal', { count: items.length }) }}</span><span>${{ subtotal }}</span></div>
              <div class="sum-row total"><span>{{ t('checkout.total') }}</span><span>${{ total }}</span></div>

              <button type="button" class="cta-ink block" style="margin-top:22px"
                      :disabled="processing || hasPurchasedItems" @click="pay">
                <app-icon v-if="!processing" name="lock" :size="16" />
                {{ processing ? t('checkout.payProcessing') : t('checkout.pay', { amount: total }) }}
              </button>
              <p v-if="hasPurchasedItems" class="owned-hint">{{ t('checkout.ownedHint') }}</p>

              <div class="trust" style="margin-top:16px;">
                <app-icon name="shield" :size="14" /> {{ t('checkout.trust') }}
              </div>
            </div>
          </div>
        </div>
      </n-form>
    </template>
  </div>
</template>

<style scoped>
/* 空購物車卡（延續設計稿 no-results 卡語彙） */
.empty-card {
  max-width: 560px; margin: 48px auto; text-align: center;
  border: var(--bw) solid var(--border-strong); border-radius: var(--r-lg); background: var(--surface);
  padding: 48px; box-shadow: var(--pop-2);
}
.empty-title { font-weight: 900; font-size: 18px; margin: 12px 0 0; }
.empty-desc { font-weight: 500; font-size: 14px; margin: 6px 0 0; color: var(--text-soft); }

/* Stripe 導轉說明（奶油底 + 紙感虛線框） */
.stripe-note {
  display: flex;
  gap: 14px;
  align-items: flex-start;
  padding: 16px 18px;
  border: 1px dashed #e4d9c2;
  border-radius: var(--r-md);
  background: var(--bg);
}
.stripe-note-icon {
  flex: none;
  width: 44px;
  height: 44px;
  display: grid;
  place-items: center;
  border-radius: 999px;
  background: var(--surface);
  border: var(--bw) solid var(--border-strong);
}
.stripe-note-title { font-weight: 900; font-size: 15px; }
.stripe-note-sub { margin: 4px 0 0; font-size: 12.5px; font-weight: 500; color: var(--text-soft); line-height: 1.7; }

/* 已購買標記與前往下載（登入買家結帳預檢命中） */
.owned-tag {
  display: inline-flex; align-items: center; gap: 3px; vertical-align: 2px;
  margin-left: 6px; padding: 1px 8px;
  border: var(--bw) solid var(--border-strong); border-radius: 999px;
  background: var(--bg); font-size: 11px; font-weight: 900; transform: rotate(-1deg);
}
.owned-download { color: var(--text); text-decoration: underline; }
.owned-hint { margin: 10px 0 0; font-size: 12.5px; font-weight: 700; color: var(--text-soft); line-height: 1.7; }
</style>
