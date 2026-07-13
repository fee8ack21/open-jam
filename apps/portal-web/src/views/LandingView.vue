<script setup lang="ts">
/* ============================================================
   LandingView — 品牌敘事 Landing（/landing）
   固定舞台 + 捲動推進的六場景敘事（依「Open Jam Landing v3」設計稿）：
   0 入口（紫）：漂浮商品卡，捲動時鏡頭放大「穿入」音樂卡
   1 音樂（粉）：可試彈的鍵盤（WebAudio 真的有聲音）+ Demo 旋律
   2 攝影（黃）：點擊相紙「顯影」
   3 電子書（綠）：翻頁試讀
   4 規則（奶油）：三條市集規則卡
   5 市集（墨黑）：散落卡片收合成網格 + 雙 CTA + 頁尾列
   prefers-reduced-motion: reduce 時退化為一般直向排列（無鏡頭
   轉場、無視差），互動（鋼琴 / 顯影 / 翻頁）仍可用。
   ============================================================ */
import { ref, computed, onMounted, onBeforeUnmount } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { useShopStore } from '@/stores/shop.js';
import { useAuthStore } from '@/stores/auth.js';
import BrandLogo from '@/components/BrandLogo.vue';

const store = useShopStore();
const auth = useAuthStore();
const router = useRouter();
const { t, tm, rt } = useI18n();

/** 每一景占用的捲動距離（vh 倍數），與設計稿一致 */
const STEP = 1.4;
const SCENES = 6;

const anim = ref(false); // 允許動效時才啟用固定舞台
const activeDot = ref(0);

// ---- 章節文案 ----
const chapters = computed(() => (tm('landingPage.chapters') as string[]).map((c) => rt(c)));

// ---- 場景 0：漂浮商品卡（文案取 i18n，位置 / 色票留在程式碼） ----
const HERO_CARDS = [
  { id: 'music', cls: 'hc-music', icon: 'note' },
  { id: 'photo', cls: 'hc-photo', icon: 'image' },
  { id: 'ebook', cls: 'hc-ebook', icon: 'book' },
  { id: 'beats', cls: 'hc-beats', icon: 'beats' },
] as const;

// ---- 場景 1：鋼琴（WebAudio） ----
const PIANO = [
  { f: 261.6, label: 'C', bg: '#ffffff' },
  { f: 293.7, label: 'D', bg: 'var(--c-pink)' },
  { f: 329.6, label: 'E', bg: '#ffffff' },
  { f: 392.0, label: 'G', bg: 'var(--c-lime)' },
  { f: 440.0, label: 'A', bg: '#ffffff' },
  { f: 523.3, label: 'C', bg: 'var(--c-cyan)' },
  { f: 587.3, label: 'D', bg: '#ffffff' },
  { f: 659.3, label: 'E', bg: 'var(--c-yellow)' },
];
let audioCtx: AudioContext | undefined;
function audio(): AudioContext {
  audioCtx ??= new AudioContext();
  if (audioCtx.state === 'suspended') void audioCtx.resume();
  return audioCtx;
}
function note(freq: number, at = 0, dur = 0.45) {
  const ctx = audio();
  const start = ctx.currentTime + at;
  const osc = ctx.createOscillator();
  const gain = ctx.createGain();
  osc.type = 'triangle';
  osc.frequency.value = freq;
  gain.gain.setValueAtTime(0.0001, start);
  gain.gain.exponentialRampToValueAtTime(0.28, start + 0.02);
  gain.gain.exponentialRampToValueAtTime(0.0001, start + dur);
  osc.connect(gain).connect(ctx.destination);
  osc.start(start);
  osc.stop(start + dur + 0.05);
}
function playMelody() {
  [0, 2, 4, 5, 4, 2, 7, 5].forEach((idx, i) => note(PIANO[idx].f, i * 0.22, 0.5));
}

// ---- 場景 2：相紙顯影 ----
const PHOTO_TILES = [
  { bg: '#ff6b35', glyph: '🍁', rot: -2 },
  { bg: '#3d2b8e', glyph: '🌃', rot: 1.5 },
  { bg: '#ffb7d5', glyph: '🌸', rot: -1 },
];
const photosDev = ref([false, false, false]);
const photoItems = computed(() =>
  (tm('landingPage.photo.photos') as { title: string; author: string; price: string }[]).map((p, i) => ({
    ...PHOTO_TILES[i],
    title: rt(p.title),
    author: rt(p.author),
    price: rt(p.price),
  })),
);
function toggleDev(i: number) {
  photosDev.value = photosDev.value.map((v, j) => (j === i ? !v : v));
}

// ---- 場景 3：電子書翻頁 ----
const page = ref(0);
const bookPages = computed(() =>
  (tm('landingPage.ebook.pages') as { title: string; text: string }[]).map((p) => ({ title: rt(p.title), text: rt(p.text) })),
);
function nextPage() { page.value = (page.value + 1) % bookPages.value.length; }
function prevPage() { page.value = (page.value + bookPages.value.length - 1) % bookPages.value.length; }

// ---- 場景 4：規則 ----
const RULE_COLORS = ['var(--c-pink)', 'var(--c-yellow)', 'var(--c-lime)'];
const rules = computed(() =>
  (tm('landingPage.rules.items') as { title: string; text: string }[]).map((r, i) => ({
    color: RULE_COLORS[i],
    title: rt(r.title),
    text: rt(r.text),
  })),
);

// ---- 場景 5：收合網格 ----
const GRID_ICONS = ['note', 'image', 'book'];
const gridCards = computed(() =>
  (tm('landingPage.final.gridCards') as { tag: string; title: string; price: string }[]).map((g, i) => ({
    icon: GRID_ICONS[i],
    cls: 'lg-' + (i + 1),
    tag: rt(g.tag),
    title: rt(g.title),
    price: rt(g.price),
  })),
);

function goMarket() { router.push('/'); }
function goLogin() { auth.login(); }

// ---- 捲動 / 滑鼠驅動（全部直接寫 DOM style，不走 Vue 反應式以保 60fps） ----
const stageEl = ref<HTMLElement | null>(null);
const sceneEls = ref<HTMLElement[]>([]);
function setSceneRef(el: unknown, i: number) {
  if (el) sceneEls.value[i] = el as HTMLElement;
}

let raf = 0;
let tmx = 0, tmy = 0, cmx = 0, cmy = 0;
let lastP = -1;
const clamp = (v: number, a: number, b: number) => Math.max(a, Math.min(b, v));
const easeS = (k: number) => k * k * (3 - 2 * k);

