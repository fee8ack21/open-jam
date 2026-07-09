<script setup lang="ts">
/* ============================================================
   FaqView — 常見問題頁（/faq）
   內容依「問題主題」分頁：全部 / 認識平台 / 購買與下載 /
   開店與上架 / 金流與結算。每則問答帶一個主題標籤（cat），
   「全部」分頁彙整所有項目並顯示主題 chip。
   問答為手風琴（accordion）展開。
   內容撈取 ContentService「已發布」的 FAQ（GET /v1/faqs/published，
   後台可維護）；撈取失敗或尚無資料時退回 i18n 靜態文案（faq.items）。
   ============================================================ */
import { ref, computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { useShopStore } from '@/stores/shop.js';
import { contentApi } from '@/api';
import { FaqCategory, type FaqItemDto } from '@/api/content-service';
import AppNav from '@/layout/AppNav.vue';
import AppFooter from '@/layout/AppFooter.vue';

const store = useShopStore();
const { t, tm, rt } = useI18n();

// 分頁：'all' 為彙整視角，其餘對應問答的 cat
const TAB_KEYS = ['all', 'platform', 'buying', 'selling', 'payments'] as const;
type TabKey = (typeof TAB_KEYS)[number];
const activeTab = ref<TabKey>('all');

interface FaqItem { i: number; cat: string; q: string; a: string; }

// FaqCategory（後端 enum）→ 分頁 cat key
const CAT_KEY: Record<FaqCategory, string> = {
  [FaqCategory.Platform]: 'platform',
  [FaqCategory.Buying]: 'buying',
  [FaqCategory.Selling]: 'selling',
  [FaqCategory.Payments]: 'payments',
};

// ── 已發布 FAQ（撈取失敗 / 尚無資料時 apiItems 為 null，退回 i18n 靜態文案）─────
const apiItems = ref<FaqItemDto[] | null>(null);

async function loadFaqs() {
  try {
    const res = await contentApi.faqs.getPublished();
    apiItems.value = res.data?.length ? res.data : null;
  } catch {
    apiItems.value = null;
  }
}
loadFaqs();

// 全部問答（帶原始索引，供手風琴開合狀態穩定對應）：優先用 API，否則退回 i18n
const items = computed<FaqItem[]>(() => {
  if (apiItems.value) {
    return apiItems.value.map((it, i) => ({
      i,
      cat: it.category ? (CAT_KEY[it.category] ?? 'platform') : 'platform',
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

const visible = computed<FaqItem[]>(() =>
  activeTab.value === 'all' ? items.value : items.value.filter((it) => it.cat === activeTab.value),
);

// 手風琴開合：以原始索引為 key
const open = ref<Set<number>>(new Set());
function toggle(i: number) {
  const next = new Set(open.value);
  next.has(i) ? next.delete(i) : next.add(i);
  open.value = next;
}
function selectTab(k: TabKey) {
  activeTab.value = k;
  open.value = new Set(); // 換分頁時收合，避免殘留展開狀態
}

// 每個主題一種便條紙色（書籤色標），對應下方問答卡片左緣色帶與主題 chip
const TAB_ACCENT: Record<TabKey, string> = {
  all: 'var(--c-violet)',
  platform: 'var(--c-cyan)',
  buying: 'var(--c-pink)',
  selling: 'var(--c-lime)',
  payments: '#f5a623',
};
function catColor(cat: string): string {
  return TAB_ACCENT[cat as TabKey] ?? 'var(--c-violet)';
}
// 書籤上的計數（該主題的問答則數）
function tabCount(k: TabKey): number {
  return k === 'all' ? items.value.length : items.value.filter((it) => it.cat === k).length;
}
</script>

<template>
  <div class="oj-root" :class="'font-' + store.font" :data-screen-label="t('faq.screenLabel')">
    <!-- ============ NAV ============ -->
    <app-nav />

    <main class="page faq-page">
      <!-- 背景貫穿細格線 + 縷空大字（全頁氛圍層，比照 AboutView 第一個區塊） -->
      <div class="faq-gridlines" aria-hidden="true"><i v-for="i in 4" :key="i"></i></div>
      <span class="faq-bigword" aria-hidden="true">FAQ</span>

      <!-- ============ 頁首：區塊主標題 ============ -->
      <header class="faq-head">
        <p class="faq-eyebrow"><app-icon name="sparkle" :size="13" /> {{ t('faq.eyebrow') }}</p>
        <h1 class="faq-title">{{ t('faq.title') }}</h1>
        <p class="faq-lede">{{ t('faq.lede') }}</p>
      </header>

      <div class="faq-body">
        <!-- ── 手機板：以下拉選單取代頁籤 ── -->
        <select
          class="faq-select"
          :value="activeTab"
          :aria-label="t('faq.title')"
          @change="selectTab(($event.target as HTMLSelectElement).value as TabKey)"
        >
          <option v-for="k in TAB_KEYS" :key="k" :value="k">{{ t('faq.tabs.' + k) }}（{{ tabCount(k) }}）</option>
        </select>

        <!-- ── 左側書籤：垂直分頁列（便條紙 / 索引標籤風） ── -->
        <nav class="faq-rail" role="tablist" :aria-label="t('faq.title')">
          <button
            v-for="k in TAB_KEYS"
            :key="k"
            type="button"
            role="tab"
            class="faq-tab"
            :class="{ on: activeTab === k }"
            :style="{ '--acc': TAB_ACCENT[k] }"
            :aria-selected="activeTab === k"
            @click="selectTab(k)"
          >
            <span class="ft-label">{{ t('faq.tabs.' + k) }}</span>
            <span class="ft-count">{{ tabCount(k) }}</span>
          </button>
        </nav>

        <!-- ── 右側問答手風琴 ── -->
        <ul class="faq-list">
          <li
            v-for="it in visible"
            :key="it.i"
            class="faq-item"
            :class="{ open: open.has(it.i) }"
            :style="{ '--acc': catColor(it.cat) }"
          >
            <button
              type="button"
              class="faq-q"
              :aria-expanded="open.has(it.i)"
              @click="toggle(it.i)"
            >
              <span class="faq-q-text">
                <span v-if="activeTab === 'all'" class="faq-chip">{{ t('faq.tabs.' + it.cat) }}</span>
                {{ it.q }}
              </span>
              <app-icon name="chevron" :size="18" class="faq-caret" />
            </button>
            <transition name="faq-a">
              <div v-if="open.has(it.i)" class="faq-a"><p>{{ it.a }}</p></div>
            </transition>
          </li>
        </ul>
      </div>
    </main>

    <!-- ============ FOOTER ============ -->
    <app-footer />
  </div>
</template>

<style scoped>
/* 整個頁面視為 AboutView 的第一個區塊：滿版（不困在 960 container），
   大字與頁首隨頁面滿版定位，僅內文區塊自行收斂寬度 —— 水平位置比照 About 第一區塊。
   （.page 已提供 max-width:none + padding: 0 clamp(20px,3.5vw,56px)） */
.faq-page { position: relative; padding-top: 30px; padding-bottom: 80px; }

/* 背景貫穿細格線（Begonia 式 frame）：整頁氛圍層，固定滿版 */
.faq-gridlines { position: fixed; inset: 0; z-index: 0; pointer-events: none; display: flex; }
.faq-gridlines i { flex: 1; border-right: 1px solid rgba(26, 22, 38, .05); }
.faq-gridlines i:last-child { border-right: none; }

/* 縷空大字（描邊空心）：比照 About 第一區塊，滿版左偏 6% */
.faq-bigword {
  position: absolute; z-index: 0; top: 40px; left: 6%;
  pointer-events: none; user-select: none;
  font-family: var(--oj-display); font-weight: 800; line-height: 1; white-space: nowrap;
  letter-spacing: -0.02em; color: transparent;
  -webkit-text-stroke: 2px rgba(26, 22, 38, .13);
  font-size: clamp(90px, 14vw, 210px);
}

/* ── 頁首：區塊主標題（抬到大字 / 格線之上） ── */
.faq-head {
  position: relative; z-index: 1; text-align: center;
  padding: clamp(28px, 5vh, 56px) 20px clamp(30px, 5vh, 48px);
  margin-bottom: 38px;
}
/* 麵包屑與內文主體：收斂置中欄（比照 AboutView Discover 區塊內容寬 1120）；頁首則隨頁面滿版置中文字 */
.faq-page .breadcrumb,
.faq-body { position: relative; z-index: 1; max-width: 1120px; margin-left: auto; margin-right: auto; }
.faq-eyebrow {
  display: inline-flex; align-items: center; justify-content: center; gap: 7px; margin: 0 0 12px;
  font-family: var(--oj-mono); font-size: 12px; letter-spacing: 1.5px; text-transform: uppercase;
  color: var(--oj-primary);
}
.faq-title {
  margin: 0; font-family: var(--oj-display); font-weight: 800;
  font-size: clamp(28px, 4vw, 44px); letter-spacing: -1.2px; color: var(--text);
}
.faq-lede {
  margin: 16px auto 0; max-width: 54ch;
  font-size: 15.5px; line-height: 1.8; color: var(--text-soft);
}

/* ── 一本便條本：左側彩色便條籤 + 右側紙頁，同屬一個區塊 ────
   便條籤（tab）為彩色便利貼，掛在紙頁左緣；翻開的那張攤平成
   與紙頁同色、蓋過欄位分隔線，讓頁籤與內容連成同一張紙。 */
/* ── 版面：左欄只顯示頁籤（無背景），右欄為紙頁 ──────────────
   頁籤（tab）為彩色便條籤，右緣塞入紙頁邊線下、只露出左半；
   翻開的那張浮到最前、底色與紙頁同色蓋過邊線，與內容連成同一張紙。 */
.faq-body {
  --paper: #ffffff;                    /* 紙頁白底 */
  --paper-line: rgba(60, 50, 90, .11); /* 問答間橫格線 */
  position: relative;
  /* 頁籤欄 + 紙頁並列於同一 grid，整體收在 container 內（不再溢出）；
     頁籤右緣仍以負 margin 塞入紙頁邊線下，維持便條本相連視覺 */
  display: grid; grid-template-columns: 148px minmax(0, 1fr); align-items: start;
}

/* 左側頁籤欄：grid 第一欄，只顯示彩色頁籤；首個頁籤對齊上緣 */
.faq-rail {
  grid-column: 1;
  display: flex; flex-direction: column; gap: 9px;
}

/* 手機板分類下拉（預設隱藏，僅窄螢幕顯示） */
.faq-select {
  display: none; width: 100%; margin-bottom: 14px; padding: 12px 40px 12px 15px;
  font-family: var(--oj-display); font-weight: 700; font-size: 15px; color: var(--text);
  background-color: var(--surface);
  background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='16' height='16' viewBox='0 0 24 24' fill='none' stroke='%231a1626' stroke-width='2.5' stroke-linecap='round' stroke-linejoin='round'%3E%3Cpath d='M6 9l6 6 6-6'/%3E%3C/svg%3E");
  background-repeat: no-repeat; background-position: right 14px center; background-size: 16px;
  border: 1.5px solid var(--border-strong); border-radius: var(--r-md); box-shadow: var(--pop-2);
  appearance: none; -webkit-appearance: none; cursor: pointer;
}
/* 每個 tab = 一張彩色便條籤 */
.faq-tab {
  position: relative; z-index: 0; display: flex; align-items: center; gap: 10px; text-align: left;
  cursor: pointer; padding: 11px 13px 11px 17px;
  background: color-mix(in srgb, var(--acc) 22%, #fffef8); color: var(--text);
  border: 1.5px solid var(--border-strong); border-radius: 11px 0 0 11px; overflow: hidden;
  box-shadow: 2px 2px 0 rgba(26, 22, 38, .16);
  margin-right: -1.5px;                 /* 右緣塞入紙頁邊線下（stretch 錨定右緣） */
  font-family: var(--oj-display); font-weight: 700; font-size: 14.5px;
  transition: margin .15s ease, box-shadow .15s ease, background .16s ease;
}
/* 便條左緣色標「書脊」 */
.faq-tab::before {
  content: ''; position: absolute; left: 0; top: 8px; bottom: 8px; width: 5px;
  background: var(--acc); border-radius: 3px; transition: top .15s ease, bottom .15s ease;
}
/* hover：往左抽出（右緣仍塞在紙頁邊線下、保持相連） */
.faq-tab:not(.on):hover { margin-left: -4px; box-shadow: 3px 3px 0 rgba(26, 22, 38, .2); }
/* 選中：浮到最前、攤平成與紙頁同色、去除右緣邊線 → 與內容連成同一張紙 */
.faq-tab.on {
  z-index: 2; background: var(--paper); box-shadow: none; transform: none;
  border-right-color: var(--paper);
}
.faq-tab.on::before { top: 0; bottom: 0; border-radius: 0; }   /* 色標拉滿貼合紙頁 */
.ft-label { flex: 1; }
.ft-count {
  flex: none; font-family: var(--oj-mono); font-weight: 700; font-size: 11px;
  min-width: 22px; text-align: center; padding: 2px 6px; border-radius: 999px;
  color: var(--text); background: rgba(255, 255, 255, .5); border: 1.5px solid var(--border-strong);
}
.faq-tab.on .ft-count { background: color-mix(in srgb, var(--acc) 18%, #fff); }

/* ── 右側紙頁：白底紙卡 + 硬陰影，問答如寫在頁上、以橫格線分隔 ── */
.faq-list {
  position: relative; z-index: 1;
  list-style: none; margin: 0; padding: 10px 22px; display: flex; flex-direction: column;
  background-color: var(--paper);
  border: 1.5px solid var(--border-strong); border-radius: 0 var(--r-lg) var(--r-lg) var(--r-lg);
  box-shadow: var(--pop-3);
}
.faq-item {
  position: relative; background: transparent; border: none; border-radius: 10px;
  transition: background .16s ease;
}
.faq-item + .faq-item { border-top: 1.5px solid var(--paper-line); }
/* 開合時整則便利貼式螢光筆highlight，並在左緣露出主題色標記 */
.faq-item:hover { background: color-mix(in srgb, var(--acc) 7%, transparent); }
.faq-item.open { background: color-mix(in srgb, var(--acc) 11%, transparent); }
.faq-item.open::before {
  content: ''; position: absolute; left: 2px; top: 12px; bottom: 12px; width: 4px;
  border-radius: 2px; background: var(--acc);
}
.faq-q {
  width: 100%; display: flex; align-items: center; justify-content: space-between; gap: 14px;
  cursor: pointer; background: none; border: none; text-align: left; padding: 16px 18px;
  font-family: var(--oj-display); font-weight: 700; font-size: 15.5px; color: var(--text); line-height: 1.45;
}
.faq-q-text { display: flex; align-items: center; gap: 10px; flex-wrap: wrap; }
.faq-chip {
  font-family: var(--oj-mono); font-size: 10px; letter-spacing: .06em; text-transform: uppercase;
  color: var(--text); background: color-mix(in srgb, var(--acc) 20%, var(--surface)); border: 1px solid var(--acc);
  padding: 2px 8px; border-radius: 999px; white-space: nowrap;
}
.faq-caret { flex: none; color: var(--text-soft); transform: rotate(90deg); transition: transform .25s ease; }
.faq-item.open .faq-caret { transform: rotate(-90deg); }

/* 展開內容：只用 opacity + transform 過場（合成器層，不觸發逐格 reflow，
   因此不會卡頓）；卡片高度在節點插入 / 移除時一次到位。 */
.faq-a { will-change: opacity, transform; }
.faq-a p { margin: 0; padding: 2px 18px 18px; font-size: 14.5px; line-height: 1.8; color: var(--text-soft); white-space: pre-wrap; }
.faq-a-enter-active { transition: opacity .2s ease, transform .2s ease; }
.faq-a-leave-active { transition: opacity .13s ease, transform .13s ease; }
.faq-a-enter-from, .faq-a-leave-to { opacity: 0; transform: translateY(-6px); }

@media (prefers-reduced-motion: reduce) {
  .faq-caret, .faq-item, .faq-tab { transition: none; }
  .faq-a-enter-active, .faq-a-leave-active { transition: none; }
}

/* 窄螢幕：側邊頁籤欄收起，改為最上方下拉選單（單欄堆疊）。
   主區塊無頁籤相接，左上角恢復圓角。 */
@media (max-width: 860px) {
  .faq-body { grid-template-columns: 1fr; }
  .faq-rail { display: none; }
  .faq-select { display: block; }
  .faq-list { border-top-left-radius: var(--r-lg); }
}
@media (max-width: 560px) {
  .faq-q { font-size: 14.5px; padding: 14px 15px; }
}
</style>
