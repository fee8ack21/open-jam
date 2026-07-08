<script setup lang="ts">
/* ============================================================
   LandingView — 品牌 landing（/landing）
   Interactive storytelling experience，以 GSAP ScrollTrigger 驅動：
   - 區塊一：平台 / 品牌介紹（進場 timeline + 裝飾視差）
   - 區塊二：三大商品類型（pin 住整個視窗，滾動「換頁」而非下滑，
             音樂 → 攝影 → 電子書 三頁翻完才放行往下）
   - 區塊四 / 五：待補充（佔位）
   - 最後區塊：創作者上架 / 消費者瀏覽雙 CTA
   - Footer
   prefers-reduced-motion: reduce 時不做 pin 與動畫，
   三個類型面板退化為一般直向排列。
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

gsap.registerPlugin(ScrollTrigger);

const store = useShopStore();
const router = useRouter();
const { t, tm, rt } = useI18n();

// 區塊二三大類型 — 圖示 / 主色留在程式碼，文案由 i18n（landing.types.<id>）提供
const TYPE_PANELS = [
  { id: 'music', icon: 'note', accent: 'var(--c-violet)' },
  { id: 'photo', icon: 'image', accent: 'var(--c-pink)' },
  { id: 'ebook', icon: 'book', accent: 'var(--c-cyan)' },
] as const;

function chipsFor(id: string): string[] {
  return (tm(`landing.types.${id}.chips`) as string[]).map((c) => rt(c));
}

const rootEl = ref<HTMLElement | null>(null);
const activeType = ref(0);
const typeCount = TYPE_PANELS.length;
const activeAccent = computed(() => TYPE_PANELS[activeType.value].accent);

function goWorkspace() { window.location.href = env.WORKSPACE_PAGE_URL; }
function goMarket() { router.push({ name: 'discover' }); }

// ----- GSAP：全部動畫收在 context 內，離開頁面 revert 還原 -----
let ctx: gsap.Context | undefined;
let typesST: ScrollTrigger | undefined;

/** 讀取 nav 高度，pin 的起點須避開 sticky nav。 */
function navH(): number {
  const v = parseFloat(getComputedStyle(document.documentElement).getPropertyValue('--nav-h'));
  return Number.isFinite(v) ? v : 72;
}

/** 進度點點擊：捲動到 pin 區間內對應頁的位置。 */
function goPage(i: number) {
  if (!typesST) return;
  const top = typesST.start + ((typesST.end - typesST.start) * i) / (typeCount - 1);
  window.scrollTo({ top, behavior: 'smooth' });
}

