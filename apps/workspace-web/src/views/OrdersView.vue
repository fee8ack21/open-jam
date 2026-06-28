<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { storeToRefs } from 'pinia'
import { useSellerOrdersStore } from '@/stores/sellerOrders'
import { useStoreApplicationStore } from '@/stores/storeApplication'
import OrderDetailModal from '@/components/OrderDetailModal.vue'
import { orderStatusOptions, formatOrderAmount, formatOrderTime, orderStatusMeta } from '@/utils/order'
import type { OrderStatus, OrderSummaryDto } from '@/api/order-service'

const { t } = useI18n()
const statusOptions = computed(() => orderStatusOptions())
const store = useSellerOrdersStore()
const storeApp = useStoreApplicationStore()
const { items, totalCount, loading, error, detail, detailLoading, detailError } = storeToRefs(store)

// 賣家本人商店 ID（開店資料由 router guard 先載入）；變動時重新綁定查詢來源。
const myStoreId = computed(() => storeApp.primaryStore?.id ?? null)

// 篩選狀態（買家信箱即時 debounce，狀態下拉即時生效）
const emailFilter = ref('')
const statusFilter = ref<OrderStatus | null>(null)
const page = ref(1)

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / store.pageSize)))

let filterTimer: ReturnType<typeof setTimeout> | undefined
async function applyFilter() {
  clearTimeout(filterTimer)
  page.value = 1
  await store.applyFilter({ buyerEmail: emailFilter.value, status: statusFilter.value })
}
watch(emailFilter, () => {
  clearTimeout(filterTimer)
  filterTimer = setTimeout(applyFilter, 300)
})
watch(statusFilter, () => {
  clearTimeout(filterTimer)
  applyFilter()
})

async function changePage(p: number) {
  page.value = p
  await store.goPage(p)
}

const detailOpen = ref(false)
async function openDetail(row: OrderSummaryDto) {
  if (!row.id) return
  detailOpen.value = true
  await store.loadDetail(row.id)
}

/** 綁定（或切換）查詢商店並重置篩選表單。 */
async function bindStore(id: string) {
  emailFilter.value = ''
  statusFilter.value = null
  page.value = 1
  await store.setStore(id)
}

watch(myStoreId, (id) => { if (id) bindStore(id) })
onMounted(() => { if (myStoreId.value) bindStore(myStoreId.value) })
</script>

