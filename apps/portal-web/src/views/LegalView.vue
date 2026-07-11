<script setup lang="ts">
/* ============================================================
   LegalView — 服務條款 / 隱私政策頁（/terms、/privacy）
   版面結構：Hero 漸層色帶（版本 meta pill）→ 資料夾式頁籤 +
   內容卡（重點速覽卡片格 + sticky 目錄 + 編號章節）→ 聯絡 CTA。
   兩份文件合併於同一頁，以分頁（tab）切換，切換時同步更新網址
   （/terms ↔ /privacy）供分享與外部書籤相容。
   內容撈取 ContentService「目前啟用中」的法律文件版本
   （GET /v1/legal-documents/active，後台可編輯 / 換版）；
   撈取失敗時退回 i18n 靜態文案。重點速覽為頁面層摘要，一律取
   i18n 靜態文案（legal.<doc>.tldr），不隨後台內容變動。
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

// 便條紙色盤：重點速覽卡與章節編號章依索引輪替取色（同 FaqView）
const PALETTE = ['var(--c-cyan)', 'var(--c-pink)', 'var(--c-lime)', '#f5a623', 'var(--c-violet)'];
function accentOf(i: number): string {
  return PALETTE[i % PALETTE.length];
}

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

// 重點速覽（i18n 靜態摘要，不隨後台內容變動）
const tldr = computed<{ t: string; d: string }[]>(() =>
  (tm(`legal.${props.doc}.tldr`) as { t: string; d: string }[]).map((c) => ({ t: rt(c.t), d: rt(c.d) })),
);

// 目錄：取有標題的章節；點擊捲至對應章節
const toc = computed(() => sections.value.filter((s) => s.h));
function jumpTo(n: string) {
  document.getElementById('legal-sec-' + n)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
}
</script>

<template>
  <div class="oj-root" :class="'font-' + store.font" :data-screen-label="title">
    <!-- ============ NAV ============ -->
    <app-nav />

    <main class="page legal-page">
      <!-- ============ HERO：漸層色帶（比照 FaqView / Discover hero） ============ -->
      <section class="legal-hero">
        <span class="lh-word" aria-hidden="true">LEGAL</span>
        <div class="lh-inner">
          <p class="lh-badge"><app-icon name="sparkle" :size="13" /> {{ t('legal.eyebrow') }}</p>
          <i18n-t keypath="legal.heroTitle" tag="h1" class="lh-title" scope="global">
            <template #mark><span class="hl hl-lime">{{ t('legal.heroTitleMark') }}</span></template>
          </i18n-t>
          <p class="lh-lede">{{ t('legal.lede') }}</p>
          <p class="lh-meta">
            {{ t('legal.metaUpdated', { date: updatedDate }) }}<template v-if="activeDoc"> · v{{ activeDoc.version }}</template>
          </p>
        </div>
      </section>

      <!-- ============ 頁籤 + 內容卡（疊在色帶下緣） ============ -->
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
          <!-- ── 重點速覽：30 秒摘要卡片格 ── -->
          <section class="legal-tldr" :aria-label="t('legal.tldrLabel')">
            <p class="ltd-label"><app-icon name="sparkle" :size="13" /> {{ t('legal.tldrLabel') }}</p>
            <div class="ltd-grid">
              <div v-for="(c, i) in tldr" :key="c.t" class="ltd-card" :style="{ '--acc': accentOf(i) }">
                <span class="ltd-title">{{ c.t }}</span>
                <span class="ltd-desc">{{ c.d }}</span>
              </div>
            </div>
          </section>

          <div class="legal-grid">
            <!-- ── 目錄：sticky 章節導覽 ── -->
            <nav class="legal-toc" :aria-label="t('legal.tocLabel')">
              <span class="toc-label">{{ t('legal.tocLabel') }}</span>
              <button v-for="s in toc" :key="s.n" type="button" class="toc-entry" @click="jumpTo(s.n)">
                <span class="toc-num">{{ s.n }}</span>
                <span>{{ s.h }}</span>
              </button>
            </nav>

            <!-- ── 內文：文件標題 + 編號章節 ── -->
            <div class="legal-doc">
              <header class="legal-head">
                <h2 class="legal-title">{{ title }}</h2>
                <p class="legal-meta">
                  {{ t('legal.metaUpdated', { date: updatedDate }) }}<template v-if="activeDoc"> · v{{ activeDoc.version }}</template>
                </p>
              </header>

              <section
                v-for="(s, i) in sections"
                :id="'legal-sec-' + s.n"
                :key="s.n"
                class="legal-sec"
                :style="{ '--acc': accentOf(i) }"
              >
                <h3 v-if="s.h"><span class="num">{{ s.n }}</span>{{ s.h }}</h3>
                <p class="sec-body">{{ s.p }}</p>
                <ul v-if="s.list">
                  <li v-for="(item, j) in s.list" :key="j"><i class="li-mark" aria-hidden="true"></i><span>{{ item }}</span></li>
                </ul>
              </section>
            </div>
          </div>
        </article>
      </div>

      <!-- ============ 聯絡 CTA ============ -->
      <section class="legal-cta">
        <div class="fc-card">
          <i class="fc-deco fc-deco-a" aria-hidden="true"></i>
          <i class="fc-deco fc-deco-b" aria-hidden="true"></i>
          <h2 class="fc-title">{{ t('legal.cta.title') }}</h2>
          <p class="fc-text">{{ t('legal.cta.text') }}</p>
          <div class="fc-actions">
            <a class="fc-btn fc-btn-light" href="mailto:support@openjam.co">
              <app-icon name="mail" :size="16" /> {{ t('legal.cta.contact') }}
            </a>
            <router-link class="fc-btn fc-btn-light" to="/faq">
              <app-icon name="chevron" :size="15" /> {{ t('legal.cta.faq') }}
            </router-link>
          </div>
        </div>
      </section>
    </main>

    <!-- ============ FOOTER ============ -->
    <app-footer />
  </div>
</template>

<style scoped>
/* 頁面滿版（覆蓋 .page 左右 gutter）：hero 色帶貼齊 viewport，比照 FaqView */
.legal-page { position: relative; max-width: none; padding: 0; }

