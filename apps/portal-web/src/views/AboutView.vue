<script setup lang="ts">
/* ============================================================
   AboutView — 品牌故事頁（/about）
   靜態版面（比照設計稿「Open Jam 關於我們 v3」）：
   Hero 紫色圓點色帶 + 三承諾卡 → 創作者能賣什麼三卡 →
   發表 spotlight（teal band）→ 品牌跑馬燈 → 創作者旅程五步
   （深色 band）→ 給探索者 collage（丁香紫 band）→
   我們相信的事四卡（黃 band）→ 雙 CTA。
   已移除先前所有 GSAP ScrollTrigger 捲動特效，純靜態頁。
   文案沿用 i18n（landing.*）；配色 / 圖示留在程式碼。
   ============================================================ */
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { useShopStore } from '@/stores/shop.js';
import { env } from '@/environment.js';
import AppNav from '@/layout/AppNav.vue';
import AppFooter from '@/layout/AppFooter.vue';

const store = useShopStore();
const router = useRouter();
const { t, tm, rt } = useI18n();

function goWorkspace() { window.location.href = env.WORKSPACE_PAGE_URL; }
function goMarket() { router.push('/'); }

// ── Hero 三承諾卡（配色 / 圖示留程式；文案取 landing.intro.facts）──
const PROMISE = [
  { icon: 'download', tint: 'var(--t-violet)', acc: 'var(--c-violet)', iconLight: true,  tape: 'rgba(255, 222, 0, .85)',   rot: '-1.5deg', tapeRot: '-3deg' },
  { icon: 'home',     tint: 'var(--t-pink)',   acc: 'var(--c-pink)',   iconLight: false, tape: 'rgba(125, 217, 255, .85)', rot: '1deg',    tapeRot: '3deg' },
  { icon: 'heart',    tint: '#c9f7e4',         acc: 'var(--c-lime)',   iconLight: false, tape: 'rgba(255, 144, 232, .85)', rot: '-0.8deg', tapeRot: '-4deg' },
] as const;
const promiseCards = computed(() =>
  (tm('landing.intro.facts') as { title: string; text: string }[]).map((f, i) => ({
    ...PROMISE[i], title: rt(f.title), text: rt(f.text),
  })),
);

// ── 創作者能賣什麼三卡（story.music/photo/ebook）──
const SELL = [
  { id: 'music', tag: 'MUSIC', head: 'var(--c-pink)',   tagBg: 'var(--c-yellow)', pattern: 'stripe', icon: 'note',  big: '',   headLight: true },
  { id: 'photo', tag: 'PHOTO', head: 'var(--c-yellow)', tagBg: 'var(--c-pink)',   pattern: 'dot',    icon: 'image', big: '',   headLight: false },
  { id: 'ebook', tag: 'WORDS', head: 'var(--c-lime)',   tagBg: 'var(--c-cyan)',   pattern: 'lines',  icon: '',      big: 'Aa', headLight: false },
] as const;
const sellCards = computed(() =>
  SELL.map((c) => ({
    ...c,
    title: t(`landing.story.${c.id}.title`),
    desc: t(`landing.story.${c.id}.desc`),
    chips: (tm(`landing.story.${c.id}.chips`) as string[]).map((x) => rt(x)),
  })),
);

// ── 發表 spotlight ──
const publishTags = computed(() => (tm('landing.publish.tags') as string[]).map((x) => rt(x)));

// ── 創作者旅程五步（flow.steps）──
const STEP = [
  { icon: 'arrowU', glyph: '',  bg: 'var(--c-pink)',   glow: '255, 144, 232', rot: '-3deg', iconLight: false, tilt: false },
  { icon: '',       glyph: '$', bg: 'var(--c-yellow)', glow: '255, 222, 0',   rot: '3deg',  iconLight: false, tilt: false },
  { icon: 'home',   glyph: '',  bg: 'var(--c-cyan)',   glow: '125, 217, 255', rot: '-3deg', iconLight: false, tilt: false },
  { icon: 'arrow',  glyph: '',  bg: 'var(--c-lime)',   glow: '184, 255, 159', rot: '3deg',  iconLight: false, tilt: true },
  { icon: 'heart',  glyph: '',  bg: 'var(--c-violet)', glow: '138, 92, 246',  rot: '-3deg', iconLight: true,  tilt: false },
] as const;
const flowSteps = computed(() =>
  (tm('landing.flow.steps') as { title: string; text: string }[]).map((s, i) => ({
    ...STEP[i], title: rt(s.title), text: rt(s.text),
  })),
);

