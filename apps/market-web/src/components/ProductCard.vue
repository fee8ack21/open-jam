<script setup lang="ts">
/* ============================================================
   ProductCard — marketplace-hub catalogue card. Registered as <product-card>.
   Links out to the creator's storefront (creator-web) on their subdomain.
   ============================================================ */
import { computed } from 'vue';
import { env } from '@/environment.js';
import type { Product } from '@/data/products';

const props = defineProps<{
  product: Product;
  /** optional corner ribbon (熱賣 / 新上架 / 精選) surfaced on the thumb */
  badge?: { label: string; tone: 'hot' | 'new' | 'feat' } | null;
}>();

const slug = computed(() => props.product.handle.replace(/^@/, ''));
const href = computed(() => `${env.CREATOR_PAGE_BASE_URL.replace('<store-slug>', slug.value)}/products/${props.product.id}`);
const initials = computed(() => props.product.creator.split(' ').map((s) => s[0]).slice(0, 2).join(''));
</script>

<template>
  <a class="mc" :href="href">
    <product-thumb :product="product" />
    <span v-if="badge" class="mc-badge" :class="'b-' + badge.tone">{{ badge.label }}</span>
    <div class="mc-body">
      <h3 class="mc-title">{{ product.title }}</h3>
      <div class="mc-creator">
        <span class="avatar" :style="{ background: product.avatar }">{{ initials }}</span>
        {{ product.creator }}
      </div>
      <div class="mc-foot">
        <span class="mc-price" :class="{ free: product.price === 0 }">
          {{ product.price === 0 ? '免費' : '$' + product.price }}
        </span>
        <stars :value="product.rating" :count="product.ratingCount" />
      </div>
    </div>
  </a>
</template>
