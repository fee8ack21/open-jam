<script setup lang="ts">
/* ============================================================
   ProductQuickView — marketplace-hub quick-look lightbox.
   Opens from a <product-card> / <featured-card> click (in place of
   navigating away), showing a preview gallery, full work details and
   a deep-link out to the creator's storefront (creator-web) to buy.
   ============================================================ */
import { ref, computed, watch, onBeforeUnmount } from 'vue';
import { useI18n } from 'vue-i18n';
import { env } from '@/environment.js';
import type { Product } from '@/data/products';

const props = defineProps<{
  product: Product | null;
  /** optional corner ribbon carried over from the originating card */
  badge?: { label: string; tone: 'hot' | 'new' | 'feat' } | null;
}>();
const emit = defineEmits<{ (e: 'close'): void }>();
const { t } = useI18n();

// ----- preview gallery -----
// Demo data carries a single real thumbnail (`image`) plus a `previews`
// count. The first slot shows the real image; the rest are seeded gradient
// teasers (matching how ProductThumb renders placeholders elsewhere).
const activeIdx = ref(0);
const previewCount = computed(() => Math.max(1, props.product?.previews ?? 1));
const previewIdxs = computed(() => Array.from({ length: previewCount.value }, (_, i) => i));

/** For slot 0 keep the real image; later slots strip it to a seeded gradient. */
function slotProduct(idx: number): Product {
  const p = props.product as Product;
  return idx === 0 ? p : { ...p, image: undefined };
}
const stageProduct = computed(() => slotProduct(activeIdx.value));

const href = computed(() =>
  props.product
    ? `${env.CREATOR_PAGE_BASE_URL.replace('<store-slug>', props.product.storeSlug)}/product/${props.product.id}`
    : '#',
);
const initials = computed(() =>
  (props.product?.creator ?? '').split(' ').map((s) => s[0]).slice(0, 2).join(''),
);
const isFree = computed(() => props.product?.price === 0);

function close() {
  emit('close');
}
function onKeydown(e: KeyboardEvent) {
  if (e.key === 'Escape') close();
}

// lock body scroll + wire Escape only while a product is shown; reset the
// gallery to the first frame each time a new work is opened.
watch(
  () => props.product,
  (p) => {
    activeIdx.value = 0;
    if (p) {
      document.body.style.overflow = 'hidden';
      window.addEventListener('keydown', onKeydown);
    } else {
      document.body.style.overflow = '';
      window.removeEventListener('keydown', onKeydown);
    }
  },
);
onBeforeUnmount(() => {
  document.body.style.overflow = '';
  window.removeEventListener('keydown', onKeydown);
});
</script>