// ── 我們相信的事四卡（why.items）──
const BELIEF = [
  { icon: 'sparkle', bg: 'var(--c-pink)',   rot: '-3deg' },
  { icon: 'note',    bg: 'var(--c-lime)',   rot: '3deg' },
  { icon: 'flame',   bg: 'var(--c-cyan)',   rot: '-3deg' },
  { icon: 'heart',   bg: 'var(--c-yellow)', rot: '3deg' },
] as const;
const beliefs = computed(() =>
  (tm('landing.why.items') as { title: string; text: string }[]).map((s, i) => ({
    ...BELIEF[i], title: rt(s.title), text: rt(s.text),
  })),
);

// ── 品牌跑馬燈 ──
const marqueeWords = computed(() => (tm('landing.marquee') as string[]).map((w) => rt(w)));
</script>

<template>
  <div class="oj-root" :class="'font-' + store.font" :data-screen-label="t('landing.screenLabel')">
    <app-nav />

    <main class="page about-page">
      <!-- ============ HERO：紫色圓點色帶 ============ -->
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

      <!-- ============ 三承諾卡（疊在色帶下緣） ============ -->
      <ul class="about-facts">
        <li
          v-for="c in promiseCards"
          :key="c.title"
          class="af-card"
          :class="{ 'af-ic-light': c.iconLight }"
          :style="{ '--tint': c.tint, '--acc': c.acc, '--tape': c.tape, '--rot': c.rot, '--tape-rot': c.tapeRot }"
        >
          <span class="af-ic"><app-icon :name="c.icon" :size="22" /></span>
          <h3>{{ c.title }}</h3>
          <p>{{ c.text }}</p>
        </li>
      </ul>

      <!-- ============ 創作者能賣什麼 ============ -->
      <section class="a-sell">
        <div class="sec-head">
          <span class="sec-eyebrow"><app-icon name="note" :size="12" /> {{ t('landing.story.eyebrow') }}</span>
          <h2 class="sec-title">{{ t('landing.story.title') }}<span class="q">？</span></h2>
        </div>
        <div class="sell-grid">
          <article
            v-for="c in sellCards"
            :key="c.id"
            class="sell-card"
            :style="{ '--head': c.head, '--tag-bg': c.tagBg }"
          >
            <div class="sc-head" :class="['sc-pat-' + c.pattern, { 'sc-head-light': c.headLight }]" aria-hidden="true">
              <app-icon v-if="c.icon" :name="c.icon" :size="50" />
              <span v-else class="sc-big">{{ c.big }}</span>
              <span class="sc-tag">{{ c.tag }}</span>
            </div>
            <div class="sc-body">
              <h3>{{ c.title }}</h3>
              <p>{{ c.desc }}</p>
              <ul class="sc-chips">
                <li v-for="chip in c.chips" :key="chip">{{ chip }}</li>
              </ul>
            </div>
          </article>
        </div>
      </section>

      <!-- ============ 發表 spotlight（teal band） ============ -->
      <section class="a-publish">
        <div class="pub-inner">
          <div class="pub-copy">
            <span class="sec-eyebrow" style="--pill-fg: var(--c-lime)"><app-icon name="note" :size="12" /> {{ t('landing.publish.eyebrow') }}</span>
            <h2 class="pub-title">{{ t('landing.publish.title') }}</h2>
            <p class="pub-text">{{ t('landing.publish.text') }}</p>
            <ul class="pub-tags">
              <li v-for="(tag, i) in publishTags" :key="tag" :class="'pub-tag-' + i">{{ tag }}</li>
            </ul>
          </div>
          <div class="pub-art" aria-hidden="true">
            <div class="pub-mini pub-mini-a">
              <span class="pub-tape"></span>
              <div class="pub-cover pub-cover-zn">Zn</div>
              <p class="pub-mini-title">城市漫遊者 Zine Vol.3</p>
              <div class="pub-mini-meta"><span class="pub-price">$6</span><span class="pub-rate"><app-icon name="star" :size="11" /> 4.8</span></div>
            </div>
            <div class="pub-mini pub-mini-b">
              <span class="pub-tape pub-tape-lime"></span>
              <div class="pub-cover pub-cover-note"><app-icon name="note" :size="36" /></div>
              <p class="pub-mini-title">手寫爵士小品譜集</p>
              <div class="pub-mini-meta"><span class="pub-free">免費</span><span class="pub-rate"><app-icon name="star" :size="11" /> 4.9</span></div>
            </div>
            <span class="pub-badge">{{ t('landing.publish.badge') }} <app-icon name="note" :size="13" /></span>
          </div>
        </div>
      </section>

      <!-- ============ 品牌跑馬燈帶 ============ -->
      <div class="a-marquee" aria-hidden="true">
        <div class="am-track">
          <template v-for="n in 2" :key="n">
            <span v-for="(w, i) in marqueeWords" :key="n + '-' + i" class="am-item">
              <app-icon name="note" :size="12" /> {{ w }}
            </span>
          </template>
        </div>
      </div>

      <!-- ============ 創作者旅程五步（深色 band） ============ -->
      <section class="a-steps">
        <div class="stp-wrap">
          <div class="sec-head">
            <span class="sec-eyebrow stp-eyebrow"><app-icon name="note" :size="12" /> {{ t('landing.flow.eyebrow') }}</span>
            <h2 class="sec-title stp-title">{{ t('landing.flow.title') }}</h2>
            <p class="stp-sub">{{ t('landing.flow.sub') }}</p>
          </div>
          <div class="stp-grid">
            <div
              v-for="(s, i) in flowSteps"
              :key="s.title"
              class="stp-card"
              :style="{ '--glow': s.glow }"
            >
              <span
                class="stp-ic"
                :class="{ 'stp-ic-light': s.iconLight, 'stp-ic-tilt': s.tilt }"
                :style="{ background: s.bg, '--rot': s.rot }"
              >
                <app-icon v-if="s.icon" :name="s.icon" :size="20" />
                <span v-else class="stp-glyph">{{ s.glyph }}</span>
              </span>
              <span class="stp-num">STEP {{ i + 1 }}</span>
              <h3>{{ s.title }}</h3>
              <p>{{ s.text }}</p>
            </div>
          </div>
        </div>
      </section>

      <!-- ============ 給探索者 collage（丁香紫 band） ============ -->
      <section class="a-discover">
        <div class="dsc-inner">
          <div class="dsc-copy">
            <span class="sec-eyebrow" style="--pill-fg: var(--c-pink)"><app-icon name="note" :size="12" /> {{ t('landing.discover.eyebrow') }}</span>
            <i18n-t keypath="landing.discover.title" tag="h2" class="dsc-title" scope="global">
              <template #mark><span class="hl dsc-mark">{{ t('landing.discover.titleMark') }}</span></template>
            </i18n-t>
            <p class="dsc-text">{{ t('landing.discover.text') }}</p>
            <button type="button" class="dsc-btn" @click="goMarket">
              {{ t('landing.cta.buyer.button') }} <app-icon name="arrow" :size="14" />
            </button>
          </div>
          <div class="dsc-art" aria-hidden="true">
            <div class="dsc-card dsc-grid">
              <p class="dsc-label"><app-icon name="image" :size="13" /> {{ t('landing.discover.gridLabel') }}</p>
              <div class="dsc-swatches">
                <span style="background: var(--c-violet)"></span>
                <span style="background: var(--c-yellow)"></span>
                <span style="background: var(--c-pink)"></span>
                <span style="background: var(--c-cyan)"></span>
              </div>
            </div>
            <div class="dsc-card dsc-player">
              <p class="dsc-label"><app-icon name="note" :size="13" /> {{ t('landing.discover.trackLabel') }}</p>
              <div class="dsc-player-row">
                <span class="dsc-play"><app-icon name="play" :size="13" /></span>
                <span class="dsc-bar"><i></i></span>
                <span class="dsc-time">0:42</span>
              </div>
            </div>
            <div class="dsc-card dsc-store">
              <p class="dsc-label"><app-icon name="home" :size="13" /> {{ t('landing.discover.profileLabel') }}</p>
              <div class="dsc-store-row">
                <span class="dsc-avatar">HT</span>
                <span class="dsc-store-meta"><b>Haru Tanaka</b><i>haru.openjam.co</i></span>
                <span class="dsc-follow">{{ t('landing.discover.follow') }}</span>
              </div>
            </div>
          </div>
        </div>
      </section>

      <!-- ============ 我們相信的事（黃 band） ============ -->
      <section class="a-beliefs">
        <div class="blf-wrap">
          <div class="sec-head">
            <span class="sec-eyebrow"><app-icon name="note" :size="12" /> {{ t('landing.why.eyebrow') }}</span>
            <h2 class="sec-title">{{ t('landing.why.title') }}</h2>
          </div>
          <div class="blf-grid">
            <div v-for="b in beliefs" :key="b.title" class="blf-card">
              <span class="blf-ic" :style="{ background: b.bg, '--rot': b.rot }"><app-icon :name="b.icon" :size="20" /></span>
              <div>
                <h3>{{ b.title }}</h3>
                <p>{{ b.text }}</p>
              </div>
            </div>
          </div>
        </div>
      </section>

      <!-- ============ 雙 CTA ============ -->
      <section class="a-cta">
        <i18n-t keypath="landing.cta.eyebrow" tag="h2" class="cta-title" scope="global">
          <template #mark><span class="hl cta-mark">{{ t('landing.cta.eyebrowMark') }}</span></template>
        </i18n-t>
        <p class="cta-note">{{ t('landing.cta.note') }}</p>
        <div class="cta-grid">
          <div class="cta-card cta-creator">
            <span class="cta-ic cta-ic-a"><app-icon name="doc" :size="21" /></span>
            <h3>{{ t('landing.cta.creator.title') }}</h3>
            <p>{{ t('landing.cta.creator.text') }}</p>
            <button type="button" class="cta-btn cta-btn-main" @click="goWorkspace">
              {{ t('landing.cta.creator.button') }} <app-icon name="arrow" :size="14" />
            </button>
          </div>
          <div class="cta-card cta-buyer">
            <span class="cta-ic cta-ic-b"><app-icon name="search" :size="21" /></span>
            <h3>{{ t('landing.cta.buyer.title') }}</h3>
            <p>{{ t('landing.cta.buyer.text') }}</p>
            <button type="button" class="cta-btn" @click="goMarket">
              {{ t('landing.cta.buyer.button') }} <app-icon name="arrow" :size="14" />
            </button>
          </div>
        </div>
      </section>
    </main>

    <app-footer />
  </div>
