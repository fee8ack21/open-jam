<script setup lang="ts">
/* ============================================================
   FeaturedCard — wide editorial card for the "精選作品" carousel.
   Image on the left, info on the right. Links out to the
   creator's storefront like <product-card>.
   ============================================================ */
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { env } from '@/environment.js';
import type { Product } from '@/data/products';

const props = defineProps<{ product: Product }>();
const emit = defineEmits<{ (e: 'select', product: Product): void }>();
const { t } = useI18n();

const href = computed(() => `${env.CREATOR_PAGE_BASE_URL.replace('<store-slug>', props.product.storeSlug)}/product/${props.product.id}`);

// Plain left-click opens the in-page quick-view; modified / middle clicks keep
// the native anchor behaviour (open the storefront in a new tab).
function onClick(e: MouseEvent) {
  if (e.metaKey || e.ctrlKey || e.shiftKey || e.altKey || e.button !== 0) return;
  e.preventDefault();
  emit('select', props.product);
}
const initials = computed(() => props.product.creator.split(' ').map((s) => s[0]).slice(0, 2).join(''));
// 誠實標示：人工策展的標「編輯精選」，演算法補入的標「熱門」，不讓熱門商品冒充編輯精選。
const tag = computed(() =>
  props.product.featured
    ? { icon: 'sparkle', label: t('card.editorPick') }
    : { icon: 'flame', label: t('card.hot') },
);
</script>

<template>
  <a class="feat-card" :href="href" @click="onClick">
    <div class="feat-media">
      <product-thumb :product="product" hide-label :glyph-size="46" />
      <span class="feat-tag" :class="{ 'feat-tag-hot': !product.featured }"><app-icon :name="tag.icon" :size="11" /> {{ tag.label }}</span>
    </div>
    <div class="feat-info">
      <h3 class="feat-title">{{ product.title }}</h3>
      <p class="feat-blurb">{{ product.blurb }}</p>
      <div class="feat-creator">
        <span class="avatar" :style="product.avatarUrl ? undefined : { background: product.avatar }">
          <img v-if="product.avatarUrl" :src="product.avatarUrl" alt="" />
          <template v-else>{{ initials }}</template>
        </span>
        {{ product.creator }}
      </div>
      <div class="feat-foot">
        <span class="feat-price" :class="{ free: product.price === 0 }">
          {{ product.price === 0 ? t('common.free') : '$' + product.price }}
        </span>
        <stars :value="product.rating" :count="product.ratingCount" />
      </div>
    </div>
  </a>
</template>
