<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useMessage } from 'naive-ui'
import { useI18n } from 'vue-i18n'
import { storeToRefs } from 'pinia'
import { useAdminOrdersStore } from '@/stores/adminOrders'
import OrderDetailModal from '@/components/OrderDetailModal.vue'
import { orderStatusOptions, formatOrderAmount, formatOrderTime, orderStatusMeta } from '@/utils/order'
import type { OrderStatus, OrderSummaryDto } from '@/api/order-service'

const { t } = useI18n()
const message = useMessage()
const statusOptions = computed(() => orderStatusOptions())
const store = useAdminOrdersStore()
const { items, totalCount, loading, error, detail, detailLoading, detailError } = storeToRefs(store)

// 載入錯誤以彈出 message 呈現，表格維持空狀態
watch(error, (msg) => { if (msg) message.error(msg) })

// 篩選狀態（買家信箱即時 debounce，狀態下拉即時生效）
const emailFilter = ref('')
const statusFilter = ref<OrderStatus | null>(null)
const page = ref(1)

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / store.pageSize)))

async function applyFilter() {
  page.value = 1
  await store.applyFilter({ buyerEmail: emailFilter.value, status: statusFilter.value })
}
// 買家信箱改由「搜尋」按鈕 / Enter 觸發；下拉狀態維持即時套用
watch(statusFilter, () => {
  applyFilter()
})

async function changePage(p: number) {
  page.value = p
  await store.goPage(p)
}

async function changePageSize(size: number) {
  page.value = 1
  await store.setPageSize(size)
}

const detailOpen = ref(false)
async function openDetail(row: OrderSummaryDto) {
  if (!row.id) return
  detailOpen.value = true
  await store.loadDetail(row.id)
}

onMounted(store.load)
</script>

<template>
  <div :data-screen-label="t('route.adminOrders')">

    <n-spin :show="loading">
      <!-- 篩選列與訂單表格合併為單一卡片：篩選在上、整寬分隔線、表格在下 -->
      <div class="card-pad history-table-card">
        <div class="list-filter">
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
              <n-button class="fb-search-btn" type="primary" :loading="loading" @click="applyFilter">
                <template #icon><app-icon name="search" :size="16" /></template>
                {{ t('common.search') }}
              </n-button>
            </div>
          </div>
        </div>

        <!-- 訂單表格：即使無資料仍顯示表頭，空狀態以 tbody 整列呈現 -->
        <div class="history-table-wrap">
          <table class="tbl history-table">
            <thead>
              <tr>
                <th>{{ t('orders.colOrderNumber') }}</th>
                <th class="hide-sm">{{ t('orders.colBuyer') }}</th>
                <th>{{ t('common.status') }}</th>
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
                <td class="hide-sm history-mono" :title="o.id" style="font-size:12px;">{{ o.id ? o.id.slice(0, 8) : '—' }}</td>
                <td><n-tag :type="orderStatusMeta(o.status).type" size="small" round>{{ t(orderStatusMeta(o.status).labelKey) }}</n-tag></td>
                <td class="num" style="font-weight:700;">{{ formatOrderAmount(o.totalAmount, o.currency) }}</td>
                <td class="hide-sm"><span class="history-mono" style="font-size:12px;">{{ formatOrderTime(o.createdAt) }}</span></td>
                <td style="text-align:right;">
                  <n-button text @click="openDetail(o)"><app-icon name="eye" :size="18" /></n-button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <div class="history-pager">
          <list-pager
            :page="page"
            :page-count="totalPages"
            :page-size="store.pageSize"
            @update:page="changePage"
            @update:page-size="changePageSize" />
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
/* 篩選區段：卡片頂部，底部整寬分隔線與表格分開 */
.list-filter {
  padding: 18px 20px;
  border-bottom: 2px solid var(--border-strong);
  background: var(--bg);
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
  font-size: 12px;
  font-weight: 900;
  color: var(--text);
}

/* 搜尋按鈕與輸入框同高、同圓角（Input heightMedium 於 App.vue 覆寫為 42px） */
.fb-search-btn {
  height: 40px;
}

.history-table-card {
  padding: 0;
  border-radius: var(--r-lg);
  overflow: hidden;
}

.history-table-wrap {
  overflow-x: auto;
  padding: 0 10px;
}

.history-table {
  min-width: 820px;
}

.history-table thead th {
  font-size: 12.5px;
  padding-top: 12px;
  vertical-align: middle;
}



.history-mono {
  font-family: var(--oj-mono);
  color: var(--text-soft);
}

.history-pager {
  display: flex;
  justify-content: flex-end;
  padding: 14px 20px;
}
</style>
