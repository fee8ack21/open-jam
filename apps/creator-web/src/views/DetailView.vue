<script setup lang="ts">
import { computed, onMounted, ref, watch, watchEffect } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { useShopStore } from '@/stores/shop';
import { CATEGORIES } from '@/data/products';
import ProductThumb from '@/components/ProductThumb.vue';
import AppIcon from '@/components/app-icon';
import Stars from '@/components/Stars.vue';
import ReviewList from '@/components/ReviewList.vue';

/* 檔案格式色塊（果醬色盤；深色底配白字） */
const FILE_COLORS: Record<string, string> = {
  PDF: '#ff6b35', MIDI: '#8a5cf6', MSCZ: '#8a5cf6', AUDIO: '#d6479b', WAV: '#d6479b',
  JPG: '#18a999', PNG: '#18a999', RAW: '#3b7fd4', XMP: '#d8a017', XLSX: '#18a999',
  EPUB: '#8a5cf6', GP: '#ff6b35', TXT: '#888888', NOTION: '#444444', FIG: '#ff6b35', DEFAULT: '#8a5cf6',
};

const store = useShopStore();
const route = useRoute();
const router = useRouter();
const { t } = useI18n();

const active = ref(0);

const p = computed(() => store.product(String(route.params.id)));
const fav = computed(() => (p.value ? store.isFav(p.value.id) : false));
const inCart = computed(() => (p.value ? store.inCart(p.value.id) : false));
const initials = computed(() => (p.value ? p.value.creator.split(' ').map((s) => s[0]).slice(0, 2).join('') : ''));
const catLabel = computed(() =>
  p.value && CATEGORIES.some((c) => c.id === p.value!.cat) ? t('category.' + p.value.cat) : '',
);
const totalFiles = computed(() => (p.value ? p.value.files.reduce((n, f) => n + f.count, 0) : 0));

// 載入完成前不可判定為不存在（避免直接連結進入時誤導向 not-found）
const ready = ref(false);
async function ensureLoaded(id: string) {
  ready.value = false;
  await store.loadCatalog();
  await store.loadProduct(id);
  ready.value = true;
  store.recordView(id); // 進頁瀏覽 +1（fire-and-forget，不阻塞畫面）
}
onMounted(() => ensureLoaded(String(route.params.id)));
watch(() => route.params.id, (id) => { active.value = 0; ensureLoaded(String(id)); });
watchEffect(() => { if (ready.value && !p.value) router.replace({ name: 'not-found' }); });

const fileColor = (t: string) => FILE_COLORS[t] || FILE_COLORS.DEFAULT;
const goList = () => router.push({ name: 'list' });
const addCart = () => { if (p.value) store.addToCart(p.value.id); };
const buyNow = () => { if (p.value && !inCart.value) store.addToCart(p.value.id); router.push({ name: 'checkout' }); };
const goCart = () => router.push({ name: 'checkout' });
</script>

