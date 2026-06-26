<script setup lang="ts">
/* ============================================================
   FeaturedCard — wide editorial card for the "精選作品" carousel.
   Image on the left, info on the right. Links out to the
   creator's storefront like <product-card>.
   ============================================================ */
import { computed } from 'vue';
import { env } from '@/environment.js';
import type { Product } from '@/data/products';

const props = defineProps<{ product: Product }>();

const href = computed(() => `${env.CREATOR_PAGE_BASE_URL.replace('<store-slug>', props.product.storeSlug)}/products/${props.product.id}`);
const initials = computed(() => props.product.creator.split(' ').map((s) => s[0]).slice(0, 2).join(''));
// 誠實標示：人工策展的標「編輯精選」，演算法補入的標「熱門」，不讓熱門商品冒充編輯精選。
const tag = computed(() =>
  props.product.featured ? { icon: 'sparkle', label: '編輯精選' } : { icon: 'star', label: '熱門' },
);
</script>

<template>
  <a class="feat-card" :href="href">
    <div class="feat-media">
      <product-thumb :product="product" hide-label />
    </div>
    <div class="feat-info">
      <span class="feat-tag" :class="{ 'feat-tag-hot': !product.featured }"><app-icon :name="tag.icon" :size="12" /> {{ tag.label }}</span>
      <h3 class="feat-title">{{ product.title }}</h3>
      <p class="feat-blurb">{{ product.blurb }}</p>
      <div class="feat-creator">
        <span class="avatar" :style="{ background: product.avatar }">{{ initials }}</span>
        {{ product.creator }}
      </div>
      <div class="feat-foot">
        <span class="feat-price" :class="{ free: product.price === 0 }">
          {{ product.price === 0 ? '免費' : '$' + product.price }}
        </span>
        <stars :value="product.rating" :count="product.ratingCount" />
      </div>
    </div>
  </a>
</template>
