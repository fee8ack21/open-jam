<script setup lang="ts">
/* ============================================================
   LegalView — 服務條款 / 隱私權政策頁（/terms、/privacy）
   版面結構：Hero 漸層色帶（版本 meta pill）→ 資料夾式頁籤 +
   內容卡（重點速覽卡片格 + sticky 目錄 + 編號章節）→ 聯絡 CTA。
   兩份文件合併於同一頁，以分頁（tab）切換，切換時同步更新網址
   （/terms ↔ /privacy）供分享與外部書籤相容。
   內容撈取 ContentService「目前啟用中」的法律文件版本
   （GET /v1/legal-documents/active，後台可編輯 / 換版）；
   撈取失敗時退回 i18n 靜態文案。重點速覽亦隨版本管理
   （highlights 欄位，每行一則「標題|描述」），後台留空或
   撈取失敗（無有效資料）時整個速覽區塊不顯示。
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

/** 將重點速覽純文字解析為卡片：每行一則、以第一個「|」分隔標題與描述。 */
function parseHighlights(text: string): { t: string; d: string }[] {
  return text
    .replace(/\r\n/g, '\n')
    .split('\n')
    .map((l) => l.trim())
    .filter((l) => l.includes('|'))
    .map((l) => {
      const i = l.indexOf('|');
      return { t: l.slice(0, i).trim(), d: l.slice(i + 1).trim() };
    })
    .filter((c) => c.t && c.d);
}

// 重點速覽：取啟用中版本的 highlights；後台留空或撈取失敗（無有效資料）則整區不顯示
const tldr = computed<{ t: string; d: string }[]>(() => parseHighlights(activeDoc.value?.highlights ?? ''));

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
        <i class="lh-deco lh-deco-s" aria-hidden="true">§</i>
        <i class="lh-deco lh-deco-dot" aria-hidden="true"></i>
        <i class="lh-deco lh-deco-sq" aria-hidden="true"></i>
        <div class="lh-inner">
          <p class="lh-badge"><app-icon name="note" :size="13" /> {{ t('legal.eyebrow') }}</p>
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
        <!-- ── 分頁：服務條款 / 隱私權政策 ── -->
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
          <!-- ── 重點速覽：30 秒摘要卡片格（無 highlights 資料則整區不顯示） ── -->
          <section v-if="tldr.length" class="legal-tldr" :aria-label="t('legal.tldrLabel')">
            <p class="ltd-label"><app-icon name="note" :size="13" /> {{ t('legal.tldrLabel') }}</p>
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

      <!-- ============ 聯絡 CTA（黃色滿版帶） ============ -->
      <section class="legal-cta">
        <i class="fc-deco fc-deco-a" aria-hidden="true"></i>
        <i class="fc-deco fc-deco-b" aria-hidden="true"></i>
        <div class="fc-card">
          <i18n-t keypath="legal.cta.title" tag="h2" class="fc-title" scope="global">
            <template #mark><span class="fc-hl">{{ t('legal.cta.titleMark') }}</span></template>
          </i18n-t>
          <p class="fc-text">{{ t('legal.cta.text') }}</p>
          <div class="fc-actions">
            <a class="fc-btn fc-btn-dark" href="mailto:support@openjam.co">
              <app-icon name="mail" :size="16" /> {{ t('legal.cta.contact') }}
            </a>
            <router-link class="fc-btn fc-btn-light" to="/faq">
              <app-icon name="arrow" :size="15" /> {{ t('legal.cta.faq') }}
            </router-link>
          </div>
          <div class="fc-note">{{ t('legal.cta.note') }}</div>
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

/* ── Hero 色帶：紫色圓點滿版帶（同 .mkt-hero 配方）────── */
.legal-hero { position: relative; padding: clamp(52px, 8vh, 72px) clamp(20px, 3.5vw, 56px) clamp(110px, 14vh, 150px); text-align: center; }
.legal-hero::before {
  content: ''; position: absolute; top: 0; bottom: 0; left: 50%;
  width: 100vw; transform: translateX(-50%); z-index: 0; pointer-events: none;
  background: #8a5cf6;
  background-image: radial-gradient(rgba(255, 255, 255, 0.18) 1.5px, transparent 1.5px);
  background-size: 26px 26px;
}

/* 色帶內的縷空大字（白色微透、傾斜，沉在置中內容後） */
.lh-word {
  position: absolute; z-index: 1; top: 44px; left: 4%;
  font-family: var(--oj-display); font-weight: 700; line-height: 1;
  font-size: clamp(100px, 13vw, 140px);
  color: rgba(255, 255, 255, .14); transform: rotate(-6deg);
  pointer-events: none; user-select: none;
}

