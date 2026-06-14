<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import { useDashboardStore } from '@/stores/dashboard'
import { useAuthStore } from '@/stores/auth'
import { ME as me } from '@/data'

/** open-drawer：點擊行動版選單鈕時通知父層展開抽屜。 */
const emit = defineEmits<{ 'open-drawer': [] }>()

const route = useRoute()
const store = useDashboardStore()
const authStore = useAuthStore()

const userMenuOpen = ref(false)

const pageTitle = computed(() => route.meta.title || '')
/** 用戶選單顯示的信箱；access token 僅提供 email，無名稱可用。 */
const accountEmail = computed(() => authStore.userEmail ?? me.email)
/** 頭像字母：信箱首字母大寫。 */
const avatarText = computed(() => (accountEmail.value.charAt(0) || '?').toUpperCase())

let outside: ((e: MouseEvent) => void) | null = null
onMounted(() => {
  outside = (e: MouseEvent) => {
    const t = e.target as Element | null
    if (userMenuOpen.value && !(t && t.closest && t.closest('.user-menu'))) userMenuOpen.value = false
  }
  document.addEventListener('click', outside)
})
onBeforeUnmount(() => {
  if (outside) document.removeEventListener('click', outside)
})

function nav(view: string) { store.go(view) }
</script>

<template>
  <header class="topbar">
    <button class="mobile-menu icon-btn" @click="emit('open-drawer')" title="開啟選單">
      <app-icon name="menu" :size="22" />
    </button>
    <h2 class="tb-title">{{ pageTitle }}</h2>

    <div class="tb-spacer"></div>

    <div class="tb-actions">
      <div class="icon-btn" title="通知">
        <app-icon name="bell" :size="20" />
        <span class="cart-badge">3</span>
      </div>
      <div class="user-menu">
        <button class="icon-btn user-trigger" :class="{ on: userMenuOpen }" @click="userMenuOpen = !userMenuOpen" title="選單">
          <app-icon name="menu" :size="20" />
        </button>
        <transition name="um">
          <div v-if="userMenuOpen" class="user-pop">
            <div class="um-head">
              <span class="avatar" :style="{ background: me.avatar }">{{ avatarText }}</span>
              <div class="um-email" :title="accountEmail">{{ accountEmail }}</div>
            </div>
            <div class="um-sep"></div>
            <button class="um-item" @click="nav('settings'); userMenuOpen = false"><app-icon name="gear" :size="17" /> 帳號設定</button>
            <button class="um-item danger" @click="authStore.logout()"><app-icon name="logout" :size="17" /> 登出</button>
          </div>
        </transition>
      </div>
    </div>
  </header>
</template>
