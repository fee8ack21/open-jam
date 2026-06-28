<script setup lang="ts">
/* ============================================================
   ReviewList — 賣家檢視自己商品的買家評價（星等 + 留言 + 日期）。
   讀公開端點 catalogReviews.list；評論者匿名呈現。
   ============================================================ */
import { onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { catalogApi } from '@/api'
import type { CatalogReviewDto } from '@/api/catalog-service'

const { t, locale } = useI18n()
const props = defineProps<{ catalogId: string }>()
const PAGE = 10

const items = ref<CatalogReviewDto[]>([])
const average = ref(0)
const count = ref(0)
const loading = ref(false)
const loaded = ref(false)

async function load(append = false) {
  if (loading.value || !props.catalogId) return
  loading.value = true
  try {
    const res = await catalogApi.catalogReviews.list(props.catalogId, {
      Offset: append ? items.value.length : 0,
      Limit: PAGE,
    })
    average.value = res.data.ratingAverage ?? 0
    count.value = res.data.ratingCount ?? 0
    const batch = res.data.items ?? []
    items.value = append ? [...items.value, ...batch] : batch
    loaded.value = true
  } catch {
    // 靜默
  } finally {
    loading.value = false
  }
}

const fmtDate = (v?: string | null) => (v ? new Date(v).toLocaleDateString(locale.value) : '')
const hasMore = () => items.value.length < count.value

// 切換商品時重新載入
watch(() => props.catalogId, () => { items.value = []; loaded.value = false; load() })
onMounted(() => load())
</script>

<template>
  <div class="rl">
    <div class="rl-summary">
      <div class="rl-avg">{{ count ? average.toFixed(1) : '—' }}</div>
      <div>
        <stars :value="average" :count="count" :size="16" />
        <div class="rl-count">{{ t('reviewList.count', { count }) }}</div>
      </div>
    </div>

    <div v-if="loaded && !count" class="rl-empty">
      <app-icon name="star" :size="26" style="opacity:.4" />
      <p>{{ t('reviewList.empty') }}</p>
    </div>

    <div v-else class="rl-items">
      <div v-for="r in items" :key="r.id" class="rl-item">
        <div class="rl-meta">
          <n-rate :value="r.rating" readonly size="small" />
          <span class="rl-date">{{ fmtDate(r.createdAt) }}</span>
        </div>
        <p v-if="r.comment" class="rl-comment">{{ r.comment }}</p>
        <p v-else class="rl-nocomment">{{ t('reviewList.noComment') }}</p>
      </div>

      <button v-if="hasMore()" type="button" class="rl-more" :disabled="loading" @click="load(true)">
        {{ t('reviewList.loadMore') }}
      </button>
    </div>
  </div>
</template>

<style scoped>
.rl { display: flex; flex-direction: column; gap: 18px; }
.rl-summary { display: flex; align-items: center; gap: 14px; padding-bottom: 14px; border-bottom: 1px solid var(--border); }
.rl-avg { font-size: 32px; font-weight: 800; line-height: 1; color: var(--text); font-family: var(--oj-display, inherit); }
.rl-count { font-size: 12.5px; color: var(--text-faint); margin-top: 4px; }
.rl-empty {
  display: flex; flex-direction: column; align-items: center; gap: 8px;
  padding: 28px; color: var(--text-faint); font-size: 14px;
}
.rl-items { display: flex; flex-direction: column; gap: 16px; }
.rl-item { padding-bottom: 14px; border-bottom: 1px dashed var(--border); }
.rl-item:last-child { border-bottom: 0; }
.rl-meta { display: flex; align-items: center; gap: 10px; }
.rl-date { font-size: 12px; color: var(--text-faint); font-family: var(--oj-mono); }
.rl-comment { margin: 6px 0 0; font-size: 14px; line-height: 1.55; color: var(--text); white-space: pre-wrap; }
.rl-nocomment { margin: 6px 0 0; font-size: 13px; color: var(--text-faint); }
.rl-more {
  align-self: flex-start; border: 1.5px solid var(--border); background: transparent; color: var(--text-soft);
  padding: 7px 14px; border-radius: 999px; font-size: 13px; font-weight: 600; cursor: pointer;
}
.rl-more:disabled { opacity: .5; cursor: default; }
</style>
