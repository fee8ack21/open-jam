<script setup lang="ts">
/* ============================================================
   LandingView — 品牌 landing（/）
   Interactive storytelling experience，以 GSAP ScrollTrigger 驅動：
   - 區塊一：Hero / 品牌宣言（情緒訴求 manifesto + 漂浮商品卡 / 波形）
   - 區塊二：Pinned Product Story——pin 住同一個舞台，滾動推進
             音樂 → 攝影 → 電子書三章（內容轉場而非整頁換頁），
             每章回答「創作者能在這裡賣什麼？」並附具象視覺
             （播放器 / 波形、preset before-after、翻頁書）
   - 區塊三：Creator Workflow（上傳 → 定價 → 開店 → 分享 → 收到支持）
   - 區塊四：Consumer Experience（探索 / 追蹤 / 收藏庫 collage）
   - 區塊五：Why Open Jam（平台精神四卡）
   - 最後區塊：創作者 / 消費者雙 CTA + Footer
   prefers-reduced-motion: reduce 時不做 pin 與動畫，
   三章退化為一般直向排列。
   ============================================================ */
import { ref, computed, onMounted, onBeforeUnmount } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import gsap from 'gsap';
import { ScrollTrigger } from 'gsap/ScrollTrigger';
import { useShopStore } from '@/stores/shop.js';
import { env } from '@/environment.js';
import AppNav from '@/layout/AppNav.vue';
import AppFooter from '@/layout/AppFooter.vue';
import LandingArt from '@/components/LandingArt.vue';
import HeroSticker from '@/components/HeroSticker.vue';

import imgSilver from '@/assets/images/mock/products/390kosmbz3zg1apt8bi5sf24a3fh.webp';
import imgAutumn from '@/assets/images/mock/products/udifsfncosj8km5jxmeif3x2y4sr.webp';
import imgSurf from '@/assets/images/mock/products/6u49mdf7jaizziz1ts6u9smeymsi.webp';
import imgColorful from '@/assets/images/mock/products/misi5oskyotvume2a5869wcca0f0.webp';
import imgInterior from '@/assets/images/mock/products/ccbnhspiu9ljrv2mrf0q051xbts9.webp';
import imgDragon from '@/assets/images/mock/products/9egb3vtgm6w1cbt814ua9modb43d.webp';

gsap.registerPlugin(ScrollTrigger);

const store = useShopStore();
const router = useRouter();
const { t, tm, rt } = useI18n();

// ----- 區塊二三章 — 圖示 / 主色留在程式碼，文案由 i18n（landing.story.<id>）提供 -----
const CHAPTERS = [
  { id: 'music', icon: 'note', accent: 'var(--c-violet)' },
  { id: 'photo', icon: 'image', accent: 'var(--c-pink)' },
  { id: 'ebook', icon: 'book', accent: 'var(--c-cyan)' },
] as const;
const chapterCount = CHAPTERS.length;

function chipsFor(id: string): string[] {
  return (tm(`landing.story.${id}.chips`) as string[]).map((c) => rt(c));
}

// 攝影章：照片格與 before-after 底圖
const photoGrid = [imgSurf, imgColorful, imgInterior];
// 區塊四 collage：市集精選格 / 收藏縮圖
const discoverGrid = [imgSilver, imgColorful, imgDragon, imgSurf];
const favThumbs = [imgAutumn, imgInterior];

// 波形條高度 — 決定性偽隨機，避免每次 render 抖動
const WAVE_BARS = Array.from({ length: 30 }, (_, i) =>
  Math.round(24 + 62 * Math.abs(Math.sin(i * 0.9) + 0.35 * Math.sin(i * 2.3)) / 1.35),
);

const rootEl = ref<HTMLElement | null>(null);
const activeChapter = ref(0);
const activeAccent = computed(() => CHAPTERS[activeChapter.value].accent);

// 各步驟 / 卡片對應的手繪插畫（LandingArt name），取代原本的單色線 icon
const flowArt = ['upload', 'price', 'storefront', 'share', 'support'];
const flowSteps = computed(() =>
  (tm('landing.flow.steps') as { title: string; text: string }[]).map((s, i) => ({
    art: flowArt[i],
    title: rt(s.title),
    text: rt(s.text),
  })),
);
const marqueeWords = computed(() => (tm('landing.marquee') as string[]).map((w) => rt(w)));
const whyArt = ['no-algo', 'focus', 'fast', 'all-crafts'];
const whyItems = computed(() =>
  (tm('landing.why.items') as { title: string; text: string }[]).map((s, i) => ({
    art: whyArt[i],
    title: rt(s.title),
    text: rt(s.text),
  })),
);

function goWorkspace() { window.location.href = env.WORKSPACE_PAGE_URL; }
function goMarket() { router.push({ name: 'discover' }); }

// ----- GSAP：全部動畫收在 context 內，離開頁面 revert 還原 -----
let ctx: gsap.Context | undefined;
let storyST: ScrollTrigger | undefined;

// 區塊二 timeline 節奏：每章停留 HOLD、章間轉場 TRANS（單位為 timeline 秒）
const HOLD = 1;
const TRANS = 0.6;
const STORY_TOTAL = chapterCount * HOLD + (chapterCount - 1) * TRANS; // 4.2

/** 讀取 nav 高度，pin 的起點須避開 sticky nav。 */
function navH(): number {
  const v = parseFloat(getComputedStyle(document.documentElement).getPropertyValue('--nav-h'));
  return Number.isFinite(v) ? v : 72;
}

/** 章節導覽點擊：捲動到 pin 區間內對應章的位置。 */
function goChapter(i: number) {
  if (!storyST) return;
  const p = [0, 0.5, 1][i];
  window.scrollTo({ top: storyST.start + (storyST.end - storyST.start) * p, behavior: 'smooth' });
}

/**
 * hero 互動層（僅 prefers-reduced-motion: no-preference 時啟用）：
 * 裝飾隨游標位置反向微漂（深度正負交錯營造空間感）。
 */
function setupHeroInteraction(hero: HTMLElement): () => void {
  const layers = gsap.utils.toArray<HTMLElement>('.lh-deco').map((el) => ({
    qx: gsap.quickTo(el, 'x', { duration: 0.9, ease: 'power3' }),
    qy: gsap.quickTo(el, 'y', { duration: 0.9, ease: 'power3' }),
    depth: parseFloat(el.dataset.depth ?? '16'), // 各裝飾自帶 data-depth：正負決定漂移方向、大小決定幅度
  }));

  const onMove = (e: PointerEvent) => {
    const rect = hero.getBoundingClientRect();
    const nx = (e.clientX - rect.left) / rect.width - 0.5;
    const ny = (e.clientY - rect.top) / rect.height - 0.5;
    for (const l of layers) {
      l.qx(nx * l.depth * 2.2);
      l.qy(ny * l.depth);
    }
  };
  const onLeave = () => {
    for (const l of layers) {
      l.qx(0);
      l.qy(0);
    }
  };

  hero.addEventListener('pointermove', onMove, { passive: true });
  hero.addEventListener('pointerleave', onLeave);

  return () => {
    hero.removeEventListener('pointermove', onMove);
    hero.removeEventListener('pointerleave', onLeave);
  };
}

