<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useMessage } from 'naive-ui'
import { useI18n } from 'vue-i18n'
import { storeToRefs } from 'pinia'
import { useAdminPaymentsStore } from '@/stores/adminPayments'
import { paymentStatusOptions, paymentStatusMeta } from '@/utils/payment'
import { formatOrderAmount, formatOrderTime } from '@/utils/order'
import type { PaymentStatus, PaymentResponse } from '@/api/payment-service'

const { t } = useI18n()
const message = useMessage()
const statusOptions = computed(() => paymentStatusOptions())
const store = useAdminPaymentsStore()
const { items, totalCount, loading, error } = storeToRefs(store)

// 載入錯誤以彈出 message 呈現，表格維持空狀態
watch(error, (msg) => { if (msg) message.error(msg) })

// 篩選狀態（買家信箱由「搜尋」按鈕 / Enter 觸發，狀態下拉即時生效）
const emailFilter = ref('')
const statusFilter = ref<PaymentStatus | 'all'>('all')
const page = ref(1)

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / store.pageSize)))

async function applyFilter() {
  page.value = 1
  await store.applyFilter({ email: emailFilter.value, status: statusFilter.value === 'all' ? null : statusFilter.value })
}
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

// 詳情彈窗：列表項即完整付款紀錄，直接展示、不需另外查詢
const detailOpen = ref(false)
const detail = ref<PaymentResponse | null>(null)
function openDetail(row: PaymentResponse) {
  detail.value = row
  detailOpen.value = true
}

/** 將 GUID 縮短顯示（前 8 碼），完整值以 title 提示。 */
function shortId(v?: string | null) {
  return v ? v.slice(0, 8) : '—'
}

/** Stripe Dashboard 付款頁深連結（test / live 模式由 Stripe 依帳號自動判定導向）。 */
const stripeUrl = computed(() =>
  detail.value?.providerPaymentId ? `https://dashboard.stripe.com/payments/${detail.value.providerPaymentId}` : null)

onMounted(store.load)
</script>

