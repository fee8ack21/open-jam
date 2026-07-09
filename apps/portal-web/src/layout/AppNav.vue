<script setup lang="ts">
/* ============================================================
   AppNav — portal-web 全站頂部導覽列
   全站一致顯示語言切換／登入／後台／登出。
   ============================================================ */
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { useAuthStore } from '@/stores/auth.js';
import BrandLogo from '@/components/BrandLogo.vue';
import NotificationBell from '@/components/NotificationBell.vue';
import { env } from '@/environment.js';
import { SUPPORTED_LOCALES, setLocale, type Locale } from '@/i18n';

const auth = useAuthStore();
const { t, locale } = useI18n();

function goWorkspace() {
  window.location.href = env.WORKSPACE_PAGE_URL;
}

const langOptions = computed(() =>
  SUPPORTED_LOCALES.map((code) => ({ label: t(`language.${code}`), key: code })),
);
function onSelectLang(key: string) {
  setLocale(key as Locale);
}
</script>

<template>
  <header class="nav">
    <div class="nav-inner">
      <BrandLogo />

      <div class="nav-spacer"></div>

      <div class="nav-actions">
        <n-dropdown trigger="click" :options="langOptions" :value="locale" @select="onSelectLang">
          <a class="nav-link" href="#" :title="t('language.label')" @click.prevent>
            <app-icon name="globe" :size="18" />
          </a>
        </n-dropdown>
        <a
          class="nav-link"
          :href="env.GITHUB_REPO_URL"
          target="_blank"
          rel="noopener noreferrer"
          :title="t('nav.github')"
        >
          <app-icon name="github" :size="18" />
        </a>
        <a
          class="nav-link"
          :href="env.DOCS_URL"
          target="_blank"
          rel="noopener noreferrer"
          :title="t('nav.docs')"
        >
          <app-icon name="book" :size="18" />
        </a>
        <template v-if="auth.isAuthenticated">
          <NotificationBell />
          <a class="nav-admin" href="#" :title="t('nav.workspace')" @click.prevent="goWorkspace">
            <app-icon name="user" :size="18" />
            <span class="nav-admin-label">{{ t('nav.workspace') }}</span>
          </a>
          <a class="nav-logout" href="#" :title="t('nav.logout')" @click.prevent="auth.logout()">
            <app-icon name="logout" :size="17" />
          </a>
        </template>
        <template v-else>
          <a class="nav-admin" href="#" :title="t('nav.login')" @click.prevent="auth.login()">
            <app-icon name="user" :size="18" />
            <span class="nav-admin-label">{{ t('nav.login') }}</span>
          </a>
        </template>
      </div>
    </div>
  </header>
</template>
