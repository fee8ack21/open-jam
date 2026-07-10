<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useMessage } from 'naive-ui';
import { useShopStore } from '@/stores/shop';
import { useAuthStore } from '@/stores/auth';
import { CATEGORIES, TAGS, type Product } from '@/data/products';
import ProductCard from '@/components/ProductCard.vue';
import AppIcon from '@/components/app-icon';

const store = useShopStore();
const s = store;
const auth = useAuthStore();
const message = useMessage();
const { t } = useI18n();

onMounted(store.loadCatalog);

// ----- hero: creator profile stats -----
const heroDesc = computed(() => store.storefront.description || t('list.heroSub'));
const avatarInitial = computed(() => (store.storefront.storeName || 'O').trim().charAt(0));
const workCount = computed(() => store.products.length);
// 全店平均評分：以各商品評分數加權；尚無評分時顯示「—」
const avgRating = computed(() => {
  const rated = store.products.filter((p) => p.ratingCount > 0);
  const n = rated.reduce((sum, p) => sum + p.ratingCount, 0);
  if (!n) return null;
  return (rated.reduce((sum, p) => sum + p.rating * p.ratingCount, 0) / n).toFixed(1);
});

// ----- hero: follow form（與 AppNav 同一 following 狀態，成功後兩處同步收合）-----
const followEmail = ref('');
const emailEdited = ref(false);      // 使用者是否手動改過信箱欄位
const followSubmitting = ref(false);

// 已登入則以登入信箱預填（仍可手動改動）；使用者一旦動過欄位就不再覆蓋。
watch(
  () => auth.userEmail,
  (email) => {
    if (email && !emailEdited.value) followEmail.value = email;
  },
  { immediate: true },
);

const subscribe = async () => {
  if (followSubmitting.value) return;
  const v = followEmail.value.trim();
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v)) {
    message.warning(t('nav.msgInvalidEmail'));
    return;
  }
  followSubmitting.value = true;
  try {
    await store.followStore(v);
    message.success(t('nav.msgFollowSuccess'));
  } catch {
    message.error(t('nav.msgFollowError'));
  } finally {
    followSubmitting.value = false;
  }
};

type SortKey = 'popular' | 'newest' | 'rating' | 'price-asc' | 'price-desc';
const sortOptions = computed<{ label: string; value: SortKey }[]>(() => [
  { label: t('list.sort.popular'), value: 'popular' },
  { label: t('list.sort.newest'), value: 'newest' },
  { label: t('list.sort.rating'), value: 'rating' },
  { label: t('list.sort.priceAsc'), value: 'price-asc' },
  { label: t('list.sort.priceDesc'), value: 'price-desc' },
]);

const cats = CATEGORIES;
const results = computed(() => store.filtered);
const availableTags = computed(() => {
  if (store.category !== 'all') return TAGS[store.category];
  return [...new Set(Object.values(TAGS).flat())];
});
const priceRange = computed<[number, number]>({
  get() { return store.priceRange; },
  set(v: number | [number, number]) { store.priceRange = (Array.isArray(v) ? v : [v, v]) as [number, number]; },
});

const setCat = (c: string) => store.setCategory(c);
const catColor = (id: string) => ({ music: 'var(--c-violet)', photo: 'var(--c-pink)', ebook: 'var(--c-cyan)' } as Record<string, string>)[id];
const catLabel = (id: string) => (CATEGORIES.some((c) => c.id === id) ? t('category.' + id) : id);
const toggleTag = (tag: string) => store.toggleTag(tag);
const clear = () => store.clearFilters();
const priceLabel = (v: number) => (v === 0 ? t('common.free') : '$' + v);

// ----- grid badges (熱賣 / 新上架) — adds rhythm to the uniform grid -----
// 後端列表已依上架時間 desc，取前 3 筆視為「新上架」
const newestIds = computed(() => new Set(store.products.slice(0, 3).map((p) => p.id)));
function badgeFor(p: Product): { label: string; tone: 'hot' | 'new' | 'feat' } | null {
  if (p.sales >= 1500) return { label: t('list.badge.hot'), tone: 'hot' };
  if (newestIds.value.has(p.id)) return { label: t('list.badge.new'), tone: 'new' };
  return null;
}

