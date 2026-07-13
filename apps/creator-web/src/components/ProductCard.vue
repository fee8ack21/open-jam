<script setup lang="ts">
/* ============================================================
   ProductCard — storefront catalogue card (.mc, portal-web style).
   Routes to the product detail page; corner badge surfaces
   熱賣 / 新上架. Favouriting lives on the detail page.
   ============================================================ */
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { type Product } from '@/data/products';
import Stars from './Stars.vue';
import ProductThumb from './ProductThumb.vue';
import AppIcon from './app-icon';

/** 徽章 icon（設計稿語彙）：熱賣一律火焰、新上架 / 精選用四角星光。 */
const BADGE_ICONS: Record<string, string> = { hot: 'flame', new: 'sparkle', feat: 'sparkle' };

const props = defineProps<{
  product: Product;
  /** optional corner ribbon (熱賣 / 新上架 / 精選) surfaced on the thumb */
  badge?: { label: string; tone: 'hot' | 'new' | 'feat' } | null;
}>();
const { t } = useI18n();

const initials = computed(() => props.product.creator.split(' ').map((s) => s[0]).slice(0, 2).join(''));
</script>

<template>
  <router-link class="mc" :to="{ name: 'product', params: { id: product.id } }">
    <product-thumb :product="product" />
    <span v-if="badge" class="mc-badge" :class="'b-' + badge.tone">
      <app-icon :name="BADGE_ICONS[badge.tone]" :size="10" />{{ badge.label }}
    </span>
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
  </router-link>
</template>