<template>
  <div :data-screen-label="t('route.adminPayments')">

    <n-spin :show="loading">
      <div class="card-pad history-table-card">
        <div class="list-filter">
          <div class="filter-bar">
            <div class="fb-group">
              <div class="fb-field" style="flex:2 1 240px;">
                <label class="fb-label">{{ t('payments.buyerEmail') }}</label>
                <n-input
                  v-model:value="emailFilter"
                  clearable
                  :placeholder="t('payments.buyerEmailPlaceholder')"
                  @keyup.enter="applyFilter">
                  <template #prefix><app-icon name="search" :size="16" /></template>
                </n-input>
              </div>
              <div class="fb-field" style="flex:1 1 180px;">
                <label class="fb-label">{{ t('payments.paymentStatus') }}</label>
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

        <!-- 付款表格：即使無資料仍顯示表頭，空狀態以 tbody 整列呈現 -->
        <div class="history-table-wrap">
          <table class="tbl history-table">
            <thead>
              <tr>
                <th>{{ t('payments.colCreatedAt') }}</th>
                <th>{{ t('payments.colBuyer') }}</th>
                <th class="hide-sm">{{ t('payments.colOrder') }}</th>
                <th>{{ t('common.status') }}</th>
                <th class="num">{{ t('payments.colAmount') }}</th>
                <th class="num hide-sm">{{ t('payments.colFee') }}</th>
                <th style="width:64px; text-align:right;">{{ t('payments.colDetail') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="!items.length">
                <td colspan="7" style="text-align:center; padding:48px 24px;">
                  <span class="kpi-ic" style="background:var(--c-violet); margin:0 auto 14px;"><app-icon name="card" :size="22" /></span>
                  <div style="font-weight:700; font-size:15px;">{{ t('payments.emptyTitle') }}</div>
                  <div style="font-size:13px; color:var(--text-faint); margin-top:4px;">
                    {{ t('payments.emptyDesc') }}
                  </div>
                </td>
              </tr>
              <tr v-for="p in items" :key="p.id">
                <td><span class="history-mono" style="font-size:12px;">{{ formatOrderTime(p.createdAt) }}</span></td>
                <td style="font-size:13px; max-width:220px; overflow:hidden; text-overflow:ellipsis; white-space:nowrap;" :title="p.email ?? ''">{{ p.email || '—' }}</td>
                <td class="hide-sm"><span class="history-mono" :title="p.orderId" style="font-size:12px;">{{ shortId(p.orderId) }}</span></td>
                <td><n-tag :type="paymentStatusMeta(p.status).type" size="small" round>{{ t(paymentStatusMeta(p.status).labelKey) }}</n-tag></td>
                <td class="num" style="font-weight:700;">{{ formatOrderAmount(p.amount, p.currency) }}</td>
                <td class="num hide-sm"><span class="history-mono" style="font-size:12.5px;">{{ formatOrderAmount(p.applicationFeeAmount, p.currency) }}</span></td>
                <td style="text-align:right;">
                  <n-button text @click="openDetail(p)"><app-icon name="eye" :size="18" /></n-button>
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

    <!-- 付款詳情彈窗 -->
    <n-modal v-model:show="detailOpen" preset="card" :title="t('payments.modalTitle')" style="max-width:640px;">
      <div v-if="detail" class="pd-body">
        <dl class="pd-dl">
          <div>
            <dt>{{ t('payments.colCreatedAt') }}</dt>
            <dd>{{ formatOrderTime(detail.createdAt) }}</dd>
          </div>
          <div>
            <dt>{{ t('common.status') }}</dt>
            <dd><n-tag :type="paymentStatusMeta(detail.status).type" size="small" round>{{ t(paymentStatusMeta(detail.status).labelKey) }}</n-tag></dd>
          </div>
          <div>
            <dt>{{ t('payments.buyerEmail') }}</dt>
            <dd>{{ detail.email || '—' }}</dd>
          </div>
          <div>
            <dt>{{ t('payments.orderId') }}</dt>
            <dd class="pd-mono" :title="detail.orderId">{{ detail.orderId || '—' }}</dd>
          </div>
          <div>
            <dt>{{ t('payments.storeId') }}</dt>
            <dd class="pd-mono" :title="detail.storeId">{{ detail.storeId || '—' }}</dd>
          </div>
          <div>
            <dt>{{ t('payments.colAmount') }}</dt>
            <dd class="pd-amount">{{ formatOrderAmount(detail.amount, detail.currency) }}</dd>
          </div>
          <div>
            <dt>{{ t('payments.colFee') }}</dt>
            <dd class="pd-mono">{{ formatOrderAmount(detail.applicationFeeAmount, detail.currency) }}</dd>
          </div>
          <div>
            <dt>{{ t('payments.netAmount') }}</dt>
            <dd class="pd-amount">{{ formatOrderAmount((detail.amount ?? 0) - (detail.applicationFeeAmount ?? 0), detail.currency) }}</dd>
          </div>
          <div>
            <dt>{{ t('payments.paidAt') }}</dt>
            <dd>{{ formatOrderTime(detail.paidAt) }}</dd>
          </div>
          <div>
            <dt>{{ t('payments.checkoutSession') }}</dt>
            <dd class="pd-mono" :title="detail.providerCheckoutId ?? ''">{{ detail.providerCheckoutId || '—' }}</dd>
          </div>
        </dl>
        <div v-if="stripeUrl" style="margin-top:16px;">
          <n-button tag="a" :href="stripeUrl" target="_blank" rel="noopener" size="small">
            <template #icon><app-icon name="card" :size="15" /></template>
            {{ t('payments.openInStripe') }}
          </n-button>
        </div>
      </div>
    </n-modal>
  </div>
</template>

<style scoped>
.list-filter {
  padding: 18px 20px;
  border-bottom: var(--bw) solid var(--border-strong);
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
  min-width: 880px;
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

/* 詳情彈窗：兩欄 definition list，與訂單詳情彈窗一致的密度 */
.pd-dl {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 14px 20px;
  margin: 0;
}

.pd-dl dt {
  font-size: 11.5px;
  font-weight: 900;
  letter-spacing: 0.06em;
  color: var(--text-faint);
  margin-bottom: 3px;
}

.pd-dl dd {
  margin: 0;
  font-size: 13.5px;
  font-weight: 500;
  overflow: hidden;
  text-overflow: ellipsis;
}

.pd-mono {
  font-family: var(--oj-mono);
  font-size: 12.5px;
}

.pd-amount {
  font-weight: 900;
}
</style>
