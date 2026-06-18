<script setup lang="ts">
/* ============================================================
   AppNav — market-web 全站頂部導覽列
   首頁顯示登入／後台／登出；法律頁顯示「返回市集」。
   ============================================================ */
import { computed } from 'vue';
import { useRoute } from 'vue-router';
import { useAuthStore } from '@/stores/auth.js';
import { env } from '@/environment.js';

const route = useRoute();
const auth = useAuthStore();

// 法律頁（服務條款 / 隱私權政策）改顯示返回連結，不顯示帳號操作
const isLegal = computed(() => route.name === 'terms' || route.name === 'privacy');

function goWorkspace() { window.location.href = env.WORKSPACE_PAGE_URL; }
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
        <a class="nav-link" :href="env.GITHUB_REPO_URL" target="_blank" rel="noopener noreferrer" title="GitHub 原始碼">
          <app-icon name="github" :size="18" />
        </a>
        <a class="nav-link" :href="env.DOCS_URL" target="_blank" rel="noopener noreferrer" title="專案文件">
          <app-icon name="book" :size="18" />
        </a>
        <router-link v-if="isLegal" class="nav-admin" to="/">返回市集</router-link>
        <template v-else-if="auth.isAuthenticated">
          <a class="nav-admin" href="#" title="前往後台" @click.prevent="goWorkspace">
            <app-icon name="user" :size="18" /> <span class="nav-admin-label">前往後台</span>
          </a>
          <a class="nav-logout" href="#" title="登出" @click.prevent="auth.logout()">
            <app-icon name="logout" :size="17" />
          </a>
        </template>
        <template v-else>
          <a class="nav-admin" href="#" title="登入" @click.prevent="auth.login()">
            <app-icon name="user" :size="18" /> 登入
          </a>
        </template>
      </div>
    </div>
  </header>
</template>