onMounted(() => {
  ctx = gsap.context(() => {
    const mm = gsap.matchMedia();
    mm.add('(prefers-reduced-motion: no-preference)', () => {
      // ---- 區塊一：manifesto 進場 ----
      gsap.timeline({ defaults: { ease: 'power3.out' } })
        .from('.lh-title', { y: 54, autoAlpha: 0, duration: 0.8 })
        .from('.lh-hl', { scale: 0, rotation: -8, duration: 0.45, stagger: 0.18, ease: 'back.out(2.2)' }, '-=0.35')
        .from('.lh-slogan', { y: 26, autoAlpha: 0, duration: 0.55 }, '-=0.2')
        .from('.lh-deco', { autoAlpha: 0, duration: 0.7, stagger: 0.08 }, '-=0.4')
        .from('.lh-hint', { autoAlpha: 0, duration: 0.5 }, '-=0.2');

      // 裝飾隨捲動視差
      gsap.utils.toArray<HTMLElement>('.lh-deco').forEach((el, i) => {
        gsap.to(el, {
          yPercent: (i % 2 ? -1 : 1) * (14 + i * 7),
          ease: 'none',
          scrollTrigger: { trigger: '.l-hero', start: 'top top', end: 'bottom top', scrub: true },
        });
      });

      // ---- hero 互動層：漂浮物件游標視差 ----
      const hero = document.querySelector<HTMLElement>('.l-hero');
      const heroCleanup = hero ? setupHeroInteraction(hero) : undefined;

      // ---- 區塊二：pinned product story（同一舞台推進三章） ----
      const stage = document.querySelector<HTMLElement>('.ls-stage');
      stage?.classList.add('ls-anim');

      const chapters = gsap.utils.toArray<HTMLElement>('.ls-chapter');
      const bgs = gsap.utils.toArray<HTMLElement>('.ls-bg');
      gsap.set(chapters.slice(1), { autoAlpha: 0, y: 60 });
      gsap.set(bgs.slice(1), { autoAlpha: 0 });

      const tl = gsap.timeline({
        defaults: { ease: 'power2.inOut' },
        scrollTrigger: {
          trigger: '.l-story',
          start: () => `top ${navH()}px`,
          end: '+=350%',
          pin: true,
          scrub: 0.6,
          snap: {
            snapTo: [0, 0.5, 1],
            directional: false, // 就近吸附：大步捲動 / rail 點擊不會被方向拉回上一章
            inertia: false, // 不做慣性投影：rail 點擊抵達時的速度不會把進度甩過頭
            duration: { min: 0.25, max: 0.7 },
            delay: 0.05,
            ease: 'power1.inOut',
          },
          onUpdate(st) {
            const tt = st.progress * STORY_TOTAL;
            activeChapter.value = tt < 1.3 ? 0 : tt < 2.9 ? 1 : 2;
          },
        },
      });
      storyST = tl.scrollTrigger;

      for (let i = 1; i < chapterCount; i++) {
        const at = HOLD * i + TRANS * (i - 1); // 1.0, 2.6
        tl.to(chapters[i - 1], { autoAlpha: 0, y: -46, duration: TRANS }, at)
          .fromTo(chapters[i], { autoAlpha: 0, y: 60 }, { autoAlpha: 1, y: 0, duration: TRANS }, at + TRANS * 0.3)
          .to(bgs[i - 1], { autoAlpha: 0, duration: TRANS, ease: 'none' }, at)
          .to(bgs[i], { autoAlpha: 1, duration: TRANS, ease: 'none' }, at);
      }
      // 章內小劇場：攝影章拉 before/after 分割線、電子書章翻頁
      tl.fromTo('.pv-ba', { '--split': '68%' }, { '--split': '26%', duration: 0.8, ease: 'power1.inOut' }, 1.85)
        .fromTo('.ev-page', { rotationY: 0 }, { rotationY: -132, duration: 0.7, ease: 'power1.inOut', transformOrigin: 'left center' }, 3.35)
        .to({}, { duration: 0.001 }, STORY_TOTAL); // 補滿總長，讓最後一章有停留

      // ---- 區塊三：workflow 連接線隨捲動長出、步驟進場 ----
      gsap.fromTo('.fl-line-fill', { scaleX: 0 }, {
        scaleX: 1,
        ease: 'none',
        transformOrigin: 'left center',
        scrollTrigger: { trigger: '.fl-steps', start: 'top 78%', end: 'bottom 55%', scrub: true },
      });
      gsap.from('.fl-step', {
        y: 44,
        autoAlpha: 0,
        duration: 0.6,
        stagger: 0.12,
        ease: 'power3.out',
        scrollTrigger: { trigger: '.fl-steps', start: 'top 80%', once: true },
      });

      // ---- 區塊四：collage 卡片錯落進場 ----
      gsap.from('.dc-card', {
        y: 54,
        autoAlpha: 0,
        rotation: (i) => [-4, 5, -3][i] ?? 0,
        duration: 0.7,
        stagger: 0.15,
        ease: 'power3.out',
        scrollTrigger: { trigger: '.dc-collage', start: 'top 78%', once: true },
      });

      // ---- 巨型出血字：隨捲動水平飄移（Begonia 式大字） ----
      gsap.utils.toArray<HTMLElement>('.l-bigword[data-drift]').forEach((el, i) => {
        gsap.fromTo(el, { xPercent: i % 2 ? -8 : 4 }, {
          xPercent: i % 2 ? 6 : -10,
          ease: 'none',
          scrollTrigger: { trigger: el.parentElement, start: 'top bottom', end: 'bottom top', scrub: true },
        });
      });

      // ---- 其餘區塊：進場 reveal ----
      gsap.utils.toArray<HTMLElement>('.lrv').forEach((el) => {
        gsap.from(el, {
          y: 48,
          autoAlpha: 0,
          duration: 0.8,
          ease: 'power3.out',
          scrollTrigger: { trigger: el, start: 'top 82%', once: true },
        });
      });

      return () => {
        heroCleanup?.();
        stage?.classList.remove('ls-anim');
        storyST = undefined;
      };
    });
  }, rootEl.value!);
});
onBeforeUnmount(() => ctx?.revert());
</script>