function onPointer(e: PointerEvent) {
  tmx = (e.clientX / window.innerWidth) * 2 - 1;
  tmy = (e.clientY / window.innerHeight) * 2 - 1;
}

function frame() {
  const vh = window.innerHeight || 800;
  const p = clamp(window.scrollY / (vh * STEP), 0, SCENES - 1);
  cmx += (tmx - cmx) * 0.08;
  cmy += (tmy - cmy) * 0.08;

  const stage = stageEl.value;
  if (stage) {
    for (let i = 0; i < SCENES; i++) {
      const el = sceneEls.value[i];
      if (!el) continue;
      const rel = p - i;
      if (i === 0) {
        // 鏡頭放大穿入音樂卡（origin 對準左上音樂卡）
        const k = clamp(rel, 0, 1);
        el.style.transform = `scale(${1 + 1.1 * easeS(k)})`;
        el.style.filter = `brightness(${1 - 0.3 * k})`;
        el.style.opacity = String(1 - Math.max(0, k - 0.72) / 0.28);
      } else if (rel < 0) {
        // 進場：由下方升起 + 微微轉正
        el.style.transform = `translateY(${-rel * 100}vh) rotate(${rel * 1.6}deg)`;
        el.style.filter = 'none';
        el.style.opacity = '1';
      } else {
        // 退場：鏡頭穿過（放大 + 變暗 + 模糊）
        const k = clamp(rel, 0, 1);
        el.style.transform = `scale(${1 + 0.12 * easeS(k)})`;
        el.style.filter = `brightness(${1 - 0.5 * k}) blur(${(k * 5).toFixed(1)}px)`;
        el.style.opacity = '1';
      }
      // 場景內視差（--pa 淺 / --pb 深，CSS 端取用）
      const local = clamp(rel, -1, 1);
      el.style.setProperty('--pa', `${Math.round(local * -46)}px`);
      el.style.setProperty('--pb', `${Math.round(local * -90)}px`);
    }

    // 場景 0：hero 卡片飛行 + 滑鼠傾斜
    const k0 = easeS(clamp(p, 0, 1));
    const tilt = (d: number) =>
      `rotateY(${(cmx * 5 * d).toFixed(2)}deg) rotateX(${(-cmy * 5 * d).toFixed(2)}deg) translate(${(cmx * 14 * d).toFixed(1)}px, ${(cmy * 14 * d).toFixed(1)}px)`;
    const s0 = sceneEls.value[0];
    if (s0) {
      const q = (sel: string) => s0.querySelector<HTMLElement>(sel);
      const music = q('.hc-music');
      if (music) {
        music.style.transform = `translate(${(k0 * 34).toFixed(2)}vw, ${(k0 * 22).toFixed(2)}vh) scale(${(1 + k0 * 2.4).toFixed(3)}) rotate(${(-6 + k0 * 6).toFixed(2)}deg) ${tilt(1.2)}`;
        music.style.opacity = String(1 - Math.max(0, k0 - 0.6) / 0.4);
      }
      const sideOp = String(1 - Math.max(0, k0 - 0.45) / 0.4);
      const photo = q('.hc-photo');
      if (photo) { photo.style.transform = `translate(${(k0 * 22).toFixed(2)}vw, ${(-k0 * 26).toFixed(2)}vh) scale(${(1 - k0 * 0.35).toFixed(3)}) rotate(${(5 + k0 * 10).toFixed(2)}deg) ${tilt(0.8)}`; photo.style.opacity = sideOp; }
      const ebook = q('.hc-ebook');
      if (ebook) { ebook.style.transform = `translate(${(-k0 * 24).toFixed(2)}vw, ${(k0 * 24).toFixed(2)}vh) scale(${(1 - k0 * 0.35).toFixed(3)}) rotate(${(4 - k0 * 10).toFixed(2)}deg) ${tilt(1.0)}`; ebook.style.opacity = sideOp; }
      const beats = q('.hc-beats');
      if (beats) { beats.style.transform = `translate(${(k0 * 26).toFixed(2)}vw, ${(k0 * 24).toFixed(2)}vh) scale(${(1 - k0 * 0.35).toFixed(3)}) rotate(${(-4 - k0 * 8).toFixed(2)}deg) ${tilt(0.7)}`; beats.style.opacity = sideOp; }
      const sticker = q('.ld-sticker');
      if (sticker) sticker.style.transform = `translate(${(cmx * 10).toFixed(1)}px, ${(cmy * 10 - k0 * 140).toFixed(1)}px) scale(${(1 - k0 * 0.4).toFixed(3)})`;
      const copy = q('.ld-copy');
      if (copy) {
        copy.style.transform = `translateY(${(-k0 * 26).toFixed(1)}vh) scale(${(1 - k0 * 0.15).toFixed(3)})`;
        copy.style.opacity = String(clamp(1 - k0 * 1.6, 0, 1));
      }
    }

    // 場景 5：散落卡片收合
    const s5 = sceneEls.value[5];
    if (s5) s5.style.setProperty('--inv', String(1 - easeS(clamp((p - 4) * 1.25, 0, 1))));
  }

  if (Math.abs(p - lastP) > 0.001) {
    lastP = p;
    activeDot.value = Math.round(p);
  }
  raf = requestAnimationFrame(frame);
}

function goTo(i: number) {
  window.scrollTo({ top: i * (window.innerHeight || 800) * STEP, behavior: 'smooth' });
}

onMounted(() => {
  if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;
  anim.value = true;
  window.addEventListener('pointermove', onPointer, { passive: true });
  raf = requestAnimationFrame(frame);
});
onBeforeUnmount(() => {
  cancelAnimationFrame(raf);
  window.removeEventListener('pointermove', onPointer);
  void audioCtx?.close();
});
</script>

