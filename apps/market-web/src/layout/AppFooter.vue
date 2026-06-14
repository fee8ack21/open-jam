<script setup lang="ts">
/* ============================================================
   AppFooter — market-web 全站頁尾
   品牌標誌 + 連結列 + 版權字樣。
   ============================================================ */
import { env } from '@/environment.js';

function goWorkspace() { window.location.href = env.WORKSPACE_PAGE_URL; }
</script>

<template>
  <footer class="mkt-foot">
    <router-link class="brand" to="/">
      <span class="brand-mark">
        <svg width="19" height="19" viewBox="0 0 24 24" fill="none">
          <path d="M15 16.4V4.5c3.7 1 5 3.9 2 6.8" stroke="#fff" stroke-width="2.3" stroke-linecap="round" stroke-linejoin="round" fill="none"></path>
          <ellipse cx="10.4" cy="16.8" rx="4.7" ry="3.5" fill="#fff" transform="rotate(-22 10.4 16.8)"></ellipse>
        </svg>
      </span>
      <span class="brand-name">Open Jam</span>
    </router-link>
    <nav class="mkt-foot-links" aria-label="頁尾連結">
      <a href="#" @click.prevent>關於 Open Jam</a>
      <a href="#" @click.prevent="goWorkspace">成為創作者</a>
      <router-link to="/privacy">隱私權政策</router-link>
      <router-link to="/terms">服務條款</router-link>
    </nav>
    <div class="mkt-foot-copy">© 2026 Open Jam · 創作者數位市集</div>
  </footer>
</template>
