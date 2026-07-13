<script setup lang="ts">
/* ============================================================
   ReviewList — 公開買家評價列表（星等 + 留言 + 日期）。
   讀公開端點 catalogReviews.list；評論者以「買家」匿名呈現。
   ============================================================ */
import { computed, onMounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { catalogApi } from '@/api';
import type { CatalogReviewDto } from '@/api/catalog-service';
import Stars from '@/components/Stars.vue';
import AppIcon from '@/components/app-icon';

const props = defineProps<{ catalogId: string }>();
const { t, locale } = useI18n();
const PAGE = 10;

const items = ref<CatalogReviewDto[]>([]);
const average = ref(0);
const count = ref(0);
const distribution = ref<number[]>([0, 0, 0, 0, 0]); // 索引 0 = 1★ … 索引 4 = 5★
const loading = ref(false);
const loaded = ref(false);

// 由高到低（5★ → 1★）的分佈列，含各星等占總數的百分比。
const distRows = computed(() =>
  [5, 4, 3, 2, 1].map((star) => {
    const c = distribution.value[star - 1] ?? 0;
    return { star, count: c, pct: count.value ? Math.round((c / count.value) * 100) : 0 };
  }),
);

async function load(append = false) {
  if (loading.value || !props.catalogId) return;
  loading.value = true;
  try {
    const res = await catalogApi.catalogReviews.list(props.catalogId, {
      Offset: append ? items.value.length : 0,
      Limit: PAGE,
    });
    average.value = res.data.ratingAverage ?? 0;
    count.value = res.data.ratingCount ?? 0;
    const dist = res.data.ratingDistribution;
    if (dist && dist.length === 5) distribution.value = dist;
    const batch = res.data.items ?? [];
    items.value = append ? [...items.value, ...batch] : batch;
    loaded.value = true;
  } catch {
    // 取不到評論時靜默（不影響商品頁其他內容）
  } finally {
    loading.value = false;
  }
}

const fmtDate = (v?: string | null) => (v ? new Date(v).toLocaleDateString(locale.value) : '');
const hasMore = () => items.value.length < count.value;

onMounted(() => load());
</script>

<template>
  <div v-if="loaded" class="review-list">
    <div class="rl-head">
      <h2 class="section-title" style="margin:0;">{{ t('review.title') }}</h2>
      <stars v-if="count" :value="average" :count="count" :size="15" />
    </div>

    <div v-if="!count" class="rl-empty">
      <app-icon name="star" :size="28" style="opacity:.4" />
      <p>{{ t('review.empty') }}</p>
    </div>

    <div v-else class="rl-dist">
      <div v-for="row in distRows" :key="row.star" class="rl-dist-row">
        <span class="rl-dist-star">{{ row.star }}<app-icon name="star" :size="12" /></span>
        <span class="rl-dist-track"><span class="rl-dist-fill" :style="{ width: row.pct + '%' }" /></span>
        <span class="rl-dist-count">{{ row.count }}</span>
      </div>
    </div>

    <div v-if="count" class="rl-items">
      <div v-for="r in items" :key="r.id" class="rl-item">
        <span class="rl-avatar"><app-icon name="user" :size="16" /></span>
        <div class="rl-body">
          <div class="rl-meta">
            <n-rate :value="r.rating" readonly size="small" />
            <span class="rl-date">{{ fmtDate(r.createdAt) }}</span>
          </div>
          <p v-if="r.comment" class="rl-comment">{{ r.comment }}</p>
          <p v-else class="rl-nocomment">{{ t('review.noComment') }}</p>
        </div>
      </div>

      <button v-if="hasMore()" type="button" class="rl-more" :disabled="loading" @click="load(true)">
        {{ t('review.loadMore') }}
      </button>
    </div>
  </div>
</template>

<style scoped>
.review-list { margin-top: 40px; }
.rl-head { display: flex; align-items: center; justify-content: space-between; margin-bottom: 16px; }
.rl-empty {
  display: flex; flex-direction: column; align-items: center; gap: 8px;
  padding: 28px; color: var(--text-soft); font-size: 14px; font-weight: 500;
  border: 2px dashed var(--border-strong); border-radius: var(--r-lg); background: var(--surface);
}
.rl-dist { display: flex; flex-direction: column; gap: 7px; margin-bottom: 24px; }
.rl-dist-row { display: flex; align-items: center; gap: 10px; }
.rl-dist-star {
  display: inline-flex; align-items: center; gap: 3px;
  width: 34px; flex: none; font-size: 13px; color: var(--text-soft);
  font-variant-numeric: tabular-nums;
}
.rl-dist-track {
  flex: 1; height: 10px; border-radius: 999px;
  background: var(--surface); border: 2px solid var(--border-strong); overflow: hidden;
}
.rl-dist-fill { display: block; height: 100%; background: var(--c-yellow); }
.rl-dist-count {
  width: 30px; flex: none; text-align: right;
  font-size: 12px; color: var(--text-faint); font-variant-numeric: tabular-nums;
}
.rl-items { display: flex; flex-direction: column; gap: 18px; }
.rl-item { display: flex; gap: 12px; }
.rl-avatar {
  width: 34px; height: 34px; border-radius: 50%; flex: none;
  display: grid; place-items: center;
  background: var(--surface); border: 2px solid var(--border-strong);
}
.rl-body { flex: 1; min-width: 0; }
.rl-meta { display: flex; align-items: center; gap: 10px; }
.rl-date { font-size: 12px; color: var(--text-faint); font-family: var(--oj-mono); }
.rl-comment { margin: 6px 0 0; font-size: 14px; line-height: 1.55; color: var(--text); white-space: pre-wrap; }
.rl-nocomment { margin: 6px 0 0; font-size: 13px; color: var(--text-faint); }
.rl-more {
  align-self: flex-start; margin-top: 4px;
  border: 2px solid var(--border-strong); background: var(--surface); color: var(--text);
  padding: 8px 18px; border-radius: 999px; font-size: 13px; font-weight: 900; cursor: pointer;
  transition: background .15s;
}
.rl-more:hover { background: var(--c-yellow); }
.rl-more:disabled { opacity: .5; cursor: default; }
</style>
