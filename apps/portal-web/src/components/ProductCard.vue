<script setup lang="ts">
/* ============================================================
   ProductCard — marketplace-hub catalogue card. Registered as <product-card>.
   Links out to the creator's storefront (creator-web) on their subdomain.
   ============================================================ */
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { env } from '@/environment.js';
import type { Product } from '@/data/products';

const props = defineProps<{
  product: Product;
  /** optional corner ribbon (熱賣 / 新上架 / 精選) surfaced on the thumb */
  badge?: { label: string; tone: 'hot' | 'new' | 'feat' } | null;
}>();
const emit = defineEmits<{ (e: 'select', product: Product): void }>();
const { t } = useI18n();

const href = computed(() => `${env.CREATOR_PAGE_BASE_URL.replace('<store-slug>', props.product.storeSlug)}/product/${props.product.id}`);
const initials = computed(() => props.product.creator.split(' ').map((s) => s[0]).slice(0, 2).join(''));

// Plain left-click opens the in-page quick-view; modified / middle clicks keep
// the native anchor behaviour (open the storefront in a new tab).
function onClick(e: MouseEvent) {
  if (e.metaKey || e.ctrlKey || e.shiftKey || e.altKey || e.button !== 0) return;
  e.preventDefault();
  emit('select', props.product);
}
</script>

<template>
  <a class="mc" :href="href" @click="onClick">
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
          {{ product.price === 0 ? t('common.free') : '$' + product.price }}
        </span>
        <stars :value="product.rating" :count="product.ratingCount" />
      </div>
    </div>
  </a>
</template>
