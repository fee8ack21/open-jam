<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { useDashboardStore } from '@/stores/dashboard'
import { useAuthStore } from '@/stores/auth'
import AppSidebar from '@/layout/AppSidebar.vue'
import AppHeader from '@/layout/AppHeader.vue'
import AppBottomNav from '@/layout/AppBottomNav.vue'

const route = useRoute()
const store = useDashboardStore()
const authStore = useAuthStore()

const drawerOpen = ref(false)

const overrides = {
  common: {
    primaryColor: '#5639d6', primaryColorHover: '#7a63ee',
    primaryColorPressed: '#4a30bd', primaryColorSuppl: '#7a63ee',
    borderRadius: '12px', borderRadiusSmall: '9px',
    fontFamily: "'Space Grotesk', 'Noto Sans TC', sans-serif",
    fontWeightStrong: '700',
  },
  Button: { fontWeight: '700' },
  Input: { borderRadius: '12px', heightMedium: '42px', heightLarge: '46px', caretColor: '#6c4cf1' },
  InternalSelection: { borderRadius: '12px', heightMedium: '42px' },
  Switch: { railColorActive: '#5639d6' },
  Popover: { borderRadius: '14px', padding: '6px' },
}

const rootClass = computed(() =>
  ['light', 'dash-theme', 'font-' + store.font, store.density === 'compact' ? 'is-compact' : ''],
)
/** 是否為一般使用者：唯一擁有賣家/上架流程的角色。 */
const canSell = computed(() => authStore.isUser)

watch(() => route.name, (name) => { if (name) store.syncModeToRoute(name as string) }, { immediate: true })
// 非賣家角色不應停留在賣家模式
watch(canSell, (can) => { if (!can && store.mode === 'sell') store.setMode('buy') }, { immediate: true })
</script>

<template>
  <n-config-provider :theme="null" :theme-overrides="overrides">
  <n-message-provider>
    <div class="oj-root" :class="rootClass">
      <div class="dash-shell">
        <app-sidebar :open="drawerOpen" @navigate="drawerOpen = false" />

        <!-- mobile drawer scrim -->
        <div class="drawer-scrim" :class="{ show: drawerOpen }" @click="drawerOpen = false"></div>

        <div class="dash-main">
          <app-header @open-drawer="drawerOpen = true" />
          <main class="dash-body">
            <router-view />
          </main>
        </div>
      </div>

      <app-bottom-nav />
    </div>
  </n-message-provider>
  </n-config-provider>
</template>
