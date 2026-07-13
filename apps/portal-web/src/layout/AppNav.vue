<script setup lang="ts">
/* ============================================================
   AppNav — portal-web 全站頂部導覽列
   果醬罐 logo + 工具鈕（語言 / GitHub / 文件 / 通知）+
   登入黑色膠囊。
   ============================================================ */
import { computed, onBeforeUnmount, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { useAuthStore } from '@/stores/auth.js';
import { useNotificationsStore } from '@/stores/notifications';
import BrandLogo from '@/components/BrandLogo.vue';
import NotificationBell from '@/components/NotificationBell.vue';
import { env } from '@/environment.js';
import { SUPPORTED_LOCALES, setLocale, type Locale } from '@/i18n';

const auth = useAuthStore();
const notifications = useNotificationsStore();
const { t, locale } = useI18n();

function goWorkspace() {
  window.location.href = env.WORKSPACE_PAGE_URL;
}

/* ---------- 語系下拉（果醬罐風格自訂 popover） ---------- */
const LOCALE_SHORT: Record<Locale, string> = { 'zh-TW': '中', en: 'EN' };
const langOptions = computed(() =>
  SUPPORTED_LOCALES.map((code) => ({ label: t(`language.${code}`), code })),
);
const currentShort = computed(() => LOCALE_SHORT[locale.value as Locale] ?? '中');
/* 手機選單顯示完整語言名稱（繁體中文 / English） */
const currentFull = computed(() => t(`language.${locale.value}`));

const langOpen = ref(false);
const langRoot = ref<HTMLElement | null>(null);

function onDocPointer(e: PointerEvent) {
  if (langRoot.value && !langRoot.value.contains(e.target as Node)) closeLang();
}
function openLang() {
  if (langOpen.value) return;
  langOpen.value = true;
  document.addEventListener('pointerdown', onDocPointer, true);
}
function closeLang() {
  if (!langOpen.value) return;
  langOpen.value = false;
  document.removeEventListener('pointerdown', onDocPointer, true);
}
function toggleLang() {
  langOpen.value ? closeLang() : openLang();
}
function onSelectLang(code: Locale) {
  setLocale(code);
  closeLang();
}
/* ---------- 手機版 burger 選單 ---------- */
const menuOpen = ref(false);
const navRoot = ref<HTMLElement | null>(null);

function onMenuDocPointer(e: PointerEvent) {
  if (navRoot.value && !navRoot.value.contains(e.target as Node)) closeMenu();
}
function closeMenu() {
  if (!menuOpen.value) return;
  menuOpen.value = false;
  document.removeEventListener('pointerdown', onMenuDocPointer, true);
}
function toggleMenu() {
  if (menuOpen.value) {
    closeMenu();
    return;
  }
  menuOpen.value = true;
  document.addEventListener('pointerdown', onMenuDocPointer, true);
}

onBeforeUnmount(() => {
  document.removeEventListener('pointerdown', onDocPointer, true);
  document.removeEventListener('pointerdown', onMenuDocPointer, true);
});
</script>

<template>
  <header ref="navRoot" class="nav" :class="{ 'menu-open': menuOpen }">
    <div class="nav-inner">
      <BrandLogo badge />

      <div class="nav-spacer"></div>

      <button
        type="button"
        class="nav-burger"
        :title="t('nav.menu')"
        :aria-label="t('nav.menu')"
        :aria-expanded="menuOpen"
        @click="toggleMenu"
      >
        <app-icon :name="menuOpen ? 'close' : 'menu'" :size="19" />
        <span
          v-if="!menuOpen && auth.isAuthenticated && notifications.unreadCount > 0"
          class="nav-burger-dot"
          aria-hidden="true"
        ></span>
      </button>

      <div class="nav-actions">
        <a
          class="nav-ic"
          :href="env.GITHUB_REPO_URL"
          target="_blank"
          rel="noopener noreferrer"
          :title="t('nav.github')"
          @click="closeMenu"
        >
          <app-icon name="github" :size="18" />
          <span class="nav-act-label">{{ t('nav.github') }}</span>
        </a>
        <a
          class="nav-ic"
          :href="env.DOCS_URL"
          target="_blank"
          rel="noopener noreferrer"
          :title="t('nav.docs')"
          @click="closeMenu"
        >
          <app-icon name="book" :size="18" />
          <span class="nav-act-label">{{ t('nav.docs') }}</span>
        </a>
        <div ref="langRoot" class="lang">
          <button
            type="button"
            class="lang-btn"
            :class="{ open: langOpen }"
            :title="t('language.label')"
            :aria-label="t('language.label')"
            aria-haspopup="menu"
            :aria-expanded="langOpen"
            @click="toggleLang"
          >
            <app-icon name="globe" :size="17" />
            <span class="lang-short">{{ currentShort }}</span>
            <span class="lang-full nav-act-label">{{ currentFull }}</span>
            <app-icon name="chevronD" class="lang-caret" :size="12" />
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
        <template v-if="auth.isAuthenticated">
          <NotificationBell />
          <a class="nav-admin" href="#" :title="t('nav.workspace')" @click.prevent="goWorkspace">
            <span class="nav-admin-label">{{ t('nav.workspace') }}</span>
            <app-icon name="arrow" :size="14" />
          </a>
          <a class="nav-logout" href="#" :title="t('nav.logout')" @click.prevent="auth.logout()">
            <app-icon name="logout" :size="17" />
            <span class="nav-act-label">{{ t('nav.logout') }}</span>
          </a>
        </template>
        <template v-else>
          <a class="nav-admin" href="#" :title="t('nav.login')" @click.prevent="auth.login()">
            <span class="nav-admin-label">{{ t('nav.login') }}</span>
            <app-icon name="arrow" :size="14" />
          </a>
        </template>
      </div>
    </div>
  </header>
</template>
