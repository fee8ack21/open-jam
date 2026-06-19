<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useMessage } from 'naive-ui'
import { storeToRefs } from 'pinia'
import { useDashboardStore } from '@/stores/dashboard'
import { useStoreListStore } from '@/stores/storeList'
import { useCatalogStore } from '@/stores/catalog'
import { CatalogStatus, type CatalogSummaryDto } from '@/api/catalog-service'

const STATUS = {
  [CatalogStatus.Published]: { label: '已發佈', type: 'success' as const },
  [CatalogStatus.Draft]:     { label: '草稿',   type: 'default' as const },
  [CatalogStatus.Archived]:  { label: '已封存', type: 'warning' as const },
  [CatalogStatus.Suspended]: { label: '已停權', type: 'error' as const },
}
function statusOf(s?: CatalogStatus) {
  return (s != null && STATUS[s]) || { label: '—', type: 'default' as const }
}

const route = useRoute()
const message = useMessage()
const dashboard = useDashboardStore()
const storeList = useStoreListStore()
const catalog = useCatalogStore()
const { items } = storeToRefs(storeList)
const { products, loading, busyId } = storeToRefs(catalog)

const storeId = computed(() => String(route.params.id ?? ''))
const store = computed(() => items.value.find((s) => s.id === storeId.value) ?? null)
const publishedCount = computed(() => products.value.filter((p) => p.status === CatalogStatus.Published).length)

function fmtDate(v?: string | null) {
  return v ? new Date(v).toLocaleDateString('zh-TW') : '—'
}

async function load() {
  // 商店列表為 admin 列表 store：若使用者直接以 URL 進入，先確保 items 已載入以取得商店資訊
  if (!items.value.length) await storeList.load()
  await catalog.loadByStore(storeId.value)
}

/** 停權商品（任一狀態 → Suspended）。僅 Admin 可操作。 */
async function onSuspend(p: CatalogSummaryDto) {
  if (!p.id) return
  const ok = await catalog.suspend(p.id, storeId.value)
  if (ok) message.success('已停權商品。')
  else message.error(catalog.error ?? '停權失敗')
}

/** 解除停權（Suspended → Archived，與後端一致）。僅 Admin 可操作。 */
async function onUnsuspend(p: CatalogSummaryDto) {
  if (!p.id) return
  const ok = await catalog.unsuspend(p.id, storeId.value)
  if (ok) message.success('已解除停權，商品回到已封存狀態。')
  else message.error(catalog.error ?? '解除停權失敗')
}

onMounted(load)
</script>

<template>
  <div data-screen-label="商店商品">
    <div class="page-head">
      <div>
        <p class="h-eyebrow">平台管理</p>
        <h1 class="h-title">{{ store?.storeName ?? '商店商品' }}</h1>
        <p class="h-sub">
          <span v-if="store" class="store-mono">{{ store.storeSlug }}.openjam.co</span>
          <span v-if="store"> · </span>{{ products.length }} 件商品，其中 {{ publishedCount }} 件已發佈
        </p>
      </div>
    </div>

    <!-- 麵包屑：返回商店列表 -->
    <div class="store-breadcrumb">
      <button @click="dashboard.go('stores')">商店列表</button>
      <span>/</span>
      <strong>{{ store?.storeName ?? storeId }}</strong>
    </div>

    <n-spin :show="loading">
      <div class="card-pad store-table-card" style="padding:8px 8px 4px;">
        <table class="tbl store-product-table">
          <thead>
            <tr>
              <th>作品</th>
              <th class="hide-sm">狀態</th>
              <th class="num hide-sm">售價</th>
              <th class="num hide-sm">上架時間</th>
              <th style="width:130px; text-align:right;">操作</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="!loading && !products.length">
              <td colspan="5" style="text-align:center; padding:48px 24px;">
                <span class="kpi-ic" style="background:var(--c-violet); margin:0 auto 14px;"><app-icon name="box" :size="22" /></span>
                <div style="font-weight:700; font-size:15px;">這間商店還沒有商品</div>
                <div style="font-size:13px; color:var(--text-faint); margin-top:4px;">商家上架作品後會顯示於此。</div>
              </td>
            </tr>
            <tr v-for="p in products" v-else :key="p.id">
              <td>
                <div class="prod-cell">
                  <span class="store-rank"><app-icon name="box" :size="16" /></span>
                  <div style="min-width:0;">
                    <div class="pc-title">{{ p.name }}</div>
                    <div class="pc-meta store-mono">{{ p.slug }}</div>
                  </div>
                </div>
              </td>
              <td class="hide-sm">
                <n-tag :type="statusOf(p.status).type" size="small" round>{{ statusOf(p.status).label }}</n-tag>
              </td>
              <td class="num hide-sm">{{ p.price === 0 ? '免費' : '$' + p.price }}</td>
              <td class="num hide-sm store-mono">{{ fmtDate(p.publishedAt) }}</td>
              <td>
                <div class="row-actions" style="justify-content:flex-end;">
                  <!-- 停權 / 解除停權（後端：Suspend 任一狀態 → Suspended；Unsuspend → Archived） -->
                  <n-button
                    v-if="p.status === CatalogStatus.Suspended"
                    size="small"
                    tertiary
                    :disabled="busyId === p.id"
                    :loading="busyId === p.id"
                    @click="onUnsuspend(p)">
                    <template #icon><app-icon name="refresh" :size="15" /></template>
                    解除停權
                  </n-button>
                  <n-popconfirm v-else @positive-click="onSuspend(p)">
                    <template #trigger>
                      <n-button size="small" type="error" :disabled="busyId === p.id">
                        <template #icon><app-icon name="lock" :size="15" /></template>
                        停權
                      </n-button>
                    </template>
                    停權後此商品將自市集下架且買家無法購買，可隨時解除。確定停權？
                  </n-popconfirm>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </n-spin>
  </div>
</template>

<style scoped>
.store-table-card {
  border-radius: 10px;
}

.store-product-table thead th {
  font-size: 12.5px;
  padding-top: 12px;
  vertical-align: middle;
}

.store-product-table thead th + th {
  border-left: 1.5px solid var(--border);
}

.store-product-table tbody td + td {
  border-left: 1.5px solid var(--border);
}

.store-rank {
  width: 30px;
  height: 30px;
  border-radius: 10px;
  display: grid;
  place-items: center;
  flex: none;
  background: var(--oj-primary-wash);
  color: var(--oj-primary);
}

.store-mono {
  font-family: var(--oj-mono);
  color: var(--text-soft);
}

.store-breadcrumb {
  display: flex;
  align-items: center;
  gap: 8px;
  margin: -10px 0 16px;
  color: var(--text-faint);
  font-size: 13px;
  font-weight: 600;
}

.store-breadcrumb button {
  border: 0;
  background: transparent;
  color: var(--oj-primary);
  cursor: pointer;
  font: inherit;
  padding: 0;
}

.store-breadcrumb strong {
  color: var(--text);
}
</style>
