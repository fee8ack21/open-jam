<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { storeToRefs } from 'pinia'
import { usePurchasesStore } from '@/stores/purchases'
import PurchaseDetailModal from '@/components/PurchaseDetailModal.vue'
import { ORDER_STATUS_OPTIONS, formatOrderAmount, formatOrderTime, orderStatusMeta } from '@/utils/order'
import type { OrderStatus, OrderSummaryDto } from '@/api/order-service'

const store = usePurchasesStore()
const { items, totalCount, loading, error, status, detail, detailLoading, detailError, downloads } = storeToRefs(store)

const page = ref(1)
const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / store.pageSize)))

async function applyStatus(next: OrderStatus | null) {
  page.value = 1
  await store.applyStatus(next)
}
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

onMounted(store.load)
</script>

<template>
  <div data-screen-label="購買紀錄">
    <div class="page-head">
      <div>
        <p class="h-eyebrow">我的收藏庫</p>
        <h1 class="h-title">購買紀錄</h1>
        <p class="h-sub">共 {{ totalCount }} 筆訂單 · 點擊明細可查看商品與下載連結</p>
      </div>
    </div>

    <!-- 篩選工具列 -->
    <div class="card-pad history-toolbar">
      <div class="filter-bar">
        <div class="fb-group">
          <div class="fb-field" style="flex:1 1 200px;">
            <label class="fb-label">訂單狀態</label>
            <n-select
              :value="status"
              :options="ORDER_STATUS_OPTIONS"
              @update:value="applyStatus" />
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
                <th>訂單編號</th>
                <th>狀態</th>
                <th class="num">金額</th>
                <th class="hide-sm">建立時間</th>
                <th class="hide-sm">完成時間</th>
                <th style="width:64px; text-align:right;">明細</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="!items.length">
                <td colspan="6" style="text-align:center; padding:48px 24px;">
                  <span class="kpi-ic" style="background:var(--c-violet); margin:0 auto 14px;"><app-icon name="bag" :size="22" /></span>
                  <div style="font-weight:700; font-size:15px;">尚無購買紀錄</div>
                  <div style="font-size:13px; color:var(--text-faint); margin-top:4px;">
                    沒有符合所選狀態的訂單，試試調整或重設篩選條件。
                  </div>
                </td>
              </tr>
              <tr v-for="o in items" :key="o.id">
                <td><span class="history-mono" style="font-size:12.5px;">{{ o.orderNumber || '—' }}</span></td>
                <td><n-tag :type="orderStatusMeta(o.status).type" size="small" round>{{ orderStatusMeta(o.status).label }}</n-tag></td>
                <td class="num" style="font-weight:700;">{{ formatOrderAmount(o.totalAmount, o.currency) }}</td>
                <td class="hide-sm"><span class="history-mono" style="font-size:12px;">{{ formatOrderTime(o.createdAt) }}</span></td>
                <td class="hide-sm"><span class="history-mono" style="font-size:12px;">{{ formatOrderTime(o.completedAt) }}</span></td>
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

    <purchase-detail-modal
      v-model:show="detailOpen"
      :order="detail"
      :loading="detailLoading"
      :error="detailError"
      :downloads="downloads" />
  </div>
</template>

<style scoped>
.history-toolbar {
  margin-bottom: 16px;
  border-radius: 10px;
}

.history-toolbar :deep(.n-base-selection),
.history-toolbar :deep(.n-base-selection__border),
.history-toolbar :deep(.n-base-selection__state-border) {
  border-radius: 10px;
}

.filter-bar {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  align-items: center;
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
