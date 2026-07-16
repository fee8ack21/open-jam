<script setup lang="ts">
/* ============================================================
   FeaturedCard — 店長精選 carousel 直式卡（樣式同 portal-web
   精選作品）。封面上、資訊下，點擊導向商品詳情頁。
   ============================================================ */
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { type Product } from '@/data/products';
import Stars from './Stars.vue';
import ProductThumb from './ProductThumb.vue';
import AppIcon from './app-icon';

const props = defineProps<{ product: Product }>();
const { t } = useI18n();

const initials = computed(() => props.product.creator.split(' ').map((s) => s[0]).slice(0, 2).join(''));
// 誠實標示：店長標記的標「店長精選」，銷量補入的標「熱門」，不讓熱門商品冒充店長精選。
const tag = computed(() =>
  props.product.featured
    ? { icon: 'sparkle', label: t('featured.tagPick') }
    : { icon: 'flame', label: t('featured.tagHot') },
);
</script>

<template>
  <router-link class="feat-card" :to="{ name: 'product', params: { id: product.id } }">
    <div class="feat-media">
      <product-thumb :product="product" hide-label :glyph-size="46" />
      <span class="feat-tag" :class="{ 'feat-tag-hot': !product.featured }"><app-icon :name="tag.icon" :size="11" /> {{ tag.label }}</span>
    </div>
    <div class="feat-info">
      <h3 class="feat-title">{{ product.title }}</h3>
      <p v-if="product.blurb" class="feat-blurb">{{ product.blurb }}</p>
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
  </router-link>
</template>
