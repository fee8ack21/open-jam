<script setup lang="ts">
/* ============================================================
   LandingArt — landing 專用手繪風「貼紙插畫」
   取代原本 landing 各區塊「單色線 icon 塞進色塊」的呆版表現。
   每個 name 是一幅小場景，畫在奶油色圓角面板上（自帶描邊），
   由外層以 filter: drop-shadow 補 hard shadow（顏色隨區塊背景而定）。
   顏色一律走設計 token（品牌六色），因 SVG presentation attribute
   不吃 var()，改以 scoped class 上色。viewBox 0 0 108 108、
   面板 6,6,96×96（rx22），場景繪於面板內並 clip。
   ============================================================ */
defineProps<{ name: string }>();
</script>

<template>
  <svg class="lart" viewBox="0 0 108 108" role="img" aria-hidden="true" fill="none">
    <!-- 奶油色面板底 -->
    <rect class="panel-fill" x="6" y="6" width="96" height="96" rx="22" />

    <g class="scene" clip-path="url(#lartPanel)">
      <!-- ===== 上傳作品：媒體卡 + 向上箭頭 ===== -->
      <template v-if="name === 'upload'">
        <path class="st-violet sw4" d="M54 42 L54 24" />
        <polyline class="st-violet sw4" points="45,31 54,22 63,31" />
        <g transform="rotate(-6 54 60)">
          <rect class="fc-cyan" x="33" y="46" width="42" height="33" rx="5" />
          <circle class="fc-yellow" cx="43" cy="56" r="4.2" />
          <polyline class="fc-white st-none" points="35,77 47,60 55,68 63,53 73,77" />
          <polyline points="35,77 47,60 55,68 63,53 73,77" />
        </g>
        <path class="fc-lime" d="M80 40 l2.4 4.8 4.8 2.4-4.8 2.4L80 54l-2.4-4.8L72.8 46.8 77.6 44.4Z" />
      </template>

      <!-- ===== 設定價格：吊牌 + 錢幣 ===== -->
      <template v-else-if="name === 'price'">
        <g transform="rotate(-10 54 48)">
          <path class="fc-pink" d="M30 54 L54 30 Q57 27 61 27 L76 27 Q81 27 81 32 L81 47 Q81 51 78 54 L54 78 Q50 82 46 78 L30 62 Q26 58 30 54 Z" />
          <circle class="fc-surf" cx="71" cy="37" r="4.6" />
        </g>
        <circle class="fc-yellow" cx="40" cy="74" r="14" />
        <path class="sw2" d="M40 66 L40 82" />
        <path class="sw2" fill="none" d="M45 69 C45 66 35 66 35 70 C35 74 45 73 45 77 C45 81 35 81 35 78" />
      </template>

      <!-- ===== 建立創作者頁：店面 ===== -->
      <template v-else-if="name === 'storefront'">
        <rect class="fc-cyan o25" x="26" y="54" width="56" height="30" rx="3" />
        <rect x="26" y="54" width="56" height="30" rx="3" />
        <!-- 遮陽棚 -->
        <rect class="fc-pink" x="20" y="43" width="68" height="12" rx="2" />
        <rect class="fc-white st-none" x="30" y="43" width="10" height="12" />
        <rect class="fc-white st-none" x="50" y="43" width="10" height="12" />
        <rect class="fc-white st-none" x="70" y="43" width="10" height="12" />
        <rect x="20" y="43" width="68" height="12" rx="2" fill="none" />
        <!-- 門 / 窗 -->
        <rect class="fc-violet" x="46" y="62" width="16" height="22" rx="2" />
        <circle class="fc-cyan" cx="58" cy="73" r="1.8" />
        <rect class="fc-yellow" x="30" y="62" width="11" height="11" rx="1.5" />
        <!-- 招牌 -->
        <path class="sw2" d="M42 43 L42 34 M66 43 L66 34" />
        <rect class="fc-lime" x="38" y="30" width="32" height="10" rx="3" />
        <path class="sw2" d="M45 35 L63 35" />
      </template>

      <!-- ===== 分享連結：紙飛機 + 軌跡 + 愛心 ===== -->
      <template v-else-if="name === 'share'">
        <path class="st-pink sw3 dash" d="M22 82 Q34 82 42 70" />
        <polygon class="fc-violet" points="24,60 84,28 62,74 53,58" />
        <path d="M53 58 L84 28" />
        <polygon class="fc-text st-none o25" points="53,58 62,74 45,64" />
        <path class="fc-pink" d="M84 24 C79 19 72 21 75 28 C77 32 82 34 84 37 C86 34 91 32 93 28 C96 21 89 19 84 24 Z" transform="scale(.55) translate(66 6)" />
        <path class="fc-pink" d="M80 24 C76 20 70 22 73 27 C74 30 78 31 80 34 C82 31 86 30 87 27 C90 22 84 20 80 24 Z" />
      </template>

      <!-- ===== 收到支持：小費罐 + 愛心 + 落下的錢幣 ===== -->
      <template v-else-if="name === 'support'">
        <circle class="fc-yellow" cx="54" cy="24" r="7.5" />
        <path class="sw2" d="M54 20 L54 28" />
        <rect class="fc-cyan o30" x="34" y="44" width="40" height="42" rx="11" />
        <rect x="34" y="44" width="40" height="42" rx="11" fill="none" />
        <rect class="fc-lime" x="31" y="35" width="46" height="10" rx="3" />
        <path class="sw3" d="M47 40 L61 40" />
        <path class="fc-pink" d="M54 76 C41 66 45 53 54 61 C63 53 67 66 54 76 Z" />
      </template>

      <!-- ===== 沒有演算法：機器人 + 禁止斜線 ===== -->
      <template v-else-if="name === 'no-algo'">
        <path class="sw3" d="M54 42 L54 33" />
        <circle class="fc-lime" cx="54" cy="30" r="3.6" />
        <rect class="fc-cyan" x="28" y="52" width="6" height="10" rx="2" />
        <rect class="fc-cyan" x="74" y="52" width="6" height="10" rx="2" />
        <rect class="fc-violet" x="34" y="42" width="40" height="30" rx="9" />
        <circle class="fc-white st-none" cx="46" cy="55" r="3.4" />
        <circle class="fc-white st-none" cx="62" cy="55" r="3.4" />
        <path class="st-white sw3" d="M46 64 L62 64" />
        <circle class="st-pink sw4" cx="54" cy="57" r="31" fill="none" />
        <path class="st-pink sw4" d="M33 78 L75 36" />
      </template>

      <!-- ===== 專注創作者與作品：畫架 + 畫布 ===== -->
      <template v-else-if="name === 'focus'">
        <path class="sw3" d="M35 86 L47 42 M73 86 L61 42 M54 46 L54 90 M40 68 L68 68" />
        <rect class="fc-yellow o20" x="37" y="26" width="34" height="34" rx="3" />
        <rect x="37" y="26" width="34" height="34" rx="3" />
        <circle class="fc-pink" cx="61" cy="37" r="5" />
        <path class="fc-cyan" d="M39 58 Q49 42 58 52 Q64 58 69 58 L69 60 L39 60 Z" />
        <path class="st-lime sw3" d="M42 49 L47 45" />
      </template>

      <!-- ===== 快速上架：火箭 + 速度線 ===== -->
      <template v-else-if="name === 'fast'">
        <g transform="rotate(8 55 54)">
          <path class="fc-surf" d="M55 26 C64 34 66 54 62 72 L48 72 C44 54 46 34 55 26 Z" />
          <circle class="fc-cyan" cx="55" cy="45" r="6" />
          <path class="fc-pink" d="M48 60 L38 74 L48 72 Z" />
          <path class="fc-pink" d="M62 60 L72 74 L62 72 Z" />
          <path class="fc-yellow" d="M48 72 Q55 90 62 72 Z" />
          <path class="fc-lime st-none" d="M51 72 Q55 82 59 72 Z" />
        </g>
        <path class="st-lime sw3" d="M26 40 L35 43 M24 53 L34 55 M28 66 L36 67" />
      </template>

      <!-- ===== 每種創作都友善：三張作品貼紙（波形 / 照片 / 書） ===== -->
      <template v-else-if="name === 'all-crafts'">
        <g transform="rotate(-14 37 56)">
          <rect class="fc-violet" x="22" y="40" width="30" height="34" rx="5" />
          <path class="st-white sw3" d="M30 62 L30 54 M37 62 L37 48 M44 62 L44 57" />
        </g>
        <g transform="rotate(14 71 56)">
          <rect class="fc-pink" x="56" y="40" width="30" height="34" rx="5" />
          <path class="st-white sw3" d="M63 51 L79 51 M63 58 L79 58 M63 65 L74 65" />
        </g>
        <rect class="fc-cyan" x="39" y="36" width="32" height="38" rx="5" />
        <circle class="fc-yellow" cx="49" cy="49" r="4" />
        <polyline class="fc-white st-none" points="41,71 51,57 58,64 66,52 73,71" />
        <polyline points="41,71 51,57 58,64 66,52 73,71" />
      </template>

      <!-- ===== CTA 創作者：OPEN 招牌 + 錢幣 ===== -->
      <template v-else-if="name === 'sell'">
        <path class="sw2" d="M34 32 L34 24 M74 32 L74 24 M28 24 L80 24" />
        <rect class="fc-lime" x="26" y="32" width="56" height="26" rx="6" />
        <text class="oj-open" x="54" y="50" text-anchor="middle">OPEN</text>
        <circle class="fc-yellow" cx="72" cy="74" r="14" />
        <path class="sw2" d="M72 66 L72 82" />
        <path class="sw2" fill="none" d="M77 69 C77 66 67 66 67 70 C67 74 77 73 77 77 C77 81 67 81 67 78" />
      </template>

      <!-- ===== CTA 探索者：作品拼貼 + 放大鏡 ===== -->
      <template v-else-if="name === 'explore'">
        <rect class="fc-cyan" x="24" y="26" width="21" height="21" rx="4" />
        <rect class="fc-lime" x="49" y="26" width="21" height="21" rx="4" />
        <rect class="fc-pink" x="24" y="51" width="21" height="21" rx="4" />
        <circle class="fc-white o60 st-none" cx="63" cy="60" r="16" />
        <path class="fc-yellow st-none" d="M63 50 l2 8 8 2-8 2-2 8-2-8-8-2 8-2Z" />
        <circle cx="63" cy="60" r="16" fill="none" class="sw4" />
        <path class="st-violet" style="stroke-width:6" d="M74 71 L87 84" />
      </template>

      <!-- ===== 分類・音樂：黑膠唱片 + 音符 + 星火 ===== -->
      <template v-else-if="name === 'cat-music'">
        <g transform="rotate(-8 47 62)">
          <circle class="fc-violet" cx="47" cy="62" r="26" />
          <circle class="st-white sw2" cx="47" cy="62" r="19" fill="none" />
          <circle class="st-white sw2" cx="47" cy="62" r="12.5" fill="none" />
          <circle class="fc-yellow" cx="47" cy="62" r="7.5" />
          <circle class="fc-text st-none" cx="47" cy="62" r="2.2" />
        </g>
        <path class="sw4" d="M73 27 L73 51" />
        <path class="sw4" fill="none" d="M73 27 Q85 25 84 33" />
        <ellipse class="fc-pink" cx="68" cy="51" rx="8" ry="6.2" transform="rotate(-18 68 51)" />
        <path class="fc-lime" d="M28 34 l1.9 4.4 4.4 1.9-4.4 1.9L28 48.6l-1.9-4.4-4.4-1.9 4.4-1.9Z" />
      </template>

      <!-- ===== 分類・攝影：相機 + 閃光星 ===== -->
      <template v-else-if="name === 'cat-photo'">
        <g transform="rotate(-4 53 62)">
          <path class="fc-pink" d="M41 46 L45 39 L61 39 L65 46 Z" />
          <rect class="fc-pink" x="26" y="46" width="54" height="36" rx="7" />
          <circle class="fc-surf" cx="53" cy="65" r="12.5" />
          <circle class="fc-cyan" cx="53" cy="65" r="7.5" />
          <circle class="fc-white st-none" cx="49" cy="61" r="2.6" />
          <rect class="fc-yellow" x="67" y="51" width="8" height="6" rx="2" />
        </g>
        <path class="fc-lime" d="M82 33 l1.9 4.4 4.4 1.9-4.4 1.9L82 47.6l-1.9-4.4-4.4-1.9 4.4-1.9Z" />
      </template>

      <!-- ===== 分類・電子書：打開的書 + 書頁 + 星火 ===== -->
      <template v-else-if="name === 'cat-ebook'">
        <path class="fc-cyan" d="M52 44 C44 38 33 38 24 41 L24 78 C33 75 44 75 52 80 Z" />
        <path class="fc-cyan" d="M52 44 C60 38 71 38 80 41 L80 78 C71 75 60 75 52 80 Z" />
        <path class="sw3" d="M52 44 L52 80" fill="none" />
        <path class="st-white sw2" d="M31 49 L45 52 M31 57 L45 60 M31 65 L44 67" />
        <path class="st-white sw2" d="M59 52 L73 49 M59 60 L73 57 M60 67 L73 65" />
        <path class="fc-lime" d="M70 26 l1.8 4 4 1.8-4 1.8L70 39l-1.8-4-4-1.8 4-1.8Z" />
      </template>
    </g>

    <!-- 面板描邊（畫在場景之上，避免場景蓋掉框） -->
    <rect class="panel-line" x="6" y="6" width="96" height="96" rx="22" fill="none" />

    <defs>
      <clipPath id="lartPanel"><rect x="6" y="6" width="96" height="96" rx="22" /></clipPath>
    </defs>
  </svg>
