<script setup lang="ts">
/* ============================================================
   CreatorSpotlight — 活躍創作者（呼應 Epidemic Sound 的
   artist spotlight）。由商品資料聚合出創作者，依累計銷量
   取前四位，整卡連到其子網域店面。
   ============================================================ */
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { env } from '@/environment.js';
import type { Product } from '@/data/products';

const props = defineProps<{ products: Product[] }>();
const { t } = useI18n();

interface CreatorInfo {
  slug: string;
  name: string;
  handle: string;
  avatar: string;
  works: number;
  sales: number;
}

const creators = computed<CreatorInfo[]>(() => {
  const map = new Map<string, CreatorInfo>();
  for (const p of props.products) {
    const c = map.get(p.storeSlug) ?? {
      slug: p.storeSlug, name: p.creator, handle: p.handle, avatar: p.avatar, works: 0, sales: 0,
    };
    c.works += 1;
    c.sales += p.sales;
    map.set(p.storeSlug, c);
  }
  return [...map.values()].sort((a, b) => b.sales - a.sales).slice(0, 4);
});

function initials(name: string): string {
  return name.split(' ').map((s) => s[0]).slice(0, 2).join('');
}
function href(slug: string): string {
  return env.CREATOR_PAGE_BASE_URL.replace('<store-slug>', slug);
}
</script>

<template>
  <div class="spotlight">
    <div class="pulse-head">
      <p class="browse-eyebrow"><app-icon name="user" :size="13" /> {{ t('market.creators.eyebrow') }}</p>
      <h2 class="pulse-title">{{ t('market.creators.title') }}</h2>
    </div>
    <div class="cs-list">
      <a v-for="c in creators" :key="c.slug" class="cs-card" :href="href(c.slug)">
        <span class="avatar cs-avatar" :style="{ background: c.avatar }">{{ initials(c.name) }}</span>
        <span class="cs-info">
          <span class="cs-name">{{ c.name }}</span>
          <span class="cs-handle">{{ c.handle }}</span>
          <span class="cs-meta">{{ t('market.creators.meta', { works: c.works, sales: c.sales.toLocaleString() }) }}</span>
        </span>
        <span class="cs-visit">{{ t('market.creators.visit') }}</span>
      </a>
    </div>
  </div>
</template>
