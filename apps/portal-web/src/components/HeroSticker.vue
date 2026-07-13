<script setup lang="ts">
/* ============================================================
   HeroSticker — 區塊一 hero 專用「手繪貼紙」（loose sticker，無面板）
   有別於 LandingArt（畫在奶油面板上、用於下方區塊）。這裡是散落在 hero
   背景、堆出 jam session 創作空間感的鬆散插畫：粗描邊 + 品牌色 + hard shadow。
   刻意走「角色 / 有機曲線」而非死板的方塊 / 正圓——小角色有表情、四肢，
   音符沿弧線流動，聲波是自由手繪弧。靜態不自播動畫，只隨游標 / 捲動位移。
   viewBox 0 0 100 100；顏色走設計 token（SVG attribute 不吃 var()，改 class）。
   一組「創作者小角色」＋音樂 / 聲波配件：
   name: singer | headphones | painter | camera | writer | notes | sound | burst
   ============================================================ */
defineProps<{ name: string }>();
</script>

<template>
  <svg class="hsticker" viewBox="0 0 100 100" fill="none" role="img" aria-hidden="true">
    <!-- ===== 開嗓小角色（哼唱中）＋ 飄出的音符 ===== -->
    <template v-if="name === 'singer'">
      <!-- 腳 -->
      <ellipse class="f-violet" cx="41" cy="85" rx="6" ry="4" />
      <ellipse class="f-violet" cx="59" cy="85" rx="6" ry="4" />
      <!-- 舉高的手（藏在身體後） -->
      <path class="f-none lw5" d="M73 56 C83 51 86 40 82 33" />
      <!-- 身體（蛋形，微歪不對稱） -->
      <path class="f-violet" d="M50 17 C67 16 79 30 78 50 C77 71 65 84 50 84 C35 84 23 71 22 51 C21 31 33 18 50 17 Z" />
      <!-- 另一隻手 -->
      <path class="f-none lw5" d="M27 57 C19 59 16 67 18 73" />
      <!-- 臉：眼睛 / 腮紅 / 哼唱張口 -->
      <circle class="f-text s-none" cx="42" cy="45" r="2.8" />
      <circle class="f-text s-none" cx="58" cy="45" r="2.8" />
      <ellipse class="f-pink s-none o55" cx="35" cy="54" rx="3.4" ry="2.6" />
      <ellipse class="f-pink s-none o55" cx="65" cy="54" rx="3.4" ry="2.6" />
      <ellipse class="f-white" cx="50" cy="60" rx="5.4" ry="7" />
      <!-- 飄出的音符 -->
      <ellipse class="f-yellow" cx="83" cy="31" rx="5" ry="4" transform="rotate(-18 83 31)" />
      <path d="M87.5 30 V15" />
    </template>

    <!-- ===== 戴耳機小角色（陶醉搖擺） ===== -->
    <template v-else-if="name === 'headphones'">
      <!-- 舉起雙手 -->
      <path class="f-none lw5" d="M27 62 C18 56 15 45 19 38" />
      <path class="f-none lw5" d="M73 62 C82 56 85 45 81 38" />
      <!-- 頭（微歪蛋形） -->
      <path class="f-cyan" d="M50 21 C67 21 80 34 80 52 C80 71 66 83 50 83 C34 83 20 71 20 52 C20 34 33 21 50 21 Z" />
      <!-- 耳機頭帶 -->
      <path class="f-none lw45" d="M15 55 C15 32 31 19 50 19 C69 19 85 32 85 55" />
      <!-- 耳罩 -->
      <ellipse class="f-violet" cx="15" cy="59" rx="7" ry="10" />
      <ellipse class="f-violet" cx="85" cy="59" rx="7" ry="10" />
      <!-- 陶醉閉眼 + 微笑 -->
      <path class="f-none lw3" d="M35 52 Q41 46 47 52" />
      <path class="f-none lw3" d="M53 52 Q59 46 65 52" />
      <path class="f-none lw3" d="M42 63 Q50 71 58 63" />
    </template>

    <!-- ===== 畫家小角色（貝雷帽 ＋ 畫筆） ===== -->
    <template v-else-if="name === 'painter'">
      <ellipse class="f-pink" cx="42" cy="86" rx="6" ry="4" />
      <ellipse class="f-pink" cx="58" cy="86" rx="6" ry="4" />
      <!-- 舉筆的手（藏在身體後） -->
      <path class="f-none lw5" d="M72 60 C82 56 88 48 89 40" />
      <!-- 畫筆：桿 + 沾料筆頭 -->
      <path class="f-none lw34" d="M89 42 L93 31" />
      <path class="f-cyan" d="M84 36 L96 30 L91 44 Z" />
      <!-- 身體 -->
      <path class="f-pink" d="M50 30 C65 30 75 42 74 57 C73 75 62 87 50 87 C38 87 27 75 26 57 C25 42 35 30 50 30 Z" />
      <!-- 貝雷帽 -->
      <ellipse class="f-violet" cx="49" cy="27" rx="18" ry="7.5" transform="rotate(-9 49 27)" />
      <circle class="f-violet" cx="64" cy="20" r="2.6" />
      <!-- 另一隻手 -->
      <path class="f-none lw5" d="M28 63 C21 65 18 71 20 77" />
      <!-- 臉 -->
      <circle class="f-text s-none" cx="43" cy="53" r="2.6" />
      <circle class="f-text s-none" cx="57" cy="53" r="2.6" />
      <path class="f-none lw3" d="M44 63 Q50 68 56 63" />
    </template>

    <!-- ===== 攝影師小角色（舉相機取景） ===== -->
    <template v-else-if="name === 'camera'">
      <ellipse class="f-orange" cx="42" cy="86" rx="6" ry="4" />
      <ellipse class="f-orange" cx="58" cy="86" rx="6" ry="4" />
      <!-- 身體 -->
      <path class="f-orange" d="M50 30 C65 30 75 42 74 57 C73 75 62 87 50 87 C38 87 27 75 26 57 C25 42 35 30 50 30 Z" />
      <!-- 相機上緣探出的眼睛 -->
      <circle class="f-text s-none" cx="40" cy="37" r="2.2" />
      <circle class="f-text s-none" cx="60" cy="37" r="2.2" />
      <!-- 雙手扶相機 -->
      <path class="f-none lw5" d="M30 63 C33 56 39 52 45 52" />
      <path class="f-none lw5" d="M70 63 C67 56 61 52 55 52" />
      <!-- 相機機身 + 觀景窗 -->
      <rect class="f-cyan" x="32" y="45" width="36" height="24" rx="6" />
      <rect class="f-cyan" x="40" y="40" width="12" height="7" rx="2" />
      <!-- 鏡頭 -->
      <circle class="f-surf" cx="50" cy="57" r="8" />
      <circle class="f-text s-none" cx="50" cy="57" r="3" />
      <!-- 快門亮點 -->
      <path class="f-yellow s-none" d="M64 46 l1.5 3 3 1.5 -3 1.5 -1.5 3 -1.5 -3 -3 -1.5 3 -1.5 Z" />
    </template>

    <!-- ===== 寫作小角色（握大鉛筆） ===== -->
    <template v-else-if="name === 'writer'">
      <ellipse class="f-lime" cx="42" cy="86" rx="6" ry="4" />
      <ellipse class="f-lime" cx="58" cy="86" rx="6" ry="4" />
      <!-- 握筆的手 -->
      <path class="f-none lw5" d="M71 62 C79 60 84 55 87 50" />
      <!-- 鉛筆（斜向上：筆桿 / 木頭尖 / 筆芯 / 橡皮擦） -->
      <g transform="rotate(40 84 46)">
        <rect class="f-yellow" x="79" y="24" width="9" height="26" rx="1.5" />
        <path class="f-surf" d="M79 24 L88 24 L83.5 15 Z" />
        <path class="f-text s-none" d="M81.6 20 L85.4 20 L83.5 15 Z" />
        <rect class="f-pink" x="79" y="48" width="9" height="5" rx="1.5" />
      </g>
      <!-- 身體 -->
      <path class="f-lime" d="M50 30 C64 30 74 42 73 57 C72 75 62 87 50 87 C38 87 28 75 27 57 C26 42 36 30 50 30 Z" />
      <!-- 另一隻手 -->
      <path class="f-none lw5" d="M29 63 C22 65 19 71 21 77" />
      <!-- 臉 -->
      <circle class="f-text s-none" cx="43" cy="53" r="2.6" />
      <circle class="f-text s-none" cx="57" cy="53" r="2.6" />
      <path class="f-none lw3" d="M44 63 Q50 68 56 63" />
    </template>

    <!-- ===== 沿弧線流動的音符 ===== -->
    <template v-else-if="name === 'notes'">
      <path class="f-none lw3 o55" d="M10 62 Q28 40 50 52 T92 44" />
      <ellipse class="f-pink" cx="27" cy="66" rx="8" ry="6.2" transform="rotate(-20 27 66)" />
      <path d="M34 63 V38" />
      <ellipse class="f-pink" cx="61" cy="60" rx="8" ry="6.2" transform="rotate(-20 61 60)" />
      <path d="M68 57 V31" />
      <ellipse class="f-pink" cx="83" cy="54" rx="8" ry="6.2" transform="rotate(-20 83 54)" />
      <path d="M90 51 V27" />
      <path class="beam" d="M68 33 L90 28" />
    </template>

    <!-- ===== 自由手繪聲波（從音源散出的弧） ===== -->
    <template v-else-if="name === 'sound'">
      <circle class="f-lime" cx="26" cy="68" r="11" />
      <circle class="f-text s-none" cx="26" cy="68" r="3" />
      <path class="f-none lw34" d="M44 58 Q54 50 50 37" />
      <path class="f-none lw34" d="M51 73 Q70 62 63 34" />
      <path class="f-none lw34" d="M57 82 Q84 68 76 30" />
    </template>

    <!-- ===== 四角亮星（sparkle） ===== -->
    <template v-else-if="name === 'burst'">
      <path class="f-yellow" d="M50 10 C54 33 67 46 90 50 C67 54 54 67 50 90 C46 67 33 54 10 50 C33 46 46 33 50 10 Z" />
    </template>
  </svg>
</template>

<style scoped>
.hsticker {
  display: block; width: 100%; height: auto; overflow: visible;
  stroke: var(--text); stroke-width: 3.4; stroke-linecap: round; stroke-linejoin: round;
  filter: drop-shadow(2px 3px 0 var(--text));
}

/* 品牌色填色（token 綁定；SVG attribute 不吃 var()，故走 class） */
.f-violet { fill: var(--c-violet); }
.f-pink   { fill: var(--c-pink); }
.f-lime   { fill: var(--c-lime); }
.f-cyan   { fill: var(--c-cyan); }
.f-yellow { fill: var(--c-yellow); }
.f-orange { fill: var(--c-orange); }
.f-white  { fill: #fff; }
.f-surf   { fill: var(--surface); }
.f-text   { fill: var(--text); }
.f-none   { fill: none; }

.s-none   { stroke: none; }
.o55      { opacity: .55; }

/* 描邊粗細變體（四肢 / 細節 / 音符連桿） */
.lw3   { stroke-width: 3; }
.lw34  { stroke-width: 3.4; }
.lw45  { stroke-width: 4.5; }
.lw5   { stroke-width: 5; }
.beam  { stroke-width: 6.5; }
</style>