<template>
  <div ref="rootEl" class="oj-root" :class="'font-' + store.font" :data-screen-label="t('landing.screenLabel')">
    <!-- ============ NAV ============ -->
    <app-nav />

    <main class="landing">
      <!-- 紙質顆粒 + 貫穿格線（全頁氛圍層） -->
      <div class="l-grain" aria-hidden="true"></div>
      <div class="l-gridlines" aria-hidden="true"><i v-for="i in 4" :key="i"></i></div>

      <!-- ============ 區塊一：Hero / 品牌宣言 ============ -->
      <section class="l-hero">
        <span class="l-bigword lh-bigword" data-drift aria-hidden="true">OPEN JAM</span>

        <!-- 背景插畫層：散落的手繪創作者小角色，堆出一個 jam session 創作社群
             （音樂 / 繪畫 / 攝影 / 寫作各一角色；情緒背景，非資訊；
             靜態，只隨游標 / 捲動位移） -->
        <span class="lh-deco lh-st st-painter" data-depth="-20" aria-hidden="true"><hero-sticker name="painter" /></span>
        <span class="lh-deco lh-st st-camera" data-depth="22" aria-hidden="true"><hero-sticker name="camera" /></span>
        <span class="lh-deco lh-st st-writer" data-depth="-14" aria-hidden="true"><hero-sticker name="writer" /></span>
        <span class="lh-deco lh-st st-notes" data-depth="16" aria-hidden="true"><hero-sticker name="notes" /></span>
        <span class="lh-deco lh-st st-singer" data-depth="28" aria-hidden="true"><hero-sticker name="singer" /></span>
        <span class="lh-deco lh-st st-headphones" data-depth="24" aria-hidden="true"><hero-sticker name="headphones" /></span>
        <span class="lh-deco lh-st st-burst" data-depth="-12" aria-hidden="true"><hero-sticker name="burst" /></span>

        <!-- 散落碎點 confetti（靜態小點 / 菱形 / 三角，補畫面呼吸感） -->
        <span class="lh-confetti cf-1" aria-hidden="true"></span>
        <span class="lh-confetti cf-2" aria-hidden="true"></span>
        <span class="lh-confetti cf-3" aria-hidden="true"></span>
        <span class="lh-confetti cf-4" aria-hidden="true"></span>
        <span class="lh-confetti cf-5" aria-hidden="true"></span>

        <div class="lh-inner">
          <i18n-t keypath="landing.hero.title" tag="h1" class="lh-title" scope="global">
            <template #collect><span class="lh-hl lh-hl-lime">{{ t('landing.hero.collect') }}</span></template>
            <template #support><span class="lh-hl lh-hl-cyan">{{ t('landing.hero.support') }}</span></template>
            <template #remember><span class="lh-hl lh-hl-pink">{{ t('landing.hero.remember') }}</span></template>
          </i18n-t>
          <p class="lh-slogan">{{ t('landing.hero.slogan') }}</p>
        </div>

        <div class="lh-hint" aria-hidden="true">
          <span>{{ t('landing.hero.scrollHint') }}</span>
          <app-icon name="chevronU" :size="18" style="transform: rotate(180deg)" />
        </div>
      </section>

      <!-- ============ 區塊二：Pinned Product Story ============ -->
      <section class="l-story">
        <div class="ls-stage">
          <!-- 章節背景層（隨章節 crossfade） -->
          <div
            v-for="c in CHAPTERS"
            :key="'bg-' + c.id"
            class="ls-bg"
            :style="{ '--accent': c.accent }"
            aria-hidden="true"
          ></div>

          <!-- 常駐標題：整段故事回答同一個問題 -->
          <header class="ls-head">
            <p class="ls-eyebrow"><app-icon name="sparkle" :size="13" /> {{ t('landing.story.eyebrow') }}</p>
            <h2 class="ls-title">{{ t('landing.story.title') }}</h2>
          </header>

          <div class="ls-chapters">
            <article
              v-for="(c, i) in CHAPTERS"
              :key="c.id"
              class="ls-chapter"
              :style="{ '--accent': c.accent }"
            >
              <span class="l-bigword ls-bigword" aria-hidden="true">{{ c.id === 'ebook' ? 'E-BOOK' : c.id.toUpperCase() }}</span>
              <div class="ls-text">
                <p class="ls-num">{{ String(i + 1).padStart(2, '0') }}</p>
                <h3 class="ls-ch-title">{{ t(`landing.story.${c.id}.title`) }}</h3>
                <p class="ls-desc">{{ t(`landing.story.${c.id}.desc`) }}</p>
                <ul class="ls-chips">
                  <li v-for="chip in chipsFor(c.id)" :key="chip">{{ chip }}</li>
                </ul>
              </div>

              <!-- 音樂章：播放器卡 + 波形 -->
              <div v-if="c.id === 'music'" class="ls-visual vis-music" aria-hidden="true">
                <div class="mv-player">
                  <div class="mv-art"><app-icon name="note" :size="40" :stroke="1.8" /></div>
                  <div class="mv-meta">
                    <span class="mv-line w1"></span>
                    <span class="mv-line w2"></span>
                    <div class="mv-progress"><span class="mv-ph"></span></div>
                  </div>
                  <span class="mv-play"><span class="mv-tri"></span></span>
                </div>
                <div class="mv-wave">
                  <span
                    v-for="(h, j) in WAVE_BARS"
                    :key="j"
                    :style="{ height: h + '%', animationDelay: (j % 5) * 0.12 + 's' }"
                  ></span>
                </div>
              </div>

              <!-- 攝影章：before/after 分割 + 照片格 -->
              <div v-else-if="c.id === 'photo'" class="ls-visual vis-photo" aria-hidden="true">
                <div class="pv-ba">
                  <img class="pv-after" :src="imgAutumn" alt="" />
                  <div class="pv-before" :style="{ backgroundImage: `url(${imgAutumn})` }"></div>
                  <span class="pv-handle"></span>
                  <span class="pv-tag pv-tag-b">Before</span>
                  <span class="pv-tag pv-tag-a">After</span>
                </div>
                <div class="pv-grid">
                  <img v-for="(src, j) in photoGrid" :key="j" :src="src" alt="" />
                </div>
              </div>

              <!-- 電子書章：書封 + 翻頁 + 章節 preview -->
              <div v-else class="ls-visual vis-ebook" aria-hidden="true">
                <div class="ev-book">
                  <div class="ev-cover">
                    <app-icon name="book" :size="34" :stroke="1.8" />
                    <span class="ev-line w1"></span>
                    <span class="ev-line w2"></span>
                  </div>
                  <div class="ev-page"></div>
                </div>
                <div class="ev-toc">
                  <span v-for="j in 4" :key="j" class="ev-toc-row">
                    <b>{{ String(j).padStart(2, '0') }}</b><i :class="'w' + j"></i>
                  </span>
                </div>
              </div>
            </article>
          </div>

          <!-- 章節導覽（僅動畫模式顯示） -->
          <div class="ls-rail" :style="{ '--accent': activeAccent }">
            <button
              v-for="(c, i) in CHAPTERS"
              :key="'rail-' + c.id"
              type="button"
              class="ls-rail-item"
              :class="{ on: activeChapter === i }"
              :aria-label="t('landing.story.pageAria', { n: i + 1 })"
              @click="goChapter(i)"
            >
              <app-icon :name="c.icon" :size="15" />
              <span>{{ t(`landing.story.${c.id}.label`) }}</span>
            </button>
            <span class="ls-rail-hint">{{ t('landing.story.hint') }}</span>
          </div>
        </div>
      </section>

      <!-- ============ 品牌跑馬燈帶 ============ -->
      <div class="l-marquee" aria-hidden="true">
        <div class="l-marquee-track">
          <template v-for="n in 2" :key="n">
            <span v-for="(w, i) in marqueeWords" :key="n + '-' + i" class="l-marquee-item">
              {{ w }} <app-icon name="sparkle" :size="20" />
            </span>
          </template>
        </div>
      </div>

      <!-- ============ 區塊三：Creator Workflow ============ -->
      <section class="l-flow">
        <span class="l-bigword lf-bigword" data-drift aria-hidden="true">WORKFLOW</span>
        <div class="lsec-head lrv">
          <p class="lsec-eyebrow"><app-icon name="sparkle" :size="13" /> {{ t('landing.flow.eyebrow') }}</p>
          <h2 class="lsec-title">{{ t('landing.flow.title') }}</h2>
          <p class="lsec-sub">{{ t('landing.flow.sub') }}</p>
        </div>
        <div class="fl-steps">
          <div class="fl-line" aria-hidden="true"><span class="fl-line-fill"></span></div>
          <div v-for="(s, i) in flowSteps" :key="s.title" class="fl-step">
            <span class="fl-art"><landing-art :name="s.art" /></span>
            <p class="fl-num">{{ i + 1 }}</p>
            <h3>{{ s.title }}</h3>
            <p class="fl-text">{{ s.text }}</p>
          </div>
        </div>
      </section>

      <!-- ============ 區塊四：Consumer Experience ============ -->
      <section class="l-discover">
        <span class="l-bigword ld-bigword" data-drift aria-hidden="true">DISCOVER</span>
        <div class="dc-inner">
          <div class="dc-copy lrv">
            <p class="lsec-eyebrow"><app-icon name="sparkle" :size="13" /> {{ t('landing.discover.eyebrow') }}</p>
            <h2 class="lsec-title">{{ t('landing.discover.title') }}</h2>
            <p class="dc-text">{{ t('landing.discover.text') }}</p>
            <button type="button" class="dc-btn" @click="goMarket">
              {{ t('landing.cta.buyer.button') }} <app-icon name="chevron" :size="15" />
            </button>
          </div>
          <div class="dc-collage" aria-hidden="true">
            <div class="dc-card dc-grid">
              <p class="dc-label"><app-icon name="search" :size="13" /> {{ t('landing.discover.gridLabel') }}</p>
              <div class="dc-grid-imgs">
                <img v-for="(src, i) in discoverGrid" :key="i" :src="src" alt="" />
              </div>
            </div>
            <div class="dc-card dc-profile">
              <p class="dc-label"><app-icon name="user" :size="13" /> {{ t('landing.discover.profileLabel') }}</p>
              <div class="dc-profile-row">
                <span class="dc-avatar"><app-icon name="note" :size="18" /></span>
                <span class="dc-pl w1"></span>
                <span class="dc-follow">{{ t('landing.discover.follow') }}</span>
              </div>
              <span class="dc-pl w2"></span>
            </div>
            <div class="dc-card dc-fav">
              <p class="dc-label"><app-icon name="heart" :size="13" /> {{ t('landing.discover.favLabel') }}</p>
              <div class="dc-fav-row">
                <img v-for="(src, i) in favThumbs" :key="i" :src="src" alt="" />
                <span class="dc-fav-heart"><app-icon name="heart" :size="16" /></span>
              </div>
            </div>
          </div>
        </div>
      </section>

      <!-- ============ 區塊五：Why Open Jam ============ -->
      <section class="l-why">
        <span class="l-bigword lw-bigword" data-drift aria-hidden="true">BELIEVE</span>
        <div class="lsec-head lrv">
          <p class="lsec-eyebrow"><app-icon name="sparkle" :size="13" /> {{ t('landing.why.eyebrow') }}</p>
          <h2 class="lsec-title">{{ t('landing.why.title') }}</h2>
        </div>
        <div class="why-grid">
          <div v-for="(w, i) in whyItems" :key="w.title" class="why-card lrv" :class="'why-card-' + (i + 1)">
            <span class="why-art"><landing-art :name="w.art" /></span>
            <h3>{{ w.title }}</h3>
            <p>{{ w.text }}</p>
          </div>
        </div>
      </section>

      <!-- ============ 最後區塊：CTA ============ -->
      <section class="l-cta lrv">
        <span class="l-bigword lc-bigword" data-drift aria-hidden="true">JAM ON!</span>
        <p class="lc-eyebrow"><app-icon name="sparkle" :size="14" /> {{ t('landing.cta.eyebrow') }}</p>
        <div class="lc-grid">
          <div class="lc-card lc-creator">
            <span class="lc-art"><landing-art name="sell" /></span>
            <h3>{{ t('landing.cta.creator.title') }}</h3>
            <p>{{ t('landing.cta.creator.text') }}</p>
            <button type="button" class="lc-btn lc-btn-main" @click="goWorkspace">
              {{ t('landing.cta.creator.button') }} <app-icon name="chevron" :size="15" />
            </button>
          </div>
          <div class="lc-card lc-buyer">
            <span class="lc-art"><landing-art name="explore" /></span>
            <h3>{{ t('landing.cta.buyer.title') }}</h3>
            <p>{{ t('landing.cta.buyer.text') }}</p>
            <button type="button" class="lc-btn" @click="goMarket">
              {{ t('landing.cta.buyer.button') }} <app-icon name="chevron" :size="15" />
            </button>
          </div>
        </div>
      </section>

      <!-- ============ FOOTER ============ -->
      <div class="l-footwrap">
        <app-footer />
      </div>
    </main>
  </div>
