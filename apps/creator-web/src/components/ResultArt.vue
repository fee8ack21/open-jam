<script setup lang="ts">
/* ============================================================
   ResultArt — 結帳結果頁專用手繪風「貼紙插畫」
   沿用 portal-web LandingArt 的畫法：奶油色圓角面板（自帶描邊），
   內部小場景以 3px 黑描邊 + 品牌色填色繪成，由外層以
   filter: drop-shadow 補 hard shadow。viewBox 0 0 108 108、
   面板 6,6,96×96（rx22），場景繪於面板內並 clip。
   兩個場景：
   - success：購物袋 + 完成勾勾徽章 + 彩色碎片（慶祝）
   - cancel ：購物車仍裝著作品（「隨時回來結帳」，語氣溫和不告警）
   ============================================================ */
defineProps<{ name: 'success' | 'cancel' }>();
</script>

<template>
  <svg class="lart" viewBox="0 0 108 108" role="img" aria-hidden="true" fill="none">
    <!-- 奶油色面板底 -->
    <rect class="panel-fill" x="6" y="6" width="96" height="96" rx="22" />

    <g class="scene" clip-path="url(#resultPanel)">
      <!-- ===== 購買完成：購物袋 + 完成徽章 + 彩色碎片 ===== -->
      <template v-if="name === 'success'">
        <!-- 彩色碎片 -->
        <path class="fc-lime" d="M25 30 l2 4.6 4.6 2-4.6 2L25 45.2l-2-4.6-4.6-2 4.6-2Z" />
        <circle class="fc-pink" cx="52" cy="23" r="4" />
        <path class="fc-cyan" d="M79 25 L88 25 L83.5 34 Z" />
        <circle class="fc-yellow" cx="31" cy="54" r="3.2" />
        <!-- 購物袋 -->
        <path class="sw3" fill="none" d="M46 51 C46 36 64 36 64 51" />
        <rect class="fc-violet" x="37" y="49" width="36" height="9" rx="3" />
        <rect class="fc-pink" x="40" y="53" width="30" height="33" rx="5" />
        <!-- 完成勾勾徽章（深色勾在 lime 上對比清楚） -->
        <circle class="fc-lime" cx="72" cy="76" r="12" />
        <polyline class="sw4" fill="none" stroke="#14321a" points="66,76 70,81 78,69" />
      </template>

      <!-- ===== 付款取消：購物車仍裝著作品 ===== -->
      <template v-else>
        <!-- 車籃內的作品（露出籃口） -->
        <rect class="fc-pink" x="44" y="37" width="13" height="13" rx="3" transform="rotate(-8 50 43)" />
        <rect class="fc-yellow" x="58" y="39" width="12" height="11" rx="3" transform="rotate(10 64 44)" />
        <!-- 車籃 -->
        <path class="fc-cyan o25" d="M35 48 L77 48 L71 68 L41 68 Z" />
        <path fill="none" d="M35 48 L77 48 L71 68 L41 68 Z" />
        <!-- 把手 -->
        <path class="sw3" fill="none" d="M21 40 L31 40 L40 68" />
        <!-- 輪子 -->
        <path class="sw3" d="M47 68 L47 73 M65 68 L65 73" />
        <circle class="fc-violet" cx="47" cy="78" r="5" />
        <circle class="fc-violet" cx="65" cy="78" r="5" />
        <!-- 取消叉叉徽章（白叉在紅底上對比清楚），比照 success 的完成徽章置右下角 -->
        <circle class="fc-red" cx="76" cy="78" r="12" />
        <path class="sw4" fill="none" stroke="#fff" d="M71.5 73.5 L80.5 82.5 M80.5 73.5 L71.5 82.5" />
      </template>
    </g>

    <!-- 面板描邊（畫在場景之上，避免場景蓋掉框） -->
    <rect class="panel-line" x="6" y="6" width="96" height="96" rx="22" fill="none" />

    <defs>
      <clipPath id="resultPanel"><rect x="6" y="6" width="96" height="96" rx="22" /></clipPath>
    </defs>
  </svg>
</template>

<style scoped>
.lart { display: block; width: 100%; height: auto; overflow: visible; }

.panel-fill { fill: var(--surface); }
/* 線寬對齊 creator-web 的細線語彙（卡片 border 1.5px、icon stroke 1.6~1.8），
   比 portal-web LandingArt 收細一半以上，避免插畫過於厚重。 */
.panel-line { stroke: var(--border-strong); stroke-width: 2; }

/* 場景預設：描邊 var(--text)、無填色、圓角接點 */
.scene { stroke: var(--text); stroke-width: 1.7; fill: none; stroke-linecap: round; stroke-linejoin: round; }

/* 品牌色填色（token 綁定；SVG attribute 不吃 var()，故走 class） */
.fc-violet { fill: var(--c-violet); }
.fc-pink   { fill: var(--c-pink); }
.fc-lime   { fill: var(--c-lime); }
.fc-cyan   { fill: var(--c-cyan); }
.fc-yellow { fill: var(--c-yellow); }
.fc-red    { fill: var(--err, #e5484d); }

.o25 { fill-opacity: .25; }

/* 描邊粗細 */
.sw3 { stroke-width: 1.7; }
.sw4 { stroke-width: 2.4; stroke-linecap: round; stroke-linejoin: round; }
</style>
