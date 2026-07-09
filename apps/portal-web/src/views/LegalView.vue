<script setup lang="ts">
/* ============================================================
   LegalView — 服務條款 / 隱私政策頁（/terms、/privacy）
   兩份文件合併於同一頁，以分頁（tab）切換，切換時同步更新網址
   （/terms ↔ /privacy）供分享與外部書籤相容。
   內容撈取 ContentService「目前啟用中」的法律文件版本
   （GET /v1/legal-documents/active，後台可編輯 / 換版）；
   撈取失敗時退回 i18n 靜態文案（src/data/legal.ts 結構）。
   ============================================================ */
import { computed, ref, watch } from 'vue';
import { useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { useShopStore } from '@/stores/shop.js';
import { LEGAL_META, type LegalKey } from '@/data/legal.js';
import { contentApi } from '@/api';
import { LegalDocumentType, type LegalDocumentDto } from '@/api/content-service';
import AppNav from '@/layout/AppNav.vue';
import AppFooter from '@/layout/AppFooter.vue';

const props = defineProps<{ doc: LegalKey }>();

const store = useShopStore();
const router = useRouter();
const { t, tm, rt } = useI18n();

interface Section {
  n: string;
  h: string;
  p: string;
  list?: string[];
}

const TABS: LegalKey[] = ['terms', 'privacy'];

const typeOf: Record<LegalKey, LegalDocumentType> = {
  terms: LegalDocumentType.TermsOfService,
  privacy: LegalDocumentType.PrivacyPolicy,
};

// ── 各分頁啟用中版本（依 doc 類型撈取並快取；null 表示尚未載到 / 撈取失敗）───
const loaded = ref<Record<LegalKey, LegalDocumentDto | null>>({ terms: null, privacy: null });
const fetched = ref<Record<LegalKey, boolean>>({ terms: false, privacy: false });

async function loadActive(key: LegalKey) {
  if (fetched.value[key]) return; // 已撈過（含失敗）即不重撈
  try {
    const res = await contentApi.legalDocuments.getActive({ type: typeOf[key] });
    loaded.value[key] = res.data?.[0] ?? null;
  } catch {
    loaded.value[key] = null; // 撈取失敗：退回 i18n 靜態文案
  } finally {
    fetched.value[key] = true;
  }
}
watch(() => props.doc, (d) => loadActive(d), { immediate: true });

function selectTab(key: LegalKey) {
  if (key !== props.doc) router.replace('/' + key); // 同步網址、驅動 doc prop 切換
}

const activeDoc = computed(() => loaded.value[props.doc]);

// 分頁標題：優先用啟用中版本標題，無則退回 i18n
function tabTitle(key: LegalKey): string {
  return loaded.value[key]?.title ?? t(`legal.${key}.title`);
}
const title = computed(() => tabTitle(props.doc));
const updatedDate = computed(() => {
  const at = activeDoc.value?.activatedAt;
  return at ? at.slice(0, 10) : LEGAL_META[props.doc].updated;
});

/** 將文件純文字內容解析為章節：「## 」＝章節標題、「- 」＝列點、其餘為段落（與 Auth 渲染慣例一致）。 */
function parseSections(content: string): Section[] {
  const sections: Section[] = [];
  let current: Section | null = null;
  for (const raw of content.replace(/\r\n/g, '\n').split('\n')) {
    const line = raw.trim();
    if (!line) continue;
    if (line.startsWith('## ')) {
      current = { n: String(sections.length + 1).padStart(2, '0'), h: line.slice(3).trim(), p: '' };
      sections.push(current);
    } else if (line.startsWith('- ') && current) {
      (current.list ??= []).push(line.slice(2).trim());
    } else if (current) {
      current.p = current.p ? `${current.p}\n${line}` : line;
    } else {
      current = { n: String(sections.length + 1).padStart(2, '0'), h: '', p: line };
      sections.push(current);
    }
  }
  return sections;
}

// 章節：優先用啟用中版本內容，無則退回 i18n（legal.<doc>.sections）
const sections = computed<Section[]>(() => {
  if (activeDoc.value?.content) return parseSections(activeDoc.value.content);
  return (tm(`legal.${props.doc}.sections`) as { h: string; p: string; list?: string[] }[]).map(
    (s, i) => ({
      n: String(i + 1).padStart(2, '0'),
      h: rt(s.h),
      p: rt(s.p),
      list: s.list ? s.list.map((item) => rt(item)) : undefined,
    }),
  );
});
</script>

<template>
  <div class="oj-root" :class="'font-' + store.font" :data-screen-label="title">
    <!-- ============ NAV ============ -->
    <app-nav />

    <main class="page legal-page">
      <!-- 全頁氛圍：紙質顆粒 + 貫穿格線（比照 AboutView 開場區塊） -->
      <div class="l-grain" aria-hidden="true"></div>
      <div class="l-gridlines" aria-hidden="true"><i v-for="i in 4" :key="i"></i></div>

      <!-- ── 開場：縷空大字 + 區塊主標題（比照 AboutView 開場區塊） ── -->
      <section class="l-intro legal-intro">
        <span class="l-bigword li-bigword" aria-hidden="true">LEGAL</span>
        <p class="lsec-eyebrow li-eyebrow"><app-icon name="sparkle" :size="13" /> {{ t('legal.eyebrow') }}</p>
        <h1 class="li-title">{{ t('legal.heroTitle') }}</h1>
        <p class="li-lede">{{ t('legal.lede') }}</p>
      </section>

      <!-- 內文置中容器（比照 AboutView：hero 滿版、內文限寬置中） -->
      <div class="legal-body">
        <!-- ── 分頁：服務條款 / 隱私政策 ── -->
        <div class="legal-tabs" role="tablist" :aria-label="t('legal.tabsLabel')">
          <button
            v-for="k in TABS"
            :key="k"
            type="button"
            role="tab"
            class="legal-tab"
            :class="[k, { on: doc === k }]"
            :aria-selected="doc === k"
            @click="selectTab(k)"
          >
            <span class="lt-badge"><app-icon :name="LEGAL_META[k].icon" :size="17" /></span>
            <span class="lt-label">{{ tabTitle(k) }}</span>
          </button>
        </div>

        <article class="legal-card">
          <header class="legal-head">
            <h2 class="legal-title">{{ title }}</h2>
            <p class="legal-meta">
              {{ t('legal.metaUpdated', { date: updatedDate }) }}<template v-if="activeDoc"> · v{{ activeDoc.version }}</template>
            </p>
          </header>

          <section v-for="s in sections" :key="s.n" class="legal-sec">
            <h3 v-if="s.h"><span class="num">{{ s.n }}</span> {{ s.h }}</h3>
            <p style="white-space: pre-wrap;">{{ s.p }}</p>
            <ul v-if="s.list">
              <li v-for="(item, i) in s.list" :key="i">{{ item }}</li>
            </ul>
          </section>
        </article>
      </div>
    </main>

    <!-- ============ FOOTER ============ -->
    <app-footer />
  </div>
</template>

<style scoped>
/* 頁面滿版（覆蓋 .page 左右 gutter）：hero 與縷空大字得以貼齊 viewport，比照 AboutView */
.legal-page { position: relative; max-width: none; padding: 0 0 80px; }
.legal-intro,
.legal-tabs,
.legal-card { position: relative; z-index: 1; }

/* 內文限寬置中（分頁 + 內容卡），比照 AboutView Workflow 區塊內容寬 1200 */
.legal-body { max-width: 1200px; margin: 0 auto; padding: 0 clamp(20px, 3.5vw, 56px); }

/* ---------- 全頁氛圍：紙質顆粒 + 貫穿細格線（比照 AboutView） ---------- */
.l-grain {
  position: fixed; inset: 0; z-index: 90; pointer-events: none; opacity: .05;
  background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='260' height='260'%3E%3Cfilter id='n'%3E%3CfeTurbulence type='fractalNoise' baseFrequency='0.85' numOctaves='2' stitchTiles='stitch'/%3E%3C/filter%3E%3Crect width='260' height='260' filter='url(%23n)'/%3E%3C/svg%3E");
  background-size: 260px 260px;
}
.l-gridlines { position: fixed; inset: 0; z-index: 0; pointer-events: none; display: flex; }
.l-gridlines i { flex: 1; border-right: 1px solid rgba(26,22,38,.05); }
.l-gridlines i:last-child { border-right: none; }

/* ---------- 開場：縷空大字 + 區塊主標題（比照 AboutView 開場區塊） ---------- */
.legal-intro {
  position: relative; text-align: center;
  padding: clamp(44px, 7vh, 88px) clamp(20px, 3.5vw, 56px) clamp(36px, 5vh, 64px);
}
.l-bigword {
  position: absolute; z-index: 0; pointer-events: none; user-select: none;
  font-family: var(--oj-display); font-weight: 800; line-height: 1; white-space: nowrap;
  letter-spacing: -0.02em; color: transparent;
  -webkit-text-stroke: 2px rgba(26,22,38,.13);
}
.li-bigword { top: .08em; left: 6%; font-size: clamp(90px, 14vw, 210px); }
.lsec-eyebrow {
  display: inline-flex; align-items: center; gap: 7px; margin: 0 0 12px;
  font-family: var(--oj-mono); font-size: 12px; letter-spacing: 1.5px; text-transform: uppercase;
  color: var(--oj-primary);
}
.li-eyebrow, .li-title, .li-lede { position: relative; z-index: 1; }
.li-eyebrow { justify-content: center; }
.li-title {
  margin: 0 auto; white-space: nowrap;
  font-family: var(--oj-display); font-weight: 800;
  font-size: clamp(26px, 4.6vw, 46px); line-height: 1.15; letter-spacing: -1.4px; color: var(--text);
}
.li-lede {
  margin: 16px auto 0; max-width: 54ch;
  font-size: 15.5px; line-height: 1.8; color: var(--text-soft);
}

/* ── 分頁列：兩份文件並列，選中者向下貼合內容卡 ─────────── */
.legal-tabs { display: flex; gap: 8px; position: relative; z-index: 1; }
.legal-tab {
  display: inline-flex; align-items: center; gap: 9px; cursor: pointer;
  padding: 11px 18px; background: var(--surface-2);
  border: 1.5px solid var(--border-strong); border-bottom: none;
  border-radius: 12px 12px 0 0;
  font-family: var(--oj-display); font-weight: 700; font-size: 14.5px; color: var(--text-soft);
  transition: background .16s ease, color .16s ease, transform .16s ease;
}
.legal-tab:hover { color: var(--text); transform: translateY(-1px); }
.legal-tab.on:hover { transform: none; }
.legal-tab.on {
  background: var(--surface); color: var(--text);
  margin-bottom: -1.5px; /* 蓋過內容卡上緣線，連成同一區塊 */
  padding-bottom: 12.5px;
}
.lt-badge {
  width: 26px; height: 26px; border-radius: 8px; display: grid; place-items: center; color: #fff;
  border: 1.5px solid var(--text); flex: none;
}
.legal-tab.terms .lt-badge { background: linear-gradient(135deg, var(--c-violet), var(--c-pink)); }
.legal-tab.privacy .lt-badge { background: linear-gradient(135deg, var(--c-cyan), var(--c-violet)); }

.legal-card {
  position: relative;
  background: var(--surface); border: 1.5px solid var(--border-strong); border-radius: 0 var(--r-lg) var(--r-lg) var(--r-lg);
  box-shadow: 6px 6px 0 var(--border); padding: 30px 40px 40px;
}

.legal-head { border-bottom: 1.5px solid var(--border); padding-bottom: 24px; margin-bottom: 28px; }
.legal-title { font-family: var(--oj-display); font-weight: 800; font-size: 32px; letter-spacing: -1px; margin: 0; color: var(--text); }
.legal-meta { font-family: var(--oj-mono); font-size: 11.5px; letter-spacing: .06em; text-transform: uppercase; color: var(--text-faint); margin: 9px 0 0; }

.legal-sec { margin-bottom: 26px; }
.legal-sec h3 { font-family: var(--oj-display); font-weight: 700; font-size: 18px; color: var(--text); margin: 0 0 9px; display: flex; align-items: baseline; gap: 10px; }
.legal-sec h3 .num { font-family: var(--oj-mono); font-size: 13px; color: var(--oj-primary); font-weight: 600; }
.legal-sec p { margin: 0; font-size: 15px; line-height: 1.78; color: var(--text-soft); }
.legal-sec ul { margin: 10px 0 0; padding-left: 22px; }
.legal-sec li { font-size: 14.5px; line-height: 1.75; color: var(--text-soft); margin-bottom: 5px; }

@media (max-width: 560px) {
  .legal-tab { padding: 10px 13px; font-size: 13.5px; }
  .legal-tab .lt-label { white-space: nowrap; }
  .legal-card { padding: 24px 20px 30px; }
  .legal-title { font-size: 26px; }
}
</style>
