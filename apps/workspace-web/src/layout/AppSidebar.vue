<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { useDashboardStore } from '@/stores/dashboard'
import { useAuthStore } from '@/stores/auth'
import { useStoreApplicationStore } from '@/stores/storeApplication'

/** 賣家 / 買家兩組導覽項目；countKey 對應 store 中的數量。 */
const NAV = {
  sell: [
    { view: 'overview', label: '儀表板', icon: 'grid' },
    { view: 'open-store', label: '開店', icon: 'rocket' },
    { view: 'products', label: '商品管理', icon: 'box', countKey: 'products' },
    { view: 'upload', label: '上架新作品', icon: 'upload' },
    { view: 'orders', label: '訂單管理', icon: 'receipt', countKey: 'orders' },
  ],
  buy: [
    { view: 'purchases', label: '購買紀錄', icon: 'bag', countKey: 'purchases' },
    { view: 'wishlist', label: 'Wishlist', icon: 'heart', countKey: 'wishlist' },
  ],
}

/** open：行動版抽屜是否展開（由父層控制）。 */
defineProps<{ open: boolean }>()
/** navigate：選單項目被點擊後通知父層收合抽屜。 */
const emit = defineEmits<{ navigate: [] }>()

const route = useRoute()
const store = useDashboardStore()
const authStore = useAuthStore()
const storeAppStore = useStoreApplicationStore()

/** 是否為一般使用者：唯一擁有賣家/上架流程的角色。 */
const canSell = computed(() => authStore.isUser)
/** 是否已開店：未開店前賣家選單只露出「開店」。 */
const hasStore = computed(() => storeAppStore.hasStore)
const navSell = computed(() => {
  // 尚未開店：賣家選單只顯示「開店」，開店成功後才顯示其餘項目
  if (!hasStore.value) return NAV.sell.filter(it => it.view === 'open-store')
  return NAV.sell
})
const navBuy = computed(() => NAV.buy)

function count(key?: string) {
  if (!key) return null
  const map: Record<string, number> = {
    products: store.products.length,
    orders: store.paidOrders.length,
    purchases: store.purchases.length,
    wishlist: store.wishlist.length,
  }
  return map[key]
}
function nav(view: string) { store.go(view); emit('navigate') }
function pickMode(m: string) { store.setMode(m) }
function isActive(view: string) { return route.name === view }
</script>

<template>
  <aside class="side" :class="{ 'drawer-open': open }">
    <div class="side-brand" @click="nav('overview')">
      <span class="brand-mark">
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
          <path d="M15 16.4V4.5c3.7 1 5 3.9 2 6.8" stroke="#fff" stroke-width="2.3" stroke-linecap="round" stroke-linejoin="round"></path>
          <ellipse cx="10.4" cy="16.8" rx="4.7" ry="3.5" fill="#fff" transform="rotate(-22 10.4 16.8)"></ellipse>
        </svg>
      </span>
      <span class="brand-name">Open Jam<small>Creator Studio</small></span>
    </div>

    <div class="mode-switch">
      <button v-if="canSell" :class="{ on: store.mode === 'sell' }" @click="pickMode('sell')"><j-icon name="rocket" :size="15" /> 賣家</button>
      <button :class="{ on: store.mode === 'buy' }" @click="pickMode('buy')"><j-icon name="bag" :size="15" /> 買家</button>
    </div>

    <nav style="flex:1; overflow-y:auto;">
      <template v-if="store.mode === 'sell'">
        <div class="nav-group">
          <div class="nav-label">賣家工作室</div>
          <div v-for="it in navSell" :key="it.view" class="nav-item" :class="{ on: isActive(it.view) }" @click="nav(it.view)">
            <span class="nav-ic"><j-icon :name="it.icon" :size="19" /></span>
            <span>{{ it.label }}</span>
            <span v-if="count(it.countKey) != null" class="nav-count">{{ count(it.countKey) }}</span>
          </div>
        </div>
      </template>
      <template v-else>
        <div class="nav-group">
          <div class="nav-label">我的收藏庫</div>
          <div v-for="it in navBuy" :key="it.view" class="nav-item" :class="{ on: isActive(it.view) }" @click="nav(it.view)">
            <span class="nav-ic"><j-icon :name="it.icon" :size="19" /></span>
            <span>{{ it.label }}</span>
            <span v-if="count(it.countKey) != null" class="nav-count">{{ count(it.countKey) }}</span>
          </div>
        </div>
      </template>
    </nav>
  </aside>
</template>