<template>
  <Transition name="qv">
    <div v-if="product" class="qv-scrim" @click.self="close">
        <div class="qv-card" role="dialog" aria-modal="true" :aria-label="product.title">
          <button class="qv-x" :aria-label="t('quickView.close')" @click="close">
            <app-icon name="close" :size="16" :stroke="2.2" />
          </button>

          <!-- ---------- preview gallery ---------- -->
          <div class="qv-gallery">
            <div class="qv-stage">
              <product-thumb :product="stageProduct" :seed="activeIdx" hide-label />
              <span v-if="badge" class="qv-badge" :class="'b-' + badge.tone">{{ badge.label }}</span>
            </div>
            <div v-if="previewCount > 1" class="qv-thumbs" role="tablist">
              <button
                v-for="i in previewIdxs"
                :key="i"
                type="button"
                class="qv-thumb"
                :class="{ on: i === activeIdx }"
                :aria-label="t('quickView.previewAria', { n: i + 1 })"
                :aria-selected="i === activeIdx"
                role="tab"
                @click="activeIdx = i"
              >
                <product-thumb :product="slotProduct(i)" :seed="i" :glyph-size="26" :show-cat="false" hide-label />
              </button>
            </div>
          </div>

          <!-- ---------- details ---------- -->
          <div class="qv-info">
            <div class="qv-info-body">
              <div v-if="product.tags.length" class="qv-tags">
                <span v-for="tag in product.tags" :key="tag" class="qv-tag">#{{ tag }}</span>
              </div>

              <h2 class="qv-title">{{ product.title }}</h2>

              <div class="qv-creator">
                <span class="avatar" :style="{ background: product.avatar }">{{ initials }}</span>
                <span class="qv-creator-txt">
                  <b>{{ product.creator }}</b>
                  <span class="qv-handle">{{ product.handle }}</span>
                </span>
              </div>

              <div class="qv-meta">
                <stars :value="product.rating" :count="product.ratingCount" />
                <span class="qv-dot" aria-hidden="true"></span>
                <span class="qv-sold">{{ t('quickView.sales', { count: product.sales }) }}</span>
              </div>

              <p class="qv-blurb">{{ product.blurb }}</p>

              <div class="qv-prose">
                <p v-for="(para, i) in product.desc" :key="i">{{ para }}</p>
              </div>

              <!-- spec chips -->
              <div class="qv-specs">
                <div class="qv-spec">
                  <span class="qv-spec-k">{{ t('quickView.formats') }}</span>
                  <span class="qv-spec-v">{{ product.formats.join(' · ') }}</span>
                </div>
                <div class="qv-spec">
                  <span class="qv-spec-k">{{ t('quickView.totalSize') }}</span>
                  <span class="qv-spec-v">{{ product.totalSize }}</span>
                </div>
              </div>

              <!-- included files -->
              <div v-if="product.contents.length" class="qv-contents">
                <p class="qv-contents-h">{{ t('quickView.includes') }}</p>
                <div v-for="(c, i) in product.contents" :key="i" class="qv-file">
                  <span class="qv-file-ic"><app-icon name="download" :size="15" /></span>
                  <span class="qv-file-name">{{ c.name }}</span>
                  <span class="qv-file-type">{{ c.type }}</span>
                  <span class="qv-file-size">{{ c.size }}</span>
                </div>
              </div>
            </div>

            <!-- sticky footer: price + storefront deep-link -->
            <div class="qv-foot">
              <span class="qv-price" :class="{ free: isFree }">
                {{ isFree ? t('common.free') : '$' + product.price }}
              </span>
              <a class="qv-cta" :href="href" target="_blank" rel="noopener noreferrer">
                {{ isFree ? t('quickView.ctaFree') : t('quickView.cta') }}
                <app-icon name="chevron" :size="17" :stroke="2.4" />
              </a>
            </div>
          </div>
        </div>
      </div>
  </Transition>
</template>

<style scoped>
.qv-scrim {
  position: fixed; inset: 0; z-index: 200;
  background: rgba(26, 22, 38, 0.42); backdrop-filter: blur(4px);
  display: grid; place-items: center; padding: 28px;
}
.qv-card {
  position: relative;
  width: 100%; max-width: 960px; max-height: min(88vh, 760px);
  display: flex; align-items: stretch;
  background: var(--surface); border: 1.5px solid var(--border-strong); border-radius: var(--r-lg);
  box-shadow: 8px 8px 0 var(--text), 0 40px 90px -30px rgba(20, 16, 60, 0.6);
  overflow: hidden;
}
.qv-x {
  position: absolute; top: 16px; right: 16px; z-index: 6; width: 36px; height: 36px; border-radius: 10px;
  cursor: pointer; border: 1.5px solid var(--border-strong); background: var(--surface);
  box-shadow: 2px 2px 0 var(--border-strong); color: var(--text);
  display: grid; place-items: center; transition: transform 0.14s, box-shadow 0.14s, color 0.14s;
}
.qv-x:hover { transform: translate(-1px, -1px); box-shadow: 3px 3px 0 var(--border-strong); color: var(--c-pink); }