onMounted(() => {
  ctx = gsap.context(() => {
    const mm = gsap.matchMedia();
    mm.add('(prefers-reduced-motion: no-preference)', () => {
      // ---- 區塊一：品牌進場 timeline ----
      gsap.timeline({ defaults: { ease: 'power3.out' } })
        .from('.lh-eyebrow', { y: 26, autoAlpha: 0, duration: 0.55 })
        .from('.lh-line', { y: 64, autoAlpha: 0, duration: 0.8, stagger: 0.14 }, '-=0.25')
        .from('.lh-lede', { y: 30, autoAlpha: 0, duration: 0.6 }, '-=0.35')
        .from('.lh-hint', { autoAlpha: 0, duration: 0.5 }, '-=0.1');

      // 裝飾形狀隨捲動視差
      gsap.utils.toArray<HTMLElement>('.lh-deco').forEach((el, i) => {
        gsap.to(el, {
          yPercent: (i % 2 ? -1 : 1) * (16 + i * 8),
          rotation: i % 2 ? -16 : 12,
          ease: 'none',
          scrollTrigger: { trigger: '.l-hero', start: 'top top', end: 'bottom top', scrub: true },
        });
      });

      // ---- 區塊二：pin + 換頁 ----
      // 啟用絕對定位堆疊（CSS 預設為一般文流，供 reduced-motion / 無 JS 退場）
      const stage = document.querySelector<HTMLElement>('.lt-stage');
      stage?.classList.add('lt-anim');

      const panels = gsap.utils.toArray<HTMLElement>('.lt-panel');
      const tl = gsap.timeline({
        defaults: { ease: 'none' },
        scrollTrigger: {
          trigger: '.l-types',
          start: () => `top ${navH()}px`,
          end: () => `+=${panels.length * 100}%`,
          pin: true,
          scrub: 0.55,
          snap: {
            snapTo: 1 / (panels.length - 1),
            duration: { min: 0.25, max: 0.6 },
            delay: 0.05,
            ease: 'power1.inOut',
          },
          onUpdate(st) { activeType.value = Math.round(st.progress * (panels.length - 1)); },
        },
      });
      typesST = tl.scrollTrigger;
      panels.forEach((panel, i) => {
        if (i === 0) return;
        // 下一頁由下方整頁蓋上，上一頁縮小淡出 — 「換頁」而非捲動
        tl.fromTo(panel, { yPercent: 103 }, { yPercent: 0, duration: 1 }, i - 1)
          .to(panels[i - 1], { yPercent: -8, scale: 0.95, autoAlpha: 0.3, duration: 1 }, i - 1);
      });

      // ---- 其餘區塊：進場 reveal ----
      gsap.utils.toArray<HTMLElement>('.ls-rv').forEach((el) => {
        gsap.from(el, {
          y: 48,
          autoAlpha: 0,
          duration: 0.8,
          ease: 'power3.out',
          scrollTrigger: { trigger: el, start: 'top 82%', once: true },
        });
      });

      return () => {
        stage?.classList.remove('lt-anim');
        typesST = undefined;
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
      <!-- ============ 區塊一：平台 / 品牌介紹 ============ -->
      <section class="l-hero">
        <span class="lh-deco lh-deco-ring" aria-hidden="true"></span>
        <span class="lh-deco lh-deco-star" aria-hidden="true"><app-icon name="sparkle" :size="46" :stroke="1.8" /></span>
        <span class="lh-deco lh-deco-note" aria-hidden="true"><app-icon name="note" :size="38" :stroke="1.8" /></span>
        <span class="lh-deco lh-deco-blob" aria-hidden="true"></span>

        <div class="lh-inner">
          <p class="lh-eyebrow"><app-icon name="sparkle" :size="14" /> {{ t('landing.hero.eyebrow') }}</p>
          <h1 class="lh-title">
            <span class="lh-line">{{ t('landing.hero.title1') }}</span>
            <i18n-t keypath="landing.hero.title2" tag="span" class="lh-line" scope="global">
              <template #hl><span class="lh-hl">{{ t('landing.hero.hl') }}</span></template>
            </i18n-t>
          </h1>
          <p class="lh-lede">{{ t('landing.hero.lede') }}</p>
        </div>

        <div class="lh-hint" aria-hidden="true">
          <span>{{ t('landing.hero.scrollHint') }}</span>
          <app-icon name="chevronU" :size="18" style="transform: rotate(180deg)" />
        </div>
      </section>

      <!-- ============ 區塊二：三大商品類型（pin 換頁） ============ -->
      <section class="l-types">
        <div class="lt-stage">
          <article
            v-for="(p, i) in TYPE_PANELS"
            :key="p.id"
            class="lt-panel"
            :style="{ '--accent': p.accent }"
          >
            <div class="lt-panel-inner">
              <div class="lt-text">
                <p class="lt-eyebrow"><app-icon name="sparkle" :size="13" /> {{ t('landing.types.eyebrow') }}</p>
                <p class="lt-num">{{ String(i + 1).padStart(2, '0') }} / {{ String(typeCount).padStart(2, '0') }}</p>
                <h2 class="lt-title">{{ t(`landing.types.${p.id}.title`) }}</h2>
                <p class="lt-tagline">{{ t(`landing.types.${p.id}.tagline`) }}</p>
                <p class="lt-desc">{{ t(`landing.types.${p.id}.desc`) }}</p>
                <ul class="lt-chips">
                  <li v-for="c in chipsFor(p.id)" :key="c">{{ c }}</li>
                </ul>
                <button type="button" class="lt-browse" @click="goMarket">
                  {{ t('landing.types.browse') }} <app-icon name="chevron" :size="15" />
                </button>
              </div>
              <div class="lt-art" aria-hidden="true">
                <div class="lt-art-tile"><app-icon :name="p.icon" :size="96" :stroke="1.5" /></div>
                <span class="lt-art-dot d1"></span>
                <span class="lt-art-dot d2"></span>
              </div>
            </div>
          </article>

          <!-- pin 期間的進度指示（僅動畫模式顯示） -->
          <div class="lt-progress" :style="{ '--accent': activeAccent }">
            <button
              v-for="(p, i) in TYPE_PANELS"
              :key="p.id"
              type="button"
              class="lt-dot"
              :class="{ on: activeType === i }"
              :aria-label="t('landing.types.pageAria', { n: i + 1 })"
              @click="goPage(i)"
            ></button>
            <span class="lt-progress-hint">{{ t('landing.types.hint') }}</span>
          </div>
        </div>
      </section>

      <!-- ============ 區塊四（待補充） ============ -->
      <!-- TODO: 區塊四內容規劃中，佔位樣式，確定文案後替換 -->
      <section class="l-plan ls-rv">
        <div class="lp-card">
          <span class="lp-badge">{{ t('landing.planned.badge') }}</span>
          <h2>{{ t('landing.planned.title') }}</h2>
          <p>{{ t('landing.planned.text') }}</p>
        </div>
      </section>

      <!-- ============ 區塊五（待補充） ============ -->
      <!-- TODO: 區塊五內容規劃中，佔位樣式，確定文案後替換 -->
      <section class="l-plan ls-rv">
        <div class="lp-card">
          <span class="lp-badge">{{ t('landing.planned.badge') }}</span>
          <h2>{{ t('landing.planned.title') }}</h2>
          <p>{{ t('landing.planned.text') }}</p>
        </div>
      </section>

      <!-- ============ 最後區塊：CTA ============ -->
      <section class="l-cta ls-rv">
        <p class="lc-eyebrow"><app-icon name="sparkle" :size="14" /> {{ t('landing.cta.eyebrow') }}</p>
        <div class="lc-grid">
          <div class="lc-card lc-creator">
            <span class="lc-ic"><app-icon name="bag" :size="24" /></span>
            <h3>{{ t('landing.cta.creator.title') }}</h3>
            <p>{{ t('landing.cta.creator.text') }}</p>
            <button type="button" class="lc-btn lc-btn-main" @click="goWorkspace">
              {{ t('landing.cta.creator.button') }} <app-icon name="chevron" :size="15" />
            </button>
          </div>
          <div class="lc-card lc-buyer">
            <span class="lc-ic"><app-icon name="search" :size="24" /></span>
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
.landing { padding: 0; }
.l-footwrap { padding: 0 clamp(20px, 3.5vw, 56px); }

/* ---------- 區塊一：hero ---------- */
.l-hero {
  position: relative;
  min-height: calc(100vh - var(--nav-h));
  display: flex; align-items: center; justify-content: center;
  padding: 0 clamp(20px, 3.5vw, 56px);
}
.lh-inner { max-width: 880px; text-align: center; padding: 40px 0 80px; }
.lh-eyebrow {
  display: inline-flex; align-items: center; gap: 7px; margin: 0 0 22px;
  font-family: var(--oj-mono); font-size: 13px; letter-spacing: 1.5px; text-transform: uppercase;
  color: var(--oj-primary); border: 1.5px solid var(--border-strong); border-radius: 999px;
  background: var(--surface); box-shadow: var(--pop-1); padding: 8px 16px;
}
.lh-title {
  margin: 0; font-family: var(--oj-display); font-weight: 800;
  font-size: clamp(38px, 6.4vw, 76px); line-height: 1.12; letter-spacing: -2px; color: var(--text);
}
.lh-line { display: block; }
.lh-hl {
  display: inline-block; padding: 0 10px; border-radius: 12px; transform: rotate(-1.5deg);
  background: var(--c-lime); border: 1.5px solid var(--text); box-shadow: var(--pop-1);
}
.lh-lede { max-width: 620px; margin: 26px auto 0; font-size: 17px; line-height: 1.9; color: var(--text-soft); }

.lh-hint {
  position: absolute; left: 50%; bottom: 30px; transform: translateX(-50%);
  display: flex; flex-direction: column; align-items: center; gap: 4px;
  font-family: var(--oj-mono); font-size: 12px; color: var(--text-faint);
}
.lh-hint :deep(svg) { animation: lh-bob 1.6s ease-in-out infinite; }
@keyframes lh-bob { 50% { translate: 0 6px; } }

.lh-deco { position: absolute; pointer-events: none; }
.lh-deco-ring {
  top: 16%; left: 8%; width: 74px; height: 74px; border-radius: 50%;
  border: 10px solid var(--c-yellow); box-shadow: var(--pop-1);
}
.lh-deco-star { top: 22%; right: 10%; color: var(--c-pink); }
.lh-deco-note { bottom: 24%; left: 13%; color: var(--c-cyan); }
.lh-deco-blob {
  bottom: 18%; right: 7%; width: 58px; height: 58px; border-radius: 40% 60% 55% 45%;
  background: var(--c-violet); border: 1.5px solid var(--text); box-shadow: var(--pop-2);
}

/* ---------- 區塊二：類型換頁 ---------- */
.l-types { margin: 0; }
.lt-stage { position: relative; }
.lt-panel {
  min-height: calc(100vh - var(--nav-h));
  display: flex; align-items: center; justify-content: center;
  padding: 48px clamp(20px, 3.5vw, 56px);
  background:
    radial-gradient(rgba(26,22,38,.05) 1.3px, transparent 1.5px),
    color-mix(in srgb, var(--accent) 9%, var(--bg));
  background-size: 22px 22px, auto;
  border-top: 1.5px solid var(--border);
}
/* GSAP 啟用時：面板絕對定位堆疊，供 timeline 換頁（預設一般文流退場） */
.lt-anim { height: calc(100vh - var(--nav-h)); overflow: hidden; }
.lt-anim .lt-panel { position: absolute; inset: 0; min-height: 0; will-change: transform; }

.lt-panel-inner {
  display: grid; grid-template-columns: minmax(0, 1.15fr) minmax(0, 1fr);
  align-items: center; gap: clamp(28px, 5vw, 80px);
  width: 100%; max-width: 1080px;
}
.lt-eyebrow {
  display: inline-flex; align-items: center; gap: 7px; margin: 0 0 18px;
  font-family: var(--oj-mono); font-size: 12px; letter-spacing: 1.5px; text-transform: uppercase;
  color: color-mix(in srgb, var(--accent) 78%, var(--text));
}
.lt-num { margin: 0 0 8px; font-family: var(--oj-mono); font-size: 14px; font-weight: 600; color: var(--text-faint); }
.lt-title {
  margin: 0; font-family: var(--oj-display); font-weight: 800;
  font-size: clamp(32px, 4.6vw, 54px); letter-spacing: -1.5px; color: var(--text);
}
.lt-tagline {
  display: inline-block; margin: 14px 0 0; padding: 6px 14px;
  font-family: var(--oj-display); font-weight: 700; font-size: 15px; color: var(--text);
  background: var(--surface); border: 1.5px solid var(--text); border-radius: 999px; box-shadow: var(--pop-1);
  transform: rotate(-1deg);
}
.lt-desc { margin: 18px 0 0; max-width: 460px; font-size: 15.5px; line-height: 1.85; color: var(--text-soft); }
.lt-chips { list-style: none; display: flex; flex-wrap: wrap; gap: 8px; margin: 18px 0 0; padding: 0; }
.lt-chips li {
  padding: 6px 12px; border: 1.5px solid var(--border-strong); border-radius: 999px;
  background: var(--surface); font-size: 13px; font-weight: 600; color: var(--text);
}
.lt-browse {
  display: inline-flex; align-items: center; gap: 7px; margin-top: 26px; cursor: pointer;
  font-family: var(--oj-display); font-weight: 700; font-size: 14.5px; color: var(--text);
  background: var(--surface); border: 1.5px solid var(--text); border-radius: var(--r-sm);
  box-shadow: var(--pop-2); padding: 10px 16px; transition: transform .12s, box-shadow .12s;
}
.lt-browse:hover { transform: translate(-1px, -1px); box-shadow: var(--pop-2-h); }
.lt-browse:active { transform: translate(2px, 2px); box-shadow: 1px 1px 0 var(--text); }

.lt-art { position: relative; display: grid; place-items: center; }
.lt-art-tile {
  width: min(320px, 34vw); aspect-ratio: 1; display: grid; place-items: center; color: #fff;
  background:
    radial-gradient(rgba(255,255,255,.35) 1.4px, transparent 1.7px),
    linear-gradient(140deg, color-mix(in srgb, var(--accent) 88%, #fff), var(--accent));
  background-size: 18px 18px, auto;
  border: 2px solid var(--text); border-radius: var(--r-lg); box-shadow: 8px 8px 0 var(--text);
  transform: rotate(2.5deg);
}
.lt-art-dot { position: absolute; border-radius: 50%; border: 1.5px solid var(--text); }
.lt-art-dot.d1 { top: 6%; left: 4%; width: 26px; height: 26px; background: var(--c-yellow); box-shadow: var(--pop-1); }
.lt-art-dot.d2 { bottom: 8%; right: 2%; width: 18px; height: 18px; background: var(--c-lime); box-shadow: var(--pop-1); }

.lt-progress {
  position: absolute; left: 50%; bottom: 26px; transform: translateX(-50%);
  display: none; align-items: center; gap: 10px;
}
.lt-anim .lt-progress { display: flex; }
.lt-dot {
  width: 12px; height: 12px; border-radius: 50%; cursor: pointer; padding: 0;
  border: 1.5px solid var(--text); background: var(--surface); transition: background .2s, transform .2s;
}
.lt-dot.on { background: var(--accent); transform: scale(1.25); }
.lt-progress-hint { margin-left: 6px; font-family: var(--oj-mono); font-size: 12px; color: var(--text-faint); }

/* ---------- 區塊四 / 五：待補充佔位 ---------- */
.l-plan { padding: 56px clamp(20px, 3.5vw, 56px); display: flex; justify-content: center; }
.lp-card {
  width: 100%; max-width: 820px; text-align: center; padding: 56px 32px;
  border: 2px dashed var(--border-strong); border-radius: var(--r-lg);
  background: color-mix(in srgb, var(--surface) 60%, transparent);
}
.lp-badge {
  display: inline-block; margin-bottom: 14px; padding: 6px 14px;
  font-family: var(--oj-mono); font-size: 12px; letter-spacing: 1.5px; text-transform: uppercase;
  color: var(--oj-primary); background: var(--oj-wash); border-radius: 999px;
}
.lp-card h2 { margin: 0; font-family: var(--oj-display); font-weight: 800; font-size: 26px; letter-spacing: -0.5px; color: var(--text); }
.lp-card p { margin: 10px 0 0; font-size: 14.5px; color: var(--text-soft); }

/* ---------- 最後區塊：CTA ---------- */
.l-cta { padding: 64px clamp(20px, 3.5vw, 56px) 96px; max-width: 1080px; margin: 0 auto; box-sizing: content-box; }
.lc-eyebrow {
  display: flex; align-items: center; justify-content: center; gap: 8px; margin: 0 0 28px;
  font-family: var(--oj-display); font-weight: 800; font-size: clamp(24px, 3.4vw, 38px);
  letter-spacing: -1px; color: var(--text); text-align: center;
}
.lc-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 22px; }
.lc-card {
  padding: 32px 30px; border: 1.5px solid var(--border-strong); border-radius: var(--r-lg);
  background: var(--surface); box-shadow: 6px 6px 0 var(--border);
}
.lc-creator { background: linear-gradient(150deg, var(--surface), color-mix(in srgb, var(--c-violet) 10%, var(--surface))); }
.lc-buyer { background: linear-gradient(150deg, var(--surface), color-mix(in srgb, var(--c-cyan) 10%, var(--surface))); }
.lc-ic {
  display: inline-grid; place-items: center; width: 46px; height: 46px; border-radius: 13px; color: #fff;
  border: 1.5px solid var(--text); box-shadow: var(--pop-1); margin-bottom: 16px;
  background: linear-gradient(135deg, var(--c-violet), var(--c-pink));
}
.lc-buyer .lc-ic { background: linear-gradient(135deg, var(--c-cyan), var(--c-violet)); }
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
@media (max-width: 860px) {
  .lt-panel-inner { grid-template-columns: 1fr; gap: 26px; }
  .lt-art { order: -1; }
  .lt-art-tile { width: min(200px, 46vw); box-shadow: 6px 6px 0 var(--text); }
  .lt-desc { max-width: none; }
  .lc-grid { grid-template-columns: 1fr; }
}
@media (max-width: 560px) {
  .lh-deco { display: none; }
  .lt-panel { padding: 32px 20px 84px; }
}

/* reduced-motion：無換頁動畫，面板一般直向排列（.lt-anim 不會被加上） */
@media (prefers-reduced-motion: reduce) {
  .lh-hint :deep(svg) { animation: none; }
}
</style>