/* ── Hero 漸層色帶：滿版飽和漸層帶 + 半調網點（同 .mkt-hero 配方）────── */
.legal-hero { position: relative; padding: clamp(48px, 7vh, 64px) clamp(20px, 3.5vw, 56px) clamp(76px, 10vh, 96px); text-align: center; }
.legal-hero::before, .legal-hero::after {
  content: ''; position: absolute; top: 0; bottom: 0; left: 50%;
  width: 100vw; transform: translateX(-50%); z-index: 0; pointer-events: none;
}
.legal-hero::before {
  background:
    radial-gradient(620px 460px at 12% 6%, rgba(255,200,58,.40), transparent 58%),
    radial-gradient(680px 520px at 92% 16%, rgba(255,77,157,.52), transparent 60%),
    radial-gradient(720px 620px at 78% 98%, rgba(31,214,198,.40), transparent 62%),
    radial-gradient(820px 720px at 16% 94%, rgba(108,76,241,.55), transparent 64%),
    linear-gradient(150deg, #6c4cf1, #8a3df1 46%, #c33ad6);
  border-bottom: 1.5px solid var(--text);
}
.legal-hero::after {
  background-image: radial-gradient(rgba(255,255,255,.55) 1.5px, transparent 1.7px);
  background-size: 18px 18px; opacity: .15; mix-blend-mode: soft-light;
}

/* 色帶內的縷空大字（白色微透，靠左與 nav logo 切齊、沉在置中內容後；
   top 為距 header 固定間距，三頁一致，以本頁原間距為準） */
.lh-word {
  position: absolute; z-index: 1; top: 88px; left: clamp(20px, 3.5vw, 56px);
  font-family: var(--oj-display); font-weight: 800; line-height: 1;
  font-size: clamp(110px, 16vw, 210px); letter-spacing: .04em;
  color: rgba(255, 255, 255, .09); pointer-events: none; user-select: none;
}

.lh-inner {
  position: relative; z-index: 2; max-width: 780px; margin: 0 auto;
  display: flex; flex-direction: column; align-items: center; gap: 18px;
}
.lh-badge {
  display: inline-flex; align-items: center; gap: 7px; margin: 0; padding: 6px 14px;
  font-family: var(--oj-mono); font-size: 12px; letter-spacing: .08em; text-transform: uppercase;
  color: #fff; background: rgba(255,255,255,.16);
  border: 1.5px solid rgba(255,255,255,.28); border-radius: 999px; backdrop-filter: blur(4px);
}
.lh-title {
  margin: 0; font-family: var(--oj-display); font-weight: 800; color: #fff;
  font-size: clamp(28px, 4.4vw, 48px); line-height: 1.2; letter-spacing: -1.2px;
}
.lh-title .hl { box-shadow: 3px 3px 0 rgba(26,22,38,.45); }
.lh-lede { margin: 0; max-width: 54ch; font-size: 15.5px; line-height: 1.8; color: rgba(255,255,255,.88); }
.lh-meta {
  display: inline-flex; align-items: center; margin: 0; padding: 5px 14px;
  font-family: var(--oj-mono); font-size: 12px; font-weight: 600; letter-spacing: .06em;
  color: rgba(255,255,255,.9); background: rgba(26,22,38,.35);
  border: 1.5px solid var(--border-strong); border-radius: 999px;
}

/* ── 頁籤 + 內容卡：疊在色帶下緣 ────── */
.legal-body { position: relative; z-index: 2; max-width: 1080px; margin: -36px auto 0; padding: 0 clamp(20px, 3.5vw, 56px); }

/* 分頁列：兩份文件並列，選中者向下貼合內容卡 */
.legal-tabs { display: flex; gap: 8px; position: relative; z-index: 1; }
.legal-tab {
  display: inline-flex; align-items: center; gap: 9px; cursor: pointer;
  padding: 12px 20px; background: var(--surface-2);
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
  padding-bottom: 13.5px;
}
.lt-badge {
  width: 26px; height: 26px; border-radius: 8px; display: grid; place-items: center; color: #fff;
  border: 1.5px solid var(--text); flex: none;
}
.legal-tab.terms .lt-badge { background: linear-gradient(135deg, var(--c-violet), var(--c-pink)); }
.legal-tab.privacy .lt-badge { background: linear-gradient(135deg, var(--c-cyan), var(--c-violet)); }

.legal-card {
  position: relative; z-index: 0;
  background: var(--surface); border: 1.5px solid var(--border-strong);
  border-radius: 0 var(--r-lg) var(--r-lg) var(--r-lg);
  box-shadow: 6px 6px 0 var(--border-strong); padding: 34px 40px 44px;
}

/* ── 重點速覽：30 秒摘要卡片格 ────── */
.legal-tldr { margin-bottom: 34px; }
.ltd-label {
  display: inline-flex; align-items: center; gap: 7px; margin: 0 0 12px;
  font-family: var(--oj-mono); font-size: 12px; font-weight: 600; letter-spacing: .08em; text-transform: uppercase;
  color: var(--oj-primary);
}
.ltd-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: 14px; }
.ltd-card {
  display: flex; flex-direction: column; gap: 8px; padding: 16px 18px;
  background: color-mix(in srgb, var(--acc) 16%, #fffef8);
  border: 1.5px solid var(--border-strong); border-radius: var(--r-md);
  box-shadow: var(--pop-2);
}
.ltd-title { font-family: var(--oj-display); font-weight: 800; font-size: 15px; color: var(--text); }
.ltd-desc { font-size: 13px; line-height: 1.7; color: var(--text-soft); }

/* ── 目錄 + 內文兩欄 ────── */
.legal-grid { display: grid; grid-template-columns: 240px minmax(0, 1fr); gap: 36px; align-items: start; }

.legal-toc {
  position: sticky; top: calc(var(--nav-h) + 20px);
  display: flex; flex-direction: column; gap: 3px; padding: 14px 12px;
  background: var(--surface-2); border: 1.5px solid var(--border-strong); border-radius: var(--r-md);
  box-shadow: var(--pop-2);
}
.toc-label {
  padding: 2px 10px 8px;
  font-family: var(--oj-mono); font-size: 11px; font-weight: 600; letter-spacing: .12em; text-transform: uppercase;
  color: var(--text-faint);
}
.toc-entry {
  display: flex; align-items: baseline; gap: 9px; padding: 7px 10px; cursor: pointer;
  background: transparent; border: none; border-radius: 9px; text-align: left;
  font-family: var(--oj-font); font-weight: 600; font-size: 13.5px; color: var(--text); line-height: 1.4;
  transition: background .14s ease;
}
.toc-entry:hover { background: var(--oj-wash); color: var(--oj-primary); }
.toc-num { flex: none; font-family: var(--oj-mono); font-size: 11px; font-weight: 700; color: var(--oj-primary); }

/* ── 內文：文件標題 + 編號章節（虛線分隔、色塊列點） ────── */
.legal-head { padding-bottom: 20px; }
.legal-title { font-family: var(--oj-display); font-weight: 800; font-size: 30px; letter-spacing: -1px; margin: 0; color: var(--text); }
.legal-meta { font-family: var(--oj-mono); font-size: 11.5px; letter-spacing: .06em; text-transform: uppercase; color: var(--text-faint); margin: 9px 0 0; }

.legal-sec { padding: 24px 0; border-top: 1.5px dashed rgba(26,22,38,.16); scroll-margin-top: calc(var(--nav-h) + 20px); }
.legal-sec h3 {
  display: flex; align-items: center; gap: 12px; margin: 0 0 11px;
  font-family: var(--oj-display); font-weight: 700; font-size: 19px; color: var(--text);
}
.legal-sec h3 .num {
  flex: none; display: inline-block; padding: 2px 9px; transform: rotate(-2deg);
  font-family: var(--oj-mono); font-size: 12px; font-weight: 700; color: var(--text);
  background: color-mix(in srgb, var(--acc) 32%, #fff);
  border: 1.5px solid var(--border-strong); border-radius: 8px;
}
.legal-sec .sec-body { margin: 0; font-size: 15px; line-height: 1.9; color: var(--text-soft); white-space: pre-wrap; }
.legal-sec ul { margin: 12px 0 0; padding: 0; list-style: none; display: flex; flex-direction: column; gap: 8px; }
.legal-sec li { display: flex; align-items: baseline; gap: 10px; font-size: 14.5px; line-height: 1.8; color: var(--text-soft); }
.li-mark {
  flex: none; width: 8px; height: 8px; border-radius: 3px; position: relative; top: -1px;
  background: color-mix(in srgb, var(--acc) 60%, #fff); border: 1.5px solid var(--border-strong);
}

/* ── 聯絡 CTA：漸層卡 + 便條裝飾（同 FaqView） ────── */
.legal-cta { max-width: 1080px; margin: 44px auto 0; padding: 0 clamp(20px, 3.5vw, 56px) 72px; }
.fc-card {
  position: relative; overflow: hidden;
  display: flex; flex-direction: column; align-items: center; gap: 14px; text-align: center;
  padding: clamp(38px, 5.5vh, 48px) 32px;
  background:
    radial-gradient(620px 460px at 14% 10%, rgba(255,200,58,.35), transparent 58%),
    radial-gradient(680px 520px at 88% 90%, rgba(31,214,198,.30), transparent 60%),
    linear-gradient(150deg, #6c4cf1, #8a3df1 46%, #c33ad6);
  border: 1.5px solid var(--border-strong); border-radius: var(--r-lg);
  box-shadow: 6px 6px 0 var(--border-strong);
}
.fc-deco { position: absolute; border: 1.5px solid var(--border-strong); }
.fc-deco-a { top: 20px; left: 40px; width: 30px; height: 30px; border-radius: 10px; background: var(--c-lime); transform: rotate(-12deg); }
.fc-deco-b { bottom: 24px; right: 48px; width: 26px; height: 26px; border-radius: 999px; background: var(--c-yellow); transform: rotate(8deg); }
.fc-title {
  margin: 0; font-family: var(--oj-display); font-weight: 800; color: #fff;
  font-size: clamp(24px, 3.2vw, 30px); letter-spacing: -.8px; text-shadow: 2px 2px 0 rgba(26,22,38,.35);
}
.fc-text { margin: 0; max-width: 44ch; font-size: 15px; line-height: 1.8; color: rgba(255,255,255,.88); }
.fc-actions { display: flex; flex-wrap: wrap; justify-content: center; gap: 12px; margin-top: 6px; }
.fc-btn {
  display: inline-flex; align-items: center; gap: 8px; padding: 12px 26px;
  font-family: var(--oj-display); font-weight: 700; font-size: 15px; text-decoration: none;
  border: 1.5px solid var(--border-strong); border-radius: 999px;
  transition: transform .12s ease, box-shadow .12s ease;
}
.fc-btn:hover { transform: translate(-1px, -1px); text-decoration: none; }
.fc-btn-light { background: var(--surface); color: var(--text); box-shadow: 3px 3px 0 rgba(26,22,38,.45); }
.fc-btn-light:hover { color: var(--text); box-shadow: 4px 4px 0 rgba(26,22,38,.45); }

@media (prefers-reduced-motion: reduce) {
  .legal-tab, .toc-entry, .fc-btn { transition: none; }
}

/* 窄螢幕：目錄收起、單欄內文 */
@media (max-width: 900px) {
  .legal-grid { grid-template-columns: 1fr; }
  .legal-toc { display: none; }
}
@media (max-width: 560px) {
  .legal-tab { padding: 10px 13px; font-size: 13.5px; }
  .legal-tab .lt-label { white-space: nowrap; }
  .legal-card { padding: 24px 20px 30px; }
  .legal-title { font-size: 25px; }
  .fc-deco { display: none; }
}
</style>
