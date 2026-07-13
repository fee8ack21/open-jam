<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { useDashboardStore } from '@/stores/dashboard'
import { useAuthStore } from '@/stores/auth'
import AppSidebar from '@/layout/AppSidebar.vue'
import AppHeader from '@/layout/AppHeader.vue'

const route = useRoute()
const store = useDashboardStore()
const authStore = useAuthStore()

const drawerOpen = ref(false)

/* v3 果醬罐色系（後台簡約變體）：primary 按鈕黃底墨線、danger 深粉、小圓角矩形 */
const ink = '#1a1a1a'
const inkBorder = `1.5px solid ${ink}`
const overrides = {
  common: {
    primaryColor: '#8a5cf6', primaryColorHover: '#a685ff',
    primaryColorPressed: '#7a4ee0', primaryColorSuppl: '#a685ff',
    errorColor: '#d6479b', errorColorHover: '#e05aac', errorColorPressed: '#c23a8b',
    borderRadius: '10px', borderRadiusSmall: '8px',
    textColorBase: ink, fontFamily: "'Noto Sans TC', sans-serif",
    fontWeightStrong: '900',
  },
  Button: {
    fontWeight: '900',
    borderRadiusTiny: '8px', borderRadiusSmall: '8px', borderRadiusMedium: '10px', borderRadiusLarge: '10px',
    // default（白底墨線、hover 黃）
    color: '#ffffff', colorHover: '#ffde00', colorPressed: '#f2d300', colorFocus: '#ffffff',
    textColor: ink, textColorHover: ink, textColorPressed: ink, textColorFocus: ink,
    border: inkBorder, borderHover: inkBorder, borderPressed: inkBorder, borderFocus: inkBorder,
    // primary（黃底墨線，硬底影在 base.css）
    colorPrimary: '#ffde00', colorHoverPrimary: '#ffde00', colorPressedPrimary: '#f2d300', colorFocusPrimary: '#ffde00',
    textColorPrimary: ink, textColorHoverPrimary: ink, textColorPressedPrimary: ink, textColorFocusPrimary: ink,
    borderPrimary: inkBorder, borderHoverPrimary: inkBorder, borderPressedPrimary: inkBorder, borderFocusPrimary: inkBorder,
    // error（深粉底白字墨線）
    colorError: '#d6479b', colorHoverError: '#e05aac', colorPressedError: '#c23a8b', colorFocusError: '#d6479b',
    textColorError: '#ffffff', textColorHoverError: '#ffffff', textColorPressedError: '#ffffff', textColorFocusError: '#ffffff',
    borderError: inkBorder, borderHoverError: inkBorder, borderPressedError: inkBorder, borderFocusError: inkBorder,
    // disabled 維持同色（naive 以 opacityDisabled 淡化）
    colorDisabled: '#ffffff', textColorDisabled: ink, borderDisabled: inkBorder,
    colorDisabledPrimary: '#ffde00', textColorDisabledPrimary: ink, borderDisabledPrimary: inkBorder,
    colorDisabledError: '#d6479b', textColorDisabledError: '#ffffff', borderDisabledError: inkBorder,
  },
  Tag: {
    // 狀態膠囊：糖果底 + 墨線 + 墨字（設計稿 pill）
    color: '#ffffff', textColor: ink, border: inkBorder,
    colorSuccess: '#b8ff9f', textColorSuccess: ink, borderSuccess: inkBorder,
    colorInfo: '#7dd9ff', textColorInfo: ink, borderInfo: inkBorder,
    colorWarning: '#fff3c4', textColorWarning: ink, borderWarning: inkBorder,
    colorError: '#ffe3f6', textColorError: '#d6479b', borderError: inkBorder,
    colorPrimary: '#e9dfff', textColorPrimary: ink, borderPrimary: inkBorder,
    fontWeightStrong: '900',
  },
  Input: { heightMedium: '40px', heightLarge: '46px', caretColor: '#8a5cf6' },
  InternalSelection: { heightMedium: '40px' },
  Switch: { railColorActive: ink },
  Checkbox: { colorChecked: '#ffde00', checkMarkColor: ink, borderChecked: inkBorder, borderFocus: inkBorder },
  Popover: { borderRadius: '10px', padding: '6px' },
}

/** 是否為一般使用者：唯一擁有賣家/上架流程的角色。 */
const canSell = computed(() => authStore.isUser)

watch(() => route.name, (name) => { if (name) store.syncModeToRoute(name as string) }, { immediate: true })
// 非賣家角色不應停留在賣家模式
watch(canSell, (can) => {
  if (!authStore.isReady || !authStore.isAuthenticated) return
  if (!can && store.mode === 'sell') store.setMode('buy')
}, { immediate: true })
</script>

<template>
  <n-config-provider :theme="null" :theme-overrides="overrides">
  <n-message-provider>
  <n-dialog-provider>
    <div class="oj-root">
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
    </div>
  </n-dialog-provider>
  </n-message-provider>
  </n-config-provider>
</template>