</template>

<style scoped>
.lart { display: block; width: 100%; height: auto; overflow: visible; }

.panel-fill { fill: var(--surface); }
.panel-line { stroke: var(--text); stroke-width: 3.5; }

/* 場景預設：描邊 var(--text)、無填色、圓角接點 */
.scene { stroke: var(--text); stroke-width: 3; fill: none; stroke-linecap: round; stroke-linejoin: round; }

/* 品牌色填色（token 綁定；SVG attribute 不吃 var()，故走 class） */
.fc-violet { fill: var(--c-violet); }
.fc-pink   { fill: var(--c-pink); }
.fc-lime   { fill: var(--c-lime); }
.fc-cyan   { fill: var(--c-cyan); }
.fc-yellow { fill: var(--c-yellow); }
.fc-white  { fill: #fff; }
.fc-surf   { fill: var(--surface); }
.fc-text   { fill: var(--text); }

.o20 { fill-opacity: .2; }
.o25 { fill-opacity: .25; }
.o30 { fill-opacity: .3; }
.o60 { fill-opacity: .6; }

/* 描邊色 / 粗細 / 無描邊 例外 */
.st-none   { stroke: none; }
.st-white  { stroke: #fff; }
.st-pink   { stroke: var(--c-pink); }
.st-violet { stroke: var(--c-violet); }
.st-lime   { stroke: var(--c-lime); }
.sw2 { stroke-width: 2.4; }
.sw3 { stroke-width: 3; }
.sw4 { stroke-width: 4; }
.dash { stroke-dasharray: 0.1 9; }

.oj-open {
  font-family: var(--oj-display), sans-serif;
  font-weight: 800; font-size: 14px; letter-spacing: -0.5px;
  fill: var(--text); stroke: none;
}
</style>
