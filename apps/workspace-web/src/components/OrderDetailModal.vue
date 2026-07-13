<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import type { OrderResponse } from '@/api/order-service'
import { formatOrderAmount, formatOrderTime, orderStatusMeta } from '@/utils/order'

const { t } = useI18n()

const props = defineProps<{
  show: boolean
  order: OrderResponse | null
  loading: boolean
  error: string | null
}>()

const emit = defineEmits<{ 'update:show': [boolean] }>()

const open = computed({
  get: () => props.show,
  set: (v: boolean) => emit('update:show', v),
})

/** 將 GUID 縮短顯示（前 8 碼），完整值以 title 提示。 */
function shortId(v?: string | null) {
  return v ? v.slice(0, 8) : '—'
}
</script>

<template>
  <n-modal v-model:show="open" preset="card" :title="t('orderModal.title')" style="max-width:680px;">
    <n-spin :show="loading">
      <div v-if="error" class="od-error">{{ error }}</div>
      <div v-else-if="order" class="od-body">
        <dl class="od-dl">
          <div>
            <dt>{{ t('orderModal.orderNumber') }}</dt>
            <dd class="od-mono">{{ order.orderNumber || '—' }}</dd>
          </div>
          <div>
            <dt>{{ t('orderModal.status') }}</dt>
            <dd><n-tag :type="orderStatusMeta(order.status).type" size="small" round>{{ t(orderStatusMeta(order.status).labelKey) }}</n-tag></dd>
          </div>
          <div>
            <dt>{{ t('orderModal.buyerEmail') }}</dt>
            <dd>{{ order.buyerEmail || '—' }}</dd>
          </div>
          <div>
            <dt>{{ t('orderModal.buyer') }}</dt>
            <dd class="od-mono" :title="order.buyerUserId ?? t('orderModal.anonymousTitle')">{{ order.buyerUserId ? shortId(order.buyerUserId) : t('orderModal.anonymous') }}</dd>
          </div>
          <div>
            <dt>{{ t('orderModal.amount') }}</dt>
            <dd class="od-amount">{{ formatOrderAmount(order.totalAmount, order.currency) }}</dd>
          </div>
          <div>
            <dt>{{ t('orderModal.currency') }}</dt>
            <dd class="od-mono">{{ (order.currency || '—').toUpperCase() }}</dd>
          </div>
          <div>
            <dt>{{ t('orderModal.createdAt') }}</dt>
            <dd>{{ formatOrderTime(order.createdAt) }}</dd>
          </div>
          <div>
            <dt>{{ t('orderModal.completedAt') }}</dt>
            <dd>{{ formatOrderTime(order.completedAt) }}</dd>
          </div>
        </dl>

        <div class="od-section-label">{{ t('orderModal.itemsLabel', { count: order.items?.length ?? 0 }) }}</div>
        <table class="tbl od-items">
          <thead>
            <tr>
              <th>{{ t('orderModal.colProduct') }}</th>
              <th class="num">{{ t('orderModal.colUnitPrice') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="!order.items?.length">
              <td colspan="2" style="text-align:center; color:var(--text-faint); padding:16px;">{{ t('orderModal.noItems') }}</td>
            </tr>
            <tr v-for="it in order.items" :key="it.id">
              <td>{{ it.catalogName || '—' }}</td>
              <td class="num">{{ formatOrderAmount(it.unitPrice, order.currency) }}</td>
            </tr>
          </tbody>
        </table>

        <div class="od-section-label">{{ t('orderModal.statusHistory') }}</div>
        <ul class="od-timeline">
          <li v-if="!order.statusHistory?.length" class="od-empty">{{ t('orderModal.noHistory') }}</li>
          <li v-for="(h, i) in order.statusHistory" :key="i" class="od-tl-item">
            <span class="od-tl-dot" :class="`tl-${orderStatusMeta(h.newStatus).type}`"></span>
            <div class="od-tl-body">
              <div class="od-tl-head">
                <n-tag :type="orderStatusMeta(h.newStatus).type" size="small" round>{{ t(orderStatusMeta(h.newStatus).labelKey) }}</n-tag>
                <span v-if="h.oldStatus" class="od-tl-from">{{ t('orderModal.changedFrom', { status: t(orderStatusMeta(h.oldStatus).labelKey) }) }}</span>
                <span v-else class="od-tl-from">{{ t('orderModal.created') }}</span>
              </div>
              <div v-if="h.reason" class="od-tl-reason">{{ h.reason }}</div>
              <div class="od-tl-time">{{ formatOrderTime(h.createdAt) }}</div>
            </div>
          </li>
        </ul>
      </div>
    </n-spin>
  </n-modal>
</template>

<style scoped>
.od-error {
  padding: 24px;
  text-align: center;
  color: var(--c-pink-deep);
}

.od-dl {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 10px 24px;
  margin: 0 0 18px;
}

.od-dl > div {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}

.od-dl dt {
  font-size: 11.5px;
  color: var(--text-faint);
  font-weight: 700;
}

.od-dl dd {
  margin: 0;
  font-size: 13px;
  word-break: break-all;
}

.od-mono {
  font-family: var(--oj-mono);
  color: var(--text-soft);
}

.od-amount {
  font-weight: 700;
}

.od-section-label {
  font-size: 11.5px;
  color: var(--text-faint);
  font-weight: 700;
  margin: 4px 0 6px;
}

.od-items {
  width: 100%;
  margin-bottom: 18px;
}

.od-items th {
  font-size: 12px;
  padding-top: 8px;
}

/* 狀態歷程時間軸 */
.od-timeline {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.od-empty {
  color: var(--text-faint);
  font-size: 13px;
}

.od-tl-item {
  display: flex;
  gap: 12px;
  align-items: flex-start;
}

.od-tl-dot {
  flex: none;
  width: 10px;
  height: 10px;
  border-radius: 50%;
  margin-top: 5px;
  border: var(--bw) solid var(--border-strong);
  background: var(--surface);
}

.tl-success { background: var(--c-lime); }
.tl-info { background: var(--c-cyan); }
.tl-warning { background: var(--c-yellow); }
.tl-error { background: var(--c-pink-deep); }

.od-tl-head {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.od-tl-from {
  font-size: 12.5px;
  color: var(--text-soft);
}

.od-tl-reason {
  font-size: 12.5px;
  color: var(--text-soft);
  margin-top: 3px;
}

.od-tl-time {
  font-family: var(--oj-mono);
  font-size: 11.5px;
  color: var(--text-faint);
  margin-top: 3px;
}

@media (max-width: 600px) {
  .od-dl {
    grid-template-columns: 1fr;
  }
}
</style>
