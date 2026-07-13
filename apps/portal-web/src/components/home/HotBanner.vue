<script setup lang="ts">
/* ============================================================
   HotBanner — 本週熱門大 Banner。以本週銷量冠軍打斷連續
   卡片牆的節奏（墨黑大幅 + 糖果光暈），整幅連到商品詳情。
   ============================================================ */
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { env } from '@/environment.js';
import type { Product } from '@/data/products';

const props = defineProps<{ product: Product }>();
const { t } = useI18n();

const href = computed(
  () => `${env.CREATOR_PAGE_BASE_URL.replace('<store-slug>', props.product.storeSlug)}/product/${props.product.id}`,
);
</script>

<template>
  <section class="sec hot-sec rv" id="hot">
    <a class="hot-banner" :href="href">
      <div class="hot-info">
        <span class="hot-eyebrow"><app-icon name="flame" :size="12" /> {{ t('market.hotBanner.eyebrow') }}</span>
        <h2 class="hot-title">{{ product.title }}</h2>
        <p class="hot-blurb">{{ product.blurb }}</p>
        <div class="hot-chips">
          <span class="hot-chip">{{ t('market.trending.sales', { count: product.sales.toLocaleString() }) }}</span>
          <span class="hot-chip"><app-icon name="star" :size="12" /> {{ product.rating }}（{{ product.ratingCount }}）</span>
          <span class="hot-chip">{{ product.creator }}</span>
        </div>
        <div class="hot-actions">
          <span class="hot-cta">{{ t('market.hotBanner.cta') }} <app-icon name="arrow" :size="15" /></span>
          <span class="hot-price">{{ product.price === 0 ? t('market.hotBanner.freeNote') : '$' + product.price }}</span>
        </div>
      </div>
      <div class="hot-media">
        <div class="hot-polaroid">
          <product-thumb :product="product" hide-label :show-cat="false" :glyph-size="52" />
          <div class="hot-polaroid-title">{{ product.title }}</div>
          <div class="hot-polaroid-author">{{ product.creator }}</div>
        </div>
      </div>
    </a>
  </section>
</template>