</template>

<style scoped>
/* 不在容器設左右 padding：區塊二 pin 時需滿版，改由各區塊自帶 */
.landing { padding: 0; position: relative; }
.l-footwrap { padding: 0 clamp(20px, 3.5vw, 56px); }

/* ---------- 全頁氛圍：紙質顆粒 + 貫穿細格線（Begonia 式 frame） ---------- */
.l-grain {
  position: fixed; inset: 0; z-index: 90; pointer-events: none; opacity: .05;
  background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='260' height='260'%3E%3Cfilter id='n'%3E%3CfeTurbulence type='fractalNoise' baseFrequency='0.85' numOctaves='2' stitchTiles='stitch'/%3E%3C/filter%3E%3Crect width='260' height='260' filter='url(%23n)'/%3E%3C/svg%3E");
  background-size: 260px 260px;
}
.l-gridlines { position: fixed; inset: 0; z-index: 60; pointer-events: none; display: flex; }
.l-gridlines i { flex: 1; border-right: 1px solid rgba(26,22,38,.05); }
.l-gridlines i:last-child { border-right: none; }

/* ---------- 巨型出血標題字（描邊空心，隨捲動飄移） ---------- */
.l-bigword {
  position: absolute; z-index: 0; pointer-events: none; user-select: none;
  font-family: var(--oj-display); font-weight: 800; line-height: 1; white-space: nowrap;
  letter-spacing: -0.02em; color: transparent;
  -webkit-text-stroke: 2px rgba(26,22,38,.13);
}

