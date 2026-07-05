<script setup lang="ts">
/* ============================================================
   LegalView — 服務條款 / 隱私政策頁（/terms、/privacy）
   內容撈取 Auth 服務「目前啟用中」的法律文件版本
   （GET /v1/legal-documents/active，後台可編輯 / 換版）；
   撈取失敗時退回 i18n 靜態文案（src/data/legal.ts 結構）。
   ============================================================ */
import { computed, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useShopStore } from '@/stores/shop.js';
import { LEGAL_META, type LegalKey } from '@/data/legal.js';
import { authApi } from '@/api';
import { LegalDocumentType, type LegalDocumentDto } from '@/api/auth-service';
import AppNav from '@/layout/AppNav.vue';
import AppFooter from '@/layout/AppFooter.vue';

const props = defineProps<{ doc: LegalKey }>();

const store = useShopStore();
const { t, tm, rt } = useI18n();

interface Section {
  n: string;
  h: string;
  p: string;
  list?: string[];
}

const meta = computed(() => LEGAL_META[props.doc]);
const otherKey = computed<LegalKey>(() => (props.doc === 'terms' ? 'privacy' : 'terms'));
const otherTitle = computed(() => t(`legal.${otherKey.value}.title`));

// ── 啟用中版本（依 doc 類型撈取；null 表示尚未載到 / 撈取失敗）─────────
const activeDoc = ref<LegalDocumentDto | null>(null);
const loading = ref(false);

const typeOf: Record<LegalKey, LegalDocumentType> = {
  terms: LegalDocumentType.TermsOfService,
  privacy: LegalDocumentType.PrivacyPolicy,
};

async function loadActive() {
  loading.value = true;
  activeDoc.value = null;
  try {
    const res = await authApi.legalDocuments.getActive({ type: typeOf[props.doc] });
    activeDoc.value = res.data?.[0] ?? null;
  } catch {
    activeDoc.value = null; // 撈取失敗：退回 i18n 靜態文案
  } finally {
    loading.value = false;
  }
}
watch(() => props.doc, loadActive, { immediate: true });

const title = computed(() => activeDoc.value?.title ?? t(`legal.${props.doc}.title`));
const updatedDate = computed(() => {
  const at = activeDoc.value?.activatedAt;
  return at ? at.slice(0, 10) : meta.value.updated;
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
      <nav class="breadcrumb" :aria-label="t('common.breadcrumb')">
        <router-link to="/">{{ t('common.marketplace') }}</router-link>
        <app-icon name="chevron" :size="14" />
        <span>{{ title }}</span>
      </nav>

      <article class="legal-card">
        <header class="legal-head">
          <div class="legal-badge" :class="doc">
            <app-icon :name="meta.icon" :size="24" />
          </div>
          <h1 class="legal-title">{{ title }}</h1>
          <p class="legal-meta">
            {{ t('legal.metaUpdated', { date: updatedDate }) }}<template v-if="activeDoc"> · v{{ activeDoc.version }}</template>
          </p>
        </header>

        <section v-for="s in sections" :key="s.n" class="legal-sec">
          <h2 v-if="s.h"><span class="num">{{ s.n }}</span> {{ s.h }}</h2>
          <p style="white-space: pre-wrap;">{{ s.p }}</p>
          <ul v-if="s.list">
            <li v-for="(item, i) in s.list" :key="i">{{ item }}</li>
          </ul>
        </section>

        <div class="legal-switch">
          <router-link :to="'/' + otherKey">
            {{ t('legal.switchTo', { title: otherTitle }) }} <app-icon name="chevron" :size="15" />
          </router-link>
        </div>
      </article>
    </main>

    <!-- ============ FOOTER ============ -->
    <app-footer />
  </div>
</template>

<style scoped>
.legal-page { max-width: 820px; margin: 0 auto; padding-top: 30px; padding-bottom: 80px; }

.legal-card {
  background: var(--surface); border: 1.5px solid var(--border-strong); border-radius: var(--r-lg);
  box-shadow: 6px 6px 0 var(--border); padding: 34px 40px 40px;
}

.legal-head { border-bottom: 1.5px solid var(--border); padding-bottom: 24px; margin-bottom: 28px; }
.legal-badge {
  width: 48px; height: 48px; border-radius: 14px; display: grid; place-items: center; color: #fff;
  border: 1.5px solid var(--text); box-shadow: 3px 3px 0 var(--text); margin-bottom: 16px;
}
.legal-badge.terms { background: linear-gradient(135deg, var(--c-violet), var(--c-pink)); }
.legal-badge.privacy { background: linear-gradient(135deg, var(--c-cyan), var(--c-violet)); }
.legal-title { font-family: var(--oj-display); font-weight: 800; font-size: 32px; letter-spacing: -1px; margin: 0; color: var(--text); }
.legal-meta { font-family: var(--oj-mono); font-size: 11.5px; letter-spacing: .06em; text-transform: uppercase; color: var(--text-faint); margin: 9px 0 0; }

.legal-sec { margin-bottom: 26px; }
.legal-sec h2 { font-family: var(--oj-display); font-weight: 700; font-size: 18px; color: var(--text); margin: 0 0 9px; display: flex; align-items: baseline; gap: 10px; }
.legal-sec h2 .num { font-family: var(--oj-mono); font-size: 13px; color: var(--oj-primary); font-weight: 600; }
.legal-sec p { margin: 0; font-size: 15px; line-height: 1.78; color: var(--text-soft); }
.legal-sec ul { margin: 10px 0 0; padding-left: 22px; }
.legal-sec li { font-size: 14.5px; line-height: 1.75; color: var(--text-soft); margin-bottom: 5px; }

.legal-switch { margin-top: 32px; padding-top: 22px; border-top: 1.5px dashed var(--border); }
.legal-switch a {
  display: inline-flex; align-items: center; gap: 7px; text-decoration: none;
  font-family: var(--oj-display); font-weight: 700; font-size: 15px; color: var(--oj-primary);
  transition: gap .15s;
}
.legal-switch a:hover { gap: 11px; }

@media (max-width: 560px) {
  .legal-card { padding: 24px 20px 30px; }
  .legal-title { font-size: 26px; }
}
</style>