<template>
  <div class="oj-root ld-root" :class="[{ 'ld-anim': anim }, 'font-' + store.font]" :data-screen-label="t('landingPage.screenLabel')">
    <!-- 捲動空間（僅動畫模式需要） -->
    <div v-if="anim" class="ld-spacer" aria-hidden="true"></div>

    <!-- 漂浮 header：logo + 登入 -->
    <header class="ld-head">
      <span class="ld-head-brand"><BrandLogo /></span>
      <button type="button" class="ld-login" @click="goLogin">
        {{ t('nav.login') }} <app-icon name="arrow" :size="13" />
      </button>
    </header>

    <!-- 章節導覽點 -->
    <nav v-if="anim" class="ld-dots" :aria-label="t('landingPage.screenLabel')">
      <button
        v-for="(c, i) in chapters"
        :key="c"
        type="button"
        class="ld-dot"
        :class="{ on: activeDot === i }"
        :title="c"
        :aria-label="c"
        @click="goTo(i)"
      ></button>
    </nav>

    <!-- 固定舞台 -->
    <div ref="stageEl" class="ld-stage">
      <!-- ============ 場景 0：入口（紫） ============ -->
      <section :ref="(el) => setSceneRef(el, 0)" class="ld-scene ld-s0">
        <div
          v-for="c in HERO_CARDS"
          :key="c.id"
          class="ld-hc"
          :class="c.cls"
        >
          <span class="ld-hc-tag">{{ t(`landingPage.cards.${c.id}.tag`) }}</span>
          <span class="ld-hc-glyph"><app-icon :name="c.icon" :size="26" /></span>
          <span class="ld-hc-title">{{ t(`landingPage.cards.${c.id}.title`) }}</span>
          <span class="ld-hc-foot">
            <b>{{ t(`landingPage.cards.${c.id}.price`) }}</b>
            <i><app-icon name="star" :size="11" /> {{ t(`landingPage.cards.${c.id}.rating`) }}</i>
          </span>
        </div>

        <!-- 100% 獨立創作貼紙 -->
        <div class="ld-sticker m-hide" aria-hidden="true">
          <svg width="104" height="104" viewBox="0 0 104 104" class="ld-sticker-spin">
            <path d="M52 3 l8 10 12 -5 3 13 13 1 -3 13 12 6 -8 10 8 10 -12 6 3 13 -13 1 -3 13 -12 -5 -8 10 -8 -10 -12 5 -3 -13 -13 -1 3 -13 -12 -6 8 -10 -8 -10 12 -6 -3 -13 13 -1 3 -13 12 5 z" fill="#FFDE00" stroke="#1A1A1A" stroke-width="2.5" />
            <text x="52" y="47" text-anchor="middle" font-family="'Noto Sans TC',sans-serif" font-weight="900" font-size="15" fill="#1A1A1A">{{ t('landingPage.hero.sticker1') }}</text>
            <text x="52" y="66" text-anchor="middle" font-family="'Noto Sans TC',sans-serif" font-weight="900" font-size="12" fill="#1A1A1A">{{ t('landingPage.hero.sticker2') }}</text>
          </svg>
        </div>

        <div class="ld-copy">
          <p class="ld-badge"><app-icon name="note" :size="13" /> {{ t('landingPage.hero.badge') }}</p>
          <i18n-t keypath="landingPage.hero.title" tag="h1" class="ld-h1" scope="global">
            <template #play><span class="ld-hl">{{ t('landingPage.hero.titleHl') }}</span></template>
          </i18n-t>
          <p class="ld-lede">{{ t('landingPage.hero.lede') }}</p>
          <div class="ld-hint" aria-hidden="true">
            <span class="ld-hint-pill">{{ t('landingPage.hero.hint') }}</span>
            <app-icon name="arrowD" :size="18" style="color: #fff" />
          </div>
        </div>
      </section>

      <!-- ============ 場景 1：音樂（粉） ============ -->
      <section :ref="(el) => setSceneRef(el, 1)" class="ld-scene ld-s1">
        <span class="ld-pa ld-vinyl m-hide" aria-hidden="true">
          <svg width="88" height="88" viewBox="0 0 88 88" class="ld-sticker-spin fast">
            <circle cx="44" cy="44" r="40" fill="#1A1A1A" />
            <circle cx="44" cy="44" r="30" fill="none" stroke="#444" stroke-width="1.5" />
            <circle cx="44" cy="44" r="22" fill="none" stroke="#444" stroke-width="1.5" />
            <circle cx="44" cy="44" r="13" fill="#FFDE00" stroke="#1A1A1A" stroke-width="2" />
            <circle cx="44" cy="44" r="3" fill="#1A1A1A" />
          </svg>
        </span>
        <div class="ld-cols">
          <div class="ld-pa">
            <p class="ld-eyebrow">{{ t('landingPage.music.eyebrow') }}</p>
            <i18n-t keypath="landingPage.music.title" tag="h2" class="ld-h2" scope="global">
              <template #mark><span class="ld-mark">{{ t('landingPage.music.titleMark') }}</span></template>
            </i18n-t>
            <p class="ld-p">{{ t('landingPage.music.desc') }}</p>
            <div class="ld-eq m-hide" aria-hidden="true">
              <span v-for="i in 7" :key="i" :class="{ lite: i === 3 || i === 5 }" :style="{ animationDelay: (i - 1) * 0.12 + 's' }"></span>
            </div>
            <router-link class="ld-btn ld-btn-ink" to="/">{{ t('landingPage.music.browse') }} <app-icon name="arrow" :size="13" /></router-link>
          </div>
          <div class="ld-pb">
            <div class="ld-panel ld-piano-panel">
              <div class="ld-panel-head">
                <span class="ld-panel-title">{{ t('landingPage.music.tryLabel') }}</span>
                <button type="button" class="ld-demo" @click="playMelody">
                  <app-icon name="note" :size="12" /> {{ t('landingPage.music.demoBtn') }}
                </button>
              </div>
              <div class="ld-piano">
                <button
                  v-for="(k, i) in PIANO"
                  :key="i"
                  type="button"
                  :style="{ background: k.bg }"
                  @pointerdown="note(k.f)"
                >{{ k.label }}</button>
              </div>
              <div class="ld-track">
                <span class="ld-track-ic"><app-icon name="note" :size="16" /></span>
                <span class="ld-track-meta">
                  <b>{{ t('landingPage.music.trackTitle') }}</b>
                  <i>{{ t('landingPage.music.trackMeta') }}</i>
                </span>
                <span class="ld-track-price">{{ t('landingPage.music.trackPrice') }}</span>
              </div>
            </div>
            <div class="ld-note m-hide">{{ t('landingPage.music.note') }}</div>
          </div>
        </div>
      </section>

      <!-- ============ 場景 2：攝影（黃） ============ -->
      <section :ref="(el) => setSceneRef(el, 2)" class="ld-scene ld-s2">
        <div class="ld-center">
          <div class="ld-pa ld-center-head">
            <p class="ld-eyebrow">{{ t('landingPage.photo.eyebrow') }}</p>
            <i18n-t keypath="landingPage.photo.title" tag="h2" class="ld-h2" scope="global">
              <template #mark><span class="ld-mark">{{ t('landingPage.photo.titleMark') }}</span></template>
            </i18n-t>
            <p class="ld-p">{{ t('landingPage.photo.desc') }}</p>
          </div>
          <div class="ld-pb ld-photos">
            <button
              v-for="(ph, i) in photoItems"
              :key="i"
              type="button"
              class="ld-photo"
              :style="{ '--rot': ph.rot + 'deg' }"
              :aria-pressed="photosDev[i]"
              @click="toggleDev(i)"
            >
              <span class="ld-photo-img" :style="{ background: ph.bg }">
                <span class="ld-photo-glyph" :class="{ dev: photosDev[i] }">{{ ph.glyph }}</span>
                <span class="ld-photo-veil" :class="{ dev: photosDev[i] }"><i>{{ t('landingPage.photo.hint') }}</i></span>
              </span>
              <span class="ld-photo-title">{{ ph.title }}</span>
              <span class="ld-photo-foot m-hide"><span>{{ ph.author }}</span><b>{{ ph.price }}</b></span>
            </button>
          </div>
        </div>
      </section>

      <!-- ============ 場景 3：電子書（綠） ============ -->
      <section :ref="(el) => setSceneRef(el, 3)" class="ld-scene ld-s3">
        <div class="ld-cols ld-cols-rev">
          <div class="ld-pb">
            <div class="ld-panel ld-book">
              <div class="ld-book-head">
                <span class="ld-panel-title"><app-icon name="book" :size="15" /> {{ t('landingPage.ebook.bookLabel') }}</span>
                <span class="ld-book-no">{{ page + 1 }} / {{ bookPages.length }}</span>
              </div>
              <div class="ld-book-page">
                <h3>{{ bookPages[page]?.title }}</h3>
                <p>{{ bookPages[page]?.text }}</p>
              </div>
              <div class="ld-book-nav">
                <button type="button" class="ld-book-prev" @click="prevPage">{{ t('landingPage.ebook.prev') }}</button>
                <button type="button" class="ld-book-next" @click="nextPage">{{ t('landingPage.ebook.next') }}</button>
              </div>
            </div>
            <div class="ld-note m-hide">{{ t('landingPage.ebook.note') }}</div>
          </div>
          <div class="ld-pa">
            <p class="ld-eyebrow">{{ t('landingPage.ebook.eyebrow') }}</p>
            <i18n-t keypath="landingPage.ebook.title" tag="h2" class="ld-h2" scope="global">
              <template #mark><span class="ld-mark">{{ t('landingPage.ebook.titleMark') }}</span></template>
            </i18n-t>
            <p class="ld-p m-hide">{{ t('landingPage.ebook.desc') }}</p>
            <router-link class="ld-btn ld-btn-ink" to="/">{{ t('landingPage.ebook.browse') }} <app-icon name="arrow" :size="13" /></router-link>
          </div>
        </div>
      </section>

      <!-- ============ 場景 4：市集規則（奶油） ============ -->
      <section :ref="(el) => setSceneRef(el, 4)" class="ld-scene ld-s4">
        <span class="ld-pa ld-rule-sticker m-hide" aria-hidden="true">
          <svg width="96" height="96" viewBox="0 0 104 104">
            <path d="M52 3 l8 10 12 -5 3 13 13 1 -3 13 12 6 -8 10 8 10 -12 6 3 13 -13 1 -3 13 -12 -5 -8 10 -8 -10 -12 5 -3 -13 -13 -1 3 -13 -12 -6 8 -10 -8 -10 12 -6 -3 -13 13 -1 3 -13 12 5 z" fill="#FF90E8" stroke="#1A1A1A" stroke-width="2.5" />
            <text x="52" y="48" text-anchor="middle" font-family="'Noto Sans TC',sans-serif" font-weight="900" font-size="14" fill="#1A1A1A">{{ t('landingPage.rules.sticker1') }}</text>
            <text x="52" y="66" text-anchor="middle" font-family="'Noto Sans TC',sans-serif" font-weight="900" font-size="14" fill="#1A1A1A">{{ t('landingPage.rules.sticker2') }}</text>
          </svg>
        </span>
        <div class="ld-center">
          <h2 class="ld-h2 ld-pa" style="text-align: center">{{ t('landingPage.rules.title') }}</h2>
          <p class="ld-rules-sub ld-pa">{{ t('landingPage.rules.sub') }}</p>
          <div class="ld-rules ld-pb">
            <div v-for="(r, i) in rules" :key="r.title" class="ld-rule">
              <span class="ld-rule-num" :style="{ background: r.color, transform: `rotate(${[-4, 3, -3][i]}deg)` }">{{ i + 1 }}</span>
              <h3>{{ r.title }}</h3>
              <p>{{ r.text }}</p>
            </div>
          </div>
        </div>
      </section>

      <!-- ============ 場景 5：市集（墨黑） ============ -->
      <section :ref="(el) => setSceneRef(el, 5)" class="ld-scene ld-s5">
        <div class="ld-pa" style="text-align: center; padding: 0 32px">
          <i18n-t keypath="landingPage.final.title" tag="h2" class="ld-h2 ld-h2-dark" scope="global">
            <template #mark><span class="ld-mark-yellow">{{ t('landingPage.final.titleMark') }}</span></template>
          </i18n-t>
          <p class="ld-p ld-p-dark">{{ t('landingPage.final.sub') }}</p>
        </div>
        <div class="ld-grid5">
          <div v-for="g in gridCards" :key="g.title" class="ld-gcard" :class="g.cls">
            <span class="ld-gtag">{{ g.tag }}</span>
            <span class="ld-gicon"><app-icon :name="g.icon" :size="22" /></span>
            <span class="ld-gtitle">{{ g.title }}</span>
            <span class="ld-gprice">{{ g.price }}</span>
          </div>
        </div>
        <div class="ld-ctarow ld-pb">
          <button type="button" class="ld-btn ld-btn-yellow" @click="goLogin">
            <app-icon name="note" :size="14" /> {{ t('landingPage.final.ctaJam') }}
          </button>
          <button type="button" class="ld-btn ld-btn-ghost" @click="goMarket">
            {{ t('landingPage.final.ctaMarket') }} <app-icon name="arrow" :size="14" />
          </button>
        </div>
        <div class="ld-note ld-note-lime">{{ t('landingPage.final.note') }}</div>
        <div class="ld-footerbar">
          <span class="ld-foot-brand"><BrandLogo inverse /></span>
          <div class="ld-foot-links m-hide">
            <router-link to="/about">{{ t('footer.about') }}</router-link>
            <router-link to="/faq">{{ t('footer.faq') }}</router-link>
            <router-link to="/privacy">{{ t('footer.privacy') }}</router-link>
          </div>
          <span class="ld-foot-copy">{{ t('footer.copyright') }}</span>
        </div>
      </section>
    </div>
  </div>
