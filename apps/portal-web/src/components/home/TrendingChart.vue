<script setup lang="ts">
/* ============================================================
   TrendingChart — 市集儀表板「熱銷排行」板。依銷量取前五，
   每列連到商品所屬店面的詳情頁。
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
  <div class="board">
    <div class="board-head bh-sales">
      <span class="board-ic"><app-icon name="mic" :size="15" /></span>
      <h3 class="board-title">{{ t('market.trending.title') }}</h3>
      <span class="board-tag">TOP 5</span>
    </div>
    <ol class="brd-list">
      <li v-for="(p, i) in top5" :key="p.id">
        <a class="brd-row" :href="href(p)">
          <span class="brd-rank" :class="{ 'brd-rank-1': i === 0 }">{{ i + 1 }}</span>
          <span class="brd-thumb"><product-thumb :product="p" :show-cat="false" hide-label :glyph-size="18" /></span>
          <span class="brd-info">
            <span class="brd-title">{{ p.title }}</span>
            <span class="brd-sub">{{ p.creator }}</span>
          </span>
          <span class="brd-side">
            <span class="brd-main" :class="{ free: p.price === 0 }">{{ p.price === 0 ? t('common.free') : '$' + p.price }}</span>
            <span class="brd-note">{{ t('market.trending.sales', { count: p.sales.toLocaleString() }) }}</span>
          </span>
        </a>
      </li>
    </ol>
  </div>
</template>
