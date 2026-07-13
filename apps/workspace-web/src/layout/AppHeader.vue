<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth'
import NotificationBell from '@/components/NotificationBell.vue'
import { env } from '@/environment'
import { SUPPORTED_LOCALES, setLocale, type Locale } from '@/i18n'

/** open-drawer：點擊行動版選單鈕時通知父層展開抽屜。 */
const emit = defineEmits<{ 'open-drawer': [] }>()

const route = useRoute()
const authStore = useAuthStore()
const { t, locale } = useI18n()

const userMenuOpen = ref(false)

const langOptions = computed(() =>
  SUPPORTED_LOCALES.map((code) => ({ label: t(`language.${code}`), code })),
)

const langOpen = ref(false)
const langRoot = ref<HTMLElement | null>(null)

function onDocPointer(e: PointerEvent) {
  if (langRoot.value && !langRoot.value.contains(e.target as Node)) closeLang()
}
function openLang() {
  if (langOpen.value) return
  langOpen.value = true
  document.addEventListener('pointerdown', onDocPointer, true)
}
function closeLang() {
  if (!langOpen.value) return
  langOpen.value = false
  document.removeEventListener('pointerdown', onDocPointer, true)
}
function toggleLang() {
  langOpen.value ? closeLang() : openLang()
}
function onSelectLang(code: Locale) {
  setLocale(code)
  closeLang()
}

const pageTitle = computed(() => {
  const key = route.meta.titleKey as string | undefined
  return key ? t(key) : ''
})
/** 用戶選單顯示的信箱；access token 僅提供 email，無名稱可用。未登入時為空字串。 */
const accountEmail = computed(() => authStore.userEmail ?? '')
/** 頭像字母：信箱首字母大寫。 */
const avatarText = computed(() => (accountEmail.value.charAt(0) || '?').toUpperCase())
/** 使用者膠囊顯示名：信箱 @ 前段（設計稿 topbar user pill）。 */
const accountName = computed(() => accountEmail.value.split('@')[0] || '')
/** 管理員顯示 ADMIN 黃籤（設計稿 topbar）。 */
const isAdmin = computed(() => authStore.isAdmin)

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
  document.removeEventListener('pointerdown', onDocPointer, true)
})

function goToMarket() { window.location.href = env.PORTAL_PAGE_URL }
</script>

<template>
  <header class="topbar">
    <button class="mobile-menu icon-btn" @click="emit('open-drawer')" :title="t('header.openMenu')">
      <app-icon name="menu" :size="22" />
    </button>
    <h2 class="tb-title">{{ pageTitle }}</h2>
    <span v-if="isAdmin" class="tb-badge">ADMIN</span>

    <div class="tb-spacer"></div>

    <div class="tb-actions">
      <div ref="langRoot" class="lang">
        <button
          type="button"
          class="icon-btn"
          :class="{ 'lang-open': langOpen }"
          :title="t('language.label')"
          :aria-label="t('language.label')"
          aria-haspopup="menu"
          :aria-expanded="langOpen"
          @click="toggleLang"
        >
          <app-icon name="globe" :size="20" />
        </button>
        <transition name="lang-pop">
          <div v-if="langOpen" class="lang-menu" role="menu">
            <button
              v-for="opt in langOptions"
              :key="opt.code"
              type="button"
              class="lang-opt"
              :class="{ active: opt.code === locale }"
              role="menuitemradio"
              :aria-checked="opt.code === locale"
              @click="onSelectLang(opt.code)"
            >
              <span class="lang-opt-label">{{ opt.label }}</span>
              <app-icon v-if="opt.code === locale" name="check" class="lang-opt-check" :size="15" />
            </button>
          </div>
        </transition>
      </div>
      <button class="icon-btn" @click="goToMarket" :title="t('header.backToMarket')" :aria-label="t('header.backToMarket')">
        <app-icon name="bag" :size="20" />
      </button>
      <NotificationBell />
      <div class="user-menu">
        <button class="user-trigger" :class="{ on: userMenuOpen }" @click="userMenuOpen = !userMenuOpen" :title="t('header.menu')">
          <span class="avatar">{{ avatarText }}</span>
          <span class="ut-name">{{ accountName }}</span>
        </button>
        <transition name="um">
          <div v-if="userMenuOpen" class="user-pop">
            <div class="um-head">
              <span class="avatar" style="background: var(--c-violet)">{{ avatarText }}</span>
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
