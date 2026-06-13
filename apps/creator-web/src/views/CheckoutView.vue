<script lang="ts">
import type { FormInst, FormRules } from 'naive-ui';
import { useShopStore } from '../stores/shop';
import ProductThumb from '../components/ProductThumb.vue';
import JIcon from '../components/JIcon.vue';

export default {
  name: 'CheckoutView',
  components: { ProductThumb, JIcon },
  setup() { return { store: useShopStore() }; },
  data() {
    const rules: FormRules = {
      name: { required: true, message: '請輸入姓名', trigger: ['blur', 'input'] },
      email: [
        { required: true, message: '請輸入電子信箱', trigger: ['blur', 'input'] },
        { validator: (_, v: string) => /^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(v), message: '信箱格式不正確', trigger: ['blur'] },
      ],
      cardName: { required: true, message: '請輸入持卡人姓名', trigger: ['blur', 'input'] },
      cardNumber: { validator: (_, v: string) => (v || '').replace(/\s/g, '').length === 16, message: '請輸入 16 位卡號', trigger: ['blur', 'input'] },
      expiry: { validator: (_, v: string) => /^\d{2}\/\d{2}$/.test(v || ''), message: 'MM/YY', trigger: ['blur', 'input'] },
      cvc: { validator: (_, v: string) => /^\d{3,4}$/.test(v || ''), message: '3–4 位', trigger: ['blur', 'input'] },
    };
    return {
      processing: false,
      model: { name: '', email: '', cardName: '', cardNumber: '', expiry: '', cvc: '' },
      rules,
    };
  },
  computed: {
    items() { return this.store.cartProducts; },
    subtotal() { return this.store.subtotal; },
    fee() { return this.subtotal === 0 ? 0 : Math.round(this.subtotal * 0.03 * 100) / 100; },
    total() { return this.subtotal + this.fee; },
    order() { return this.store.order; },
    cardMasked() {
      const raw = this.model.cardNumber.replace(/\s/g, '');
      return raw.padEnd(16, '•').replace(/(.{4})/g, '$1 ').trim();
    },
  },
  mounted() {
    this.store.startCheckout();
  },
  methods: {
    goList() { this.$router.push({ name: 'list' }); },
    openProduct(id: string) { this.$router.push({ name: 'product', params: { id } }); },
    initials(name: string) { return name.split(' ').map((s) => s[0]).slice(0, 2).join(''); },
    onCardInput(v: string) {
      const raw = v.replace(/\D/g, '').slice(0, 16);
      this.model.cardNumber = raw.replace(/(.{4})/g, '$1 ').trim();
    },
    onExpiry(v: string) {
      let raw = v.replace(/\D/g, '').slice(0, 4);
      if (raw.length >= 3) raw = raw.slice(0, 2) + '/' + raw.slice(2);
      this.model.expiry = raw;
    },
    onCvc(v: string) { this.model.cvc = v.replace(/\D/g, '').slice(0, 4); },
    async pay() {
      try {
        await (this.$refs.form as FormInst).validate();
      } catch (e) { return; }
      this.processing = true;
      setTimeout(() => {
        this.store.completeOrder({ name: this.model.name, email: this.model.email });
        this.processing = false;
        window.scrollTo({ top: 0 });
      }, 1300);
    },
  },
};
</script>

<template>
  <div class="page page-pad" data-screen-label="結帳頁">

    <!-- SUCCESS -->
    <div v-if="order" class="success-wrap">
      <div class="success-ring"><j-icon name="check" :size="44" :stroke="2.4" /></div>
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
                <template #icon><j-icon name="download" :size="15" /></template>
                下載
              </n-button>
            </div>
          </div>
        </div>
      </div>

      <div style="display:flex; gap:12px; margin-top:24px; justify-content:center;">
        <n-button size="large" @click="goList">繼續探索</n-button>
        <n-button size="large" type="primary">
          <template #icon><j-icon name="download" :size="18" /></template>
          下載全部
        </n-button>
      </div>
    </div>

    <!-- EMPTY -->
    <div v-else-if="!items.length" class="empty">
      <j-icon name="cart" :size="44" style="margin-bottom:14px; opacity:.5;" />
      <p style="font-size:18px; font-weight:600; color:var(--text-soft);">購物車是空的</p>
      <p style="margin-top:6px;">挑幾件喜歡的作品，回來這裡結帳吧。</p>
      <n-button type="primary" style="margin-top:18px" @click="goList">去逛逛</n-button>
    </div>

    <!-- CHECKOUT -->
    <template v-else>
      <div class="breadcrumb">
        <a @click="goList">探索</a>
        <j-icon name="chevron" :size="14" />
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
                      <j-icon name="trash" :size="14" /> 移除
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
                <j-icon name="mail" :size="14" /> 下載連結與收據會寄到這個信箱
              </div>
            </div>

            <!-- payment -->
            <div class="panel">
              <div class="panel-head">
                <span class="step-num">3</span>
                <h3>付款方式</h3>
              </div>

              <div class="cc-visual">
                <j-icon name="card" :size="26" />
                <div class="cc-num">{{ cardMasked }}</div>
                <div class="cc-bottom">
                  <div><span class="lab">持卡人</span>{{ model.cardName || 'YOUR NAME' }}</div>
                  <div><span class="lab">有效期限</span>{{ model.expiry || 'MM/YY' }}</div>
                </div>
              </div>

              <div class="field-grid">
                <n-form-item label="持卡人姓名" path="cardName">
                  <n-input v-model:value="model.cardName" placeholder="WANG XIAO MING" size="large" />
                </n-form-item>
                <n-form-item label="卡號" path="cardNumber">
                  <n-input :value="model.cardNumber" @update:value="onCardInput"
                           placeholder="1234 5678 9012 3456" size="large">
                    <template #suffix><j-icon name="card" :size="18" style="color:var(--text-faint)" /></template>
                  </n-input>
                </n-form-item>
                <div class="field-grid two">
                  <n-form-item label="有效期限" path="expiry">
                    <n-input :value="model.expiry" @update:value="onExpiry" placeholder="MM/YY" size="large" />
                  </n-form-item>
                  <n-form-item label="安全碼 CVC" path="cvc">
                    <n-input :value="model.cvc" @update:value="onCvc" placeholder="123" size="large" />
                  </n-form-item>
                </div>
              </div>
            </div>
          </div>

          <!-- right column: summary -->
          <div class="summary">
            <div class="panel">
              <h3 style="margin:0 0 18px; font-size:18px; font-weight:700;">訂單摘要</h3>
              <div class="sum-row"><span>小計（{{ items.length }} 件）</span><span>${{ subtotal }}</span></div>
              <div class="sum-row"><span>平台手續費 (3%)</span><span>${{ fee }}</span></div>
              <div class="sum-row total"><span>總計</span><span>${{ total }}</span></div>

              <n-button type="primary" size="large" block strong style="margin-top:22px;"
                        :loading="processing" :disabled="processing" @click="pay">
                <template #icon v-if="!processing"><j-icon name="lock" :size="17" /></template>
                {{ processing ? '安全付款中…' : '確認付款 $' + total }}
              </n-button>

              <div class="trust" style="margin-top:16px;">
                <j-icon name="shield" :size="14" /> 256-bit 加密 · 7 天不滿意退款
              </div>
            </div>
          </div>
        </div>
      </n-form>
    </template>
  </div>
</template>