/* 共用區塊標頭 */
.lsec-head { text-align: center; max-width: 760px; margin: 0 auto 44px; }
.lsec-eyebrow {
  display: inline-flex; align-items: center; gap: 7px; margin: 0 0 12px;
  font-family: var(--oj-mono); font-size: 12px; letter-spacing: 1.5px; text-transform: uppercase;
  color: var(--oj-primary);
}
.lsec-title {
  margin: 0; font-family: var(--oj-display); font-weight: 800;
  font-size: clamp(28px, 4vw, 44px); letter-spacing: -1.2px; color: var(--text);
}
.lsec-sub { margin: 14px 0 0; font-size: 15.5px; line-height: 1.8; color: var(--text-soft); }

/* ---------- 區塊一：Hero / 品牌宣言 ---------- */
.l-hero {
  position: relative;
  min-height: calc(100vh - var(--nav-h));
  display: flex; align-items: center; justify-content: center;
  padding: 0 clamp(20px, 3.5vw, 56px);
  overflow: hidden;
}
.lh-inner { position: relative; z-index: 1; max-width: 940px; text-align: center; padding: 40px 0 120px; }
.lh-title {
  margin: 0; font-family: var(--oj-display); font-weight: 800;
  font-size: clamp(38px, 6.6vw, 84px); line-height: 1.16; letter-spacing: -2.2px; color: var(--text);
}
.lh-bigword { bottom: -0.12em; left: -2%; font-size: clamp(110px, 17vw, 260px); }
.lh-hl {
  display: inline-block; padding: 1px 12px; border-radius: 12px;
  border: 1.5px solid var(--text); box-shadow: var(--pop-1);
}
.lh-hl-lime { background: var(--c-lime); transform: rotate(-1.5deg); }
.lh-hl-cyan { background: var(--c-cyan); transform: rotate(1.2deg); }
.lh-hl-pink { background: var(--c-pink); color: #fff; transform: rotate(-1deg); }
.lh-slogan { max-width: 560px; margin: 24px auto 0; font-size: 16.5px; line-height: 1.85; color: var(--text-soft); }

.lh-deco { position: absolute; pointer-events: none; z-index: 0; will-change: transform; }
.lh-st { display: block; }
/* 各貼紙位置 / 大小 / 微傾（沿四角與左右外緣環繞，避開中央標題文字） */
.st-painter    { top: 10%; left: 4%;  width: clamp(66px, 7.5vw, 94px);  transform: rotate(-8deg); }
.st-camera     { top: 10%; right: 4%; width: clamp(66px, 7.5vw, 94px);  transform: rotate(7deg); }
.st-writer     { top: 41%; left: 1%;  width: clamp(60px, 6.8vw, 86px);  transform: rotate(-5deg); }
.st-notes      { top: 39%; right: 1%; width: clamp(56px, 6.4vw, 82px);  transform: rotate(6deg); }
.st-singer     { top: 56%; left: 6%;  width: clamp(78px, 9.5vw, 122px); transform: rotate(-7deg); }
.st-headphones { top: 53%; right: 6%; width: clamp(82px, 10vw, 132px);  transform: rotate(6deg); }
.st-burst      { top: 68%; left: 41%; width: clamp(30px, 3.4vw, 46px);  transform: rotate(-4deg); }

/* 散落碎點 confetti（多形狀，避免只有正圓） */
.lh-confetti { position: absolute; z-index: 0; pointer-events: none; }
.cf-1 { top: 25%; left: 30%; width: 8px; height: 8px; border-radius: 50%; background: var(--c-cyan); box-shadow: 2px 2px 0 var(--text); }
.cf-2 { top: 43%; left: 19%; width: 9px; height: 9px; background: var(--c-pink); transform: rotate(45deg); box-shadow: 2px 2px 0 var(--text); }
.cf-3 { top: 72%; right: 30%; width: 0; height: 0; border-left: 6px solid transparent; border-right: 6px solid transparent; border-bottom: 11px solid var(--c-violet); }
.cf-4 { top: 40%; right: 33%; width: 7px; height: 7px; border-radius: 50%; background: var(--c-orange); box-shadow: 2px 2px 0 var(--text); }
.cf-5 { top: 78%; left: 40%; width: 9px; height: 9px; background: var(--c-lime); transform: rotate(45deg); box-shadow: 2px 2px 0 var(--text); }

/* lh-eq keyframes 供區塊二音樂章 .mv-wave 使用 */
@keyframes lh-eq { 50% { transform: scaleY(.45); } }

.lh-hint {
  position: absolute; left: 50%; bottom: 22px; transform: translateX(-50%); z-index: 1;
  display: flex; flex-direction: column; align-items: center; gap: 4px;
  font-family: var(--oj-mono); font-size: 12px; color: var(--text-faint);
}
.lh-hint :deep(svg) { animation: lh-bob 1.6s ease-in-out infinite; }
@keyframes lh-bob { 50% { translate: 0 6px; } }

/* ---------- 區塊二：Pinned Product Story ---------- */
.ls-stage {
  position: relative;
  display: flex; flex-direction: column;
  border-top: 1.5px solid var(--border);
}
/* GSAP 啟用時鎖滿版高、章節絕對堆疊（預設一般文流退場） */
.ls-anim { height: calc(100vh - var(--nav-h)); overflow: hidden; }

/* 每章整屏飽和色塊（Begonia 式色塊節奏），文字反白 */
.ls-bg {
  display: none;
  position: absolute; inset: 0; z-index: 0;
  background:
    radial-gradient(rgba(255,255,255,.09) 1.3px, transparent 1.5px),
    linear-gradient(90deg, rgba(255,255,255,.06) 1px, transparent 1px),
    color-mix(in srgb, var(--accent) 62%, #221a38);
  background-size: 22px 22px, 25% 100%, auto;
}
.ls-anim .ls-bg { display: block; }

.ls-head { position: relative; z-index: 2; text-align: center; padding: clamp(24px, 4vh, 44px) 20px 0; }
.ls-eyebrow {
  display: inline-flex; align-items: center; gap: 7px; margin: 0 0 10px;
  font-family: var(--oj-mono); font-size: 12px; letter-spacing: 1.5px; text-transform: uppercase;
  color: rgba(255,255,255,.72);
}
.ls-title {
  margin: 0; font-family: var(--oj-display); font-weight: 800;
  font-size: clamp(28px, 4vw, 46px); letter-spacing: -1.2px; color: #fff;
  text-shadow: 3px 3px 0 rgba(26,22,38,.35);
}

.ls-chapters { position: relative; z-index: 1; flex: 1; }
.ls-chapter {
  display: grid; grid-template-columns: minmax(0, 1.05fr) minmax(0, 1fr);
  align-items: center; gap: clamp(28px, 5vw, 76px);
  max-width: 1080px; margin: 0 auto;
  padding: 40px clamp(20px, 3.5vw, 56px) 90px;
}
/* 非動畫模式：每章自帶背景與間隔 */
.ls-chapter { position: relative; }
.ls-stage:not(.ls-anim) .ls-chapter {
  background:
    radial-gradient(rgba(255,255,255,.09) 1.3px, transparent 1.5px),
    linear-gradient(90deg, rgba(255,255,255,.06) 1px, transparent 1px),
    color-mix(in srgb, var(--accent) 62%, #221a38);
  background-size: 22px 22px, 25% 100%, auto;
  max-width: none;
}
.ls-anim .ls-chapter { position: absolute; inset: 0; height: 100%; will-change: transform, opacity; }

.ls-bigword { top: 0.02em; right: -3%; font-size: clamp(96px, 15vw, 230px); -webkit-text-stroke: 2px rgba(255,255,255,.16); }

.ls-num { margin: 0 0 6px; font-family: var(--oj-mono); font-size: 14px; font-weight: 600; color: rgba(255,255,255,.65); }
.ls-ch-title {
  margin: 0; font-family: var(--oj-display); font-weight: 800;
  font-size: clamp(30px, 4.2vw, 50px); letter-spacing: -1.4px; color: #fff;
  text-shadow: 3px 3px 0 rgba(26,22,38,.35);
}
.ls-desc { margin: 16px 0 0; max-width: 440px; font-size: 15.5px; line-height: 1.85; color: rgba(255,255,255,.85); }
.ls-chips { list-style: none; display: flex; flex-wrap: wrap; gap: 8px; margin: 20px 0 0; padding: 0; }
.ls-chips li {
  padding: 7px 13px; border: 1.5px solid var(--text); border-radius: 999px;
  background: var(--surface); box-shadow: var(--pop-1);
  font-family: var(--oj-display); font-weight: 600; font-size: 13.5px; color: var(--text);
}

/* flex column（非 grid auto track）：避免子元素原始尺寸撐開軌道把內容推出畫面 */
.ls-visual { position: relative; display: flex; flex-direction: column; align-items: center; gap: 18px; min-width: 0; }

/* 音樂章視覺 */
.vis-music { width: 100%; }
.mv-player {
  display: flex; align-items: center; gap: 14px; width: min(380px, 100%);
  background: var(--surface); border: 1.5px solid var(--text); border-radius: var(--r-md);
  box-shadow: 6px 6px 0 var(--text); padding: 14px 16px; transform: rotate(-1.5deg);
}
.mv-art {
  width: 64px; height: 64px; flex: none; display: grid; place-items: center; color: #fff;
  border: 1.5px solid var(--text); border-radius: 12px;
  background: linear-gradient(140deg, var(--c-violet), var(--c-pink));
}
.mv-meta { flex: 1; min-width: 0; }
.mv-line { display: block; height: 9px; border-radius: 5px; background: var(--border); }
.mv-line.w1 { width: 72%; background: var(--text); opacity: .82; }
.mv-line.w2 { width: 46%; margin-top: 7px; }
.mv-progress { position: relative; height: 5px; border-radius: 3px; background: var(--border); margin-top: 12px; }
.mv-ph { position: absolute; left: 0; top: 0; bottom: 0; width: 38%; border-radius: 3px; background: var(--c-violet); }
.mv-play {
  width: 44px; height: 44px; flex: none; display: grid; place-items: center;
  border: 1.5px solid var(--text); border-radius: 50%; background: var(--c-lime); box-shadow: var(--pop-1);
}
.mv-tri {
  width: 0; height: 0; margin-left: 3px;
  border-top: 8px solid transparent; border-bottom: 8px solid transparent; border-left: 13px solid var(--text);
}
.mv-wave { display: flex; align-items: flex-end; gap: 5px; height: 72px; width: min(380px, 100%); }
.mv-wave span {
  flex: 1; border-radius: 3px 3px 0 0; transform-origin: bottom;
  background: linear-gradient(180deg, #fff, rgba(255,255,255,.5));
  animation: lh-eq 1.6s ease-in-out infinite;
}

/* 攝影章視覺 */
.vis-photo { width: 100%; }
.pv-ba {
  --split: 50%;
  position: relative; width: min(400px, 100%); aspect-ratio: 16 / 10; overflow: hidden;
  border: 1.5px solid var(--text); border-radius: var(--r-md); box-shadow: 6px 6px 0 var(--text);
  transform: rotate(1.5deg);
}
.pv-after { position: absolute; inset: 0; width: 100%; height: 100%; object-fit: cover; }
.pv-before {
  position: absolute; top: 0; bottom: 0; left: 0; width: var(--split);
  background-size: cover; background-position: left center;
  filter: grayscale(.92) contrast(.82) brightness(1.05);
  border-right: 2px solid #fff;
}
.pv-handle {
  position: absolute; top: 50%; left: var(--split); transform: translate(-50%, -50%);
  width: 30px; height: 30px; border-radius: 50%;
  background: var(--surface); border: 1.5px solid var(--text); box-shadow: var(--pop-1);
}
.pv-handle::before { content: '‹›'; display: grid; place-items: center; height: 100%; font-size: 14px; font-weight: 700; color: var(--text); }
.pv-tag {
  position: absolute; bottom: 8px; padding: 3px 9px; border-radius: 999px;
  border: 1.5px solid var(--text); font-family: var(--oj-mono); font-size: 10.5px; font-weight: 600;
}
.pv-tag-b { left: 8px; background: var(--surface); color: var(--text); }
.pv-tag-a { right: 8px; background: var(--c-lime); color: var(--text); }
.pv-grid { display: flex; gap: 10px; width: min(400px, 100%); }
.pv-grid img {
  flex: 1 1 0; width: 0; min-width: 0; aspect-ratio: 1; object-fit: cover;
  border: 1.5px solid var(--text); border-radius: 12px; box-shadow: var(--pop-1);
}

/* 電子書章視覺 */
.vis-ebook { width: 100%; }
.ev-book { position: relative; width: min(240px, 52%); aspect-ratio: 3 / 4; perspective: 900px; }
.ev-cover {
  position: absolute; inset: 0; display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 12px;
  color: #fff; border: 1.5px solid var(--text); border-radius: 6px 14px 14px 6px;
  background:
    linear-gradient(90deg, rgba(0,0,0,.18) 0, transparent 14px),
    linear-gradient(150deg, color-mix(in srgb, var(--accent) 85%, #fff), var(--accent));
  box-shadow: 7px 7px 0 var(--text);
  transform: rotate(-2deg);
}
.ev-line { display: block; height: 8px; border-radius: 4px; background: rgba(255,255,255,.85); }
.ev-line.w1 { width: 56%; }
.ev-line.w2 { width: 34%; opacity: .7; }
.ev-page {
  position: absolute; inset: 4% 2% 4% 50%;
  background: var(--surface); border: 1.5px solid var(--text); border-radius: 0 10px 10px 0;
  transform-origin: left center; will-change: transform;
  background-image: repeating-linear-gradient(var(--surface) 0 14px, var(--border) 14px 16px);
  background-clip: content-box; padding: 12px 10px;
}
.ev-toc {
  width: min(230px, 44%); display: flex; flex-direction: column; gap: 10px;
  background: var(--surface); border: 1.5px solid var(--text); border-radius: var(--r-md);
  box-shadow: var(--pop-2); padding: 16px; transform: rotate(2deg);
}
.ev-toc-row { display: flex; align-items: center; gap: 10px; }
.ev-toc-row b { font-family: var(--oj-mono); font-size: 11px; color: var(--oj-primary); }
.ev-toc-row i { display: block; height: 8px; border-radius: 4px; background: var(--border); }
.ev-toc-row i.w1 { width: 78%; }
.ev-toc-row i.w2 { width: 62%; }
.ev-toc-row i.w3 { width: 70%; }
.ev-toc-row i.w4 { width: 48%; }
@media (min-width: 861px) {
  .vis-ebook { flex-direction: row; justify-content: center; gap: 26px; }
}

/* 章節導覽 rail */
.ls-rail {
  position: absolute; left: 50%; bottom: 22px; transform: translateX(-50%); z-index: 2;
  display: none; align-items: center; gap: 10px; flex-wrap: wrap; justify-content: center;
}
.ls-anim .ls-rail { display: flex; }
.ls-rail-item {
  display: inline-flex; align-items: center; gap: 6px; cursor: pointer;
  padding: 7px 13px; border: 1.5px solid var(--text); border-radius: 999px;
  background: var(--surface); font-family: var(--oj-display); font-weight: 600; font-size: 13px; color: var(--text);
  transition: background .2s, color .2s, transform .2s;
}
.ls-rail-item.on { background: var(--accent); color: #fff; transform: translateY(-2px); box-shadow: var(--pop-1); }
.ls-rail-hint { margin-left: 6px; font-family: var(--oj-mono); font-size: 12px; color: rgba(255,255,255,.72); }

/* ---------- 品牌跑馬燈帶 ---------- */
.l-marquee {
  position: relative; z-index: 1; overflow: hidden;
  width: 104%; margin-left: -2%; transform: rotate(-1.2deg);
  background: var(--text); color: #fff;
  border-top: 1.5px solid var(--text); border-bottom: 1.5px solid var(--text);
  padding: 15px 0;
}
.l-marquee-track {
  display: inline-flex; align-items: center; gap: 34px; width: max-content;
  animation: l-marquee-run 22s linear infinite;
}
.l-marquee-item {
  display: inline-flex; align-items: center; gap: 34px; white-space: nowrap;
  font-family: var(--oj-display); font-weight: 700; font-size: 19px; letter-spacing: .5px;
}
.l-marquee-item :deep(svg) { color: var(--c-lime); flex: none; }
@keyframes l-marquee-run { to { transform: translateX(-50%); } }

/* ---------- 區塊三：Creator Workflow（深色滿版 band） ---------- */
.l-flow {
  position: relative; overflow: hidden;
  margin-top: -24px; padding: 110px clamp(20px, 3.5vw, 56px) 96px;
  background:
    radial-gradient(rgba(255,255,255,.07) 1.3px, transparent 1.5px),
    linear-gradient(90deg, rgba(255,255,255,.05) 1px, transparent 1px),
    var(--text);
  background-size: 22px 22px, 25% 100%, auto;
}
.lf-bigword { top: .08em; left: -2%; font-size: clamp(90px, 14vw, 210px); -webkit-text-stroke: 2px rgba(255,255,255,.12); }
.l-flow .lsec-head { position: relative; z-index: 1; }
.l-flow .lsec-eyebrow { color: var(--c-lime); }
.l-flow .lsec-title { color: #fff; text-shadow: 3px 3px 0 rgba(0,0,0,.4); }
.l-flow .lsec-sub { color: rgba(255,255,255,.7); }
.fl-steps { position: relative; z-index: 1; display: grid; grid-template-columns: repeat(5, 1fr); gap: 18px; max-width: 1200px; margin: 0 auto; }
.fl-line {
  position: absolute; top: 26px; left: 6%; right: 6%; height: 3px; z-index: 0;
  background: repeating-linear-gradient(90deg, rgba(255,255,255,.4) 0 8px, transparent 8px 16px);
  opacity: .35;
}
.fl-line-fill {
  position: absolute; inset: 0; display: block;
  background: linear-gradient(90deg, var(--c-lime), var(--c-cyan), var(--c-pink));
}
.fl-step { position: relative; z-index: 1; text-align: center; padding: 0 6px; }
/* 插畫在深色 band 上——以淺色 hard shadow 補立體（呼應原 .fl-ic 手法） */
.fl-art { display: block; width: 84px; margin: 0 auto 14px; }
.fl-art :deep(.lart) { filter: drop-shadow(4px 5px 0 rgba(255,255,255,.26)); }
.fl-num { margin: 0 0 4px; font-family: var(--oj-mono); font-size: 12px; color: rgba(255,255,255,.55); }
.fl-step h3 { margin: 0 0 6px; font-family: var(--oj-display); font-weight: 700; font-size: 16px; color: #fff; }
.fl-text { margin: 0; font-size: 13px; line-height: 1.7; color: rgba(255,255,255,.68); }

/* ---------- 區塊四：Consumer Experience ---------- */
.l-discover {
  position: relative; overflow: hidden;
  padding: 96px clamp(20px, 3.5vw, 56px);
  background:
    radial-gradient(rgba(26,22,38,.05) 1.3px, transparent 1.5px),
    color-mix(in srgb, var(--c-violet) 10%, var(--bg));
  background-size: 22px 22px, auto;
}
.ld-bigword { top: .06em; left: -3%; font-size: clamp(90px, 14vw, 210px); }
.dc-inner {
  display: grid; grid-template-columns: minmax(0, 1fr) minmax(0, 1.05fr);
  align-items: center; gap: clamp(30px, 5vw, 76px);
  max-width: 1120px; margin: 0 auto;
}
.dc-copy .lsec-eyebrow { color: var(--c-violet); }
.dc-copy .lsec-title { text-align: left; }
.dc-copy { text-align: left; }
.dc-text { margin: 16px 0 0; font-size: 15.5px; line-height: 1.85; color: var(--text-soft); }
.dc-btn {
  display: inline-flex; align-items: center; gap: 7px; margin-top: 24px; cursor: pointer;
  font-family: var(--oj-display); font-weight: 700; font-size: 14.5px; color: var(--text);
  background: var(--surface); border: 1.5px solid var(--text); border-radius: var(--r-sm);
  box-shadow: var(--pop-2); padding: 10px 16px; transition: transform .12s, box-shadow .12s;
}
.dc-btn:hover { transform: translate(-1px, -1px); box-shadow: var(--pop-2-h); }
.dc-btn:active { transform: translate(2px, 2px); box-shadow: 1px 1px 0 var(--text); }

.dc-collage { position: relative; display: flex; flex-direction: column; gap: 16px; align-items: center; }
.dc-card {
  background: var(--surface); border: 1.5px solid var(--text); border-radius: var(--r-md);
  box-shadow: 5px 5px 0 var(--text); padding: 14px 16px;
}
.dc-label {
  display: flex; align-items: center; gap: 6px; margin: 0 0 10px;
  font-family: var(--oj-mono); font-size: 11px; letter-spacing: 1px; text-transform: uppercase; color: var(--text-soft);
}
.dc-grid { width: min(360px, 100%); transform: rotate(-2deg); }
.dc-grid-imgs { display: grid; grid-template-columns: repeat(4, 1fr); gap: 8px; }
.dc-grid-imgs img { width: 100%; aspect-ratio: 1; object-fit: cover; border: 1.5px solid var(--text); border-radius: 9px; }
.dc-profile { width: min(320px, 100%); transform: rotate(1.6deg) translateX(8%); }
.dc-profile-row { display: flex; align-items: center; gap: 10px; }
.dc-avatar {
  width: 38px; height: 38px; flex: none; display: grid; place-items: center; color: #fff;
  border: 1.5px solid var(--text); border-radius: 50%;
  background: linear-gradient(135deg, var(--c-violet), var(--c-pink));
}
.dc-pl { display: block; height: 9px; border-radius: 5px; background: var(--border); }
.dc-pl.w1 { flex: 1; background: var(--text); opacity: .8; }
.dc-pl.w2 { width: 62%; margin-top: 10px; }
.dc-follow {
  padding: 5px 12px; border: 1.5px solid var(--text); border-radius: 999px;
  background: var(--c-lime); box-shadow: var(--pop-1);
  font-family: var(--oj-display); font-weight: 700; font-size: 12.5px; color: var(--text);
}
.dc-fav { width: min(300px, 100%); transform: rotate(-1.4deg) translateX(-6%); }
.dc-fav-row { display: flex; align-items: center; gap: 8px; }
.dc-fav-row img { width: 54px; aspect-ratio: 1; object-fit: cover; border: 1.5px solid var(--text); border-radius: 9px; }
.dc-fav-heart {
  width: 34px; height: 34px; display: grid; place-items: center; margin-left: auto;
  border: 1.5px solid var(--text); border-radius: 50%; background: var(--c-pink); color: #fff; box-shadow: var(--pop-1);
}

/* ---------- 區塊五：Why Open Jam（滿版黃 band） ---------- */
.l-why {
  position: relative; overflow: hidden;
  padding: 100px clamp(20px, 3.5vw, 56px) 88px;
  background:
    radial-gradient(rgba(26,22,38,.07) 1.3px, transparent 1.5px),
    linear-gradient(90deg, rgba(26,22,38,.05) 1px, transparent 1px),
    var(--c-yellow);
  background-size: 22px 22px, 25% 100%, auto;
  border-top: 1.5px solid var(--text); border-bottom: 1.5px solid var(--text);
}
.lw-bigword { top: .06em; right: -3%; -webkit-text-stroke: 2px rgba(26,22,38,.15); font-size: clamp(90px, 14vw, 210px); }
.l-why .lsec-head { position: relative; z-index: 1; }
.l-why .lsec-eyebrow { color: var(--text); }
.why-grid { position: relative; z-index: 1; display: grid; grid-template-columns: repeat(2, 1fr); gap: 18px; max-width: 1120px; margin: 0 auto; }
.why-card {
  padding: 24px 24px 26px; border: 1.5px solid var(--text); border-radius: var(--r-lg);
  background: var(--surface); box-shadow: 6px 6px 0 var(--text);
}
.why-art { display: block; width: 88px; margin: 0 0 16px; }
.why-art :deep(.lart) { filter: drop-shadow(5px 6px 0 var(--text)); }
.why-card h3 { margin: 0 0 8px; font-family: var(--oj-display); font-weight: 800; font-size: 18px; color: var(--text); }
.why-card p { margin: 0; font-size: 14px; line-height: 1.75; color: var(--text-soft); }

/* ---------- 最後區塊：CTA ---------- */
.l-cta { position: relative; overflow: hidden; padding: 88px clamp(20px, 3.5vw, 56px) 110px; max-width: 1080px; margin: 0 auto; box-sizing: content-box; }
.lc-bigword { top: -0.05em; left: 4%; font-size: clamp(100px, 15vw, 230px); }
.lc-eyebrow {
  position: relative; z-index: 1;
  display: flex; align-items: center; justify-content: center; gap: 8px; margin: 0 0 34px;
  font-family: var(--oj-display); font-weight: 800; font-size: clamp(28px, 4.4vw, 52px);
  letter-spacing: -1.4px; color: var(--text); text-align: center;
}
.lc-grid { position: relative; z-index: 1; }
.lc-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 22px; }
.lc-card {
  padding: 32px 30px; border: 1.5px solid var(--border-strong); border-radius: var(--r-lg);
  background: var(--surface); box-shadow: 6px 6px 0 var(--border);
}
.lc-creator { background: linear-gradient(150deg, var(--surface), color-mix(in srgb, var(--c-violet) 10%, var(--surface))); }
.lc-buyer { background: linear-gradient(150deg, var(--surface), color-mix(in srgb, var(--c-cyan) 10%, var(--surface))); }
.lc-art { display: block; width: 94px; margin: 0 0 18px; }
.lc-art :deep(.lart) { filter: drop-shadow(5px 6px 0 var(--text)); }
.lc-card h3 { margin: 0; font-family: var(--oj-display); font-weight: 800; font-size: 22px; color: var(--text); }
.lc-card p { margin: 8px 0 22px; font-size: 14.5px; line-height: 1.75; color: var(--text-soft); }
.lc-btn {
  display: inline-flex; align-items: center; gap: 7px; cursor: pointer;
  font-family: var(--oj-display); font-weight: 700; font-size: 15px; color: var(--text);
  background: var(--surface); border: 1.5px solid var(--text); border-radius: var(--r-sm);
  box-shadow: var(--pop-2); padding: 11px 18px; transition: transform .12s, box-shadow .12s;
}
.lc-btn-main { background: var(--oj-primary); color: #fff; }
.lc-btn:hover { transform: translate(-1px, -1px); box-shadow: var(--pop-2-h); }
.lc-btn:active { transform: translate(2px, 2px); box-shadow: 1px 1px 0 var(--text); }

/* ---------- RWD ---------- */
@media (max-width: 980px) {
  .fl-steps { grid-template-columns: repeat(2, 1fr); gap: 26px 18px; }
  .fl-line { display: none; }
}
@media (max-width: 860px) {
  .ls-chapter { grid-template-columns: 1fr; gap: 22px; padding-bottom: 110px; align-content: center; }
  .ls-visual { order: -1; }
  .mv-player, .mv-wave, .pv-ba, .pv-grid { width: min(340px, 100%); }
  .ev-book { width: min(170px, 40%); }
  .dc-inner { grid-template-columns: 1fr; }
  .lc-grid { grid-template-columns: 1fr; }
  .why-grid { grid-template-columns: 1fr; }
}
@media (max-width: 560px) {
  .lh-deco, .lh-confetti { display: none; }
  .fl-steps { grid-template-columns: 1fr; }
  .ls-chapter { padding-bottom: 128px; }
  .ls-chips li { font-size: 12.5px; padding: 6px 11px; }
}

/* reduced-motion：停用裝飾動畫（pin / 轉場由 matchMedia 控制不啟用） */
@media (prefers-reduced-motion: reduce) {
  .lh-hint :deep(svg), .mv-wave span, .l-marquee-track { animation: none; }
}
</style>
