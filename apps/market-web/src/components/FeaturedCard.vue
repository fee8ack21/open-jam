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
</script>

<template>
  <a class="feat-card" :href="href">
    <div class="feat-media">
      <product-thumb :product="product" hide-label />
    </div>
    <div class="feat-info">
      <span class="feat-tag"><app-icon name="sparkle" :size="12" /> 編輯精選</span>
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
