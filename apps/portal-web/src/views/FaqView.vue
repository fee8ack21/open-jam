<script setup lang="ts">
/* ============================================================
   FaqView — 常見問題頁（/faq）
   版面結構：Hero 搜尋色帶（關鍵字過濾 + 熱門字）→ 主題卡片
   （疊在色帶下緣）→ 主題 pills + 依主題分組的編號手風琴
   → 聯絡 CTA。
   內容撈取 ContentService「已發布」的 FAQ（GET /v1/faqs/published，
   後台可維護）；撈取失敗或尚無資料時退回 i18n 靜態文案（faq.items）。
   ============================================================ */
import { ref, computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { useShopStore } from '@/stores/shop.js';
import { env } from '@/environment.js';
import { contentApi } from '@/api';
import { type FaqCategoryDto, type FaqItemDto } from '@/api/content-service';
import AppNav from '@/layout/AppNav.vue';
import AppFooter from '@/layout/AppFooter.vue';

const store = useShopStore();
const { t, te, tm, rt } = useI18n();

const ALL = 'all';

interface FaqItem { i: number; cat: string; q: string; a: string; }
interface Pill { key: string; label: string; count: number; }
interface Section { slug: string; name: string; color: string; items: (FaqItem & { num: string })[]; }

// 主題分類（後台可 CRUD）撈取失敗時退回的靜態 slug；標籤取自 i18n faq.tabs.<slug>
const FALLBACK_SLUGS = ['platform', 'buying', 'selling', 'payments'] as const;

// 便條紙色盤：分類依索引輪替取色（分類資料表不存顏色），對應主題卡片底色、
// 分組標頭色籤與展開狀態的強調色
const PALETTE = ['var(--c-lime)', 'var(--c-pink)', 'var(--c-cyan)', 'var(--c-yellow)', 'var(--c-violet)'];

// ── 已發布 FAQ + 主題分類（撈取失敗 / 尚無資料時為 null，退回 i18n 靜態文案）─────
const apiCats = ref<FaqCategoryDto[] | null>(null);
const apiItems = ref<FaqItemDto[] | null>(null);

async function loadFaqs() {
  const [cats, items] = await Promise.allSettled([
    contentApi.faqCategories.list(),
    contentApi.faqs.getPublished(),
  ]);
  apiCats.value = cats.status === 'fulfilled' && cats.value.data?.length ? cats.value.data : null;
  apiItems.value = items.status === 'fulfilled' && items.value.data?.length ? items.value.data : null;
}
loadFaqs();

// 主題分類（slug + 顯示名稱 + 卡片敘述）：優先用 API，否則退回 i18n 靜態分類。
// API 分類未填敘述時，退回同 slug 的 i18n 敘述（無對應 key 則留空）。
const categories = computed<{ slug: string; name: string; desc: string }[]>(() => {
  if (apiCats.value) {
    return apiCats.value.map((c) => {
      const slug = c.slug ?? '';
      const fallbackDesc = te('faq.tabDesc.' + slug) ? t('faq.tabDesc.' + slug) : '';
      return { slug, name: c.name ?? '', desc: c.description || fallbackDesc };
    });
  }
  return FALLBACK_SLUGS.map((s) => ({ slug: s, name: t('faq.tabs.' + s), desc: t('faq.tabDesc.' + s) }));
});

// 分類 slug → 便條紙色（依分類索引輪替）
function catColor(slug: string): string {
  const idx = categories.value.findIndex((c) => c.slug === slug);
  return PALETTE[Math.max(idx, 0) % PALETTE.length];
}

// 全部問答（帶原始索引，供手風琴開合狀態穩定對應）：優先用 API，否則退回 i18n
const items = computed<FaqItem[]>(() => {
  if (apiItems.value) {
    return apiItems.value.map((it, i) => ({
      i,
      cat: it.categorySlug ?? '',
      q: it.question ?? '',
      a: it.answer ?? '',
    }));
  }
  return (tm('faq.items') as { cat: string; q: string; a: string }[]).map((it, i) => ({
    i,
    cat: it.cat,
    q: rt(it.q),
    a: rt(it.a),
  }));
});

// ── 關鍵字搜尋：問題與答案全文比對（不分大小寫） ─────
const query = ref('');
const hasQuery = computed(() => query.value.trim().length > 0);
const matched = computed<FaqItem[]>(() => {
  const q = query.value.trim().toLowerCase();
  if (!q) return items.value;
  return items.value.filter((it) => it.q.toLowerCase().includes(q) || it.a.toLowerCase().includes(q));
});
function clearQuery() {
  query.value = '';
}

// 熱門關鍵字（點了直接帶入搜尋、回到「全部」視角）
const hotChips = computed<string[]>(() => (tm('faq.search.hot') as string[]).map((c) => rt(c)));
function selectChip(chip: string) {
  query.value = chip;
  activeTab.value = ALL;
}

// ── 主題篩選 pills（計數隨搜尋結果變動） ─────
const activeTab = ref<string>(ALL);
const pills = computed<Pill[]>(() => [
  { key: ALL, label: t('faq.tabs.all'), count: matched.value.length },
  ...categories.value.map((c) => ({
    key: c.slug,
    label: c.name,
    count: matched.value.filter((it) => it.cat === c.slug).length,
  })),
]);
function selectTab(k: string) {
  activeTab.value = k;
  open.value = new Set(); // 換主題時收合，避免殘留展開狀態
}

// 主題卡片計數（該主題全部則數，不受搜尋影響）
function catCount(slug: string): number {
  return items.value.filter((it) => it.cat === slug).length;
}

// 主題卡片點選：切換視角並捲到問題列表
const listEl = ref<HTMLElement | null>(null);
function selectTopic(slug: string) {
  selectTab(slug);
  listEl.value?.scrollIntoView({ behavior: 'smooth', block: 'start' });
}

// ── 依主題分組的顯示區塊（每組重新編號；空組不顯示） ─────
const sections = computed<Section[]>(() =>
  categories.value
    .filter((c) => activeTab.value === ALL || c.slug === activeTab.value)
    .map((c) => ({
      slug: c.slug,
      name: c.name,
      color: catColor(c.slug),
      items: matched.value
        .filter((it) => it.cat === c.slug)
        .map((it, n) => ({ ...it, num: String(n + 1).padStart(2, '0') })),
    }))
    .filter((s) => s.items.length > 0),
);
const isEmpty = computed(() => sections.value.length === 0);

// 手風琴開合：以原始索引為 key
const open = ref<Set<number>>(new Set());
function toggle(i: number) {
  const next = new Set(open.value);
  if (next.has(i)) next.delete(i);
  else next.add(i);
  open.value = next;
}
</script>

<template>
  <div class="oj-root" :class="'font-' + store.font" :data-screen-label="t('faq.screenLabel')">
    <!-- ============ NAV ============ -->
    <app-nav />

    <main class="page faq-page">
      <!-- ============ HERO：搜尋色帶（比照 Discover hero 的滿版漸層帶） ============ -->
      <section class="faq-hero">
        <span class="fh-word" aria-hidden="true">FAQ</span>
        <i class="fh-deco fh-deco-q" aria-hidden="true">?</i>
        <i class="fh-deco fh-deco-dot" aria-hidden="true"></i>
        <i class="fh-deco fh-deco-sq" aria-hidden="true"></i>
        <div class="fh-inner">
          <p class="fh-badge">
            <app-icon name="note" :size="13" /> {{ t('faq.badge', { count: items.length }) }}
          </p>
          <i18n-t keypath="faq.title" tag="h1" class="fh-title" scope="global">
            <template #mark><span class="hl hl-lime">{{ t('faq.titleMark') }}</span></template>
          </i18n-t>
          <p class="fh-lede">{{ t('faq.lede') }}</p>

          <div class="fh-search">
            <span class="s-ic"><app-icon name="search" :size="18" /></span>
            <input
              v-model="query"
              type="text"
              :placeholder="t('faq.search.placeholder')"
              :aria-label="t('faq.search.placeholder')"
            />
            <button v-if="hasQuery" type="button" class="fh-clear" @click="clearQuery">
              {{ t('faq.search.clear') }}
            </button>
          </div>

          <div class="fh-hot">
            <span class="fh-hot-label">{{ t('faq.search.hotLabel') }}</span>
            <button v-for="chip in hotChips" :key="chip" type="button" class="kw" @click="selectChip(chip)">
              {{ chip }}
            </button>
          </div>
        </div>
      </section>

      <!-- ============ 主題卡片（疊在色帶下緣） ============ -->
      <section class="faq-topics">
        <button
          v-for="c in categories"
          :key="c.slug"
          type="button"
          class="faq-topic"
          :class="{ on: activeTab === c.slug }"
          :style="{ '--acc': catColor(c.slug) }"
          @click="selectTopic(c.slug)"
        >
          <span class="ftp-head">
            <span class="ftp-name">{{ c.name }}</span>
            <span class="ftp-count">{{ catCount(c.slug) }}</span>
          </span>
          <span v-if="c.desc" class="ftp-desc">{{ c.desc }}</span>
        </button>
      </section>

      <!-- ============ 問題列表：主題 pills + 分組編號手風琴 ============ -->
      <section ref="listEl" class="faq-list">
        <nav class="faq-pills" role="tablist" :aria-label="t('faq.screenLabel')">
          <button
            v-for="p in pills"
            :key="p.key"
            type="button"
            role="tab"
            class="faq-pill"
            :class="{ on: activeTab === p.key }"
            :aria-selected="activeTab === p.key"
            @click="selectTab(p.key)"
          >
            {{ p.label }}<span class="fp-count">{{ p.count }}</span>
          </button>
        </nav>

        <!-- 搜尋無結果 -->
        <div v-if="isEmpty" class="faq-empty">
          <p class="fe-title">
            {{ hasQuery ? t('faq.empty.title', { query: query.trim() }) : t('faq.empty.hint') }}
          </p>
          <p v-if="hasQuery" class="fe-hint">{{ t('faq.empty.hint') }}</p>
          <button v-if="hasQuery" type="button" class="fe-clear" @click="clearQuery">
            {{ t('faq.empty.clear') }}
          </button>
        </div>

        <!-- 分組手風琴 -->
        <div v-else class="faq-secs">
          <section v-for="sec in sections" :key="sec.slug" class="faq-sec" :style="{ '--acc': sec.color }">
            <header class="fs-head">
              <span class="fs-name">{{ sec.name }}</span>
              <span class="fs-meta">{{ t('faq.sectionMeta', { count: sec.items.length }) }}</span>
            </header>
            <ul class="fs-card">
              <li v-for="it in sec.items" :key="it.i" class="fs-item" :class="{ open: open.has(it.i) }">
                <button type="button" class="fs-q" :aria-expanded="open.has(it.i)" @click="toggle(it.i)">
                  <span class="fs-num">{{ it.num }}</span>
                  <span class="fs-q-text">{{ it.q }}</span>
                  <span class="fs-mark" aria-hidden="true">{{ open.has(it.i) ? '−' : '+' }}</span>
                </button>
                <transition name="faq-a">
                  <div v-if="open.has(it.i)" class="fs-a"><p>{{ it.a }}</p></div>
                </transition>
              </li>
            </ul>
          </section>
        </div>
      </section>

      <!-- ============ 聯絡 CTA（黃色滿版帶） ============ -->
      <section class="faq-cta">
        <i class="fc-deco fc-deco-a" aria-hidden="true"></i>
        <i class="fc-deco fc-deco-b" aria-hidden="true"></i>
        <div class="fc-card">
          <i18n-t keypath="faq.cta.title" tag="h2" class="fc-title" scope="global">
            <template #mark><span class="fc-hl">{{ t('faq.cta.titleMark') }}</span></template>
          </i18n-t>
          <p class="fc-text">{{ t('faq.cta.text') }}</p>
          <div class="fc-actions">
            <a class="fc-btn fc-btn-dark" href="mailto:support@openjam.co">
              <app-icon name="mail" :size="16" /> {{ t('faq.cta.contact') }}
            </a>
            <a class="fc-btn fc-btn-light" :href="env.GITHUB_REPO_URL + '/issues'" target="_blank" rel="noopener">
              <app-icon name="chat" :size="16" /> {{ t('faq.cta.github') }}
            </a>
          </div>
          <div class="fc-note">{{ t('faq.cta.note') }}</div>
        </div>
      </section>
    </main>

    <!-- ============ FOOTER ============ -->
    <app-footer />
  </div>
</template>

<style scoped>
/* 頁面滿版（覆蓋 .page 左右 gutter）：hero 色帶貼齊 viewport，比照 LegalView / AboutView */
.faq-page { position: relative; max-width: none; padding: 0; }

/* ── Hero 搜尋色帶：紫色圓點滿版帶（同 .mkt-hero 配方）────── */
.faq-hero { position: relative; padding: clamp(52px, 8vh, 76px) clamp(20px, 3.5vw, 56px) clamp(120px, 15vh, 160px); text-align: center; }
.faq-hero::before {
  content: ''; position: absolute; top: 0; bottom: 0; left: 50%;
  width: 100vw; transform: translateX(-50%); z-index: 0; pointer-events: none;
  background: #8a5cf6;
  background-image: radial-gradient(rgba(255, 255, 255, 0.18) 1.5px, transparent 1.5px);
  background-size: 26px 26px;
}

/* 色帶內的縷空大字（白色微透、傾斜，沉在置中內容後） */
.fh-word {
  position: absolute; z-index: 1; top: 44px; left: 5%;
  font-family: var(--oj-display); font-weight: 700; line-height: 1;
  font-size: clamp(110px, 15vw, 150px);
  color: rgba(255, 255, 255, .14); transform: rotate(-6deg);
  pointer-events: none; user-select: none;
}

/* 漂浮貼紙（? 方塊 / 圓點 / 方點） */
.fh-deco { position: absolute; z-index: 1; font-style: normal; pointer-events: none; }
.fh-deco-q {
  right: 6%; top: 64px; width: 64px; height: 64px;
  display: grid; place-items: center; font-weight: 900; font-size: 34px; color: var(--text);
  background: var(--c-yellow); border: 2px solid var(--border-strong); border-radius: 18px;
  box-shadow: 0 8px 20px rgba(26, 26, 26, 0.25); transform: rotate(8deg);
}
.fh-deco-dot { right: 14%; bottom: 96px; width: 44px; height: 44px; background: var(--c-lime); border: 2px solid var(--border-strong); border-radius: 50%; transform: rotate(-10deg); }
.fh-deco-sq { left: 12%; bottom: 110px; width: 40px; height: 40px; background: var(--c-pink); border: 2px solid var(--border-strong); border-radius: 12px; transform: rotate(14deg); }

.fh-inner {
  position: relative; z-index: 2; max-width: 780px; margin: 0 auto;
  display: flex; flex-direction: column; align-items: center; gap: 20px;
}
.fh-badge {
  display: inline-flex; align-items: center; gap: 8px; margin: 0; padding: 7px 18px;
  font-family: var(--oj-font); font-size: 13px; font-weight: 900; letter-spacing: 2px;
  color: var(--c-yellow); background: var(--text);
  border-radius: 999px; transform: rotate(-1deg); white-space: nowrap;
}
.fh-title {
  margin: 0; font-family: var(--oj-font); font-weight: 900; color: #fff;
  font-size: clamp(30px, 5vw, 54px); line-height: 1.28;
}
.fh-title .hl { box-shadow: var(--ink-drop); margin: 0 8px; }
.fh-lede { margin: 0; max-width: 52ch; font-size: 16px; font-weight: 700; line-height: 1.8; color: #fff; }

/* 搜尋列：白色膠囊 + 黃色動作鈕（比照 .mkt-search） */
.fh-search {
  width: 100%; max-width: 560px; display: flex; align-items: center; gap: 0;
  height: 56px; padding: 0; overflow: hidden; background: var(--surface);
  border: 2px solid var(--border-strong); border-radius: 999px;
  box-shadow: 0 10px 24px rgba(26, 26, 26, 0.25); transition: transform .16s, box-shadow .16s;
}
.fh-search:focus-within { transform: translateY(-2px); box-shadow: 0 14px 30px rgba(26, 26, 26, 0.3); }
.fh-search .s-ic { color: var(--text); flex: none; display: grid; place-items: center; padding: 0 8px 0 20px; }
.fh-search input {
  flex: 1; min-width: 0; border: 0; outline: 0; background: transparent;
  font-family: var(--oj-font); font-size: 16px; color: var(--text);
}
.fh-search input::placeholder { color: var(--text-faint); }
.fh-clear {
  flex: none; height: 100%; padding: 0 24px; cursor: pointer;
  font-family: var(--oj-font); font-weight: 900; font-size: 15px; color: var(--text);
  background: var(--c-yellow); border: 0; border-left: 2px solid var(--border-strong);
  transition: background .15s;
}
.fh-clear:hover { background: var(--c-pink); }

/* 熱門關鍵字：沿用市集 .kw 膠囊 chips */
.fh-hot { display: flex; align-items: center; justify-content: center; flex-wrap: wrap; gap: 10px; }
.fh-hot-label { font-family: var(--oj-font); font-size: 13px; font-weight: 900; color: #fff; }

/* ── 主題卡片：疊在色帶下緣，依分類輪替便條紙色 + 交錯傾斜 ────── */
.faq-topics {
  position: relative; z-index: 2; max-width: 1160px; margin: -96px auto 0;
  padding: 0 clamp(20px, 3.5vw, 56px);
  display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: 24px;
}
.faq-topic {
  display: flex; flex-direction: column; gap: 8px; padding: 22px; text-align: left; cursor: pointer;
  font-family: var(--oj-font); color: var(--text);
  background: color-mix(in srgb, var(--acc) 28%, #fff);
  border: 2px solid var(--border-strong); border-radius: var(--r-lg);
  box-shadow: 0 12px 26px rgba(26, 26, 26, 0.16);
  transition: transform .2s var(--ease-pop), box-shadow .2s;
}
.faq-topic:nth-child(odd) { transform: rotate(-1.2deg); }
.faq-topic:nth-child(even) { transform: rotate(0.8deg); }
.faq-topic:hover, .faq-topic.on { transform: translateY(-4px) rotate(0deg); box-shadow: 0 18px 34px rgba(26, 26, 26, 0.2); }
.ftp-head { display: flex; align-items: center; justify-content: space-between; gap: 10px; }
.ftp-name { font-family: var(--oj-font); font-weight: 900; font-size: 18px; white-space: nowrap; }
.ftp-count {
  flex: none; width: 30px; height: 30px; display: grid; place-items: center;
  font-family: var(--oj-display); font-size: 13px; font-weight: 700; border-radius: 999px;
  background: #fff; border: 2px solid var(--border-strong);
}
.ftp-desc { font-size: 13px; font-weight: 500; line-height: 1.55; color: var(--text); }

/* ── 問題列表：主題 pills + 分組編號手風琴 ────── */
.faq-list {
  position: relative; z-index: 1; max-width: 1160px; margin: 0 auto;
  padding: 48px clamp(20px, 3.5vw, 56px) 24px;
  scroll-margin-top: calc(var(--nav-h) + 14px);
}
.faq-pills { display: flex; align-items: center; flex-wrap: wrap; gap: 10px; margin-bottom: 28px; }
.faq-pill {
  display: inline-flex; align-items: center; gap: 8px; padding: 8px 18px; cursor: pointer;
  font-family: var(--oj-font); font-weight: 900; font-size: 14px; color: var(--text);
  background: var(--surface); border: 2px solid var(--border-strong); border-radius: 999px;
  transition: transform .2s var(--ease-pop), box-shadow .2s, background .15s, color .15s;
}
.faq-pill:hover { transform: translateY(-2px); box-shadow: 0 6px 14px rgba(26, 26, 26, 0.15); }
.faq-pill.on { background: var(--text); color: var(--c-yellow); }
.fp-count {
  font-family: var(--oj-display); font-size: 11px; font-weight: 700; color: var(--text);
  background: var(--bg); border: 2px solid var(--border); border-radius: 999px; padding: 0 8px;
}
.faq-pill.on .fp-count { background: var(--c-yellow); border-color: var(--border-strong); }

/* 搜尋無結果 */
.faq-empty {
  display: flex; flex-direction: column; align-items: center; gap: 12px;
  padding: 48px 32px; text-align: center;
  background: var(--surface); border: 2px solid var(--border-strong); border-radius: var(--r-lg);
  box-shadow: var(--pop-2);
}
.fe-title { margin: 0; font-family: var(--oj-font); font-weight: 900; font-size: 20px; }
.fe-hint { margin: 0; font-size: 14px; font-weight: 500; color: var(--text-soft); }
.fe-clear {
  padding: 9px 24px; cursor: pointer;
  font-family: var(--oj-font); font-weight: 900; font-size: 14px; color: var(--text);
  background: var(--c-yellow); border: 2px solid var(--border-strong); border-radius: 999px;
  transition: transform .2s var(--ease-pop), box-shadow .2s;
}
.fe-clear:hover { transform: translateY(-2px); box-shadow: 0 6px 14px rgba(26, 26, 26, 0.2); }

/* 分組：標頭色籤 + 白紙卡 */
.faq-secs { display: flex; flex-direction: column; gap: 44px; }
.fs-head { display: flex; align-items: center; gap: 12px; margin-bottom: 16px; }
.fs-name {
  display: inline-block; padding: 6px 18px; transform: rotate(-1deg);
  font-family: var(--oj-font); font-weight: 900; font-size: 16px; color: var(--text);
  background: color-mix(in srgb, var(--acc) 40%, #fff);
  border: 2px solid var(--border-strong); border-radius: 999px; white-space: nowrap;
}
.fs-meta { font-size: 13px; font-weight: 700; color: var(--text-soft); }
.fs-card {
  list-style: none; margin: 0; padding: 0; overflow: hidden;
  background: var(--surface); border: 2px solid var(--border-strong); border-radius: var(--r-lg);
  box-shadow: 0 8px 20px rgba(26, 26, 26, 0.08);
}
.fs-item + .fs-item { border-top: 2px dashed var(--border); }
.fs-item.open { background: var(--bg); }
.fs-q {
  width: 100%; display: flex; align-items: center; gap: 16px; padding: 19px 24px;
  cursor: pointer; background: transparent; border: none; text-align: left;
  font-family: var(--oj-font);
  transition: background .15s ease;
}
.fs-item:not(.open) .fs-q:hover { background: var(--bg); }
.fs-num { flex: none; min-width: 24px; font-family: var(--oj-display); font-size: 13px; font-weight: 700; color: var(--text-faint); }
.fs-q-text { flex: 1; font-weight: 900; font-size: 16px; line-height: 1.5; color: var(--text); }
.fs-mark {
  flex: none; width: 32px; height: 32px; display: grid; place-items: center;
  font-size: 18px; font-weight: 900; line-height: 1; color: var(--text);
  background: color-mix(in srgb, var(--acc) 40%, #fff); border: 2px solid var(--border-strong); border-radius: 999px;
  transition: background .15s ease, color .15s ease, transform .2s var(--ease-pop);
}
.fs-item.open .fs-mark { background: var(--text); color: var(--c-yellow); }

/* 展開答案：白色便箋（縮排對齊問題文字），只用 opacity + transform 過場 */
.fs-a { margin: 0 24px 22px 64px; will-change: opacity, transform; }
.fs-a p {
  margin: 0; padding: 18px 22px;
  font-size: 14.5px; font-weight: 500; line-height: 1.8; color: var(--text); white-space: pre-wrap;
  background: #fff;
  border: 2px solid var(--border); border-radius: var(--r-md);
}
.faq-a-enter-active { transition: opacity .2s ease, transform .2s ease; }
.faq-a-leave-active { transition: opacity .13s ease, transform .13s ease; }
.faq-a-enter-from, .faq-a-leave-to { opacity: 0; transform: translateY(-6px); }

/* ── 聯絡 CTA：黃色滿版帶 + 貼紙裝飾 + 手寫註記 ────── */
.faq-cta {
  position: relative; overflow: hidden;
  margin: 48px calc(50% - 50vw) 0; padding: 80px 32px;
  background: var(--c-yellow); border-top: 2px solid var(--border-strong);
}
.fc-deco { position: absolute; border: 2px solid var(--border-strong); }
.fc-deco-a { top: 40px; left: 8%; width: 48px; height: 48px; border-radius: 14px; background: var(--c-lime); transform: rotate(12deg); }
.fc-deco-b { bottom: 44px; right: 10%; width: 40px; height: 40px; border-radius: 999px; background: var(--c-pink); transform: rotate(8deg); }
.fc-card {
  position: relative; max-width: 620px; margin: 0 auto; text-align: center;
  display: flex; flex-direction: column; align-items: center; gap: 16px;
}
.fc-title {
  margin: 0; font-family: var(--oj-font); font-weight: 900; color: var(--text);
  font-size: clamp(28px, 3.6vw, 40px); line-height: 1.35;
}
.fc-hl {
  display: inline-block; background: var(--text); color: var(--c-yellow);
  padding: 0 16px; border-radius: 14px; transform: rotate(-1.5deg); margin: 0 4px;
}
.fc-text { margin: 0; max-width: 44ch; font-size: 16px; font-weight: 700; line-height: 1.7; color: var(--text); }
.fc-actions { display: flex; flex-wrap: wrap; justify-content: center; gap: 16px; margin-top: 8px; }
.fc-btn {
  display: inline-flex; align-items: center; gap: 9px; padding: 15px 34px;
  font-family: var(--oj-font); font-weight: 900; font-size: 16px; text-decoration: none;
  border-radius: 999px;
  transition: transform .2s var(--ease-pop), box-shadow .2s;
}
.fc-btn:hover { text-decoration: none; }
.fc-btn-dark { background: var(--text); color: var(--c-yellow); }
.fc-btn-dark:hover { transform: translateY(-3px) rotate(-1deg); box-shadow: 0 12px 26px rgba(26, 26, 26, 0.3); color: var(--c-yellow); }
.fc-btn-light { background: #fff; color: var(--text); border: 2px solid var(--border-strong); }
.fc-btn-light:hover { transform: translateY(-3px) rotate(1deg); box-shadow: 0 10px 22px rgba(26, 26, 26, 0.18); color: var(--text); }
.fc-note {
  font-family: var(--oj-hand); font-weight: 700; font-size: 24px; color: var(--text);
  margin-top: 2px; transform: rotate(-2deg);
}

@media (prefers-reduced-motion: reduce) {
  .fh-search, .fh-clear, .faq-topic, .faq-pill, .fe-clear, .fs-q, .fs-mark, .fc-btn { transition: none; }
  .faq-a-enter-active, .faq-a-leave-active { transition: none; }
}

/* 窄螢幕：卡片改單欄堆疊、手風琴縮排收窄 */
@media (max-width: 640px) {
  .faq-hero { padding-bottom: 96px; }
  .faq-topics { margin-top: -64px; gap: 14px; }
  .faq-topic, .faq-topic:nth-child(odd), .faq-topic:nth-child(even) { transform: none; }
  .fh-deco { display: none; }
  .fs-q { gap: 12px; padding: 15px 16px; }
  .fs-q-text { font-size: 15px; }
  .fs-a { margin: 0 16px 16px; }
  .fc-deco { display: none; }
  .faq-cta { padding: 56px 20px; }
}
</style>
