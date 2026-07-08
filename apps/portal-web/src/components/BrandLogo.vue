<script setup lang="ts">
/* ============================================================
   BrandLogo — Open Jam 品牌標誌（AppNav / AppFooter 共用）
   mark 為音波 equalizer 小動畫：常態緩慢律動，hover 加速並讓
   字母依序如節拍彈跳；「Jam」上品牌漸層。樣式在 base.css。
   ============================================================ */
// 空白以 NBSP 呈現，避免 inline-block 字母 span 之間的空白被摺疊
const LETTERS = 'Open Jam'.split('').map((ch) => (ch === ' ' ? '\u00A0' : ch));
</script>

<template>
  <router-link class="brand" to="/" aria-label="Open Jam">
    <span class="brand-mark" aria-hidden="true">
      <span class="brand-eq">
        <span v-for="i in 5" :key="i" class="brand-eq-bar"></span>
      </span>
    </span>
    <span class="brand-name" aria-hidden="true">
      <span
        v-for="(ch, i) in LETTERS"
        :key="i"
        class="brand-ch"
        :class="{ 'brand-ch-jam': i >= 5 }"
        :style="{ '--ch-i': i }"
        >{{ ch }}</span
      >
    </span>
  </router-link>
</template>
