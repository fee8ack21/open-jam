<script setup lang="ts">
/* ============================================================
   AboutView — 品牌故事頁（/about）
   版面結構：Hero 漸層色帶（比照 FaqView / LegalView）→ 三特色卡
   （疊在色帶下緣）→ 敘事區塊（GSAP ScrollTrigger 驅動）：
   - Pinned Product Story（創作者能在這裡賣什麼？——音樂 / 攝影 / 電子書
     三章，pin 住同一舞台，滾動推進，內容轉場而非整頁換頁）
   - Creator Workflow（上傳 → 定價 → 開店 → 分享 → 收到支持）
   - Consumer Experience（探索 / 追蹤 / 收藏庫 collage）
   - Why Open Jam（平台精神四卡）
   - 雙 CTA
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

// ----- 三章 — 圖示 / 主色留在程式碼，文案由 i18n（landing.story.<id>）提供 -----
// v3 章節底色 = 糖果色滿版（粉 / 黃 / 綠），文字用墨色
const CHAPTERS = [
  { id: 'music', icon: 'note', accent: 'var(--c-pink)' },
  { id: 'photo', icon: 'image', accent: 'var(--c-yellow)' },
  { id: 'ebook', icon: 'book', accent: 'var(--c-lime)' },
] as const;
const chapterCount = CHAPTERS.length;

function chipsFor(id: string): string[] {
  return (tm(`landing.story.${id}.chips`) as string[]).map((c) => rt(c));
}

// ----- 開場介紹三特色（圖示 / 主色留在程式碼，文案由 i18n 提供） -----
const introIcons = ['download', 'bag', 'heart'];
const introAccents = ['var(--c-violet)', 'var(--c-pink)', 'var(--c-lime)'];
const introFacts = computed(() =>
  (tm('landing.intro.facts') as { title: string; text: string }[]).map((f, i) => ({
    icon: introIcons[i],
    accent: introAccents[i],
    title: rt(f.title),
    text: rt(f.text),
  })),
);

// 攝影章：照片格與 before-after 底圖
const photoGrid = [imgSurf, imgColorful, imgInterior];
// Consumer collage：市集精選格 / 收藏縮圖
const discoverGrid = [imgSilver, imgColorful, imgDragon, imgSurf];
const favThumbs = [imgAutumn, imgInterior];

// 波形條高度 — 決定性偽隨機，避免每次 render 抖動
const WAVE_BARS = Array.from({ length: 30 }, (_, i) =>
  Math.round(24 + 62 * Math.abs(Math.sin(i * 0.9) + 0.35 * Math.sin(i * 2.3)) / 1.35),
);

const rootEl = ref<HTMLElement | null>(null);
const activeChapter = ref(0);
const activeAccent = computed(() => CHAPTERS[activeChapter.value].accent);

// 各步驟 / 卡片對應的手繪插畫（LandingArt name）
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
function goMarket() { router.push('/'); }

// ----- GSAP：全部動畫收在 context 內，離開頁面 revert 還原 -----
let ctx: gsap.Context | undefined;
let storyST: ScrollTrigger | undefined;

// pinned story timeline 節奏：每章停留 HOLD、章間轉場 TRANS（單位為 timeline 秒）
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

onMounted(() => {
  ctx = gsap.context(() => {
    const mm = gsap.matchMedia();
    mm.add('(prefers-reduced-motion: no-preference)', () => {
      // ---- Pinned product story（同一舞台推進三章） ----
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

      // ---- workflow 連接線隨捲動長出、步驟進場 ----
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

      // ---- collage 卡片錯落進場 ----
      gsap.from('.dc-card', {
        y: 54,
        autoAlpha: 0,
        rotation: (i) => [-4, 5, -3][i] ?? 0,
        duration: 0.7,
        stagger: 0.15,
        ease: 'power3.out',
        scrollTrigger: { trigger: '.dc-collage', start: 'top 78%', once: true },
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

    <main class="page about-page">
      <!-- ============ HERO：漸層色帶（比照 FaqView / LegalView） ============ -->
      <section class="about-hero">
        <span class="ah-word" aria-hidden="true">ABOUT</span>
        <i class="ah-deco ah-deco-sq" aria-hidden="true"></i>
        <i class="ah-deco ah-deco-dot" aria-hidden="true"></i>
        <span class="ah-deco ah-deco-note" aria-hidden="true"><app-icon name="note" :size="32" /></span>
        <div class="ah-inner">
          <p class="ah-badge"><app-icon name="note" :size="13" /> {{ t('landing.intro.eyebrow') }}</p>
          <i18n-t keypath="landing.intro.title" tag="h1" class="ah-title" scope="global">
            <template #mark><span class="hl hl-lime">{{ t('landing.intro.titleMark') }}</span></template>
          </i18n-t>
          <p class="ah-lede">{{ t('landing.intro.lead') }}</p>
        </div>
      </section>

      <!-- ============ 三特色卡（疊在色帶下緣） ============ -->
      <ul class="about-facts">
        <li v-for="f in introFacts" :key="f.title" class="af-card" :style="{ '--acc': f.accent }">
          <span class="af-ic"><app-icon :name="f.icon" :size="22" /></span>
          <h3>{{ f.title }}</h3>
          <p>{{ f.text }}</p>
        </li>
      </ul>

      <!-- ============ Pinned Product Story（創作者能在這裡賣什麼？） ============ -->
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
            <p class="ls-eyebrow"><app-icon name="note" :size="13" /> {{ t('landing.story.eyebrow') }}</p>
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
              <div class="ls-inner">
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
                  <div class="mv-art"><app-icon name="note" :size="40" /></div>
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
                    <app-icon name="book" :size="34" />
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
              {{ w }} <app-icon name="note" :size="20" />
            </span>
          </template>
        </div>
      </div>

      <!-- ============ Creator Workflow ============ -->
      <section class="l-flow">
        <div class="lsec-head lrv">
          <p class="lsec-eyebrow"><app-icon name="note" :size="13" /> {{ t('landing.flow.eyebrow') }}</p>
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

      <!-- ============ Consumer Experience ============ -->
      <section class="l-discover">
        <div class="dc-inner">
          <div class="dc-copy lrv">
            <p class="lsec-eyebrow"><app-icon name="note" :size="13" /> {{ t('landing.discover.eyebrow') }}</p>
            <h2 class="lsec-title">{{ t('landing.discover.title') }}</h2>
            <p class="dc-text">{{ t('landing.discover.text') }}</p>
            <button type="button" class="dc-btn" @click="goMarket">
              {{ t('landing.cta.buyer.button') }} <app-icon name="arrow" :size="15" />
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

      <!-- ============ Why Open Jam ============ -->
      <section class="l-why">
        <div class="lsec-head lrv">
          <p class="lsec-eyebrow"><app-icon name="note" :size="13" /> {{ t('landing.why.eyebrow') }}</p>
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
        <p class="lc-eyebrow">{{ t('landing.cta.eyebrow') }}</p>
        <p class="lc-note">{{ t('landing.cta.note') }}</p>
        <div class="lc-grid">
          <div class="lc-card lc-creator">
            <span class="lc-art"><landing-art name="sell" /></span>
            <h3>{{ t('landing.cta.creator.title') }}</h3>
            <p>{{ t('landing.cta.creator.text') }}</p>
            <button type="button" class="lc-btn lc-btn-main" @click="goWorkspace">
              {{ t('landing.cta.creator.button') }} <app-icon name="arrow" :size="15" />
            </button>
          </div>
          <div class="lc-card lc-buyer">
            <span class="lc-art"><landing-art name="explore" /></span>
            <h3>{{ t('landing.cta.buyer.title') }}</h3>
            <p>{{ t('landing.cta.buyer.text') }}</p>
            <button type="button" class="lc-btn" @click="goMarket">
              {{ t('landing.cta.buyer.button') }} <app-icon name="arrow" :size="15" />
            </button>
          </div>
        </div>
      </section>
    </main>

    <!-- ============ FOOTER ============ -->
    <app-footer />
  </div>
</template>

<style scoped>
/* 頁面滿版（覆蓋 .page 左右 gutter）：hero 色帶與 Pinned story 貼齊 viewport，比照 FaqView / LegalView */
.about-page { position: relative; max-width: none; padding: 0; }

