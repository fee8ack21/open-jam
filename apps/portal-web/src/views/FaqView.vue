<script setup lang="ts">
/* ============================================================
   FaqView — 常見問題頁（/faq）
   內容依「問題主題」分頁：全部 / 認識平台 / 購買與下載 /
   開店與上架 / 金流與結算。每則問答帶一個主題標籤（cat），
   「全部」分頁彙整所有項目並顯示主題 chip。
   問答為手風琴（accordion）展開，內容由 i18n（faq.*）提供。
   ============================================================ */
import { ref, computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { useShopStore } from '@/stores/shop.js';
import AppNav from '@/layout/AppNav.vue';
import AppFooter from '@/layout/AppFooter.vue';

const store = useShopStore();
const { t, tm, rt } = useI18n();

// 分頁：'all' 為彙整視角，其餘對應問答的 cat
const TAB_KEYS = ['all', 'platform', 'buying', 'selling', 'payments'] as const;
type TabKey = (typeof TAB_KEYS)[number];
const activeTab = ref<TabKey>('all');

interface FaqItem { i: number; cat: string; q: string; a: string; }

// 全部問答（帶原始索引，供手風琴開合狀態穩定對應）
const items = computed<FaqItem[]>(() =>
  (tm('faq.items') as { cat: string; q: string; a: string }[]).map((it, i) => ({
    i,
    cat: it.cat,
    q: rt(it.q),
    a: rt(it.a),
  })),
);

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
</script>

<template>
  <div class="oj-root" :class="'font-' + store.font" :data-screen-label="t('faq.screenLabel')">
    <!-- ============ NAV ============ -->
    <app-nav />

    <main class="page faq-page">
      <nav class="breadcrumb" :aria-label="t('common.breadcrumb')">
        <router-link to="/discover">{{ t('common.marketplace') }}</router-link>
        <app-icon name="chevron" :size="14" />
        <span>{{ t('faq.breadcrumb') }}</span>
      </nav>

      <header class="faq-head">
        <div class="faq-badge"><app-icon name="sparkle" :size="24" /></div>
        <h1 class="faq-title">{{ t('faq.title') }}</h1>
        <p class="faq-lede">{{ t('faq.lede') }}</p>
      </header>

      <div class="faq-tabs" role="tablist" :aria-label="t('faq.title')">
        <button
          v-for="k in TAB_KEYS"
          :key="k"
          type="button"
          role="tab"
          class="faq-tab"
          :class="{ on: activeTab === k }"
          :aria-selected="activeTab === k"
          @click="selectTab(k)"
        >
          {{ t('faq.tabs.' + k) }}
        </button>
      </div>

      <ul class="faq-list">
        <li v-for="it in visible" :key="it.i" class="faq-item" :class="{ open: open.has(it.i) }">
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
    </main>

    <!-- ============ FOOTER ============ -->
    <app-footer />
  </div>
</template>

<style scoped>
.faq-page { max-width: 820px; margin: 0 auto; padding-top: 30px; padding-bottom: 80px; }

.faq-head { margin-bottom: 26px; }
.faq-badge {
  width: 48px; height: 48px; border-radius: 14px; display: grid; place-items: center; color: #fff;
  border: 1.5px solid var(--text); box-shadow: 3px 3px 0 var(--text); margin-bottom: 16px;
  background: linear-gradient(135deg, var(--c-violet), var(--c-cyan));
}
.faq-title { font-family: var(--oj-display); font-weight: 800; font-size: 32px; letter-spacing: -1px; margin: 0; color: var(--text); }
.faq-lede { font-size: 16px; line-height: 1.8; color: var(--text-soft); margin: 12px 0 0; max-width: 60ch; }

/* 主題分頁列 */
.faq-tabs { display: flex; flex-wrap: wrap; gap: 10px; margin-bottom: 22px; }
.faq-tab {
  cursor: pointer; padding: 9px 16px; border: 1.5px solid var(--text); border-radius: 999px;
  background: var(--surface); box-shadow: 2px 2px 0 var(--text);
  font-family: var(--oj-display); font-weight: 700; font-size: 14px; color: var(--text);
  transition: transform .12s, box-shadow .12s, background .15s, color .15s;
}
.faq-tab:hover { transform: translate(-1px, -1px); box-shadow: 3px 3px 0 var(--text); }
.faq-tab:active { transform: translate(1px, 1px); box-shadow: 1px 1px 0 var(--text); }
.faq-tab.on { background: var(--oj-primary); color: #fff; }

/* 問答手風琴 */
.faq-list { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 12px; }
.faq-item {
  background: var(--surface); border: 1.5px solid var(--border-strong); border-radius: var(--r-md);
  box-shadow: 4px 4px 0 var(--border); overflow: hidden;
  transition: box-shadow .15s, border-color .15s;
}
.faq-item.open { border-color: var(--text); box-shadow: 5px 5px 0 var(--text); }
.faq-q {
  width: 100%; display: flex; align-items: center; justify-content: space-between; gap: 14px;
  cursor: pointer; background: none; border: none; text-align: left; padding: 16px 18px;
  font-family: var(--oj-display); font-weight: 700; font-size: 15.5px; color: var(--text); line-height: 1.45;
}
.faq-q-text { display: flex; align-items: center; gap: 10px; flex-wrap: wrap; }
.faq-chip {
  font-family: var(--oj-mono); font-size: 10px; letter-spacing: .06em; text-transform: uppercase;
  color: var(--oj-primary); background: var(--oj-wash); border: 1px solid var(--border);
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
@media (max-width: 560px) {
  .faq-title { font-size: 26px; }
  .faq-q { font-size: 14.5px; padding: 14px 15px; }
}
</style>
