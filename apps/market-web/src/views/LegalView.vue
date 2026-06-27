<script setup lang="ts">
/* ============================================================
   LegalView — 服務條款 / 隱私政策 靜態頁（/terms、/privacy）
   文案沿用 Auth 服務的 legal dialog（src/data/legal.ts）。
   ============================================================ */
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { useShopStore } from '@/stores/shop.js';
import { LEGAL_META, type LegalKey } from '@/data/legal.js';
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

const title = computed(() => t(`legal.${props.doc}.title`));
const otherTitle = computed(() => t(`legal.${otherKey.value}.title`));

// 章節文案來自 i18n（legal.<doc>.sections）；編號 n 由索引推算
const sections = computed<Section[]>(() =>
  (tm(`legal.${props.doc}.sections`) as { h: string; p: string; list?: string[] }[]).map(
    (s, i) => ({
      n: String(i + 1).padStart(2, '0'),
      h: rt(s.h),
      p: rt(s.p),
      list: s.list ? s.list.map((item) => rt(item)) : undefined,
    }),
  ),
);
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
          <p class="legal-meta">{{ t('legal.metaUpdated', { date: meta.updated }) }}</p>
        </header>

        <section v-for="s in sections" :key="s.n" class="legal-sec">
          <h2><span class="num">{{ s.n }}</span> {{ s.h }}</h2>
          <p>{{ s.p }}</p>
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
