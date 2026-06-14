<script setup lang="ts">
/* ============================================================
   LegalView — 服務條款 / 隱私政策 靜態頁（/terms、/privacy）
   文案沿用 Auth 服務的 legal dialog（src/data/legal.ts）。
   ============================================================ */
import { computed } from 'vue';
import { useShopStore } from '@/stores/shop.js';
import { LEGAL, type LegalKey } from '@/data/legal.js';
import AppNav from '@/layout/AppNav.vue';
import AppFooter from '@/layout/AppFooter.vue';

const props = defineProps<{ doc: LegalKey }>();

const store = useShopStore();
const current = computed(() => LEGAL[props.doc]);
const other = computed(() => LEGAL[props.doc === 'terms' ? 'privacy' : 'terms']);
</script>

<template>
  <div class="oj-root" :class="'font-' + store.font" :data-screen-label="current.title">
    <!-- ============ NAV ============ -->
    <app-nav />

    <main class="page legal-page">
      <nav class="breadcrumb" aria-label="麵包屑">
        <router-link to="/">市集</router-link>
        <app-icon name="chevron" :size="14" />
        <span>{{ current.title }}</span>
      </nav>

      <article class="legal-card">
        <header class="legal-head">
          <div class="legal-badge" :class="doc">
            <app-icon :name="current.icon" :size="24" />
          </div>
          <h1 class="legal-title">{{ current.title }}</h1>
          <p class="legal-meta">Open Jam · 最後更新 {{ current.updated }}</p>
        </header>

        <section v-for="s in current.sections" :key="s.n" class="legal-sec">
          <h2><span class="num">{{ s.n }}</span> {{ s.h }}</h2>
          <p>{{ s.p }}</p>
          <ul v-if="s.list">
            <li v-for="(item, i) in s.list" :key="i">{{ item }}</li>
          </ul>
        </section>

        <div class="legal-switch">
          <router-link :to="'/' + other.key">
            前往閱讀「{{ other.title }}」 <app-icon name="chevron" :size="15" />
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
