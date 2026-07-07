<script setup lang="ts">
/* ============================================================
   TrendingChart — 本週熱銷排行 Top 5。榜單形式（大名次 +
   縮圖 + 銷量），每列連到商品所屬店面的詳情頁。
   ============================================================ */
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { env } from '@/environment.js';
import type { Product } from '@/data/products';

const props = defineProps<{ products: Product[] }>();
const { t } = useI18n();

const top5 = computed(() =>
  props.products.slice().sort((a, b) => b.sales - a.sales).slice(0, 5),
);

function href(p: Product): string {
  return `${env.CREATOR_PAGE_BASE_URL.replace('<store-slug>', p.storeSlug)}/product/${p.id}`;
}
</script>

<template>
  <div class="trending">
    <div class="pulse-head">
      <p class="browse-eyebrow"><app-icon name="sparkle" :size="13" /> {{ t('market.trending.eyebrow') }}</p>
      <h2 class="pulse-title">{{ t('market.trending.title') }}</h2>
    </div>
    <ol class="tr-list">
      <li v-for="(p, i) in top5" :key="p.id">
        <a class="tr-row" :href="href(p)">
          <span class="tr-rank" :class="{ 'tr-rank-1': i === 0 }">{{ i + 1 }}</span>
          <span class="tr-thumb"><product-thumb :product="p" :show-cat="false" hide-label :glyph-size="22" /></span>
          <span class="tr-info">
            <span class="tr-title">{{ p.title }}</span>
            <span class="tr-creator">{{ p.creator }}</span>
          </span>
          <span class="tr-sales">{{ t('market.trending.sales', { count: p.sales.toLocaleString() }) }}</span>
          <span class="tr-price" :class="{ free: p.price === 0 }">
            {{ p.price === 0 ? t('common.free') : '$' + p.price }}
          </span>
        </a>
      </li>
    </ol>
  </div>
</template>
