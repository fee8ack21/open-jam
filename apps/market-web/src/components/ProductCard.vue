<script setup>
/* ============================================================
   ProductCard — storefront grid card. Registered as <product-card>.
   Click opens the detail route; heart toggles favorite.
   ============================================================ */
import { computed } from 'vue';
import { useRouter } from 'vue-router';
import { useShopStore } from '@/stores/shop.js';

const props = defineProps({ product: Object });

const store = useShopStore();
const router = useRouter();

const fav = computed(() => store.isFav(props.product.id));
const accent = computed(() => `hsl(${props.product.hue} 85% 58%)`);
const initials = computed(() => props.product.creator.split(' ').map((s) => s[0]).slice(0, 2).join(''));

function open() { router.push(`/shop/product/${props.product.id}`); }
function toggleFav(e) { e.stopPropagation(); store.toggleFav(props.product.id); }
</script>

<template>
  <div class="card" :style="{ '--accent': accent }" @click="open">
    <div
      class="fav"
      :class="{ on: fav }"
      @click="toggleFav"
      :title="fav ? '已收藏' : '加入最愛'"
    >
      <j-icon name="heart" :size="17" :fill="fav" />
    </div>
    <product-thumb :product="product" />
    <div class="card-body">
      <h3 class="card-title">{{ product.title }}</h3>
      <div class="card-creator">
        <span class="avatar" :style="{ background: product.avatar }">{{ initials }}</span>
        {{ product.creator }}
      </div>
      <div class="tagrow">
        <span v-for="t in product.tags" :key="t" class="chip">{{ t }}</span>
      </div>
      <div class="card-foot">
        <span class="price" :class="{ free: product.price === 0 }">
          {{ product.price === 0 ? '免費' : '$' + product.price }}
        </span>
        <stars :value="product.rating" :count="product.ratingCount" />
      </div>
    </div>
  </div>
</template>