/* ---------- 巨型出血標題字（描邊空心，僅章節舞台使用） ---------- */
.l-bigword {
  position: absolute; z-index: 0; pointer-events: none; user-select: none;
  font-family: var(--oj-display); font-weight: 700; line-height: 1; white-space: nowrap;
  letter-spacing: -0.02em; color: transparent;
}

/* 共用區塊標頭 */
.lsec-head { text-align: center; max-width: 760px; margin: 0 auto 44px; }
.lsec-eyebrow {
  display: inline-flex; align-items: center; gap: 7px; margin: 0 0 16px; padding: 5px 14px;
  font-family: var(--oj-font); font-size: 12px; font-weight: 900; letter-spacing: 2px;
  color: var(--c-yellow); background: var(--text); border-radius: 999px;
  transform: rotate(-2deg); white-space: nowrap;
}
.lsec-title {
  margin: 0; font-family: var(--oj-font); font-weight: 900;
  font-size: clamp(28px, 3.6vw, 38px); color: var(--text);
}
.lsec-sub { margin: 14px 0 0; font-size: 15px; font-weight: 500; line-height: 1.8; color: var(--text-soft); }

/* ── Hero 色帶：紫色圓點滿版帶（同 .mkt-hero 配方）────── */
.about-hero { position: relative; padding: clamp(52px, 8vh, 80px) clamp(20px, 3.5vw, 56px) clamp(64px, 9vh, 84px); text-align: center; }
.about-hero::before {
  content: ''; position: absolute; top: 0; bottom: 0; left: 50%;
  width: 100vw; transform: translateX(-50%); z-index: 0; pointer-events: none;
  background: #8a5cf6;
  background-image: radial-gradient(rgba(255, 255, 255, 0.18) 1.5px, transparent 1.5px);
  background-size: 26px 26px;
  border-bottom: 2px solid var(--border-strong);
}