</template>

<style scoped>
/* 頁面滿版（覆蓋 .page 左右 gutter）：色帶貼齊 viewport，比照 FaqView / LegalView */
.about-page { position: relative; max-width: none; padding: 0; }

/* 共用區塊標頭 */
.sec-head { text-align: center; max-width: 760px; margin: 0 auto 40px; }
.sec-eyebrow {
  display: inline-flex; align-items: center; gap: 6px; padding: 5px 14px;
  font-family: var(--oj-font); font-size: 12px; font-weight: 900; letter-spacing: 2px;
  color: var(--pill-fg, var(--c-yellow)); background: var(--text); border-radius: 999px;
  transform: rotate(-2deg); white-space: nowrap;
}
.sec-title {
  margin: 16px 0 0; font-family: var(--oj-font); font-weight: 900;
  font-size: clamp(28px, 3.6vw, 38px); color: var(--text);
}
.sec-title .q { color: var(--c-violet); }

/* ── Hero 色帶：紫色圓點滿版帶 ────── */
.about-hero { position: relative; padding: clamp(52px, 8vh, 80px) clamp(20px, 3.5vw, 56px) clamp(64px, 9vh, 84px); text-align: center; }
.about-hero::before {
  content: ''; position: absolute; top: 0; bottom: 0; left: 50%;
  width: 100vw; transform: translateX(-50%); z-index: 0; pointer-events: none;
  background: var(--c-violet);
  background-image: radial-gradient(rgba(255, 255, 255, 0.18) 1.5px, transparent 1.5px);
  background-size: 26px 26px;
  border-bottom: var(--bw) solid var(--border-strong);
}
.ah-word {
  position: absolute; z-index: 1; top: 20px; left: -30px;
  font-family: var(--oj-display); font-weight: 700; line-height: 1;
  font-size: clamp(120px, 17vw, 190px); letter-spacing: -8px;
  color: rgba(255, 255, 255, .13); pointer-events: none; user-select: none;
}
.ah-deco { position: absolute; z-index: 1; font-style: normal; pointer-events: none; }
.ah-deco-sq { right: 7%; top: 48px; width: 56px; height: 56px; background: var(--c-lime); border: var(--bw) solid var(--border-strong); border-radius: 16px; transform: rotate(14deg); }
.ah-deco-dot { right: 14%; bottom: 44px; width: 44px; height: 44px; background: var(--c-yellow); border: var(--bw) solid var(--border-strong); border-radius: 50%; }
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

