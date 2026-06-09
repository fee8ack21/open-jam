<script setup lang="ts">
/* ============================================================
   MktCard — marketplace-hub catalogue card. Registered as <mkt-card>.
   Links out to the creator's storefront (creator-web) on their subdomain.
   ============================================================ */
import { computed } from 'vue';
import { env } from '@/environment.js';
import type { Product } from '@/data/catalogue.js';

const props = defineProps<{ product: Product }>();

const BASE = env.CREATOR_BASE_URL;
const href = computed(() => `${BASE}/product/${props.product.id}`);
const initials = computed(() => props.product.creator.split(' ').map((s) => s[0]).slice(0, 2).join(''));
</script>

<template>
  <a class="mc" :href="href">
    <product-thumb :product="product" />
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