</template>

<style scoped>
.ld-root { background: #1a1a1a; }
.ld-spacer { height: 800vh; }

/* ---------- 漂浮 header ---------- */
.ld-head {
  position: fixed; top: 0; left: 0; right: 0; z-index: 100;
  display: flex; align-items: center; justify-content: space-between;
  padding: 16px 32px; pointer-events: none;
}
.ld-head-brand { pointer-events: auto; display: inline-flex; }
.ld-head-brand :deep(.brand-name) {
  background: var(--bg); border: 2px solid var(--border-strong); border-radius: 999px;
  box-shadow: 0 4px 10px rgba(26, 26, 26, 0.18); padding: 5px 16px; font-size: 20px;
}
.ld-login {
  pointer-events: auto; display: inline-flex; align-items: center; gap: 7px; cursor: pointer;
  font-family: var(--oj-font); font-weight: 900; font-size: 14px; color: var(--c-yellow);
  background: var(--text); border: 0; border-radius: 999px; padding: 9px 22px;
  box-shadow: 0 4px 12px rgba(26, 26, 26, 0.3);
  transition: transform .2s var(--ease-pop), box-shadow .2s;
}
.ld-login:hover { transform: translateY(-2px) rotate(-1deg); box-shadow: 0 8px 18px rgba(26, 26, 26, 0.35); }

/* ---------- 章節導覽點 ---------- */
.ld-dots {
  position: fixed; right: 24px; top: 50%; transform: translateY(-50%); z-index: 100;
  display: flex; flex-direction: column; gap: 10px;
  background: #fff; border: 2px solid var(--border-strong); border-radius: 999px;
  box-shadow: 0 8px 20px rgba(26, 26, 26, 0.18); padding: 12px 8px;
}
.ld-dot {
  width: 22px; height: 22px; border: 2px solid var(--border-strong); border-radius: 999px;
  cursor: pointer; background: #fff; padding: 0; transition: transform .15s, background .15s;
}
.ld-dot:hover { transform: scale(1.2); }
.ld-dot.on { background: var(--c-yellow); }

/* ---------- 舞台與場景 ---------- */
/* 動畫模式：固定舞台、場景絕對堆疊；否則場景照文流直排 */
.ld-anim .ld-stage { position: fixed; inset: 0; overflow: hidden; perspective: 1200px; }
.ld-scene {
  position: relative; min-height: 100vh;
  display: flex; flex-direction: column; align-items: center; justify-content: center;
  will-change: transform;
}
.ld-anim .ld-scene { position: absolute; inset: 0; min-height: 0; }
.ld-s0 { z-index: 1; transform-origin: 22% 34%; }
.ld-s1 { z-index: 2; }
.ld-s2 { z-index: 3; }
.ld-s3 { z-index: 4; }
.ld-s4 { z-index: 5; }
.ld-s5 { z-index: 6; }
.ld-anim .ld-scene + .ld-scene { border-top: 2px solid var(--border-strong); border-radius: 28px 28px 0 0; }

.ld-s0 {
  background: #8a5cf6;
  background-image: radial-gradient(rgba(255, 255, 255, 0.18) 1.5px, transparent 1.5px);
  background-size: 26px 26px;
}
.ld-s1 {
  background: var(--c-pink);
  background-image: radial-gradient(rgba(26, 26, 26, 0.12) 1.5px, transparent 1.5px);
  background-size: 26px 26px;
}
.ld-s2 {
  background: var(--c-yellow);
  background-image: radial-gradient(rgba(26, 26, 26, 0.08) 1.5px, transparent 1.5px);
  background-size: 26px 26px;
}
.ld-s3 {
  background: var(--c-lime);
  background-image: radial-gradient(rgba(26, 26, 26, 0.1) 1.5px, transparent 1.5px);
  background-size: 26px 26px;
}
.ld-s4 {
  background: var(--bg);
  background-image: radial-gradient(rgba(26, 26, 26, 0.06) 1.5px, transparent 1.5px);
  background-size: 24px 24px;
}
.ld-s5 { background: #1a1a1a; gap: 26px; padding-bottom: 80px; }

/* 場景內視差層（--pa / --pb 由 rAF 寫入） */
.ld-anim .ld-pa { transform: translateY(var(--pa, 0)); }
.ld-anim .ld-pb { transform: translateY(var(--pb, 0)); }

/* ---------- 場景 0 ---------- */
.ld-hc {
  position: absolute; width: min(180px, 13vw); padding: 14px;
  background: var(--hc-bg, var(--c-pink)); border: 2px solid var(--border-strong); border-radius: 16px;
  box-shadow: 0 10px 24px rgba(26, 26, 26, 0.25); will-change: transform;
}
.ld-hc::before {
  content: ''; position: absolute; top: -11px; left: 50%; width: 56px; height: 18px;
  margin-left: -28px; background: var(--hc-tape, rgba(255, 222, 0, 0.85)); border-radius: 3px;
  transform: rotate(-4deg); box-shadow: 0 1px 3px rgba(26, 26, 26, 0.15);
}
.hc-music { left: 4%; top: 15%; z-index: 6; --hc-bg: var(--c-pink); transform: rotate(-6deg); }
.hc-photo { right: 3%; top: 17%; --hc-bg: var(--c-yellow); --hc-tape: rgba(255, 144, 232, 0.85); transform: rotate(5deg); }
.hc-ebook { left: 5%; bottom: 11%; --hc-bg: var(--c-lime); --hc-tape: rgba(125, 217, 255, 0.85); transform: rotate(4deg); }
.hc-beats { right: 4%; bottom: 10%; --hc-bg: var(--c-cyan); --hc-tape: rgba(184, 255, 159, 0.85); transform: rotate(-4deg); }
.ld-hc-tag {
  display: inline-block; font-size: 11px; font-weight: 900; color: #fff;
  background: var(--text); border-radius: 999px; padding: 2px 10px; white-space: nowrap;
}
.ld-hc-glyph { display: block; margin: 8px 0 4px; color: var(--text); }
.ld-hc-title { display: block; font-weight: 900; font-size: 14px; color: var(--text); }
.ld-hc-foot { display: flex; justify-content: space-between; align-items: center; margin-top: 6px; }
.ld-hc-foot b { font-family: var(--oj-display); font-weight: 700; font-size: 13px; color: var(--text); }
.ld-hc-foot i { font-style: normal; font-family: var(--oj-display); font-weight: 700; font-size: 12px; color: var(--text); display: inline-flex; align-items: center; gap: 3px; }

.ld-sticker { position: absolute; left: 16%; top: 56%; z-index: 4; will-change: transform; }
.ld-sticker-spin { animation: ld-spin 24s linear infinite; filter: drop-shadow(0 6px 12px rgba(26, 26, 26, 0.25)); }
.ld-sticker-spin.fast { animation-duration: 14s; }
@keyframes ld-spin { to { transform: rotate(360deg); } }

.ld-copy { text-align: center; max-width: 720px; padding: 0 32px; position: relative; z-index: 5; will-change: transform; }
.ld-badge {
  display: inline-flex; align-items: center; gap: 8px; margin: 0; padding: 7px 18px;
  font-size: 13px; font-weight: 900; letter-spacing: 2px;
  color: var(--c-yellow); background: var(--text); border-radius: 999px;
  transform: rotate(-1deg); white-space: nowrap;
}
.ld-h1 { font-size: clamp(38px, 5.6vw, 64px); font-weight: 900; line-height: 1.3; color: #fff; margin: 28px 0 14px; }
.ld-hl {
  display: inline-block; background: var(--c-yellow); color: var(--text);
  border: 2px solid var(--border-strong); border-radius: 16px; padding: 0 16px;
  box-shadow: var(--ink-drop); transform: rotate(-1.5deg);
}
.ld-lede { color: #fff; font-weight: 700; font-size: 17px; line-height: 1.8; margin: 0 0 30px; white-space: pre-line; }
.ld-hint { display: inline-flex; flex-direction: column; align-items: center; gap: 6px; animation: ld-hint 1.6s ease-in-out infinite; }
.ld-hint-pill {
  background: #fff; border: 2px solid var(--border-strong); border-radius: 999px;
  box-shadow: 0 6px 14px rgba(26, 26, 26, 0.25);
  font-weight: 900; font-size: 14px; padding: 9px 20px; white-space: nowrap; color: var(--text);
}
@keyframes ld-hint { 0%, 100% { transform: translateY(0); } 50% { transform: translateY(8px); } }

/* ---------- 場景共用 ---------- */
.ld-cols {
  display: grid; grid-template-columns: 1fr 1.1fr; gap: 48px; align-items: center;
  max-width: 1060px; width: 100%; padding: 0 32px;
}
.ld-cols-rev { grid-template-columns: 1.1fr 1fr; }
.ld-center { max-width: 1060px; width: 100%; padding: 0 32px; }
.ld-center-head { text-align: center; margin-bottom: 36px; }
.ld-eyebrow {
  display: inline-block; margin: 0; padding: 6px 16px;
  font-size: 12px; font-weight: 900; letter-spacing: 2px;
  color: #fff; background: var(--text); border-radius: 999px;
  transform: rotate(-1.5deg); white-space: nowrap;
}
.ld-h2 { font-size: clamp(26px, 3.8vw, 44px); font-weight: 900; line-height: 1.35; margin: 20px 0 16px; color: var(--text); }
.ld-mark {
  display: inline-block; background: #fff; border: 2px solid var(--border-strong);
  border-radius: 12px; padding: 0 12px; box-shadow: var(--ink-drop-sm); transform: rotate(-1deg);
}
.ld-p { font-weight: 700; font-size: 16px; line-height: 1.8; margin: 0 0 20px; color: var(--text); }
.ld-btn {
  display: inline-flex; align-items: center; gap: 7px; cursor: pointer; text-decoration: none;
  font-family: var(--oj-font); font-weight: 900; font-size: 15px;
  border: 0; border-radius: 999px; padding: 14px 30px;
  transition: transform .2s var(--ease-pop), box-shadow .2s;
}
.ld-btn:hover { transform: translateY(-3px) rotate(-1deg); box-shadow: 0 10px 22px rgba(26, 26, 26, 0.3); }
.ld-btn-ink { background: var(--text); color: var(--c-yellow); }
.ld-btn-ink:hover { color: var(--c-yellow); }
.ld-note { margin-top: 14px; text-align: right; font-family: var(--oj-hand); font-weight: 700; font-size: 24px; color: var(--text); transform: rotate(-2deg); }

/* ---------- 場景 1：鋼琴 ---------- */
.ld-vinyl { position: absolute; right: 5%; top: 12%; transform: rotate(8deg); }
.ld-eq { display: flex; align-items: flex-end; gap: 3px; height: 34px; margin-bottom: 22px; }
.ld-eq span {
  width: 6px; height: 100%; background: var(--text); border-radius: 3px;
  transform-origin: bottom; animation: ld-wave 1s ease-in-out infinite;
}
.ld-eq span.lite { background: #fff; }
@keyframes ld-wave { 0%, 100% { transform: scaleY(0.4); } 50% { transform: scaleY(1); } }
.ld-panel {
  position: relative; background: #fff; border: 2px solid var(--border-strong); border-radius: 20px;
  box-shadow: 0 16px 32px rgba(26, 26, 26, 0.22); padding: 24px;
}
.ld-panel::after {
  content: ''; position: absolute; top: -13px; right: 26px; width: 70px; height: 22px;
  background: rgba(255, 222, 0, 0.85); border-radius: 3px; transform: rotate(4deg);
  box-shadow: 0 1px 3px rgba(26, 26, 26, 0.15);
}
.ld-panel-head { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; gap: 12px; }
.ld-panel-title { font-weight: 900; font-size: 15px; color: var(--text); display: inline-flex; align-items: center; gap: 8px; }
.ld-demo {
  display: inline-flex; align-items: center; gap: 6px; cursor: pointer; white-space: nowrap;
  background: var(--c-yellow); border: 2px solid var(--border-strong); border-radius: 999px;
  font-family: var(--oj-font); font-weight: 900; font-size: 13px; padding: 8px 16px; color: var(--text);
  transition: transform .2s var(--ease-pop), box-shadow .2s;
}
.ld-demo:hover { transform: translateY(-2px) rotate(-1deg); box-shadow: 0 6px 14px rgba(26, 26, 26, 0.2); }
.ld-piano { display: flex; gap: 8px; }
.ld-piano button {
  flex: 1; height: 110px; border: 2px solid var(--border-strong); border-radius: 0 0 10px 10px;
  box-shadow: var(--ink-drop-sm); cursor: pointer;
  display: flex; flex-direction: column; justify-content: flex-end; align-items: center; padding-bottom: 10px;
  font-family: var(--oj-display); font-weight: 700; font-size: 13px; color: var(--text);
  transition: transform 0.06s;
}
.ld-piano button:active { transform: translateY(4px); box-shadow: none; }
.ld-track { margin-top: 16px; border-top: 2px dashed var(--border); padding-top: 14px; display: flex; align-items: center; gap: 10px; }
.ld-track-ic {
  width: 34px; height: 34px; flex: none; display: grid; place-items: center; color: var(--text);
  background: var(--c-pink); border: 2px solid var(--border-strong); border-radius: 12px;
}
.ld-track-meta { flex: 1; min-width: 0; display: flex; flex-direction: column; }
.ld-track-meta b { font-weight: 900; font-size: 14px; color: var(--text); }
.ld-track-meta i { font-style: normal; font-weight: 700; font-size: 12px; color: var(--text-soft); }
.ld-track-price { font-family: var(--oj-display); font-weight: 700; font-size: 18px; color: var(--text); }

/* ---------- 場景 2：相紙 ---------- */
.ld-photos { display: grid; grid-template-columns: repeat(3, 1fr); gap: 32px; }
.ld-photo {
  position: relative; text-align: left; cursor: pointer;
  background: #fff; border: 2px solid var(--border-strong); border-radius: 14px;
  box-shadow: 0 12px 26px rgba(26, 26, 26, 0.18); padding: 16px 16px 18px;
  font-family: var(--oj-font); transform: rotate(var(--rot, 0deg));
  transition: transform .3s var(--ease-pop), box-shadow .3s;
}
.ld-photo:hover { transform: rotate(0deg) translateY(-4px); box-shadow: 0 18px 34px rgba(26, 26, 26, 0.22); }
.ld-photo::before {
  content: ''; position: absolute; top: -12px; left: 50%; width: 64px; height: 20px;
  margin-left: -32px; background: rgba(125, 217, 255, 0.85); border-radius: 3px;
  transform: rotate(-3deg); box-shadow: 0 1px 3px rgba(26, 26, 26, 0.15); z-index: 2;
}
.ld-photo-img {
  display: flex; align-items: center; justify-content: center; height: 170px;
  border: 2px solid var(--border-strong); border-radius: 10px; position: relative; overflow: hidden;
}
.ld-photo-glyph { font-size: 52px; filter: blur(6px) grayscale(1); opacity: .5; transition: filter 1.2s, opacity 1.2s; }
.ld-photo-glyph.dev { filter: blur(0) grayscale(0); opacity: 1; }
.ld-photo-veil {
  position: absolute; inset: 0; background: #1a1030; opacity: .72; transition: opacity 1.2s;
  display: flex; align-items: center; justify-content: center;
}
.ld-photo-veil.dev { opacity: 0; }
.ld-photo-veil i {
  font-style: normal; color: #fff; font-weight: 900; font-size: 14px;
  border: 2px solid #fff; border-radius: 999px; padding: 6px 14px; white-space: nowrap;
}
.ld-photo-veil.dev i { opacity: 0; }
.ld-photo-title { display: block; margin-top: 14px; font-weight: 900; font-size: 15px; color: var(--text); }
.ld-photo-foot { display: flex; justify-content: space-between; margin-top: 6px; font-weight: 700; font-size: 13px; color: var(--text); }
.ld-photo-foot b { font-family: var(--oj-display); }

/* ---------- 場景 3：電子書 ---------- */
/* 不用 overflow:hidden（會截掉頂部膠帶貼條），改為各自圓角內側元素 */
.ld-book { padding: 0; }
.ld-book::after { right: auto; left: 30px; transform: rotate(-4deg); background: rgba(255, 144, 232, 0.85); }
.ld-book-head {
  display: flex; justify-content: space-between; align-items: center; gap: 12px;
  padding: 14px 20px; border-bottom: 2px solid var(--border-strong); background: var(--bg);
  border-radius: 18px 18px 0 0;
}
.ld-book-head .ld-panel-title { font-size: 14px; }
.ld-book-no {
  flex: none; font-family: var(--oj-display); font-weight: 700; font-size: 12px; color: var(--text);
  border: 2px solid var(--border-strong); border-radius: 999px; padding: 2px 12px; background: var(--c-lime);
  white-space: nowrap;
}
.ld-book-page { padding: 30px 34px; min-height: 200px; }
.ld-book-page h3 { margin: 0 0 12px; font-weight: 900; font-size: 19px; color: var(--text); }
.ld-book-page p { margin: 0; font-weight: 500; font-size: 15px; line-height: 2; color: var(--text); }
.ld-book-nav { display: flex; border-top: 2px solid var(--border-strong); }
.ld-book-nav button {
  flex: 1; cursor: pointer; font-family: var(--oj-font); font-weight: 900; font-size: 14px; padding: 15px;
  border: 0; transition: background .15s, color .15s;
}
.ld-book-prev { background: #fff; color: var(--text); border-right: 2px solid var(--border-strong) !important; border-bottom-left-radius: 18px; }
.ld-book-prev:hover { background: var(--c-yellow); }
.ld-book-next { background: var(--text); color: var(--c-lime); border-bottom-right-radius: 18px; }
.ld-book-next:hover { color: var(--c-yellow); }

/* ---------- 場景 4：規則 ---------- */
.ld-rule-sticker { position: absolute; right: 8%; top: 14%; transform: rotate(9deg); filter: drop-shadow(0 6px 12px rgba(26, 26, 26, 0.18)); }
.ld-rules-sub { font-weight: 700; font-size: 15px; margin: 0 0 40px; color: #444; text-align: center; }
.ld-rules { display: grid; grid-template-columns: repeat(3, 1fr); gap: 28px; }
.ld-rule {
  position: relative; text-align: center; background: #fff;
  border: 2px solid var(--border-strong); border-radius: 20px;
  box-shadow: 0 12px 26px rgba(26, 26, 26, 0.12); padding: 30px 24px;
}
.ld-rule::before {
  content: ''; position: absolute; top: -12px; left: 50%; width: 60px; height: 19px;
  margin-left: -30px; background: rgba(255, 222, 0, 0.85); border-radius: 3px;
  transform: rotate(-3deg); box-shadow: 0 1px 3px rgba(26, 26, 26, 0.15);
}
.ld-rule-num {
  display: flex; align-items: center; justify-content: center; width: 62px; height: 62px; margin: 0 auto;
  font-family: var(--oj-display); font-weight: 700; font-size: 32px; color: var(--text);
  border: 2px solid var(--border-strong); border-radius: 18px;
}
.ld-rule h3 { margin: 16px 0 0; font-weight: 900; font-size: 18px; color: var(--text); }
.ld-rule p { margin: 8px 0 0; font-weight: 500; font-size: 14px; line-height: 1.7; color: var(--text); }

/* ---------- 場景 5：市集 ---------- */
.ld-h2-dark { color: #fff; margin: 0 0 10px; }
.ld-mark-yellow {
  display: inline-block; background: var(--c-yellow); color: var(--text);
  border-radius: 16px; padding: 0 16px; transform: rotate(-1.5deg);
}
.ld-p-dark { color: #ccc; margin: 0; }
.ld-grid5 { display: grid; grid-template-columns: repeat(3, 150px); gap: 16px; }
.ld-gcard {
  background: var(--gc-bg, var(--c-pink)); border: 2px solid var(--border-strong); border-radius: 14px;
  box-shadow: 0 10px 22px rgba(0, 0, 0, 0.4); padding: 12px; will-change: transform;
}
/* 散落 → 收合（--inv 由 rAF 寫入：1 = 散落、0 = 歸位） */
.ld-anim .lg-1 { transform: translate(calc(var(--inv, 0) * -420px), calc(var(--inv, 0) * -260px)) rotate(calc(var(--inv, 0) * -18deg)); }
.ld-anim .lg-2 { transform: translate(calc(var(--inv, 0) * 300px), calc(var(--inv, 0) * -320px)) rotate(calc(var(--inv, 0) * 14deg)); }
.ld-anim .lg-3 { transform: translate(calc(var(--inv, 0) * -260px), calc(var(--inv, 0) * 300px)) rotate(calc(var(--inv, 0) * 10deg)); }
.lg-1 { --gc-bg: var(--c-pink); }
.lg-2 { --gc-bg: var(--c-yellow); }
.lg-3 { --gc-bg: var(--c-lime); }
.ld-gtag {
  display: inline-block; font-size: 10px; font-weight: 900; color: #fff;
  background: var(--text); border-radius: 999px; padding: 1px 8px; white-space: nowrap;
}
.ld-gicon { display: flex; align-items: center; margin: 7px 0 3px; height: 26px; color: var(--text); }
.ld-gtitle { display: block; font-weight: 900; font-size: 12px; line-height: 1.4; min-height: 33px; color: var(--text); }
.ld-gprice { display: block; font-family: var(--oj-display); font-weight: 700; font-size: 12px; margin-top: 4px; color: var(--text); }
.ld-ctarow { display: flex; gap: 18px; justify-content: center; align-items: center; flex-wrap: wrap; }
.ld-btn-yellow { background: var(--c-yellow); color: var(--text); }
.ld-btn-yellow:hover { box-shadow: 0 12px 26px rgba(255, 222, 0, 0.35); }
.ld-btn-ghost { background: transparent; color: #fff; border: 2px solid #fff; }
.ld-btn-ghost:hover { color: var(--c-yellow); border-color: var(--c-yellow); transform: translateY(-3px); box-shadow: none; }
.ld-note-lime { color: var(--c-lime); text-align: center; margin-top: 4px; }
.ld-footerbar {
  position: absolute; bottom: 0; left: 0; right: 0;
  border-top: 2px solid #333; padding: 14px 32px;
  display: flex; align-items: center; justify-content: space-between; flex-wrap: wrap; gap: 8px;
}
/* 非動畫模式：場景照文流直排，footer 列改一般文流 */
.ld-root:not(.ld-anim) .ld-footerbar { position: static; margin-top: 40px; width: 100%; }
.ld-foot-links { display: flex; gap: 20px; }
.ld-foot-links a { color: #fff; font-weight: 700; font-size: 12px; text-decoration: none; transition: color .15s; }
.ld-foot-links a:hover { color: var(--c-yellow); }
.ld-foot-copy { color: #888; font-weight: 500; font-size: 12px; }

/* ---------- RWD ---------- */
@media (max-width: 760px) {
  .m-hide { display: none !important; }
  .ld-dots { display: none; }
  .ld-h1 { font-size: 34px; margin: 20px 0 10px; }
  .ld-h2 { font-size: 25px; margin: 12px 0 10px; }
  .ld-p, .ld-lede { font-size: 14px; line-height: 1.7; }
  .ld-cols, .ld-cols-rev { grid-template-columns: 1fr; gap: 18px; padding: 0 20px; }
  .ld-hc { width: 104px; padding: 10px; }
  .ld-hc-title { font-size: 12px; }
  .ld-piano { gap: 5px; }
  .ld-piano button { height: 60px; font-size: 11px; }
  .ld-photos { gap: 10px; }
  .ld-photo { padding: 8px 8px 10px; }
  .ld-photo-img { height: 76px; }
  .ld-photo-glyph { font-size: 30px; }
  .ld-photo-veil i { font-size: 10px; padding: 3px 8px; max-width: 90%; overflow: hidden; text-overflow: ellipsis; }
  .ld-photo-title { font-size: 11px; margin-top: 8px; }
  .ld-rules { grid-template-columns: 1fr; gap: 11px; }
  .ld-rule { padding: 12px 16px; }
  .ld-rule-num { width: 40px; height: 40px; font-size: 20px; border-radius: 12px; }
  .ld-grid5 { grid-template-columns: repeat(3, 100px); gap: 8px; }
  .ld-ctarow { flex-direction: column; gap: 10px; }
  .ld-btn { font-size: 15px; padding: 12px 28px; }
  .ld-footerbar { padding: 10px 16px; }
  .ld-book-page { padding: 20px; min-height: 160px; }
  .ld-scene { padding-top: 72px; padding-bottom: 40px; }
}

@media (prefers-reduced-motion: reduce) {
  .ld-sticker-spin, .ld-hint, .ld-eq span { animation: none; }
}
</style>