// active filter chips — surface everything currently narrowing the grid (excludes sort)
const activeChips = computed(() => {
  const chips: { key: string; label: string; clear: () => void }[] = [];
  const q = s.search.trim();
  if (q) chips.push({ key: 'q', label: `「${q}」`, clear: () => { s.search = ''; } });
  if (s.category !== 'all') chips.push({ key: 'cat', label: catLabel(s.category), clear: () => setCat('all') });
  for (const tag of s.activeTags) chips.push({ key: 'tag-' + tag, label: tag, clear: () => toggleTag(tag) });
  if (s.onlyFree) chips.push({ key: 'free', label: t('common.free'), clear: () => { s.onlyFree = false; } });
  if (s.priceRange[0] !== 0 || s.priceRange[1] !== 40) {
    const hi = s.priceRange[1] >= 40 ? '$40+' : '$' + s.priceRange[1];
    chips.push({ key: 'price', label: `${priceLabel(s.priceRange[0])} – ${hi}`, clear: () => { priceRange.value = [0, 40]; } });
  }
  return chips;
});
</script>

<template>
  <div class="page page-pad" :data-screen-label="t('list.screenLabel')">
    <section class="hero">
      <div class="hero-shapes">
        <span class="shape s1"></span>
        <span class="shape s2"></span>
        <span class="shape s3"></span>
      </div>

      <div class="hero-grid">
        <!-- 左：創作者資訊 -->
        <div class="hero-left">
          <p class="hero-eyebrow"><app-icon name="sparkle" :size="14" /> OPEN JAM · {{ t('list.heroEyebrow') }}</p>
          <div class="hero-id">
            <span class="hero-avatar">
              <img v-if="store.storefront.avatarUrl" :src="store.storefront.avatarUrl" :alt="store.storefront.storeName" />
              <template v-else>{{ avatarInitial }}</template>
            </span>
            <div class="hero-id-text">
              <h1 class="hero-title">{{ store.storefront.storeName }}</h1>
              <p class="hero-handle">@{{ store.storefront.storeSlug }}</p>
            </div>
          </div>
          <p class="hero-sub">{{ heroDesc }}</p>
          <div class="hero-stats">
            <div class="hero-stat">
              <b>{{ workCount }}</b>
              <span>{{ t('list.statWorks') }}</span>
            </div>
            <div class="hero-stat">
              <b><app-icon name="star" :size="19" :stroke="2.2" class="stat-star" />{{ avgRating ?? '—' }}</b>
              <span>{{ t('list.statRating') }}</span>
            </div>
            <div class="hero-stat">
              <b>{{ store.followerCount.toLocaleString() }}</b>
              <span>{{ t('list.statFollowers') }}</span>
            </div>
          </div>
        </div>

        <!-- 右：信箱追蹤卡 -->
        <aside class="hero-follow">
          <div class="follow-card">
            <template v-if="!store.following">
              <div class="fc-head">
                <span class="fc-mark"><app-icon name="mail" :size="19" /></span>
                <h2 class="fc-title">{{ t('follow.title') }}</h2>
              </div>
              <p class="fc-desc">{{ t('follow.desc') }}</p>
              <form class="fc-form" @submit.prevent="subscribe">
                <div class="fc-input-row">
                  <span class="fc-input-ic"><app-icon name="mail" :size="16" /></span>
                  <input class="fc-input" type="email" v-model="followEmail" @input="emailEdited = true"
                         :placeholder="t('follow.placeholder')" :aria-label="t('nav.followEmailAria')" :disabled="followSubmitting" />
                </div>
                <button class="fc-btn" type="submit" :disabled="followSubmitting">
                  {{ followSubmitting ? t('follow.submitting') : t('follow.cta') }}
                </button>
              </form>
              <p class="fc-note"><app-icon name="shield" :size="13" /> {{ t('follow.note') }}</p>
            </template>
            <div v-else class="fc-done">
              <span class="fc-done-ring"><app-icon name="check" :size="22" :stroke="2.6" /></span>
              <p class="fc-title">{{ t('follow.doneTitle') }}</p>
              <p class="fc-desc">{{ t('follow.doneDesc') }}</p>
            </div>
          </div>
        </aside>
      </div>

      <div class="hero-cats">
        <span class="cat-pill c-all" :class="{ on: s.category === 'all' }" @click="setCat('all')">
          <span class="dot" style="background:var(--c-violet)"></span>{{ t('list.allWorks') }}
        </span>
        <span v-for="c in cats" :key="c.id" class="cat-pill" :class="['c-' + c.id, { on: s.category === c.id }]" @click="setCat(c.id)">
          <span class="dot" :style="{ background: catColor(c.id) }"></span>{{ t('category.' + c.id) }}
        </span>
      </div>
    </section>

    <section class="browse">
      <!-- toolbar: result count + sort tabs -->
      <div class="browse-toolbar">
        <i18n-t keypath="list.count" tag="span" class="browse-count" scope="global">
          <template #count><b>{{ results.length }}</b></template>
        </i18n-t>
        <div class="sort-tabs">
          <span class="sort-lab">{{ t('list.sortLabel') }}</span>
          <button type="button" v-for="o in sortOptions" :key="o.value" class="sort-tab"
                  :class="{ on: s.sort === o.value }" :aria-pressed="s.sort === o.value" @click="s.sort = o.value">
            {{ o.label }}
          </button>
          <select class="sort-select" v-model="s.sort" :aria-label="t('list.sortAria')">
            <option v-for="o in sortOptions" :key="o.value" :value="o.value">{{ o.label }}</option>
          </select>
        </div>
      </div>

      <div class="browse-body">
        <!-- filter rail -->
        <aside class="browse-side">
          <div class="side-card">
            <p class="side-title"><app-icon name="sparkle" :size="15" /> {{ t('list.filter') }}</p>

            <div class="side-group">
              <p class="side-label">{{ t('list.tagsLabel') }}</p>
              <div class="tag-wrap">
                <span v-for="tag in availableTags" :key="tag" class="tag-toggle"
                      :class="{ on: s.activeTags.includes(tag) }" @click="toggleTag(tag)">
                  {{ tag }}
                </span>
              </div>
            </div>

            <div class="side-group">
              <p class="side-label">{{ t('list.priceRangeLabel') }}</p>
              <n-slider v-model:value="priceRange" range :min="0" :max="40" :step="1"
                        :format-tooltip="priceLabel" :marks="{ 0: t('common.free'), 40: t('list.priceMax') }" />
              <div class="side-price-vals">
                <span>{{ priceLabel(priceRange[0]) }}</span>
                <span>{{ priceRange[1] >= 40 ? t('list.priceMax') : '$' + priceRange[1] }}</span>
              </div>
            </div>

            <div class="side-group">
              <div class="side-row">
                <span>{{ t('list.onlyFree') }}</span>
                <n-switch v-model:value="s.onlyFree" size="small" />
              </div>
            </div>

            <button v-if="activeChips.length" type="button" class="side-reset" @click="clear">{{ t('list.clearAll') }}</button>
          </div>
        </aside>

        <!-- results -->
        <div class="browse-main">
          <div v-if="activeChips.length" class="active-chips">
            <span class="active-chips-lab">{{ t('list.filtering') }}</span>
            <button v-for="f in activeChips" :key="f.key" type="button" class="fchip" @click="f.clear()">
              {{ f.label }}
              <span class="fchip-x"><app-icon name="close" :size="13" :stroke="2.4" /></span>
            </button>
            <button type="button" class="fchip-clear" @click="clear">{{ t('list.clearAllShort') }}</button>
          </div>

          <div v-if="results.length" class="grid">
            <product-card v-for="p in results" :key="p.id" :product="p" :badge="badgeFor(p)" />
          </div>
          <div v-else class="empty">
            <app-icon name="search" :size="40" style="margin-bottom:14px; opacity:.5;" />
            <p style="font-size:17px; font-weight:600; color:var(--text-soft);">{{ t('list.emptyTitle') }}</p>
            <p style="margin-top:6px;">{{ t('list.emptyDesc') }}</p>
            <n-button style="margin-top:16px" @click="clear">{{ t('list.emptyClear') }}</n-button>
          </div>
        </div>
      </div>
    </section>
  </div>
</template>
