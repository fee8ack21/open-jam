<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useMessage } from 'naive-ui'
import { storeToRefs } from 'pinia'
import { useDashboardStore } from '@/stores/dashboard'
import { useCatalogStore } from '@/stores/catalog'
import { useStoreApplicationStore } from '@/stores/storeApplication'
import { CatalogStatus, type CatalogSummaryDto } from '@/api/catalog-service'

const STATUS = {
  [CatalogStatus.Published]: { label: '已上架', type: 'success' as const },
  [CatalogStatus.Draft]:     { label: '草稿',   type: 'default' as const },
  [CatalogStatus.Archived]:  { label: '已下架', type: 'warning' as const },
  [CatalogStatus.Suspended]: { label: '已停權', type: 'error' as const },
}
function statusOf(s?: CatalogStatus) {
  return (s != null && STATUS[s]) || { label: '—', type: 'default' as const }
}

const dashboard = useDashboardStore()
const message = useMessage()
const catalog = useCatalogStore()
const storeApp = useStoreApplicationStore()
const { products, loading, busyId } = storeToRefs(catalog)

const filterKey = ref<'all' | CatalogStatus>('all')

// 目前登入創作者的商店 id（取第一間）
const storeId = computed(() => storeApp.stores[0]?.store?.id ?? '')

const filters = computed(() => [
  { key: 'all' as const, label: '全部', n: products.value.length },
  { key: CatalogStatus.Published, label: '上架中', n: catalog.statusCount(CatalogStatus.Published) },
  { key: CatalogStatus.Draft, label: '草稿', n: catalog.statusCount(CatalogStatus.Draft) },
  { key: CatalogStatus.Archived, label: '已下架', n: catalog.statusCount(CatalogStatus.Archived) },
  { key: CatalogStatus.Suspended, label: '已停權', n: catalog.statusCount(CatalogStatus.Suspended) },
])
const rows = computed(() =>
  filterKey.value === 'all' ? products.value : products.value.filter((p) => p.status === filterKey.value),
)

function fmtDate(v?: string | null) {
  return v ? new Date(v).toLocaleDateString('zh-TW') : '—'
}
function coverStyle(hue?: number) {
  const h = hue ?? 256
  return { background: `linear-gradient(135deg, hsl(${h} 88% 62%), hsl(${(h + 42) % 360} 90% 54%))` }
}
// 只有 Published / Archived 可用開關切換上下架；Draft 需先補版本後於精靈上架，Suspended 僅 Admin 可解
function canToggle(p: CatalogSummaryDto) {
  return p.status === CatalogStatus.Published || p.status === CatalogStatus.Archived
}

async function toggle(p: CatalogSummaryDto) {
  if (!p.id) return
  const wasPublished = p.status === CatalogStatus.Published
  const ok = wasPublished
    ? await catalog.archive(p.id, storeId.value)
    : await catalog.publish(p.id, storeId.value)
  if (ok) message.success(wasPublished ? '已下架。' : '已上架。')
  else message.error(catalog.error ?? '操作失敗')
}

async function load() {
  if (!storeApp.stores.length) await storeApp.load()
  await catalog.load(storeId.value)
}

onMounted(load)
</script>

<template>
  <div data-screen-label="商品管理">
    <div class="page-head">
      <div>
        <p class="h-eyebrow">賣家工作室</p>
        <h1 class="h-title">商品管理</h1>
        <p class="h-sub">{{ products.length }} 件作品 · {{ catalog.publishedCount }} 件上架中</p>
      </div>
      <button class="cta-pop" @click="dashboard.go('upload')"><app-icon name="plus" :size="16" :stroke="2.4" style="vertical-align:-3px; margin-right:4px;" />上架新商品</button>
    </div>

    <div style="display:flex; align-items:center; justify-content:space-between; gap:14px; margin-bottom:18px; flex-wrap:wrap;">
      <div class="tabs">
        <button v-for="f in filters" :key="String(f.key)" :class="{ on: filterKey === f.key }" @click="filterKey = f.key">
          {{ f.label }} <span class="tcount">{{ f.n }}</span>
        </button>
      </div>
    </div>

    <n-spin :show="loading">
      <div class="card-pad" style="padding:8px 8px 4px;">
        <table class="tbl">
          <thead>
            <tr>
              <th>作品</th>
              <th class="hide-sm">狀態</th>
              <th class="num hide-sm">售價</th>
              <th class="num hide-sm">上架時間</th>
              <th style="width:160px; text-align:right;">上架</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="p in rows" :key="p.id">
              <td>
                <div class="prod-cell">
                  <span class="prod-cover" :style="coverStyle(p.coverHue)">
                    <img v-if="p.thumbnailUrl" :src="p.thumbnailUrl" alt="" />
                  </span>
                  <div style="min-width:0;">
                    <div class="pc-title">{{ p.name }}</div>
                    <div class="pc-meta">{{ p.summary || p.slug }}</div>
                  </div>
                </div>
              </td>
              <td class="hide-sm"><n-tag :type="statusOf(p.status).type" size="small" round>{{ statusOf(p.status).label }}</n-tag></td>
              <td class="num hide-sm">{{ p.price === 0 ? '免費' : '$' + p.price }}</td>
              <td class="num hide-sm" style="font-family:var(--oj-mono); color:var(--text-soft);">{{ fmtDate(p.publishedAt) }}</td>
              <td>
                <div class="row-actions">
                  <n-switch v-if="canToggle(p)" :value="p.status === CatalogStatus.Published" :loading="busyId === p.id" :disabled="busyId === p.id" @update:value="toggle(p)" size="medium" />
                  <span v-else style="font-size:12px; color:var(--text-faint); font-family:var(--oj-mono); margin-right:6px;">
                    {{ p.status === CatalogStatus.Suspended ? '已停權' : '草稿' }}
                  </span>
                  <button class="ic-act" title="編輯" @click="dashboard.go('upload')"><app-icon name="edit" :size="17" /></button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
        <div v-if="!loading && !rows.length" class="empty-box">
          <div class="eb-ic"><app-icon name="box" :size="30" /></div>
          <div class="eb-t">這個分類還沒有作品</div>
        </div>
      </div>
    </n-spin>
  </div>
</template>

<style scoped>
.prod-cover {
  width: 42px;
  height: 42px;
  border-radius: 10px;
  flex: none;
  overflow: hidden;
  display: grid;
  place-items: center;
}
.prod-cover img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}
</style>