/* ---------- gallery ---------- */
.qv-gallery {
  flex: 0 0 47%;
  display: flex; flex-direction: column; justify-content: center; min-width: 0; min-height: 0;
  border-right: 1.5px solid var(--border-strong); background: var(--surface-2);
  padding: 18px; gap: 14px;
}
.qv-stage {
  position: relative; border: 1.5px solid var(--border-strong); border-radius: var(--r-md);
  overflow: hidden; box-shadow: var(--pop-2);
}
.qv-stage :deep(.thumb) { position: relative; inset: auto; aspect-ratio: 4 / 3; height: auto; border-radius: 0; }
.qv-badge {
  position: absolute; top: 12px; left: 12px; z-index: 4;
  font-family: var(--oj-font); font-weight: 800; font-size: 11px; letter-spacing: 0.02em; color: var(--text);
  padding: 4px 10px; border-radius: 999px; border: 1.5px solid var(--text); box-shadow: var(--pop-1);
}
.qv-badge.b-hot { background: var(--c-lime); }
.qv-badge.b-new { background: var(--c-pink); color: #fff; }
.qv-badge.b-feat { background: var(--c-yellow); }
.qv-thumbs { display: flex; gap: 9px; flex-wrap: wrap; }
.qv-thumb {
  width: 60px; padding: 0; cursor: pointer; background: none;
  border: 1.5px solid var(--border-strong); border-radius: 10px; overflow: hidden;
  transition: transform 0.14s, box-shadow 0.14s; box-shadow: var(--pop-1);
}
.qv-thumb:hover { transform: translate(-1px, -1px); box-shadow: var(--pop-1-h); }
.qv-thumb.on { outline: 2.5px solid var(--c-violet); outline-offset: 1px; }
.qv-thumb :deep(.thumb) { position: relative; inset: auto; aspect-ratio: 1 / 1; height: auto; border-radius: 0; }

/* ---------- info ---------- */
.qv-info { flex: 1 1 0; display: flex; flex-direction: column; min-width: 0; min-height: 0; }
.qv-info-body { flex: 1; min-height: 0; overflow-y: auto; padding: 30px 26px 18px; }
.qv-tags { display: flex; flex-wrap: wrap; gap: 7px; margin-bottom: 12px; }
.qv-tag {
  font-family: var(--oj-mono); font-size: 11.5px; font-weight: 600; color: var(--oj-primary);
  background: var(--oj-wash); border-radius: 999px; padding: 3px 10px;
}
.qv-title {
  font-family: var(--oj-display); font-weight: 800; font-size: 24px; line-height: 1.2;
  letter-spacing: -0.6px; color: var(--text); margin: 0 0 14px; text-wrap: pretty;
}
.qv-creator { display: flex; align-items: center; gap: 10px; margin-bottom: 12px; }
.qv-creator .avatar {
  width: 34px; height: 34px; border-radius: 50%; flex: none; color: #fff;
  display: grid; place-items: center; font-family: var(--oj-font); font-weight: 700; font-size: 12px;
  border: 1.5px solid var(--text);
}
.qv-creator-txt { display: flex; flex-direction: column; line-height: 1.3; min-width: 0; }
.qv-creator-txt b { font-weight: 700; font-size: 14.5px; color: var(--text); }
.qv-handle { font-size: 12.5px; color: var(--text-soft); }
.qv-meta { display: flex; align-items: center; gap: 11px; margin-bottom: 18px; }
.qv-dot { width: 4px; height: 4px; border-radius: 50%; background: var(--text-faint); }
.qv-sold { font-size: 12.5px; color: var(--text-soft); font-weight: 500; }
.qv-blurb { margin: 0 0 16px; font-size: 15px; line-height: 1.65; color: var(--text); font-weight: 500; }
.qv-prose p { margin: 0 0 12px; font-size: 14.5px; line-height: 1.75; color: var(--text-soft); }
.qv-specs { display: flex; gap: 10px; flex-wrap: wrap; margin: 18px 0; }
.qv-spec {
  flex: 1 1 auto; min-width: 130px;
  background: var(--surface-2); border: 1.5px solid var(--border); border-radius: var(--r-md); padding: 11px 14px;
}
.qv-spec-k {
  display: block; font-family: var(--oj-mono); font-size: 10.5px; letter-spacing: 0.08em;
  text-transform: uppercase; color: var(--text-faint); margin-bottom: 4px;
}
.qv-spec-v { font-size: 14px; font-weight: 600; color: var(--text); }
.qv-contents-h {
  font-family: var(--oj-display); font-weight: 700; font-size: 15px; color: var(--text); margin: 0 0 10px;
}
.qv-file {
  display: flex; align-items: center; gap: 11px; padding: 10px 13px;
  background: var(--surface-2); border: 1.5px solid var(--border); border-radius: var(--r-md);
}
.qv-file + .qv-file { margin-top: 8px; }
.qv-file-ic { color: var(--oj-primary); flex: none; display: grid; place-items: center; }
.qv-file-name {
  flex: 1; min-width: 0; font-size: 13.5px; font-weight: 500; color: var(--text);
  white-space: nowrap; overflow: hidden; text-overflow: ellipsis;
}
.qv-file-type {
  flex: none; font-family: var(--oj-mono); font-size: 10.5px; font-weight: 600; color: var(--text-soft);
  background: var(--surface); border: 1.5px solid var(--border); border-radius: 6px; padding: 2px 6px;
}
.qv-file-size { flex: none; font-size: 12px; color: var(--text-faint); font-variant-numeric: tabular-nums; }

/* ---------- sticky footer ---------- */
.qv-foot {
  flex: none; display: flex; align-items: center; justify-content: space-between; gap: 16px;
  padding: 16px 26px; border-top: 1.5px solid var(--border); background: var(--surface);
}
.qv-price {
  font-family: var(--oj-display); font-weight: 800; font-size: 26px; color: var(--text); letter-spacing: -0.6px;
}
.qv-price.free { color: #11a36a; }
.qv-cta {
  display: inline-flex; align-items: center; gap: 6px; text-decoration: none;
  cursor: pointer; border: 1.5px solid var(--text);
  font-family: var(--oj-font); font-weight: 700; font-size: 15px; color: #fff;
  padding: 12px 20px; border-radius: 14px;
  background: linear-gradient(135deg, var(--c-violet), var(--c-pink));
  box-shadow: 4px 4px 0 var(--text); transition: transform 0.14s, box-shadow 0.14s;
}
.qv-cta:hover { transform: translate(-2px, -2px); box-shadow: 6px 6px 0 var(--text); }
.qv-cta:active { transform: translate(1px, 1px); box-shadow: 1px 1px 0 var(--text); }

/* ---------- transitions ---------- */
.qv-enter-active, .qv-leave-active { transition: opacity 0.22s ease; }
.qv-enter-from, .qv-leave-to { opacity: 0; }
.qv-enter-active .qv-card { animation: qv-in 0.34s cubic-bezier(0.2, 1.15, 0.4, 1); }
@keyframes qv-in {
  from { opacity: 0; transform: translateY(18px) scale(0.97); }
  to { opacity: 1; transform: none; }
}

/* ---------- responsive ---------- */
@media (max-width: 780px) {
  .qv-scrim { padding: 0; align-items: flex-end; }
  .qv-card {
    flex-direction: column;
    max-width: 100%; max-height: 92vh;
    border-radius: var(--r-lg) var(--r-lg) 0 0; border-bottom: 0;
    box-shadow: 0 -8px 40px rgba(20, 16, 60, 0.4);
  }
  .qv-gallery { flex: 0 0 auto; border-right: 0; border-bottom: 1.5px solid var(--border-strong); }
  .qv-info-body { padding: 22px 20px 14px; }
  .qv-foot { padding: 14px 20px; }
  .qv-title { font-size: 21px; }
}
</style>
