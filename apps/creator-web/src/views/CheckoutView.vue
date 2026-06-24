<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue';
import { useRouter } from 'vue-router';
import { useMessage, type FormInst, type FormRules } from 'naive-ui';
import { useShopStore } from '@/stores/shop';
import ProductThumb from '@/components/ProductThumb.vue';
import AppIcon from '@/components/app-icon';

const store = useShopStore();
const router = useRouter();
const message = useMessage();

const form = ref<FormInst | null>(null);
const processing = ref(false);
const model = reactive({ name: '', email: '' });

const rules: FormRules = {
  name: { required: true, message: '請輸入姓名', trigger: ['blur', 'input'] },
  email: [
    { required: true, message: '請輸入電子信箱', trigger: ['blur', 'input'] },
    { validator: (_, v: string) => /^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(v), message: '信箱格式不正確', trigger: ['blur'] },
  ],
};

const items = computed(() => store.cartProducts);
const subtotal = computed(() => store.subtotal);
const total = computed(() => subtotal.value);

onMounted(() => {
  store.startCheckout();
});

const goList = () => router.push({ name: 'list' });
const openProduct = (id: string) => router.push({ name: 'product', params: { id } });

/** 建立訂單 + Stripe Checkout Session，導向 Stripe 安全頁面填寫信用卡資訊。 */
const pay = async () => {
  try {
    await form.value!.validate();
  } catch { return; }
  processing.value = true;
  try {
    const url = await store.checkout({ name: model.name, email: model.email });
    // 整頁導向 Stripe 託管的付款頁（卡號 / 有效期限 / CVC 一律在 Stripe 上輸入）。
    window.location.href = url;
  } catch (e) {
    processing.value = false;
    message.error(e instanceof Error ? e.message : '結帳失敗，請稍後再試');
  }
};
</script>

<template>
  <div class="page page-pad" data-screen-label="結帳頁">

    <!-- EMPTY -->
    <div v-if="!items.length" class="empty">
      <app-icon name="cart" :size="44" style="margin-bottom:14px; opacity:.5;" />
      <p style="font-size:18px; font-weight:600; color:var(--text-soft);">購物車是空的</p>
      <p style="margin-top:6px;">挑幾件喜歡的作品，回來這裡結帳吧。</p>
      <n-button type="primary" style="margin-top:18px" @click="goList">去逛逛</n-button>
    </div>

    <!-- CHECKOUT -->
    <template v-else>
      <div class="breadcrumb">
        <a @click="goList">探索</a>
        <app-icon name="chevron" :size="14" />
        <span style="color:var(--text-soft)">結帳</span>
      </div>
      <h1 class="h-title" style="margin-bottom:26px">結帳</h1>

      <n-form ref="form" :model="model" :rules="rules" :show-require-mark="false">
        <div class="checkout-grid">
          <!-- left column -->
          <div>
            <!-- cart -->
            <div class="panel">
              <div class="panel-head">
                <span class="step-num">1</span>
                <h3>購物車（{{ items.length }} 件）</h3>
              </div>
              <div v-for="it in items" :key="it.id" class="cart-item">
                <product-thumb :product="it" :glyph-size="30" :show-cat="false" hide-label style="cursor:pointer;"
                               @click="openProduct(it.id)" />
                <div class="cart-item-body">
                  <div class="cart-item-title">{{ it.title }}</div>
                  <div class="cart-item-creator">{{ it.creator }} · {{ it.formats.join(' · ') }}</div>
                  <div class="cart-item-foot">
                    <button class="link-btn" @click="store.removeFromCart(it.id)">
                      <app-icon name="trash" :size="14" /> 移除
                    </button>
                    <span class="price" style="font-size:15px">{{ it.price === 0 ? '免費' : '$' + it.price }}</span>
                  </div>
                </div>
              </div>
            </div>

            <!-- buyer info -->
            <div class="panel">
              <div class="panel-head">
                <span class="step-num">2</span>
                <h3>購買人資訊</h3>
              </div>
              <div class="field-grid two">
                <n-form-item label="姓名" path="name">
                  <n-input v-model:value="model.name" placeholder="王小明" size="large" />
                </n-form-item>
                <n-form-item label="電子信箱" path="email">
                  <n-input v-model:value="model.email" placeholder="you@example.com" size="large" />
                </n-form-item>
              </div>
              <div style="display:flex; align-items:center; gap:8px; color:var(--text-faint); font-size:12.5px; margin-top:-4px;">
                <app-icon name="mail" :size="14" /> 下載連結與收據會寄到這個信箱
              </div>
            </div>

            <!-- payment -->
            <div class="panel">
              <div class="panel-head">
                <span class="step-num">3</span>
                <h3>付款方式</h3>
              </div>

              <div class="stripe-note">
                <div class="stripe-note-icon"><app-icon name="lock" :size="20" /></div>
                <div>
                  <div class="stripe-note-title">由 Stripe 安全處理付款</div>
                  <p class="stripe-note-sub">
                    點擊「前往付款」後，將跳轉至 Stripe 託管的安全頁面輸入信用卡資訊。
                    我們不會接觸或儲存您的卡號。
                  </p>
                </div>
              </div>
            </div>
          </div>

          <!-- right column: summary -->
          <div class="summary">
            <div class="panel">
              <h3 style="margin:0 0 18px; font-size:18px; font-weight:700;">訂單摘要</h3>
              <div class="sum-row"><span>小計（{{ items.length }} 件）</span><span>${{ subtotal }}</span></div>
              <div class="sum-row total"><span>總計</span><span>${{ total }}</span></div>

              <n-button type="primary" size="large" block strong style="margin-top:22px;"
                        :loading="processing" :disabled="processing" @click="pay">
                <template #icon v-if="!processing"><app-icon name="lock" :size="17" /></template>
                {{ processing ? '前往付款頁…' : '前往付款 $' + total }}
              </n-button>

              <div class="trust" style="margin-top:16px;">
                <app-icon name="shield" :size="14" /> 256-bit 加密 · 7 天不滿意退款
              </div>
            </div>
          </div>
        </div>
      </n-form>
    </template>
  </div>
</template>

<style scoped>
.stripe-note {
  display: flex;
  gap: 14px;
  align-items: flex-start;
  padding: 16px 18px;
  border: 1px solid var(--border);
  border-radius: 12px;
  background: var(--surface-2, rgba(127, 127, 127, 0.05));
}
.stripe-note-icon {
  flex: none;
  width: 40px;
  height: 40px;
  display: grid;
  place-items: center;
  border-radius: 10px;
  color: var(--brand, #635bff);
  background: color-mix(in srgb, var(--brand, #635bff) 12%, transparent);
}
.stripe-note-title { font-weight: 600; font-size: 15px; }
.stripe-note-sub { margin: 4px 0 0; font-size: 12.5px; color: var(--text-faint); line-height: 1.6; }
</style>