<template>
  <div class="page page-pad" :data-screen-label="t('detail.screenLabel')" v-if="p">
    <div class="breadcrumb">
      <a @click="goList">{{ t('common.explore') }}</a>
      <span class="sep">›</span>
      <a @click="goList">{{ catLabel }}</a>
      <span class="sep">›</span>
      <span>{{ p.title }}</span>
    </div>

    <div class="detail-grid">
      <!-- left: gallery + content -->
      <div>
        <product-thumb :product="p" class="gallery-main" :seed="active" :glyph-size="120"
                       :label="t('detail.previewLabel', { current: active + 1, total: p.previews })" />
        <div class="gallery-thumbs">
          <product-thumb v-for="i in p.previews" :key="i" :product="p" :seed="i - 1"
                         :class="{ on: active === i - 1 }" :show-cat="false" :glyph-size="26"
                         hide-label @click="active = i - 1" />
        </div>

        <div style="margin-top:44px">
          <h2 class="section-title">{{ t('detail.aboutTitle') }}</h2>
          <div class="prose">
            <p v-for="(para, i) in p.desc" :key="i">{{ para }}</p>
          </div>
        </div>

        <div style="margin-top:44px">
          <h2 class="section-title">{{ t('detail.contentPreviewTitle') }}</h2>
          <div class="preview-locked">
            <product-thumb :product="p" :seed="active + 2"
                           style="aspect-ratio:16/7;"
                           :glyph-size="80" :show-cat="false" hide-label />
            <div class="lock-veil">
              <div class="lock-mark">
                <app-icon name="lock" :size="18" />
              </div>
              <span class="lock-txt">{{ t('detail.lockedText') }}</span>
            </div>
          </div>
        </div>

        <div style="margin-top:44px">
          <i18n-t keypath="detail.contentsTitle" tag="h2" class="section-title" scope="global">
            <template #count><small>{{ t('detail.contentsCount', { count: totalFiles }) }}</small></template>
          </i18n-t>
          <div class="file-list">
            <div v-for="(f, i) in p.contents" :key="i" class="file-row">
              <div class="file-ic" :style="{ background: fileColor(f.type) }">{{ f.type }}</div>
              <div style="flex:1; min-width:0;">
                <div class="file-name">{{ f.name }}</div>
                <div class="file-meta">{{ f.type }}・{{ f.size }}</div>
              </div>
              <span class="file-lock"><app-icon name="lock" :size="15" /></span>
            </div>
          </div>
        </div>

        <review-list :catalog-id="p.id" />
      </div>

      <!-- right: buy card -->
      <div class="buy-card">
          <div style="display:flex; align-items:center; justify-content:space-between;">
            <span class="chip" style="background:var(--t-green)">{{ catLabel }}</span>
            <button class="fav" :class="{ on: fav }" :title="t('detail.favTitle')"
                    @click="store.toggleFav(p.id)">
              <app-icon :name="fav ? 'heart' : 'heartLine'" :size="18" />
            </button>
          </div>

          <h1 class="detail-title">{{ p.title }}</h1>

          <div class="creator-row">
            <span class="avatar" :style="{ background: p.avatar }">{{ initials }}</span>
            <div>
              <div class="creator-name">{{ p.creator }}</div>
              <div class="creator-handle">{{ p.handle }}</div>
            </div>
          </div>

          <div class="price-row">
            <span class="price-xl" :class="{ free: p.price === 0 }">{{ p.price === 0 ? t('common.free') : '$' + p.price }}</span>
            <stars :value="p.rating" :count="p.ratingCount" :size="14" />
          </div>

          <button type="button" class="cta-ink block" @click="buyNow">
            <app-icon :name="p.price === 0 ? 'arrowD' : 'cart'" :size="15" />
            {{ p.price === 0 ? t('detail.getFree') : t('detail.buyNow') }}
          </button>
          <button v-if="!inCart" type="button" class="cta-line block" style="margin-top:12px" @click="addCart">
            <app-icon name="cart" :size="15" />
            {{ t('detail.addToCart') }}
          </button>
          <button v-else type="button" class="cta-line block" style="margin-top:12px; --hover-c:var(--t-green)" @click="goCart">
            <app-icon name="check" :size="15" />
            {{ t('detail.inCart') }}
          </button>

          <div class="trust">
            <app-icon name="shield" :size="14" /> {{ t('detail.trust') }}
          </div>

          <div class="spec-list">
            <div class="spec-row">
              <span class="spec-k"><app-icon name="doc" :size="13" /> {{ t('detail.specFiles') }}</span>
              <i18n-t keypath="detail.specFilesValue" tag="span" class="spec-v" scope="global">
                <template #count><b>{{ totalFiles }}</b></template>
              </i18n-t>
            </div>
            <div class="spec-row">
              <span class="spec-k"><app-icon name="copy" :size="13" /> {{ t('detail.specFormats') }}</span>
              <span class="spec-v"><b>{{ p.formats.join('・') }}</b></span>
            </div>
            <div class="spec-row">
              <span class="spec-k"><app-icon name="download" :size="13" /> {{ t('detail.specSize') }}</span>
              <span class="spec-v"><b>{{ p.totalSize }}</b></span>
            </div>
            <div class="spec-row">
              <span class="spec-k"><app-icon name="tag" :size="13" /> {{ t('detail.specSales') }}</span>
              <i18n-t keypath="detail.specSalesValue" tag="span" class="spec-v" scope="global">
                <template #count><b>{{ p.sales.toLocaleString() }}</b></template>
              </i18n-t>
            </div>
            <div class="spec-row">
              <span class="spec-k"><app-icon name="eye" :size="13" /> {{ t('detail.specViews') }}</span>
              <i18n-t keypath="detail.specViewsValue" tag="span" class="spec-v" scope="global">
                <template #count><b>{{ (p.views ?? 0).toLocaleString() }}</b></template>
              </i18n-t>
            </div>
          </div>

          <div v-if="p.price === 0" class="hand-note" style="display:block; text-align:center; font-size:22px; margin-top:14px;">
            {{ t('detail.handNoteFree') }}
          </div>
        </div>
    </div>
  </div>
</template>
