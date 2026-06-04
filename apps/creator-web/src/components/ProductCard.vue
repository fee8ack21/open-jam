<script>
import { useShopStore } from '../stores/shop.js';
import JIcon from './JIcon.vue';
import Stars from './Stars.vue';
import ProductThumb from './ProductThumb.vue';

export default {
  name: 'ProductCard',
  components: { ProductThumb, JIcon, Stars },
  props: { product: Object },
  setup() { return { store: useShopStore() }; },
  computed: {
    fav() { return this.store.isFav(this.product.id); },
    accent() { return `hsl(${this.product.hue} 85% 58%)`; },
    initials() { return this.product.creator.split(' ').map((s) => s[0]).slice(0, 2).join(''); },
  },
  methods: {
    open() { this.$router.push({ name: 'product', params: { id: this.product.id } }); },
    toggleFav(e) { e.stopPropagation(); this.store.toggleFav(this.product.id); },
  },
};
</script>

<template>
  <div class="card" :style="{ '--accent': accent }" @click="open">
    <div class="fav" :class="{ on: fav }" @click="toggleFav" :title="fav ? '已收藏' : '加入最愛'">
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
