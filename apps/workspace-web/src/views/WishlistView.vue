<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useDashboardStore } from '@/stores/dashboard'
import { JFmt as F } from '@/utils/format'
import type { WishlistItem } from '@/data/products'

const { t } = useI18n()
const store = useDashboardStore()

const PAGE_SIZE = 12
const page = ref(1)

const totalPages = computed(() => Math.max(1, Math.ceil(store.wishlist.length / PAGE_SIZE)))
const list = computed(() => {
  const start = (page.value - 1) * PAGE_SIZE
  return store.wishlist.slice(start, start + PAGE_SIZE)
})

// 移除項目使總頁數縮減時，將頁碼夾回有效範圍。
watch(totalPages, (n) => { if (page.value > n) page.value = n })

function accent(p: WishlistItem) { return `hsl(${p.hue} 85% 58%)` }
</script>

<template>
  <div data-screen-label="Wishlist">

    <div v-if="store.wishlist.length" class="grid">
      <div v-for="p in list" :key="p.id" class="card" :style="{ '--accent': accent(p) }">
        <div class="fav on" :title="t('wishlist.remove')" @click="store.removeWish(p.id)">
          <app-icon name="heart" :size="17" fill />
        </div>
        <product-thumb :product="p" />
        <div class="card-body">
          <h3 class="card-title">{{ p.title }}</h3>
          <div class="card-creator">
            <span class="avatar" :style="{ background: p.avatar }">{{ F.initials(p.creator) }}</span>
            {{ p.creator }}
          </div>
          <div class="tagrow">
            <span v-for="tag in p.tags" :key="tag" class="chip">{{ tag }}</span>
          </div>
          <div class="card-foot">
            <span class="price" :class="{ free: p.price === 0 }">{{ p.price === 0 ? t('common.free') : '$' + p.price }}</span>
            <stars :value="p.rating" :count="p.ratingCount" />
          </div>
          <n-button type="primary" block strong style="margin-top:14px;">
            <template #icon><app-icon name="cart" :size="16" /></template>
            {{ t('wishlist.addToCart') }}
          </n-button>
        </div>
      </div>
    </div>

    <div v-if="store.wishlist.length" class="wish-pager">
      <n-pagination
        :page="page"
        :page-count="totalPages"
        @update:page="(p: number) => (page = p)" />
    </div>

    <div v-if="!store.wishlist.length" class="empty-box">
      <div class="eb-ic"><app-icon name="heart" :size="30" /></div>
      <div class="eb-t">{{ t('wishlist.emptyTitle') }}</div>
      <p style="margin-top:6px;">{{ t('wishlist.emptyDesc') }}</p>
    </div>
  </div>
</template>

<style scoped>
/* 願望清單採較密的網格與精簡卡片，讓單頁能看到更多筆 */
.grid {
  gap: 16px;
  grid-template-columns: repeat(auto-fill, minmax(210px, 1fr));
}

.card :deep(.thumb) {
  height: 120px;
}

.card-body {
  padding: 12px 13px 13px;
}

.card-title {
  font-size: 15px;
  margin-bottom: 5px;
}

.card-creator {
  font-size: 12px;
}

.tagrow {
  margin-top: 8px;
}

.card-foot {
  margin-top: 10px;
}

.price {
  font-size: 16px;
}

.card-body :deep(.n-button) {
  margin-top: 10px !important;
}

.wish-pager {
  display: flex;
  justify-content: flex-end;
  padding: 20px 4px 4px;
}
</style>
