<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { useDashboardStore } from '@/stores/dashboard'
import { useAuthStore } from '@/stores/auth'
import { useStoreApplicationStore } from '@/stores/storeApplication'

const route = useRoute()
const store = useDashboardStore()
const authStore = useAuthStore()
const storeAppStore = useStoreApplicationStore()

/** 是否為一般使用者：唯一擁有賣家/上架流程的角色。 */
const canSell = computed(() => authStore.isUser)
/** 是否已開店：未開店前賣家選單只露出「開店」。 */
const hasStore = computed(() => storeAppStore.hasStore)

const mobileNav = computed(() => {
  const buy = { view: 'purchases', label: '已購', icon: 'bag' }
  const settings = { view: 'settings', label: '設定', icon: 'gear' }
  if (!canSell.value) {
    return [buy, { view: 'wishlist', label: '收藏', icon: 'heart' }, settings]
  }
  if (!hasStore.value) {
    return [{ view: 'open-store', label: '開店', icon: 'rocket' }, buy, settings]
  }
  return [
    { view: 'overview', label: '總覽', icon: 'grid' },
    { view: 'products', label: '商品', icon: 'box' },
    { view: 'upload', label: '上架', icon: 'plus' },
    buy,
    settings,
  ]
})

function nav(view: string) { store.go(view) }
function isActive(view: string) { return route.name === view }
</script>

<template>
  <nav class="bottom-nav">
    <button v-for="it in mobileNav" :key="it.view" class="bn-item" :class="{ on: isActive(it.view) }"
            @click="nav(it.view)">
      <app-icon :name="it.icon" :size="21" />
      {{ it.label }}
    </button>
  </nav>
</template>
