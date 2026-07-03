<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth'
import NotificationBell from '@/components/NotificationBell.vue'
import { ME as me } from '@/data/products'
import { env } from '@/environment'
import { SUPPORTED_LOCALES, setLocale, type Locale } from '@/i18n'

/** open-drawer：點擊行動版選單鈕時通知父層展開抽屜。 */
const emit = defineEmits<{ 'open-drawer': [] }>()

const route = useRoute()
const authStore = useAuthStore()
const { t, locale } = useI18n()

const userMenuOpen = ref(false)

const langOptions = computed(() =>
  SUPPORTED_LOCALES.map((code) => ({ label: t(`language.${code}`), key: code })),
)
function onSelectLang(key: string) { setLocale(key as Locale) }

const pageTitle = computed(() => {
  const key = route.meta.titleKey as string | undefined
  return key ? t(key) : ''
})
/** 用戶選單顯示的信箱；access token 僅提供 email，無名稱可用。未登入時為空字串。 */
const accountEmail = computed(() => authStore.userEmail ?? '')
/** 頭像字母：信箱首字母大寫。 */
const avatarText = computed(() => (accountEmail.value.charAt(0) || '?').toUpperCase())

let outside: ((e: MouseEvent) => void) | null = null
onMounted(() => {
  outside = (e: MouseEvent) => {
    const target = e.target as Element | null
    if (userMenuOpen.value && !(target && target.closest && target.closest('.user-menu'))) userMenuOpen.value = false
  }
  document.addEventListener('click', outside)
})
onBeforeUnmount(() => {
  if (outside) document.removeEventListener('click', outside)
})

function goToMarket() { window.location.href = env.MARKET_PAGE_URL }
</script>

<template>
  <header class="topbar">
    <button class="mobile-menu icon-btn" @click="emit('open-drawer')" :title="t('header.openMenu')">
      <app-icon name="menu" :size="22" />
    </button>
    <h2 class="tb-title">{{ pageTitle }}</h2>

    <div class="tb-spacer"></div>

    <div class="tb-actions">
      <n-dropdown trigger="click" :options="langOptions" :value="locale" @select="onSelectLang">
        <button class="icon-btn" :title="t('language.label')" :aria-label="t('language.label')">
          <app-icon name="globe" :size="20" />
        </button>
      </n-dropdown>
      <button class="icon-btn" @click="goToMarket" :title="t('header.backToMarket')" :aria-label="t('header.backToMarket')">
        <app-icon name="bag" :size="20" />
      </button>
      <NotificationBell />
      <div class="user-menu">
        <button class="icon-btn user-trigger" :class="{ on: userMenuOpen }" @click="userMenuOpen = !userMenuOpen" :title="t('header.menu')">
          <app-icon name="menu" :size="20" />
        </button>
        <transition name="um">
          <div v-if="userMenuOpen" class="user-pop">
            <div class="um-head">
              <span class="avatar" :style="{ background: me.avatar }">{{ avatarText }}</span>
              <div v-if="accountEmail" class="um-email" :title="accountEmail">{{ accountEmail }}</div>
            </div>
            <div class="um-sep"></div>
            <button class="um-item danger" @click="authStore.logout()"><app-icon name="logout" :size="17" /> {{ t('header.logout') }}</button>
          </div>
        </transition>
      </div>
    </div>
  </header>
</template>
