<script setup lang="ts">
/* ============================================================
   CategoryShowcase — 三大分類導覽大卡。即時數量 + 該分類
   熱門作品縮圖堆疊；點擊整卡直接過濾瀏覽區（emit pick）。
   ============================================================ */
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { CATEGORIES, type Product } from '@/data/products';
import LandingArt from '@/components/LandingArt.vue';

const props = defineProps<{ products: Product[] }>();
const emit = defineEmits<{ pick: [catId: string] }>();
const { t } = useI18n();

interface Tile {
  id: string;
  art: string;
  count: number;
  tops: Product[];
}

const tiles = computed<Tile[]>(() =>
  CATEGORIES.map((c) => {
    const inCat = props.products.filter((p) => p.cat === c.id);
    return {
      id: c.id,
      art: 'cat-' + c.id,
      count: inCat.length,
      tops: inCat.slice().sort((a, b) => b.sales - a.sales).slice(0, 3),
    };
  }),
);
</script>

<template>
  <section class="sec cat-show rv">
    <div class="browse-head">
      <p class="browse-eyebrow"><app-icon name="sparkle" :size="13" /> {{ t('market.cats.eyebrow') }}</p>
      <h2 class="browse-title">{{ t('market.cats.title') }}</h2>
    </div>
    <div class="cat-tiles">
      <button
        v-for="tile in tiles"
        :key="tile.id"
        type="button"
        class="cat-tile"
        :class="'ct-' + tile.id"
        :aria-label="t('market.cats.tileAria', { cat: t('category.' + tile.id) })"
        @click="emit('pick', tile.id)"
      >
        <span class="ct-art"><landing-art :name="tile.art" /></span>
        <span class="ct-text">
          <span class="ct-name">{{ t('category.' + tile.id) }}</span>
          <span class="ct-count">{{ t('market.cats.count', { count: tile.count }) }}</span>
        </span>
        <span v-if="tile.tops.length" class="ct-thumbs">
          <product-thumb
            v-for="p in tile.tops"
            :key="p.id"
            :product="p"
            :show-cat="false"
            hide-label
            :glyph-size="18"
          />
        </span>
        <span class="ct-arrow"><app-icon name="chevron" :size="18" :stroke="2.4" /></span>
      </button>
    </div>
  </section>
</template>
