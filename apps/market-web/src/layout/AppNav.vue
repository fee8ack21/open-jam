<script setup lang="ts">
/* ============================================================
   AppNav — market-web 全站頂部導覽列
   全站一致顯示語言切換／登入／後台／登出。
   ============================================================ */
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { useAuthStore } from '@/stores/auth.js';
import { env } from '@/environment.js';
import { SUPPORTED_LOCALES, setLocale, type Locale } from '@/i18n';

const auth = useAuthStore();
const { t, locale } = useI18n();

function goWorkspace() { window.location.href = env.WORKSPACE_PAGE_URL; }

const langOptions = computed(() =>
  SUPPORTED_LOCALES.map((code) => ({ label: t(`language.${code}`), key: code })),
);
function onSelectLang(key: string) { setLocale(key as Locale); }
</script>

<template>
  <header class="nav">
    <div class="nav-inner">
      <router-link class="brand" to="/">
        <span class="brand-mark">
          <svg width="19" height="19" viewBox="0 0 24 24" fill="none">
            <path d="M15 16.4V4.5c3.7 1 5 3.9 2 6.8" stroke="#fff" stroke-width="2.3" stroke-linecap="round" stroke-linejoin="round" fill="none"></path>
            <ellipse cx="10.4" cy="16.8" rx="4.7" ry="3.5" fill="#fff" transform="rotate(-22 10.4 16.8)"></ellipse>
          </svg>
        </span>
        <span class="brand-name">Open Jam</span>
      </router-link>

      <div class="nav-spacer"></div>

      <div class="nav-actions">
        <n-dropdown trigger="click" :options="langOptions" :value="locale" @select="onSelectLang">
          <a class="nav-link" href="#" :title="t('language.label')" @click.prevent>
            <app-icon name="globe" :size="18" />
          </a>
        </n-dropdown>
        <a class="nav-link" :href="env.GITHUB_REPO_URL" target="_blank" rel="noopener noreferrer" :title="t('nav.github')">
          <app-icon name="github" :size="18" />
        </a>
        <a class="nav-link" :href="env.DOCS_URL" target="_blank" rel="noopener noreferrer" :title="t('nav.docs')">
          <app-icon name="book" :size="18" />
        </a>
        <template v-if="auth.isAuthenticated">
          <a class="nav-admin" href="#" :title="t('nav.workspace')" @click.prevent="goWorkspace">
            <app-icon name="user" :size="18" /> <span class="nav-admin-label">{{ t('nav.workspace') }}</span>
          </a>
          <a class="nav-logout" href="#" :title="t('nav.logout')" @click.prevent="auth.logout()">
            <app-icon name="logout" :size="17" />
          </a>
        </template>
        <template v-else>
          <a class="nav-admin" href="#" :title="t('nav.login')" @click.prevent="auth.login()">
            <app-icon name="user" :size="18" /> {{ t('nav.login') }}
          </a>
        </template>
      </div>
    </div>
  </header>
</template>
