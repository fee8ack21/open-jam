<script setup lang="ts">
/* ============================================================
   BrandLogo — Open Jam 品牌標誌（AppNav / AppFooter 共用）
   mark 為音符 SVG，hover 時輕微旋轉放大（樣式在 base.css）。
   ============================================================ */
</script>

<template>
  <router-link class="brand" to="/" aria-label="Open Jam">
    <span class="brand-mark" aria-hidden="true">
      <svg width="19" height="19" viewBox="0 0 24 24" fill="none">
        <path
          d="M15 16.4V4.5c3.7 1 5 3.9 2 6.8"
          stroke="#fff"
          stroke-width="2.3"
          stroke-linecap="round"
          stroke-linejoin="round"
          fill="none"
        ></path>
        <ellipse
          cx="10.4"
          cy="16.8"
          rx="4.7"
          ry="3.5"
          fill="#fff"
          transform="rotate(-22 10.4 16.8)"
        ></ellipse>
      </svg>
    </span>
    <span class="brand-name">Open Jam</span>
  </router-link>
</template>
