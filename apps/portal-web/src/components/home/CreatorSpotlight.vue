<script setup lang="ts">
/* ============================================================
   CreatorSpotlight — 市集儀表板「人氣創作者」板。由商品資料
   聚合創作者，依累計銷量取前五，每列連到其子網域店面。
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
  avatarUrl?: string | null;
  works: number;
  sales: number;
}

const creators = computed<CreatorInfo[]>(() => {
  const map = new Map<string, CreatorInfo>();
  for (const p of props.products) {
    const c = map.get(p.storeSlug) ?? {
      slug: p.storeSlug, name: p.creator, handle: p.handle, avatar: p.avatar, avatarUrl: p.avatarUrl, works: 0, sales: 0,
    };
    c.works += 1;
    c.sales += p.sales;
    map.set(p.storeSlug, c);
  }
  return [...map.values()].sort((a, b) => b.sales - a.sales).slice(0, 5);
});

function initials(name: string): string {
  return name.split(' ').map((s) => s[0]).slice(0, 2).join('');
}
function href(slug: string): string {
  return env.CREATOR_PAGE_BASE_URL.replace('<store-slug>', slug);
}
</script>

<template>
  <div class="board">
    <div class="board-head bh-creators">
      <span class="board-ic"><app-icon name="user" :size="15" /></span>
      <h3 class="board-title">{{ t('market.creators.title') }}</h3>
      <span class="board-tag">TOP 5</span>
    </div>
    <ol class="brd-list">
      <li v-for="(c, i) in creators" :key="c.slug">
        <a class="brd-row" :href="href(c.slug)">
          <span class="brd-rank" :class="{ 'brd-rank-1': i === 0 }">{{ i + 1 }}</span>
          <span class="avatar brd-avatar" :style="c.avatarUrl ? undefined : { background: c.avatar }">
            <img v-if="c.avatarUrl" :src="c.avatarUrl" alt="" />
            <template v-else>{{ initials(c.name) }}</template>
          </span>
          <span class="brd-info">
            <span class="brd-title">{{ c.name }}</span>
            <span class="brd-sub">{{ c.handle }}</span>
          </span>
          <span class="brd-side">
            <span class="brd-main">{{ t('market.creators.works', { count: c.works }) }}</span>
            <span class="brd-note">{{ t('market.creators.sold', { count: c.sales.toLocaleString() }) }}</span>
          </span>
        </a>
      </li>
    </ol>
  </div>
</template>
