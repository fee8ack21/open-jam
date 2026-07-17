<script setup lang="ts">
/* ============================================================
   HotBanner — 本週熱門大 Banner。左側為銷量冠軍（墨黑大幅 +
   斜貼拍立得縮圖），右側粉紅區列出第 2–4 名榜單，各自連到
   商品詳情，以排行內容打斷連續卡片牆的節奏。
   ============================================================ */
import { useI18n } from 'vue-i18n';
import { env } from '@/environment.js';
import type { Product } from '@/data/products';

const props = withDefaults(defineProps<{ product: Product; runnersUp?: Product[] }>(), {
  runnersUp: () => [],
});
const { t } = useI18n();

const hrefFor = (p: Product) =>
  `${env.CREATOR_PAGE_BASE_URL.replace('<store-slug>', p.storeSlug)}/product/${p.id}`;
</script>

<template>
  <section class="sec hot-sec rv" id="hot">
    <div class="hot-banner" :class="{ 'no-list': !props.runnersUp.length }">
      <a class="hot-info" :href="hrefFor(product)">
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
        <div class="hot-champ">
          <product-thumb :product="product" hide-label :show-cat="false" :glyph-size="36" />
          <div class="hot-champ-title">{{ product.title }}</div>
          <div class="hot-champ-author">{{ product.creator }}</div>
        </div>
      </a>
      <div v-if="props.runnersUp.length" class="hot-media">
        <div class="hot-rank-head">{{ t('market.hotBanner.runnersUp') }}</div>
        <a v-for="(p, i) in props.runnersUp" :key="p.id" class="hot-rank-row" :href="hrefFor(p)">
          <span class="hot-rank-num">{{ i + 2 }}</span>
          <product-thumb :product="p" hide-label :show-cat="false" :glyph-size="20" />
          <span class="hot-rank-text">
            <span class="hot-rank-name">{{ p.title }}</span>
            <span class="hot-rank-meta">
              {{ t('market.trending.sales', { count: p.sales.toLocaleString() }) }} ·
              <app-icon name="star" :size="10" /> {{ p.rating }}
            </span>
          </span>
        </a>
      </div>
    </div>
  </section>
</template>