<template>
  <div :data-screen-label="t('route.orders')">
    <div class="page-head">
      <div>
        <p class="h-eyebrow">{{ t('sidebar.sellerStudio') }}</p>
        <h1 class="h-title">{{ t('route.orders') }}</h1>
        <p class="h-sub">{{ t('orders.total', { count: totalCount }) }}</p>
      </div>
    </div>

    <!-- 篩選工具列 -->
    <div class="card-pad history-toolbar">
      <div class="filter-bar">
        <div class="fb-group">
          <div class="fb-field" style="flex:2 1 240px;">
            <label class="fb-label">{{ t('orders.buyerEmail') }}</label>
            <n-input
              v-model:value="emailFilter"
              clearable
              :placeholder="t('orders.buyerEmailPlaceholder')"
              @keyup.enter="applyFilter">
              <template #prefix><app-icon name="search" :size="16" /></template>
            </n-input>
          </div>
          <div class="fb-field" style="flex:1 1 180px;">
            <label class="fb-label">{{ t('orders.orderStatus') }}</label>
            <n-select
              v-model:value="statusFilter"
              :options="statusOptions" />
          </div>
        </div>
      </div>
    </div>

    <div v-if="error" class="card-pad od-load-error">{{ error }}</div>

    <n-spin :show="loading">
      <!-- 訂單表格：即使無資料仍顯示表頭，空狀態以 tbody 整列呈現 -->
      <div class="card-pad history-table-card" style="padding:8px 8px 4px;">
        <div class="history-table-wrap">
          <table class="tbl history-table">
            <thead>
              <tr>
                <th>{{ t('orders.colOrderNumber') }}</th>
                <th>{{ t('orders.colBuyer') }}</th>
                <th class="hide-sm">{{ t('common.status') }}</th>
                <th class="num">{{ t('orders.colAmount') }}</th>
                <th class="hide-sm">{{ t('orders.colCreatedAt') }}</th>
                <th style="width:64px; text-align:right;">{{ t('orders.colDetail') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="!items.length">
                <td colspan="6" style="text-align:center; padding:48px 24px;">
                  <span class="kpi-ic" style="background:var(--c-violet); margin:0 auto 14px;"><app-icon name="receipt" :size="22" /></span>
                  <div style="font-weight:700; font-size:15px;">{{ t('orders.emptyTitle') }}</div>
                  <div style="font-size:13px; color:var(--text-faint); margin-top:4px;">
                    {{ t('orders.emptyDesc') }}
                  </div>
                </td>
              </tr>
              <tr v-for="o in items" :key="o.id">
                <td><span class="history-mono" style="font-size:12.5px;">{{ o.orderNumber || '—' }}</span></td>
                <td>
                  <span class="history-mono" :title="o.id" style="font-size:12px;">{{ o.id ? o.id.slice(0, 8) : '—' }}</span>
                </td>
                <td class="hide-sm"><n-tag :type="orderStatusMeta(o.status).type" size="small" round>{{ t(orderStatusMeta(o.status).labelKey) }}</n-tag></td>
                <td class="num" style="font-weight:700;">{{ formatOrderAmount(o.totalAmount, o.currency) }}</td>
                <td class="hide-sm"><span class="history-mono" style="font-size:12px;">{{ formatOrderTime(o.createdAt) }}</span></td>
                <td style="text-align:right;">
                  <n-button text @click="openDetail(o)"><app-icon name="eye" :size="18" /></n-button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <div v-if="totalPages > 1" class="history-pager">
          <n-pagination
            :page="page"
            :page-count="totalPages"
            @update:page="changePage" />
        </div>
      </div>
    </n-spin>

    <order-detail-modal
      v-model:show="detailOpen"
      :order="detail"
      :loading="detailLoading"
      :error="detailError" />
  </div>
</template>

<style scoped>
.history-toolbar {
  margin-bottom: 16px;
  border-radius: 10px;
}

.history-toolbar :deep(.n-input),
.history-toolbar :deep(.n-input-wrapper),
.history-toolbar :deep(.n-base-selection),
.history-toolbar :deep(.n-base-selection__border),
.history-toolbar :deep(.n-base-selection__state-border) {
  border-radius: 10px;
}

.history-toolbar :deep(.n-input__border),
.history-toolbar :deep(.n-input__state-border) {
  border-radius: 10px;
}

.filter-bar {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  align-items: flex-end;
}

.fb-group {
  display: flex;
  gap: 12px;
  align-items: flex-end;
  flex: 1 1 360px;
  min-width: 0;
}

.fb-field {
  display: flex;
  flex-direction: column;
  gap: 6px;
  min-width: 0;
}

.fb-label {
  font-size: 12.5px;
  font-weight: 600;
  color: var(--text-soft);
}

.od-load-error {
  margin-bottom: 16px;
  border-radius: 10px;
  color: var(--c-red, #e5484d);
  font-size: 13px;
}

.history-table-card {
  border-radius: 10px;
}

.history-table-wrap {
  overflow-x: auto;
}

.history-table {
  min-width: 760px;
}

.history-table thead th {
  font-size: 12.5px;
  padding-top: 12px;
  vertical-align: middle;
}

.history-table thead th + th {
  border-left: 1.5px solid var(--border);
}

.history-table tbody td + td {
  border-left: 1.5px solid var(--border);
}

.history-mono {
  font-family: var(--oj-mono);
  color: var(--text-soft);
}

.history-pager {
  display: flex;
  justify-content: flex-end;
  padding: 12px 8px;
}
</style>
