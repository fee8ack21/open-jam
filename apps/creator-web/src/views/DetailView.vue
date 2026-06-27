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

const FILE_COLORS: Record<string, string> = {
  PDF: '#e0573e', MIDI: '#6151f0', MSCZ: '#7a6cff', AUDIO: '#c94f9e', WAV: '#c94f9e',
  JPG: '#2f9e6b', PNG: '#16a07a', RAW: '#3b7fd4', XMP: '#d8a017', XLSX: '#1f9d57',
  EPUB: '#8b5cf6', GP: '#d65a3a', TXT: '#888', NOTION: '#444', FIG: '#e0573e', DEFAULT: '#6151f0',
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
      <app-icon name="chevron" :size="14" />
      <a @click="goList">{{ catLabel }}</a>
      <app-icon name="chevron" :size="14" />
      <span style="color:var(--text-soft)">{{ p.title }}</span>
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

        <div style="margin-top:40px">
          <h2 class="section-title">{{ t('detail.contentPreviewTitle') }}</h2>
          <div class="preview-locked">
            <div class="lock-veil">
              <div class="lock-mark">
                <app-icon name="lock" :size="22" />
              </div>
              <span class="lock-txt">{{ t('detail.lockedText') }}</span>
            </div>
            <product-thumb :product="p" :seed="active + 2"
                           style="aspect-ratio:16/7; border-radius:var(--r-lg);"
                           :glyph-size="80" :label="t('detail.previewBlurred')" :show-cat="false" />
          </div>
        </div>

        <div style="margin-top:40px">
          <h2 class="section-title">{{ t('detail.contentsTitle', { count: totalFiles }) }}</h2>
          <div class="file-list">
            <div v-for="(f, i) in p.contents" :key="i" class="file-row">
              <div class="file-ic" :style="{ background: fileColor(f.type) }">{{ f.type }}</div>
              <div style="flex:1; min-width:0;">
                <div class="file-name">{{ f.name }}</div>
                <div class="file-meta">{{ f.type }} · {{ f.size }}</div>
              </div>
              <app-icon name="lock" :size="16" style="color:var(--text-faint)" />
            </div>
          </div>
        </div>

        <review-list :catalog-id="p.id" />
      </div>

      <!-- right: buy card -->
      <div>
        <div class="buy-card">
          <div style="display:flex; align-items:center; justify-content:space-between;">
            <span class="chip">{{ catLabel }}</span>
            <button class="fav" :class="{ on: fav }" style="position:static;"
                    @click="store.toggleFav(p.id)">
              <app-icon name="heart" :size="18" :fill="fav" />
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

          <div style="display:flex; align-items:baseline; justify-content:space-between; margin:20px 0 4px;">
            <span class="price-xl" :class="{ free: p.price === 0 }">{{ p.price === 0 ? t('common.free') : '$' + p.price }}</span>
            <stars :value="p.rating" :count="p.ratingCount" :size="15" />
          </div>

          <div style="display:flex; flex-direction:column; gap:10px; margin-top:22px;">
            <n-button type="primary" size="large" block strong @click="buyNow">
              {{ p.price === 0 ? t('detail.getFree') : t('detail.buyNow') }}
            </n-button>
            <n-button v-if="!inCart" size="large" block secondary @click="addCart">
              <template #icon><app-icon name="cart" :size="18" /></template>
              {{ t('detail.addToCart') }}
            </n-button>
            <n-button v-else size="large" block secondary @click="goCart">
              <template #icon><app-icon name="check" :size="18" /></template>
              {{ t('detail.inCart') }}
            </n-button>
          </div>

          <div class="trust">
            <app-icon name="shield" :size="14" /> {{ t('detail.trust') }}
          </div>

          <div class="spec-list" style="margin-top:22px;">
            <div class="spec-row">
              <span class="spec-k"><app-icon name="file" :size="16" /> {{ t('detail.specFiles') }}</span>
              <span class="spec-v">{{ t('detail.specFilesValue', { count: totalFiles }) }}</span>
            </div>
            <div class="spec-row">
              <span class="spec-k"><app-icon name="bag" :size="16" /> {{ t('detail.specFormats') }}</span>
              <span class="spec-v">{{ p.formats.join(' · ') }}</span>
            </div>
            <div class="spec-row">
              <span class="spec-k"><app-icon name="download" :size="16" /> {{ t('detail.specSize') }}</span>
              <span class="spec-v">{{ p.totalSize }}</span>
            </div>
            <div class="spec-row">
              <span class="spec-k"><app-icon name="user" :size="16" /> {{ t('detail.specSales') }}</span>
              <span class="spec-v">{{ t('detail.specSalesValue', { count: p.sales.toLocaleString() }) }}</span>
            </div>
            <div class="spec-row">
              <span class="spec-k"><app-icon name="search" :size="16" /> {{ t('detail.specViews') }}</span>
              <span class="spec-v">{{ t('detail.specViewsValue', { count: (p.views ?? 0).toLocaleString() }) }}</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