/* 色帶內的縷空大字（白色微透、貼齊左上，沉在置中內容後） */
.ah-word {
  position: absolute; z-index: 1; top: 20px; left: -30px;
  font-family: var(--oj-display); font-weight: 700; line-height: 1;
  font-size: clamp(120px, 17vw, 190px); letter-spacing: -8px;
  color: rgba(255, 255, 255, .13); pointer-events: none; user-select: none;
}

/* 漂浮貼紙 */
.ah-deco { position: absolute; z-index: 1; font-style: normal; pointer-events: none; }
.ah-deco-sq { right: 7%; top: 48px; width: 56px; height: 56px; background: var(--c-lime); border: 2px solid var(--border-strong); border-radius: 16px; transform: rotate(14deg); }
.ah-deco-dot { right: 14%; bottom: 44px; width: 44px; height: 44px; background: var(--c-yellow); border: 2px solid var(--border-strong); border-radius: 50%; }
.ah-deco-note { left: 10%; bottom: 56px; color: var(--c-yellow); transform: rotate(-8deg); }

.ah-inner {
  position: relative; z-index: 2; max-width: 820px; margin: 0 auto;
  display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 22px;
}
.ah-badge {
  display: inline-flex; align-items: center; gap: 8px; margin: 0; padding: 7px 18px;
  font-family: var(--oj-font); font-size: 13px; font-weight: 900; letter-spacing: 2px;
  color: var(--c-yellow); background: var(--text);
  border-radius: 999px; transform: rotate(-1deg); white-space: nowrap;
}
.ah-title {
  margin: 0; font-family: var(--oj-font); font-weight: 900; color: #fff;
  font-size: clamp(28px, 4.6vw, 52px); line-height: 1.3;
}
.ah-title .hl { box-shadow: var(--ink-drop); margin: 0 8px; }
.ah-lede { margin: 0; max-width: 40em; font-size: 17px; font-weight: 700; line-height: 1.9; color: #fff; }

/* ── 三特色卡：疊在色帶下緣（設計稿 promise cards：淡彩 + 膠帶 + 微傾） ────── */
.about-facts {
  position: relative; z-index: 2; list-style: none; max-width: 1080px; margin: -44px auto 0;
  padding: 0 clamp(20px, 3.5vw, 56px);
  display: grid; grid-template-columns: repeat(auto-fit, minmax(240px, 1fr)); gap: 24px;
}
.af-card {
  position: relative; text-align: left; padding: 22px;
  background: color-mix(in srgb, var(--acc) 22%, #fff);
  border: 2px solid var(--border-strong); border-radius: 18px;
  box-shadow: 0 10px 24px rgba(26, 26, 26, 0.22);
}
.af-card:nth-child(1) { transform: rotate(-1.5deg); }
.af-card:nth-child(2) { transform: rotate(1deg); }
.af-card:nth-child(3) { transform: rotate(-0.8deg); }
/* 膠帶貼條 */
.af-card::before {
  content: ''; position: absolute; top: -12px; left: 50%; width: 60px; height: 19px;
  margin-left: -30px; background: rgba(255, 222, 0, 0.85); border-radius: 3px;
  transform: rotate(-3deg); box-shadow: 0 1px 3px rgba(26, 26, 26, 0.15);
}
.af-card:nth-child(2)::before { background: rgba(125, 217, 255, 0.85); transform: rotate(3deg); }
.af-card:nth-child(3)::before { background: rgba(255, 144, 232, 0.85); transform: rotate(-4deg); }
.af-ic {
  display: grid; place-items: center; width: 44px; height: 44px; margin: 0 0 14px;
  color: var(--text); background: var(--acc);
  border: 2px solid var(--border-strong); border-radius: 14px; transform: rotate(-3deg);
}
.af-card:nth-child(1) .af-ic { color: #fff; }
.af-card h3 { margin: 0 0 6px; font-family: var(--oj-font); font-weight: 900; font-size: 17px; color: var(--text); }
.af-card p { margin: 0; font-size: 13px; font-weight: 500; line-height: 1.7; color: var(--text); }

/* ---------- 區塊二：Pinned Product Story ---------- */
.l-story { margin-top: clamp(48px, 7vh, 72px); }
.ls-stage {
  position: relative;
  display: flex; flex-direction: column;
  border-top: 2px solid var(--border-strong);
}
/* GSAP 啟用時鎖滿版高、章節絕對堆疊（預設一般文流退場） */
.ls-anim { height: calc(100vh - var(--nav-h)); overflow: hidden; }

/* 每章整屏糖果色塊（v3：糖果底 + 墨點圖紋 + 墨色文字） */
.ls-bg {
  display: none;
  position: absolute; inset: 0; z-index: 0;
  background: var(--accent);
  background-image: radial-gradient(rgba(26, 26, 26, 0.1) 1.5px, transparent 1.5px);
  background-size: 26px 26px;
}
.ls-anim .ls-bg { display: block; }

.ls-head { position: relative; z-index: 2; text-align: center; padding: clamp(24px, 4vh, 44px) 20px 0; }
.ls-eyebrow {
  display: inline-flex; align-items: center; gap: 7px; margin: 0 0 12px; padding: 6px 16px;
  font-family: var(--oj-font); font-size: 12px; font-weight: 900; letter-spacing: 2px;
  color: #fff; background: var(--text); border-radius: 999px; transform: rotate(-1.5deg);
  white-space: nowrap;
}
.ls-title {
  margin: 0; font-family: var(--oj-font); font-weight: 900;
  font-size: clamp(28px, 4vw, 44px); color: var(--text);
}
/* 非動畫模式（reduced-motion）：常駐標題落在奶油頁面底上 */
.ls-stage:not(.ls-anim) .ls-head { padding-bottom: clamp(20px, 3.5vh, 36px); }

.ls-chapters { position: relative; z-index: 1; flex: 1; }
.ls-chapter {
  position: relative;
  display: flex; align-items: center; justify-content: center;
  padding: 40px clamp(20px, 3.5vw, 56px) 90px;
}
/* 兩欄內容維持 1080 置中；大字則相對滿版章節定位以貼齊右上角 */
.ls-inner {
  display: grid; grid-template-columns: minmax(0, 1.05fr) minmax(0, 1fr);
  align-items: center; gap: clamp(28px, 5vw, 76px);
  width: 100%; max-width: 1080px;
}
.ls-stage:not(.ls-anim) .ls-chapter {
  background: var(--accent);
  background-image: radial-gradient(rgba(26, 26, 26, 0.1) 1.5px, transparent 1.5px);
  background-size: 26px 26px;
  border-top: 2px solid var(--border-strong);
}
.ls-anim .ls-chapter { position: absolute; inset: 0; height: 100%; will-change: transform, opacity; }

.ls-bigword { top: .06em; right: 3%; font-size: clamp(96px, 15vw, 230px); -webkit-text-stroke: 2px rgba(26, 26, 26, .18); }

.ls-num {
  display: inline-block; margin: 0 0 12px; padding: 5px 16px;
  font-family: var(--oj-font); font-size: 12px; font-weight: 900; letter-spacing: 2px;
  color: #fff; background: var(--text); border-radius: 999px; transform: rotate(-1.5deg);
}
.ls-ch-title {
  margin: 0; font-family: var(--oj-font); font-weight: 900;
  font-size: clamp(30px, 4vw, 44px); line-height: 1.35; color: var(--text);
}
.ls-desc { margin: 16px 0 0; max-width: 440px; font-size: 16px; font-weight: 700; line-height: 1.8; color: var(--text); }
.ls-chips { list-style: none; display: flex; flex-wrap: wrap; gap: 8px; margin: 20px 0 0; padding: 0; }
.ls-chips li {
  padding: 6px 16px; border: 2px solid var(--border-strong); border-radius: 999px;
  background: #fff;
  font-family: var(--oj-font); font-weight: 700; font-size: 13px; color: var(--text);
}

/* flex column（非 grid auto track）：避免子元素原始尺寸撐開軌道把內容推出畫面 */
.ls-visual { position: relative; display: flex; flex-direction: column; align-items: center; gap: 18px; min-width: 0; }

/* 音樂章視覺 */
.vis-music { width: 100%; }
.mv-player {
  display: flex; align-items: center; gap: 14px; width: min(380px, 100%);
  background: var(--surface); border: 2px solid var(--border-strong); border-radius: var(--r-md);
  box-shadow: 0 16px 32px rgba(26, 26, 26, 0.22); padding: 14px 16px; transform: rotate(-1.5deg);
}
.mv-art {
  width: 64px; height: 64px; flex: none; display: grid; place-items: center; color: var(--text);
  border: 2px solid var(--border-strong); border-radius: 12px;
  background: var(--c-pink);
}
.mv-meta { flex: 1; min-width: 0; }
.mv-line { display: block; height: 9px; border-radius: 5px; background: var(--border); }
.mv-line.w1 { width: 72%; background: var(--text); opacity: .82; }
.mv-line.w2 { width: 46%; margin-top: 7px; }
.mv-progress { position: relative; height: 5px; border-radius: 3px; background: var(--border); margin-top: 12px; }
.mv-ph { position: absolute; left: 0; top: 0; bottom: 0; width: 38%; border-radius: 3px; background: var(--c-violet); }
.mv-play {
  width: 44px; height: 44px; flex: none; display: grid; place-items: center;
  border: 2px solid var(--border-strong); border-radius: 50%; background: var(--c-yellow); box-shadow: var(--ink-drop-sm);
}
.mv-tri {
  width: 0; height: 0; margin-left: 3px;
  border-top: 8px solid transparent; border-bottom: 8px solid transparent; border-left: 13px solid var(--text);
}
.mv-wave { display: flex; align-items: flex-end; gap: 5px; height: 72px; width: min(380px, 100%); }
.mv-wave span {
  flex: 1; border-radius: 3px 3px 0 0; transform-origin: bottom;
  background: linear-gradient(180deg, #1a1a1a, rgba(26, 26, 26, .5));
  animation: lh-eq 1.6s ease-in-out infinite;
}
/* lh-eq keyframes 供音樂章 .mv-wave 使用 */
@keyframes lh-eq { 50% { transform: scaleY(.45); } }

/* 攝影章視覺 */
.vis-photo { width: 100%; }
.pv-ba {
  --split: 50%;
  position: relative; width: min(400px, 100%); aspect-ratio: 16 / 10; overflow: hidden;
  border: 2px solid var(--border-strong); border-radius: var(--r-md);
  box-shadow: 0 16px 32px rgba(26, 26, 26, 0.22);
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
  background: var(--surface); border: 2px solid var(--border-strong);
}
.pv-handle::before { content: '‹›'; display: grid; place-items: center; height: 100%; font-size: 14px; font-weight: 700; color: var(--text); }
.pv-tag {
  position: absolute; bottom: 8px; padding: 2px 10px; border-radius: 999px;
  border: 2px solid var(--border-strong); font-family: var(--oj-display); font-size: 10.5px; font-weight: 700;
}
.pv-tag-b { left: 8px; background: var(--surface); color: var(--text); }
.pv-tag-a { right: 8px; background: var(--c-lime); color: var(--text); }
.pv-grid { display: flex; gap: 10px; width: min(400px, 100%); }
.pv-grid img {
  flex: 1 1 0; width: 0; min-width: 0; aspect-ratio: 1; object-fit: cover;
  border: 2px solid var(--border-strong); border-radius: 12px;
}

/* 電子書章視覺 */
.vis-ebook { width: 100%; }
.ev-book { position: relative; width: min(240px, 52%); aspect-ratio: 3 / 4; perspective: 900px; }
.ev-cover {
  position: absolute; inset: 0; display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 12px;
  color: var(--text); border: 2px solid var(--border-strong); border-radius: 6px 14px 14px 6px;
  background:
    linear-gradient(90deg, rgba(0,0,0,.14) 0, transparent 14px),
    var(--c-violet);
  box-shadow: 0 14px 28px rgba(26, 26, 26, 0.25);
  transform: rotate(-2deg); color: #fff;
}
.ev-line { display: block; height: 8px; border-radius: 4px; background: rgba(255,255,255,.85); }
.ev-line.w1 { width: 56%; }
.ev-line.w2 { width: 34%; opacity: .7; }
.ev-page {
  position: absolute; inset: 4% 2% 4% 50%;
  background: var(--surface); border: 2px solid var(--border-strong); border-radius: 0 10px 10px 0;
  transform-origin: left center; will-change: transform;
  background-image: repeating-linear-gradient(var(--surface) 0 14px, var(--border) 14px 16px);
  background-clip: content-box; padding: 12px 10px;
}
.ev-toc {
  width: min(230px, 44%); display: flex; flex-direction: column; gap: 10px;
  background: var(--surface); border: 2px solid var(--border-strong); border-radius: var(--r-md);
  box-shadow: 0 8px 20px rgba(26, 26, 26, 0.12); padding: 16px; transform: rotate(2deg);
}
.ev-toc-row { display: flex; align-items: center; gap: 10px; }
.ev-toc-row b { font-family: var(--oj-display); font-size: 11px; font-weight: 700; color: var(--c-violet); }
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
  padding: 7px 15px; border: 2px solid var(--border-strong); border-radius: 999px;
  background: var(--surface); font-family: var(--oj-font); font-weight: 700; font-size: 13px; color: var(--text);
  transition: background .2s, color .2s, transform .2s var(--ease-pop);
}
.ls-rail-item.on { background: var(--text); color: var(--c-yellow); transform: translateY(-2px); font-weight: 900; }
.ls-rail-hint { margin-left: 6px; font-family: var(--oj-hand); font-weight: 700; font-size: 18px; color: var(--text); transform: rotate(-2deg); }

/* ---------- 品牌跑馬燈帶 ---------- */
.l-marquee {
  position: relative; z-index: 1; overflow: hidden;
  background: var(--text); color: #fff;
  padding: 14px 0;
}
.l-marquee-track {
  display: inline-flex; align-items: center; gap: 32px; width: max-content;
  animation: l-marquee-run 26s linear infinite;
}
.l-marquee-item {
  display: inline-flex; align-items: center; gap: 32px; white-space: nowrap;
  font-family: var(--oj-font); font-weight: 900; font-size: 15px;
}
.l-marquee-item:nth-child(5n+1) { color: var(--c-yellow); }
.l-marquee-item:nth-child(5n+2) { color: var(--c-pink); }
.l-marquee-item:nth-child(5n+4) { color: var(--c-lime); }
.l-marquee-item:nth-child(5n) { color: var(--c-cyan); }
.l-marquee-item :deep(svg) { color: currentColor; flex: none; }
@keyframes l-marquee-run { to { transform: translateX(-50%); } }

/* ---------- 區塊三：Creator Workflow（深色滿版 band） ---------- */
.l-flow {
  position: relative; overflow: hidden;
  padding: 96px clamp(20px, 3.5vw, 56px) 88px;
  background: #1a1626;
  background-image: radial-gradient(rgba(255, 255, 255, 0.08) 1.5px, transparent 1.5px);
  background-size: 26px 26px;
  border-bottom: 2px solid var(--border-strong);
}
.l-flow .lsec-head { position: relative; z-index: 1; }
.l-flow .lsec-eyebrow { background: var(--c-yellow); color: var(--text); border: 2px solid var(--border-strong); }
.l-flow .lsec-title { color: #fff; }
.l-flow .lsec-sub { color: #bbb; }
.fl-steps { position: relative; z-index: 1; display: grid; grid-template-columns: repeat(5, 1fr); gap: 20px; max-width: 1200px; margin: 0 auto; }
.fl-line {
  position: absolute; top: 26px; left: 6%; right: 6%; height: 3px; z-index: 0;
  background: repeating-linear-gradient(90deg, rgba(255,255,255,.4) 0 8px, transparent 8px 16px);
  opacity: .35;
}
.fl-line-fill {
  position: absolute; inset: 0; display: block;
  background: linear-gradient(90deg, var(--c-lime), var(--c-cyan), var(--c-pink));
}
/* v3：步驟改為白色墨線卡（插畫保留），各卡糖果色光暈 */
.fl-step {
  position: relative; z-index: 1; text-align: center; padding: 20px 14px 22px;
  background: #fff; border: 2px solid var(--border-strong); border-radius: 18px;
  transition: transform .2s var(--ease-pop);
}
.fl-step:nth-child(2) { box-shadow: 0 10px 22px rgba(255, 144, 232, 0.25); }
.fl-step:nth-child(3) { box-shadow: 0 10px 22px rgba(255, 222, 0, 0.25); }
.fl-step:nth-child(4) { box-shadow: 0 10px 22px rgba(125, 217, 255, 0.25); }
.fl-step:nth-child(5) { box-shadow: 0 10px 22px rgba(184, 255, 159, 0.25); }
.fl-step:nth-child(6) { box-shadow: 0 10px 22px rgba(138, 92, 246, 0.3); }
.fl-step:hover { transform: translateY(-4px) rotate(-1deg); }
.fl-art { display: block; width: 80px; margin: 0 auto 12px; }
.fl-art :deep(.lart) { filter: drop-shadow(3px 4px 0 rgba(26, 26, 26, .2)); }
.fl-num {
  display: inline-block; margin: 0 0 6px; padding: 2px 10px;
  font-family: var(--oj-font); font-size: 10px; font-weight: 900; letter-spacing: 1px;
  color: #fff; background: var(--text); border-radius: 999px;
}
.fl-step h3 { margin: 0 0 6px; font-family: var(--oj-font); font-weight: 900; font-size: 16px; color: var(--text); }
.fl-text { margin: 0; font-size: 12px; font-weight: 500; line-height: 1.7; color: #444; }

/* ---------- 區塊四：Consumer Experience（丁香紫 band） ---------- */
.l-discover {
  position: relative; overflow: hidden;
  padding: 88px clamp(20px, 3.5vw, 56px);
  background: var(--t-violet);
  background-image: radial-gradient(rgba(26, 26, 26, 0.08) 1.2px, transparent 1.2px);
  background-size: 24px 24px;
  border-bottom: 2px solid var(--border-strong);
}
.dc-inner {
  display: grid; grid-template-columns: minmax(0, 1fr) minmax(0, 1.05fr);
  align-items: center; gap: clamp(30px, 5vw, 76px);
  max-width: 1120px; margin: 0 auto;
}
.dc-copy .lsec-eyebrow { background: var(--text); color: var(--c-pink); }
.dc-copy .lsec-title { text-align: left; }
.dc-copy { text-align: left; }
.dc-text { margin: 16px 0 0; font-size: 15px; font-weight: 500; line-height: 1.9; color: var(--text); max-width: 440px; }
.dc-btn {
  display: inline-flex; align-items: center; gap: 7px; margin-top: 26px; cursor: pointer;
  font-family: var(--oj-font); font-weight: 900; font-size: 15px; color: var(--c-yellow);
  background: var(--text); border: 0; border-radius: 999px;
  padding: 14px 30px; transition: transform .2s var(--ease-pop), box-shadow .2s;
}
.dc-btn:hover { transform: translateY(-3px) rotate(-1deg); box-shadow: 0 10px 22px rgba(26, 26, 26, 0.3); }
.dc-btn:active { transform: translateY(0); }

.dc-collage { position: relative; display: flex; flex-direction: column; gap: 16px; align-items: center; }
.dc-card {
  background: var(--surface); border: 2px solid var(--border-strong); border-radius: 16px;
  box-shadow: 0 12px 26px rgba(26, 26, 26, 0.16); padding: 16px;
}
.dc-label {
  display: flex; align-items: center; gap: 6px; margin: 0 0 10px;
  font-family: var(--oj-font); font-size: 12px; font-weight: 900; color: var(--text);
}
.dc-grid { width: min(360px, 100%); transform: rotate(-2deg); }
.dc-grid-imgs { display: grid; grid-template-columns: repeat(4, 1fr); gap: 8px; }
.dc-grid-imgs img { width: 100%; aspect-ratio: 1; object-fit: cover; border: 2px solid var(--border-strong); border-radius: 8px; }
.dc-profile { width: min(320px, 100%); transform: rotate(2deg) translateX(8%); }
.dc-profile-row { display: flex; align-items: center; gap: 10px; }
.dc-avatar {
  width: 38px; height: 38px; flex: none; display: grid; place-items: center; color: var(--text);
  border: 2px solid var(--border-strong); border-radius: 50%;
  background: var(--c-yellow);
}
.dc-pl { display: block; height: 9px; border-radius: 5px; background: var(--border); }
.dc-pl.w1 { flex: 1; background: var(--text); opacity: .8; }
.dc-pl.w2 { width: 62%; margin-top: 10px; }
.dc-follow {
  padding: 4px 14px; border: 2px solid var(--border-strong); border-radius: 999px;
  background: var(--c-lime);
  font-family: var(--oj-font); font-weight: 900; font-size: 12px; color: var(--text); white-space: nowrap;
}
.dc-fav { width: min(300px, 100%); transform: rotate(-1.4deg) translateX(-6%); }
.dc-fav-row { display: flex; align-items: center; gap: 8px; }
.dc-fav-row img { width: 54px; aspect-ratio: 1; object-fit: cover; border: 2px solid var(--border-strong); border-radius: 8px; }
.dc-fav-heart {
  width: 34px; height: 34px; display: grid; place-items: center; margin-left: auto;
  border: 2px solid var(--border-strong); border-radius: 50%; background: var(--c-pink); color: var(--text);
}

/* ---------- 區塊五：Why Open Jam（滿版黃 band） ---------- */
.l-why {
  position: relative; overflow: hidden;
  padding: 88px clamp(20px, 3.5vw, 56px) 80px;
  background: #ffc01f;
  border-bottom: 2px solid var(--border-strong);
}
.l-why .lsec-head { position: relative; z-index: 1; }
.why-grid { position: relative; z-index: 1; display: grid; grid-template-columns: repeat(2, 1fr); gap: 26px; max-width: 1020px; margin: 0 auto; }
.why-card {
  display: flex; gap: 18px; align-items: flex-start;
  padding: 26px; border: 2px solid var(--border-strong); border-radius: var(--r-lg);
  background: var(--surface); box-shadow: 0 10px 22px rgba(26, 26, 26, 0.12);
  transition: transform .2s var(--ease-pop), box-shadow .2s;
}
.why-card:nth-child(odd):hover { transform: translateY(-4px) rotate(-.5deg); box-shadow: 0 16px 30px rgba(26, 26, 26, 0.16); }
.why-card:nth-child(even):hover { transform: translateY(-4px) rotate(.5deg); box-shadow: 0 16px 30px rgba(26, 26, 26, 0.16); }
.why-art { display: block; width: 84px; flex: none; }
.why-art :deep(.lart) { filter: drop-shadow(3px 4px 0 rgba(26, 26, 26, .2)); }
.why-card h3 { margin: 0 0 8px; font-family: var(--oj-font); font-weight: 900; font-size: 19px; color: var(--text); }
.why-card p { margin: 0; font-size: 14px; font-weight: 500; line-height: 1.8; color: var(--text); }

/* ---------- 最後區塊：CTA ---------- */
.l-cta { position: relative; overflow: hidden; padding: 80px clamp(20px, 3.5vw, 56px) 88px; text-align: center; }
.lc-eyebrow {
  position: relative; z-index: 1;
  margin: 0 0 8px;
  font-family: var(--oj-font); font-weight: 900; font-size: clamp(28px, 4vw, 42px);
  color: var(--text); text-align: center;
}
.lc-note {
  position: relative; z-index: 1; margin: 0 0 36px;
  font-family: var(--oj-hand); font-weight: 700; font-size: 26px; color: var(--c-violet);
  transform: rotate(-2deg);
}
.lc-grid { position: relative; z-index: 1; text-align: left; }
.lc-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 28px; max-width: 1020px; margin: 0 auto; }
.lc-card {
  padding: 32px; border: 2px solid var(--border-strong); border-radius: 22px;
  box-shadow: 0 12px 26px rgba(26, 26, 26, 0.12);
  transition: transform .2s var(--ease-pop), box-shadow .2s;
}
.lc-card:hover { transform: translateY(-5px); box-shadow: 0 18px 34px rgba(26, 26, 26, 0.16); }
.lc-creator { background: var(--t-violet); }
.lc-buyer { background: #c9f7e4; }
.lc-art { display: block; width: 92px; margin: 0 0 18px; }
.lc-art :deep(.lart) { filter: drop-shadow(3px 4px 0 rgba(26, 26, 26, .2)); }
.lc-card h3 { margin: 0; font-family: var(--oj-font); font-weight: 900; font-size: 23px; color: var(--text); }
.lc-card p { margin: 8px 0 22px; font-size: 14px; font-weight: 500; line-height: 1.8; color: var(--text); }
.lc-btn {
  display: inline-flex; align-items: center; gap: 7px; cursor: pointer;
  font-family: var(--oj-font); font-weight: 900; font-size: 15px; color: var(--c-yellow);
  background: var(--text); border: 0; border-radius: 999px;
  padding: 13px 26px; transition: transform .2s var(--ease-pop), box-shadow .2s;
}
.lc-btn-main { background: var(--c-violet); color: #fff; }
.lc-btn:hover { transform: translateY(-3px) rotate(-1deg); box-shadow: 0 10px 22px rgba(26, 26, 26, 0.3); }
.lc-btn-main:hover { box-shadow: 0 10px 22px rgba(138, 92, 246, 0.4); }
.lc-btn:active { transform: translateY(0); box-shadow: none; }

/* ---------- RWD ---------- */
@media (max-width: 980px) {
  .fl-steps { grid-template-columns: repeat(2, 1fr); gap: 26px 18px; }
  .fl-line { display: none; }
}
@media (max-width: 860px) {
  .ls-inner { grid-template-columns: 1fr; gap: 22px; }
  .ls-chapter { padding-bottom: 110px; }
  .ls-visual { order: -1; }
  .mv-player, .mv-wave, .pv-ba, .pv-grid { width: min(340px, 100%); }
  .ev-book { width: min(170px, 40%); }
  .dc-inner { grid-template-columns: 1fr; }
  .lc-grid { grid-template-columns: 1fr; }
  .why-grid { grid-template-columns: 1fr; }
}
@media (max-width: 640px) {
  .about-facts { margin-top: -36px; }
}
@media (max-width: 560px) {
  .fl-steps { grid-template-columns: 1fr; }
  .ls-chapter { padding-bottom: 128px; }
  .ls-chips li { font-size: 12.5px; padding: 6px 11px; }
}

/* reduced-motion：停用裝飾動畫（pin / 轉場由 matchMedia 控制不啟用） */
@media (prefers-reduced-motion: reduce) {
  .mv-wave span, .l-marquee-track { animation: none; }
}
</style>
