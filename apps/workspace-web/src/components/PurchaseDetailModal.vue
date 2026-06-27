<script setup lang="ts">
import { computed } from 'vue'
import type { OrderResponse } from '@/api/order-service'
import { OrderStatus } from '@/api/order-service'
import type { ItemDownloads } from '@/stores/purchases'
import { formatOrderAmount, formatOrderTime, orderStatusMeta } from '@/utils/order'

const props = defineProps<{
  show: boolean
  order: OrderResponse | null
  loading: boolean
  error: string | null
  /** key：OrderItemResponse.id，value：該項目可下載檔案載入狀態。 */
  downloads: Record<string, ItemDownloads>
}>()

const emit = defineEmits<{ 'update:show': [boolean] }>()

const open = computed({
  get: () => props.show,
  set: (v: boolean) => emit('update:show', v),
})

const isCompleted = computed(() => props.order?.status === OrderStatus.Completed)

/** 將 bytes 轉為可讀大小字串。 */
function formatBytes(bytes?: number): string {
  if (!bytes || bytes <= 0) return '—'
  const units = ['B', 'KB', 'MB', 'GB', 'TB']
  let v = bytes
  let i = 0
  while (v >= 1024 && i < units.length - 1) { v /= 1024; i++ }
  return `${v.toFixed(v >= 10 || i === 0 ? 0 : 1)} ${units[i]}`
}
</script>

<template>
  <n-modal v-model:show="open" preset="card" title="購買明細" style="max-width:680px;">
    <n-spin :show="loading">
      <div v-if="error" class="pd-error">{{ error }}</div>
      <div v-else-if="order" class="pd-body">
        <dl class="pd-dl">
          <div>
            <dt>訂單編號</dt>
            <dd class="pd-mono">{{ order.orderNumber || '—' }}</dd>
          </div>
          <div>
            <dt>狀態</dt>
            <dd><n-tag :type="orderStatusMeta(order.status).type" size="small" round>{{ orderStatusMeta(order.status).label }}</n-tag></dd>
          </div>
          <div>
            <dt>金額</dt>
            <dd class="pd-amount">{{ formatOrderAmount(order.totalAmount, order.currency) }}</dd>
          </div>
          <div>
            <dt>購買時間</dt>
            <dd>{{ formatOrderTime(order.completedAt || order.createdAt) }}</dd>
          </div>
        </dl>

        <div v-if="!isCompleted" class="pd-notice">
          此訂單尚未完成，完成付款後即可下載商品內容。
        </div>

        <div class="pd-section-label">商品（{{ order.items?.length ?? 0 }}）</div>
        <div v-if="!order.items?.length" class="pd-empty">無項目</div>

        <div v-for="it in order.items" :key="it.id" class="pd-item">
          <div class="pd-item-head">
            <div class="pd-item-name">{{ it.catalogName || '—' }}</div>
            <div class="pd-item-price">{{ formatOrderAmount(it.unitPrice, order.currency) }}</div>
          </div>

          <!-- 可下載檔案：僅已完成訂單載入 -->
          <template v-if="isCompleted && it.id">
            <div v-if="downloads[it.id]?.loading" class="pd-files-state">
              <n-spin size="small" /> 載入下載連結…
            </div>
            <div v-else-if="downloads[it.id]?.error" class="pd-files-state pd-files-error">
              {{ downloads[it.id].error }}
            </div>
            <div v-else-if="downloads[it.id]?.files.length" class="pd-files">
              <a
                v-for="f in downloads[it.id].files"
                :key="f.id"
                class="pd-file"
                :href="f.downloadUrl || undefined"
                :download="f.fileName || undefined"
                target="_blank"
                rel="noopener">
                <span class="pd-file-main">
                  <app-icon name="file" :size="16" />
                  <span class="pd-file-name">{{ f.fileName || '—' }}</span>
                </span>
                <span class="pd-file-size">{{ formatBytes(f.fileSize) }}</span>
                <app-icon name="download" :size="16" class="pd-file-dl" />
              </a>
            </div>
            <div v-else class="pd-files-state">此商品版本無可下載檔案。</div>
          </template>
        </div>
      </div>
    </n-spin>
  </n-modal>
</template>

<style scoped>
.pd-error {
  padding: 24px;
  text-align: center;
  color: var(--c-red, #e5484d);
}

.pd-dl {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 10px 24px;
  margin: 0 0 18px;
}

.pd-dl > div {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}

.pd-dl dt {
  font-size: 11.5px;
  color: var(--text-faint);
  font-weight: 600;
}

.pd-dl dd {
  margin: 0;
  font-size: 13px;
  word-break: break-all;
}

.pd-mono {
  font-family: var(--oj-mono);
  color: var(--text-soft);
}

.pd-amount {
  font-weight: 700;
}

.pd-notice {
  font-size: 12.5px;
  color: var(--text-soft);
  background: var(--c-amber-soft, rgba(245, 166, 35, 0.12));
  border-radius: 8px;
  padding: 10px 12px;
  margin-bottom: 16px;
}

.pd-section-label {
  font-size: 11.5px;
  color: var(--text-faint);
  font-weight: 600;
  margin: 4px 0 8px;
}

.pd-empty {
  color: var(--text-faint);
  font-size: 13px;
}

.pd-item {
  border: 1.5px solid var(--border);
  border-radius: 10px;
  padding: 12px 14px;
  margin-bottom: 12px;
}

.pd-item-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.pd-item-name {
  font-weight: 700;
  font-size: 14px;
  min-width: 0;
}

.pd-item-price {
  font-family: var(--oj-mono);
  font-size: 13px;
  color: var(--text-soft);
  flex: none;
}

.pd-files {
  display: grid;
  gap: 6px;
  margin-top: 12px;
}

.pd-file {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 8px 10px;
  border-radius: 8px;
  background: var(--bg-soft, rgba(0, 0, 0, 0.03));
  color: inherit;
  text-decoration: none;
  transition: background 0.12s;
}

.pd-file:hover {
  background: var(--c-violet-soft, rgba(124, 92, 246, 0.12));
}

.pd-file-main {
  display: flex;
  align-items: center;
  gap: 9px;
  min-width: 0;
  flex: 1 1 auto;
}

.pd-file-name {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  font-size: 13px;
}

.pd-file-size {
  font-family: var(--oj-mono);
  font-size: 11.5px;
  color: var(--text-faint);
  flex: none;
}

.pd-file-dl {
  color: var(--c-violet, #7c5cf6);
  flex: none;
}

.pd-files-state {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-top: 12px;
  font-size: 12.5px;
  color: var(--text-faint);
}

.pd-files-error {
  color: var(--c-red, #e5484d);
}

@media (max-width: 600px) {
  .pd-dl {
    grid-template-columns: 1fr;
  }
}
</style>
