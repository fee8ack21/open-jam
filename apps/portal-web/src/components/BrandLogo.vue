<script setup lang="ts">
/* ============================================================
   BrandLogo — Open Jam 品牌標誌（AppNav / AppFooter 共用）
   mark 為果醬罐 SVG；hover 時罐蓋彈開、果醬噴濺、JAM 抹上
   果醬印（動畫樣式在 base.css 的 .brand 區塊）。
   ============================================================ */
import { useI18n } from 'vue-i18n';

withDefaults(defineProps<{
  /** 顯示「創作者數位市集」黃色小徽章（頂部導覽用） */
  badge?: boolean;
  /** 深色底（頁尾）：字反白、尺寸縮小 */
  inverse?: boolean;
}>(), {
  badge: false,
  inverse: false,
});

const { t } = useI18n();
</script>

<template>
  <router-link class="brand" :class="{ 'brand-inverse': inverse }" to="/" aria-label="Open Jam">
    <svg class="brand-jar" :width="inverse ? 30 : 40" :height="inverse ? 34 : 45" viewBox="0 0 48 54" aria-hidden="true">
      <g class="oj-splash">
        <path d="M17 8 c-1.5 -5 1 -8.5 3.5 -9.5 c-0.5 3 2 4 1.5 7 c-0.4 2.4 -2.5 3.5 -5 2.5 z" fill="#FF90E8" stroke="#1A1A1A" stroke-width="1.8" />
        <circle cx="12" cy="1" r="2.6" fill="#D6479B" stroke="#1A1A1A" stroke-width="1.6" />
        <circle cx="27" cy="-4" r="3.2" fill="#FF90E8" stroke="#1A1A1A" stroke-width="1.8" />
        <circle cx="34" cy="-8" r="1.9" fill="#D6479B" stroke="#1A1A1A" stroke-width="1.5" />
        <circle cx="21" cy="-9" r="1.6" fill="#FF90E8" stroke="#1A1A1A" stroke-width="1.5" />
      </g>
      <rect x="7" y="12" width="34" height="38" rx="11" fill="#FF90E8" stroke="#1A1A1A" stroke-width="2.5" />
      <path d="M12 13 h24 v3 c-2.5 3.5 -5.5 -1 -8.5 2.5 c-3 3.5 -6 -2 -9 1 c-2.8 2.8 -5.5 -0.5 -6.5 -1.5 z" fill="#D6479B" stroke="#1A1A1A" stroke-width="2" />
      <rect x="12" y="25" width="24" height="16" rx="5" fill="#FFFFFF" stroke="#1A1A1A" stroke-width="2" />
      <text x="24" y="37.5" text-anchor="middle" font-family="'Space Grotesk','Noto Sans TC',sans-serif" font-weight="700" font-size="12.5" fill="#1A1A1A">OJ</text>
      <g class="oj-lid"><rect x="10" y="3" width="28" height="9" rx="4" fill="#FFDE00" stroke="#1A1A1A" stroke-width="2.5" /></g>
    </svg>
    <span class="brand-name" aria-hidden="true">
      <span>OPEN</span>
      <span class="brand-word">
        <svg class="oj-smear" viewBox="0 0 78 34" preserveAspectRatio="none" style="position: absolute; left: -8px; right: -9px; top: -3px; bottom: -4px; width: calc(100% + 17px); height: calc(100% + 7px)">
          <path d="M6 16 c-2 -4 0 -8 4 -8 c3 0 4 -3 8 -3 c5 0 6 -4 11 -3 c4 1 7 -1 12 0 c4 1 8 -2 12 0 c4 2 8 0 11 3 c3 2 8 1 9 5 c1 3 -3 4 -1 7 c2 3 -1 6 -5 6 c-3 0 -5 3 -9 2 c-4 -1 -6 2 -10 1 c-4 -1 -8 2 -12 0 c-3 -2 -8 1 -11 -1 c-3 -2 -7 0 -9 -3 c-2 -2 -6 -1 -7 -4 c-1 -2 1 -4 -3 -2 z" fill="#FF90E8" />
          <circle cx="73" cy="7" r="2.4" fill="#FF90E8" />
          <circle cx="4" cy="28" r="1.8" fill="#FF90E8" />
          <circle cx="70" cy="29" r="1.6" fill="#FF90E8" />
        </svg>
        <span>JAM</span>
      </span>
    </span>
    <span v-if="badge" class="brand-badge">{{ t('brand.tagline') }}</span>
  </router-link>
</template>

<style scoped>
.brand-inverse { gap: 10px; }
.brand-inverse .brand-name { color: #fff; font-size: 16px; }
</style>