/* 漂浮貼紙（§ 方塊 / 圓點 / 方點） */
.lh-deco { position: absolute; z-index: 1; font-style: normal; pointer-events: none; }
.lh-deco-s {
  right: 6%; top: 64px; width: 64px; height: 64px;
  display: grid; place-items: center; font-weight: 900; font-size: 30px; color: var(--text);
  background: var(--c-yellow); border: var(--bw) solid var(--border-strong); border-radius: 18px;
  box-shadow: 0 8px 20px rgba(26, 26, 26, 0.25); transform: rotate(8deg);
}
.lh-deco-dot { right: 14%; bottom: 84px; width: 44px; height: 44px; background: var(--c-lime); border: var(--bw) solid var(--border-strong); border-radius: 50%; transform: rotate(-10deg); }
.lh-deco-sq { left: 11%; bottom: 100px; width: 40px; height: 40px; background: var(--c-pink); border: var(--bw) solid var(--border-strong); border-radius: 12px; transform: rotate(14deg); }

.lh-inner {
  position: relative; z-index: 2; max-width: 780px; margin: 0 auto;
  display: flex; flex-direction: column; align-items: center; gap: 18px;
}
.lh-badge {
  display: inline-flex; align-items: center; gap: 8px; margin: 0; padding: 7px 18px;
  font-family: var(--oj-font); font-size: 13px; font-weight: 900; letter-spacing: 2px;
  color: var(--c-yellow); background: var(--text);
  border-radius: 999px; transform: rotate(-1deg); white-space: nowrap;
}
.lh-title {
  margin: 0; font-family: var(--oj-font); font-weight: 900; color: #fff;
  font-size: clamp(28px, 4.4vw, 52px); line-height: 1.3;
}
.lh-title .hl { box-shadow: var(--ink-drop); margin: 0 8px; }
.lh-lede { margin: 0; max-width: 54ch; font-size: 16px; font-weight: 700; line-height: 1.8; color: #fff; }
.lh-meta {
  display: inline-flex; align-items: center; margin: 0; padding: 8px 20px;
  font-family: var(--oj-display); font-size: 13px; font-weight: 700; letter-spacing: 1px;
  color: var(--text); background: #fff;
  border: var(--bw) solid var(--border-strong); border-radius: 999px;
  box-shadow: 0 6px 14px rgba(26, 26, 26, 0.2); white-space: nowrap;
}

/* ── 頁籤 + 內容卡：疊在色帶下緣 ────── */
.legal-body { position: relative; z-index: 2; max-width: 1080px; margin: -84px auto 0; padding: 0 clamp(20px, 3.5vw, 56px); }

/* 分頁列：兩份文件並列（未選＝紫底白字、選中＝白底貼合內容卡） */
.legal-tabs { display: flex; gap: 10px; position: relative; z-index: 1; padding-left: 24px; }
.legal-tab {
  display: inline-flex; align-items: center; gap: 9px; cursor: pointer;
  padding: 13px 24px 15px; background: var(--c-violet);
  border: var(--bw) solid var(--border-strong); border-bottom: none;
  border-radius: 16px 16px 0 0;
  font-family: var(--oj-font); font-weight: 900; font-size: 15px; color: #fff;
  transition: background .16s ease, color .16s ease;
}
.legal-tab:hover { background: var(--c-yellow); color: var(--text); }
.legal-tab.on {
  background: var(--surface); color: var(--text);
  margin-bottom: -2px; /* 蓋過內容卡上緣線，連成同一區塊 */
  padding-bottom: 17px;
}
.lt-badge {
  width: 26px; height: 26px; border-radius: 8px; display: grid; place-items: center;
  color: currentColor; flex: none;
}

.legal-card {
  position: relative; z-index: 0;
  background: var(--surface); border: var(--bw) solid var(--border-strong);
  border-radius: 24px;
  box-shadow: 0 16px 36px rgba(26, 26, 26, 0.14); padding: 38px 42px 50px;
}

/* ── 重點速覽：30 秒摘要卡片格 ────── */
.legal-tldr { margin-bottom: 40px; }
.ltd-label {
  display: inline-flex; align-items: center; gap: 8px; margin: 0 0 18px; padding: 6px 16px;
  font-family: var(--oj-font); font-size: 13px; font-weight: 900; letter-spacing: 1px;
  color: var(--c-yellow); background: var(--text);
  border-radius: 999px; transform: rotate(-1deg); white-space: nowrap;
}
.ltd-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: 20px; }
.ltd-card {
  display: flex; flex-direction: column; gap: 8px; padding: 20px;
  background: color-mix(in srgb, var(--acc) 45%, #fff);
  border: var(--bw) solid var(--border-strong); border-radius: 18px;
  box-shadow: 0 8px 18px rgba(26, 26, 26, 0.1);
}
.ltd-card:nth-child(odd) { transform: rotate(-0.8deg); }
.ltd-card:nth-child(even) { transform: rotate(0.9deg); }
.ltd-title { font-family: var(--oj-font); font-weight: 900; font-size: 16px; color: var(--text); }
.ltd-desc { font-size: 13.5px; font-weight: 500; line-height: 1.7; color: var(--text); }

/* ── 目錄 + 內文兩欄 ────── */
.legal-grid { display: grid; grid-template-columns: 230px minmax(0, 1fr); gap: 38px; align-items: start; }

.legal-toc {
  position: sticky; top: calc(var(--nav-h) + 20px);
  display: flex; flex-direction: column; gap: 2px; padding: 20px;
  background: var(--bg); border: var(--bw) solid var(--border-strong); border-radius: 18px;
  box-shadow: 0 6px 16px rgba(26, 26, 26, 0.08);
}
.toc-label {
  padding: 0 0 12px;
  font-family: var(--oj-font); font-size: 13px; font-weight: 900; letter-spacing: 1px;
  color: var(--text);
}
.toc-entry {
  display: flex; align-items: baseline; gap: 8px; padding: 7px 8px; cursor: pointer;
  background: transparent; border: none; border-radius: 10px; text-align: left;
  font-family: var(--oj-font); font-weight: 700; font-size: 13.5px; color: var(--text); line-height: 1.4;
  transition: background .15s ease;
}
.toc-entry:hover { background: var(--c-yellow); }
.toc-num { flex: none; font-family: var(--oj-display); font-size: 12px; font-weight: 700; color: var(--c-violet); }

/* ── 內文：文件標題 + 編號章節（虛線分隔、色塊列點） ────── */
.legal-head { padding-bottom: 20px; }
.legal-title { font-family: var(--oj-font); font-weight: 900; font-size: 32px; margin: 0; color: var(--text); }
.legal-meta { font-family: var(--oj-display); font-size: 12.5px; font-weight: 500; letter-spacing: 1px; color: var(--text-soft); margin: 9px 0 0; }

.legal-sec { padding: 26px 0 6px; border-top: 1px dashed var(--border); scroll-margin-top: calc(var(--nav-h) + 20px); }
.legal-sec h3 {
  display: flex; align-items: center; gap: 12px; margin: 0 0 14px;
  font-family: var(--oj-font); font-weight: 900; font-size: 21px; color: var(--text);
}
.legal-sec h3 .num {
  flex: none; display: inline-block; padding: 2px 12px;
  font-family: var(--oj-display); font-size: 14px; font-weight: 700; color: var(--text);
  background: color-mix(in srgb, var(--acc) 55%, #fff);
  border: var(--bw) solid var(--border-strong); border-radius: 999px;
}
.legal-sec .sec-body { margin: 0; font-size: 15px; font-weight: 500; line-height: 1.9; color: var(--text); white-space: pre-wrap; }
.legal-sec ul { margin: 12px 0 0; padding: 0; list-style: none; display: flex; flex-direction: column; gap: 10px; }
.legal-sec li { display: flex; align-items: baseline; gap: 10px; font-size: 15px; font-weight: 500; line-height: 1.8; color: var(--text); }
.li-mark {
  flex: none; width: 10px; height: 10px; border-radius: 50%; position: relative; top: -1px;
  background: var(--c-pink); border: var(--bw) solid var(--border-strong);
}

/* ── 聯絡 CTA：黃色滿版帶 + 貼紙裝飾 + 手寫註記（同 FaqView） ────── */
.legal-cta {
  position: relative; overflow: hidden;
  margin: 56px calc(50% - 50vw) 0; padding: 80px 32px;
  background: var(--c-yellow); border-top: var(--bw) solid var(--border-strong);
}
.fc-deco { position: absolute; border: var(--bw) solid var(--border-strong); }
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
.fc-btn-light { background: #fff; color: var(--text); border: var(--bw) solid var(--border-strong); }
.fc-btn-light:hover { transform: translateY(-3px) rotate(1deg); box-shadow: 0 10px 22px rgba(26, 26, 26, 0.18); color: var(--text); }
.fc-note {
  font-family: var(--oj-hand); font-weight: 700; font-size: 24px; color: var(--text);
  margin-top: 2px; transform: rotate(-2deg);
}

@media (prefers-reduced-motion: reduce) {
  .legal-tab, .toc-entry, .fc-btn { transition: none; }
}

/* 窄螢幕：目錄收起、單欄內文 */
@media (max-width: 900px) {
  .legal-grid { grid-template-columns: 1fr; }
  .legal-toc { display: none; }
  .lh-deco { display: none; }
}
@media (max-width: 560px) {
  .legal-hero { padding-bottom: 96px; }
  .legal-body { margin-top: -64px; }
  .legal-tabs { padding-left: 12px; }
  .legal-tab { padding: 10px 14px 12px; font-size: 13.5px; }
  .legal-tab .lt-label { white-space: nowrap; }
  .legal-card { padding: 24px 20px 30px; }
  .legal-title { font-size: 25px; }
  .ltd-card, .ltd-card:nth-child(odd), .ltd-card:nth-child(even) { transform: none; }
  .fc-deco { display: none; }
  .legal-cta { padding: 56px 20px; }
}
</style>
