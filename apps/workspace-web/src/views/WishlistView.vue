<script setup lang="ts">
import { computed } from 'vue'
import { useDashboardStore } from '@/stores/dashboard'
import { JFmt as F } from '@/utils/format'
import type { WishlistItem } from '@/data'

const store = useDashboardStore()
const list = computed(() => store.wishlist)

function accent(p: WishlistItem) { return `hsl(${p.hue} 85% 58%)` }
</script>

<template>
  <div data-screen-label="Wishlist">
    <div class="page-head">
      <div>
        <p class="h-eyebrow">我的收藏庫</p>
        <h1 class="h-title">Wishlist 願望清單</h1>
        <p class="h-sub">{{ list.length }} 件想收藏的作品。降價或上新時我們會通知你。</p>
      </div>
    </div>

    <div v-if="list.length" class="grid">
      <div v-for="p in list" :key="p.id" class="card" :style="{ '--accent': accent(p) }">
        <div class="fav on" title="從願望清單移除" @click="store.removeWish(p.id)">
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
            <span v-for="t in p.tags" :key="t" class="chip">{{ t }}</span>
          </div>
          <div class="card-foot">
            <span class="price" :class="{ free: p.price === 0 }">{{ p.price === 0 ? '免費' : '$' + p.price }}</span>
            <stars :value="p.rating" :count="p.ratingCount" />
          </div>
          <n-button type="primary" block strong style="margin-top:14px;">
            <template #icon><app-icon name="cart" :size="16" /></template>
            加入購物車
          </n-button>
        </div>
      </div>
    </div>

    <div v-else class="empty-box">
      <div class="eb-ic"><app-icon name="heart" :size="30" /></div>
      <div class="eb-t">願望清單是空的</div>
      <p style="margin-top:6px;">在商城點愛心，把心儀的作品存到這裡。</p>
    </div>
  </div>
</template>