/* ── 三承諾卡：疊在色帶下緣（淡彩 + 膠帶 + 微傾） ────── */
.about-facts {
  position: relative; z-index: 2; list-style: none; max-width: 1080px; margin: -44px auto 0;
  padding: 0 clamp(20px, 3.5vw, 56px);
  display: grid; grid-template-columns: repeat(auto-fit, minmax(240px, 1fr)); gap: 24px;
}
.af-card {
  position: relative; text-align: left; padding: 22px;
  background: var(--tint);
  border: var(--bw) solid var(--border-strong); border-radius: 18px;
  box-shadow: 0 10px 24px rgba(26, 26, 26, 0.22);
  transform: rotate(var(--rot));
}
.af-card::before {
  content: ''; position: absolute; top: -12px; left: 50%; width: 60px; height: 19px;
  margin-left: -30px; background: var(--tape); border-radius: 3px;
  transform: rotate(var(--tape-rot)); box-shadow: 0 1px 3px rgba(26, 26, 26, 0.15);
}
.af-ic {
  display: grid; place-items: center; width: 44px; height: 44px; margin: 0 0 14px;
  color: var(--text); background: var(--acc);
  border: var(--bw) solid var(--border-strong); border-radius: 14px; transform: rotate(-3deg);
}
.af-ic-light .af-ic { color: #fff; }
.af-card h3 { margin: 0 0 6px; font-family: var(--oj-font); font-weight: 900; font-size: 17px; color: var(--text); }
.af-card p { margin: 0; font-size: 13px; font-weight: 500; line-height: 1.7; color: var(--text); }

/* ---------- 創作者能賣什麼 ---------- */
.a-sell { max-width: 1160px; margin: 0 auto; padding: clamp(56px, 8vh, 80px) clamp(20px, 3.5vw, 56px) 72px; }
.sell-grid { display: grid; grid-template-columns: repeat(3, 1fr); gap: 26px; }
.sell-card {
  background: var(--surface); border: var(--bw) solid var(--border-strong); border-radius: 20px; overflow: hidden;
  transition: transform .2s var(--ease-pop), box-shadow .2s;
}
.sell-card:nth-child(odd):hover { transform: translateY(-5px) rotate(-0.6deg); box-shadow: 0 14px 28px rgba(26, 26, 26, 0.14); }
.sell-card:nth-child(even):hover { transform: translateY(-5px) rotate(0.6deg); box-shadow: 0 14px 28px rgba(26, 26, 26, 0.14); }
.sc-head {
  position: relative; height: 140px; display: grid; place-items: center;
  background: var(--head); border-bottom: var(--bw) solid var(--border-strong); color: var(--text);
}
.sc-head-light { color: #fff; }
.sc-pat-stripe { background-image: repeating-linear-gradient(45deg, transparent, transparent 14px, rgba(255, 255, 255, 0.16) 14px, rgba(255, 255, 255, 0.16) 16px); }
.sc-pat-dot { background-image: radial-gradient(rgba(26, 26, 26, 0.12) 1.5px, transparent 1.5px); background-size: 20px 20px; }
.sc-pat-lines { background-image: repeating-linear-gradient(0deg, transparent, transparent 14px, rgba(255, 255, 255, 0.24) 14px, rgba(255, 255, 255, 0.24) 16px); }
.sc-big { font-family: var(--oj-display); font-size: 48px; font-weight: 700; color: var(--text); }
.sc-tag {
  position: absolute; top: 10px; left: 10px; padding: 2px 10px;
  background: var(--tag-bg); border: var(--bw) solid var(--border-strong); border-radius: 999px;
  font-family: var(--oj-display); font-weight: 700; font-size: 11px; color: var(--text);
  transform: rotate(-3deg); white-space: nowrap;
}
.sc-body { padding: 24px; }
.sc-body h3 { margin: 0; font-family: var(--oj-font); font-weight: 900; font-size: 21px; color: var(--text); }
.sc-body > p { margin: 8px 0 0; font-size: 14px; font-weight: 500; line-height: 1.8; color: var(--text); }
.sc-chips { list-style: none; display: flex; flex-wrap: wrap; gap: 8px; margin: 14px 0 0; padding: 0; }
.sc-chips li {
  padding: 2px 12px; border: var(--bw) solid var(--border-strong); border-radius: 999px; background: var(--bg);
  font-family: var(--oj-font); font-weight: 700; font-size: 12px; color: var(--text); white-space: nowrap;
}

/* ---------- 發表 spotlight（teal band） ---------- */
.a-publish {
  position: relative; overflow: hidden;
  padding: 72px clamp(20px, 3.5vw, 56px);
  background: #18a999;
  background-image: radial-gradient(rgba(255, 255, 255, 0.14) 1.5px, transparent 1.5px);
  background-size: 26px 26px;
  border-top: var(--bw) solid var(--border-strong); border-bottom: var(--bw) solid var(--border-strong);
}
.pub-inner {
  display: grid; grid-template-columns: 1.1fr 1fr; gap: clamp(30px, 4vw, 48px); align-items: center;
  max-width: 1100px; margin: 0 auto;
}
.pub-title { margin: 18px 0 16px; font-family: var(--oj-font); font-weight: 900; font-size: clamp(26px, 3.4vw, 36px); line-height: 1.4; color: #fff; }
.pub-text { margin: 0 0 22px; font-size: 15px; font-weight: 700; line-height: 1.9; color: #fff; }
.pub-tags { list-style: none; display: flex; flex-wrap: wrap; gap: 10px; margin: 0; padding: 0; }
.pub-tags li {
  padding: 6px 16px; border: var(--bw) solid var(--border-strong); border-radius: 999px;
  font-family: var(--oj-font); font-weight: 900; font-size: 13px; color: var(--text); white-space: nowrap;
}
.pub-tag-0 { background: var(--surface); }
.pub-tag-1 { background: var(--c-yellow); }
.pub-tag-2 { background: var(--c-pink); }
.pub-tag-3 { background: var(--c-lime); }

.pub-art { position: relative; height: 320px; }
.pub-mini {
  position: absolute; width: 190px; padding: 16px;
  background: var(--surface); border: var(--bw) solid var(--border-strong); border-radius: 16px;
  box-shadow: 0 12px 26px rgba(26, 26, 26, 0.25);
}
.pub-mini-a { left: 8%; top: 20px; transform: rotate(-5deg); }
.pub-mini-b { right: 6%; top: 64px; transform: rotate(4deg); }
.pub-tape {
  position: absolute; top: -12px; left: 50%; width: 60px; height: 19px; margin-left: -30px;
  background: rgba(255, 222, 0, 0.85); border-radius: 3px; transform: rotate(-3deg);
  box-shadow: 0 1px 3px rgba(26, 26, 26, 0.15);
}
.pub-tape-lime { background: rgba(184, 255, 159, 0.85); transform: rotate(3deg); }
.pub-cover {
  height: 110px; display: grid; place-items: center;
  border: var(--bw) solid var(--border-strong); border-radius: 12px;
}
.pub-cover-zn { background: var(--c-violet); color: #fff; font-family: var(--oj-display); font-weight: 700; font-size: 38px; }
.pub-cover-note { background: var(--c-yellow); color: var(--text); }
.pub-mini-title { margin: 10px 0 0; font-family: var(--oj-font); font-weight: 900; font-size: 14px; color: var(--text); }
.pub-mini-meta { display: flex; align-items: center; justify-content: space-between; margin-top: 6px; font-weight: 700; font-size: 12px; color: var(--text); }
.pub-price { font-family: var(--oj-display); }
.pub-free { padding: 0 8px; background: var(--c-lime); border: var(--bw) solid var(--border-strong); border-radius: 999px; }
.pub-rate { display: inline-flex; align-items: center; gap: 3px; color: var(--text); }
.pub-rate :deep(svg) { color: var(--c-yellow); }
.pub-badge {
  position: absolute; left: 30%; bottom: 0; display: inline-flex; align-items: center; gap: 6px;
  padding: 11px 20px; background: var(--text); color: var(--c-yellow);
  border-radius: 999px; box-shadow: 0 8px 18px rgba(26, 26, 26, 0.3);
  font-family: var(--oj-font); font-weight: 900; font-size: 14px; transform: rotate(-2deg); white-space: nowrap;
}

/* ---------- 品牌跑馬燈帶 ---------- */
.a-marquee { overflow: hidden; background: var(--text); padding: 14px 0; }
.am-track { display: inline-flex; align-items: center; gap: 32px; width: max-content; animation: am-run 26s linear infinite; }
.am-item {
  display: inline-flex; align-items: center; gap: 6px; white-space: nowrap;
  font-family: var(--oj-font); font-weight: 900; font-size: 15px; color: #fff;
}
.am-item:nth-child(6n+1) { color: var(--c-yellow); }
.am-item:nth-child(6n+3) { color: var(--c-pink); }
.am-item:nth-child(6n+5) { color: var(--c-lime); }
.am-item:nth-child(6n) { color: var(--c-cyan); }
.am-item :deep(svg) { color: currentColor; flex: none; }
@keyframes am-run { to { transform: translateX(-50%); } }

/* ---------- 創作者旅程五步（深色 band） ---------- */
.a-steps {
  position: relative; overflow: hidden;
  padding: 72px clamp(20px, 3.5vw, 56px);
  background: #1a1626;
  background-image: radial-gradient(rgba(255, 255, 255, 0.08) 1.5px, transparent 1.5px);
  background-size: 26px 26px;
  border-bottom: var(--bw) solid var(--border-strong);
}
.stp-wrap { max-width: 1160px; margin: 0 auto; }
.stp-eyebrow { background: var(--c-yellow); color: var(--text); border: var(--bw) solid var(--border-strong); }
.stp-title { color: #fff; }
.stp-sub { margin: 10px 0 0; font-size: 15px; font-weight: 500; color: #bbb; }
.stp-grid { display: grid; grid-template-columns: repeat(5, 1fr); gap: 20px; }
.stp-card {
  text-align: center; padding: 20px 14px 22px;
  background: var(--surface); border: var(--bw) solid var(--border-strong); border-radius: 18px;
  box-shadow: 0 10px 22px rgba(var(--glow), 0.28);
  transition: transform .2s var(--ease-pop);
}
.stp-card:nth-child(odd):hover { transform: translateY(-4px) rotate(-1deg); }
.stp-card:nth-child(even):hover { transform: translateY(-4px) rotate(1deg); }
.stp-ic {
  display: grid; place-items: center; width: 52px; height: 52px; margin: 0 auto;
  border: var(--bw) solid var(--border-strong); border-radius: 16px; color: var(--text);
  transform: rotate(var(--rot));
}
.stp-ic-light { color: #fff; }
.stp-ic-tilt :deep(svg) { transform: rotate(-45deg); }
.stp-glyph { font-family: var(--oj-display); font-weight: 700; font-size: 22px; }
.stp-num {
  display: inline-block; margin: 12px 0 0; padding: 2px 10px;
  font-family: var(--oj-font); font-weight: 900; font-size: 10px; letter-spacing: 1px;
  color: #fff; background: var(--text); border-radius: 999px;
}
.stp-card h3 { margin: 8px 0 0; font-family: var(--oj-font); font-weight: 900; font-size: 16px; color: var(--text); }
.stp-card p { margin: 6px 0 0; font-size: 12px; font-weight: 500; line-height: 1.7; color: #444; }

/* ---------- 給探索者 collage（丁香紫 band） ---------- */
.a-discover {
  position: relative; overflow: hidden;
  padding: 72px clamp(20px, 3.5vw, 56px);
  background: var(--t-violet);
  background-image: radial-gradient(rgba(26, 26, 26, 0.08) 1.2px, transparent 1.2px);
  background-size: 24px 24px;
  border-bottom: var(--bw) solid var(--border-strong);
}
.dsc-inner { display: grid; grid-template-columns: 1fr 1fr; gap: clamp(30px, 4vw, 48px); align-items: center; max-width: 1100px; margin: 0 auto; }
.dsc-title { margin: 18px 0 16px; font-family: var(--oj-font); font-weight: 900; font-size: clamp(28px, 3.4vw, 40px); line-height: 1.5; color: var(--text); }
.dsc-mark { background: var(--c-yellow); }
.dsc-text { margin: 0 0 26px; max-width: 440px; font-size: 15px; font-weight: 500; line-height: 1.9; color: var(--text); }
.dsc-btn {
  display: inline-flex; align-items: center; gap: 7px; cursor: pointer; border: 0;
  padding: 14px 30px; border-radius: 999px; background: var(--text); color: var(--c-yellow);
  font-family: var(--oj-font); font-weight: 900; font-size: 15px;
  transition: transform .2s var(--ease-pop), box-shadow .2s;
}
.dsc-btn:hover { transform: translateY(-3px) rotate(-1deg); box-shadow: 0 10px 22px rgba(26, 26, 26, 0.3); }
.dsc-btn:active { transform: translateY(0); box-shadow: none; }

.dsc-art { position: relative; height: 340px; }
.dsc-card {
  position: absolute; padding: 16px;
  background: var(--surface); border: var(--bw) solid var(--border-strong); border-radius: 16px;
  box-shadow: 0 12px 26px rgba(26, 26, 26, 0.16);
}
.dsc-label { display: flex; align-items: center; gap: 6px; margin: 0 0 10px; font-family: var(--oj-font); font-weight: 900; font-size: 12px; color: var(--text); }
.dsc-grid { left: 0; top: 10px; width: 320px; transform: rotate(-2deg); }
.dsc-swatches { display: grid; grid-template-columns: repeat(4, 1fr); gap: 8px; }
.dsc-swatches span { height: 54px; border: var(--bw) solid var(--border-strong); border-radius: 8px; }
.dsc-player { right: 0; top: 130px; width: 300px; transform: rotate(2deg); }
.dsc-player-row { display: flex; align-items: center; gap: 10px; }
.dsc-play { display: grid; place-items: center; width: 36px; height: 36px; flex: none; background: var(--c-violet); color: #fff; border: var(--bw) solid var(--border-strong); border-radius: 50%; }
.dsc-bar { position: relative; flex: 1; height: 10px; background: var(--bg); border: var(--bw) solid var(--border-strong); border-radius: 999px; overflow: hidden; }
.dsc-bar i { position: absolute; left: 0; top: 0; bottom: 0; width: 60%; background: var(--c-pink); }
.dsc-time { font-family: var(--oj-display); font-weight: 700; font-size: 11px; color: var(--text); }
.dsc-store { left: 40px; bottom: 0; width: 280px; transform: rotate(-1deg); }
.dsc-store-row { display: flex; align-items: center; gap: 10px; }
.dsc-avatar { display: grid; place-items: center; width: 34px; height: 34px; flex: none; background: var(--c-yellow); border: var(--bw) solid var(--border-strong); border-radius: 50%; font-family: var(--oj-font); font-weight: 900; font-size: 11px; color: var(--text); }
.dsc-store-meta { flex: 1; min-width: 0; }
.dsc-store-meta b { display: block; font-family: var(--oj-font); font-weight: 900; font-size: 13px; color: var(--text); }
.dsc-store-meta i { display: block; font-style: normal; font-weight: 500; font-size: 11px; color: var(--text-soft); }
.dsc-follow { padding: 3px 12px; background: var(--c-lime); border: var(--bw) solid var(--border-strong); border-radius: 999px; font-family: var(--oj-font); font-weight: 900; font-size: 11px; color: var(--text); white-space: nowrap; }

/* ---------- 我們相信的事（黃 band） ---------- */
.a-beliefs {
  position: relative; overflow: hidden;
  padding: 72px clamp(20px, 3.5vw, 56px);
  background: #ffc01f; border-bottom: var(--bw) solid var(--border-strong);
}
.blf-wrap { max-width: 1020px; margin: 0 auto; }
.blf-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 26px; }
.blf-card {
  display: flex; gap: 18px; align-items: flex-start; padding: 26px;
  background: var(--surface); border: var(--bw) solid var(--border-strong); border-radius: 20px;
  box-shadow: 0 10px 22px rgba(26, 26, 26, 0.12);
  transition: transform .2s var(--ease-pop), box-shadow .2s;
}
.blf-card:nth-child(odd):hover { transform: translateY(-4px) rotate(-0.5deg); box-shadow: 0 16px 30px rgba(26, 26, 26, 0.16); }
.blf-card:nth-child(even):hover { transform: translateY(-4px) rotate(0.5deg); box-shadow: 0 16px 30px rgba(26, 26, 26, 0.16); }
.blf-ic { display: grid; place-items: center; width: 52px; height: 52px; flex: none; border: var(--bw) solid var(--border-strong); border-radius: 16px; color: var(--text); transform: rotate(var(--rot)); }
.blf-card h3 { margin: 0; font-family: var(--oj-font); font-weight: 900; font-size: 19px; color: var(--text); }
.blf-card p { margin: 6px 0 0; font-size: 14px; font-weight: 500; line-height: 1.8; color: var(--text); }

/* ---------- 雙 CTA ---------- */
.a-cta { max-width: 1020px; margin: 0 auto; padding: 80px clamp(20px, 3.5vw, 56px) 88px; text-align: center; }
.cta-title { margin: 0 0 12px; font-family: var(--oj-font); font-weight: 900; font-size: clamp(28px, 4vw, 42px); line-height: 1.5; color: var(--text); text-align: center; }
.cta-mark { background: var(--c-violet); color: #fff; }
.cta-note { margin: 0 0 36px; font-family: var(--oj-hand); font-weight: 700; font-size: 26px; color: var(--c-violet); transform: rotate(-2deg); }
.cta-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 28px; text-align: left; }
.cta-card {
  padding: 32px; border: var(--bw) solid var(--border-strong); border-radius: 22px;
  box-shadow: 0 12px 26px rgba(26, 26, 26, 0.12);
  transition: transform .2s var(--ease-pop), box-shadow .2s;
}
.cta-creator { background: var(--t-violet); }
.cta-buyer { background: #c9f7e4; }
.cta-card:nth-child(odd):hover { transform: translateY(-5px) rotate(-0.6deg); box-shadow: 0 18px 34px rgba(26, 26, 26, 0.16); }
.cta-card:nth-child(even):hover { transform: translateY(-5px) rotate(0.6deg); box-shadow: 0 18px 34px rgba(26, 26, 26, 0.16); }
.cta-ic { display: grid; place-items: center; width: 56px; height: 56px; background: var(--surface); border: var(--bw) solid var(--border-strong); border-radius: 18px; color: var(--text); }
.cta-ic-a { transform: rotate(-4deg); }
.cta-ic-b { transform: rotate(4deg); }
.cta-card h3 { margin: 18px 0 0; font-family: var(--oj-font); font-weight: 900; font-size: 23px; color: var(--text); }
.cta-card p { margin: 8px 0 20px; font-size: 14px; font-weight: 500; line-height: 1.8; color: var(--text); }
.cta-btn {
  display: inline-flex; align-items: center; gap: 7px; cursor: pointer; border: 0;
  padding: 13px 26px; border-radius: 999px; background: var(--text); color: var(--c-yellow);
  font-family: var(--oj-font); font-weight: 900; font-size: 15px;
  transition: transform .2s var(--ease-pop), box-shadow .2s;
}
.cta-btn-main { background: var(--c-violet); color: #fff; }
.cta-btn:hover { transform: translateY(-3px) rotate(-1deg); box-shadow: 0 10px 22px rgba(26, 26, 26, 0.3); }
.cta-btn-main:hover { box-shadow: 0 10px 22px rgba(138, 92, 246, 0.4); }
.cta-btn:active { transform: translateY(0); box-shadow: none; }

/* ---------- RWD ---------- */
@media (max-width: 980px) {
  .sell-grid { grid-template-columns: repeat(2, 1fr); }
  .stp-grid { grid-template-columns: repeat(2, 1fr); gap: 26px 18px; }
}
@media (max-width: 860px) {
  .pub-inner, .dsc-inner { grid-template-columns: 1fr; }
  .pub-art, .dsc-art { height: 300px; }
  .blf-grid, .cta-grid { grid-template-columns: 1fr; }
}
@media (max-width: 640px) {
  .about-facts { margin-top: -36px; }
  .sell-grid { grid-template-columns: 1fr; }
  .stp-grid { grid-template-columns: 1fr; }
}

/* reduced-motion：停用跑馬燈動畫 */
@media (prefers-reduced-motion: reduce) {
  .am-track { animation: none; }
}
</style>
