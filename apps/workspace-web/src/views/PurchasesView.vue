<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useDashboardStore } from '@/stores/dashboard'
import { JFmt as F } from '@/utils/format'

const store = useDashboardStore()

const PAGE_SIZE = 8
const page = ref(1)
/** 日期範圍篩選（[起, 迄] 毫秒時間戳），null 表示不限。 */
const dateRange = ref<[number, number] | null>(null)

/** 將毫秒時間戳轉為當地 YYYY-MM-DD，與購買紀錄的 date 欄位同格式以利字串比較。 */
function dayKey(ts: number): string {
  const d = new Date(ts)
  const p = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}`
}

const filtered = computed(() => {
  if (!dateRange.value) return store.purchases
  const [from, to] = [dayKey(dateRange.value[0]), dayKey(dateRange.value[1])]
  return store.purchases.filter((p) => p.date >= from && p.date <= to)
})

const totalPages = computed(() => Math.max(1, Math.ceil(filtered.value.length / PAGE_SIZE)))
const list = computed(() => {
  const start = (page.value - 1) * PAGE_SIZE
  return filtered.value.slice(start, start + PAGE_SIZE)
})

const spent = computed(() => filtered.value.reduce((s, p) => s + p.price, 0))
const fileCount = computed(() => filtered.value.reduce((s, p) => s + p.product.contents.length, 0))

// 篩選條件變動時回到第一頁，避免停在已不存在的頁碼。
watch(dateRange, () => { page.value = 1 })
</script>

<template>
  <div data-screen-label="購買紀錄">
    <div class="page-head">
      <div>
        <p class="h-eyebrow">我的收藏庫</p>
        <h1 class="h-title">購買紀錄</h1>
        <p class="h-sub">{{ filtered.length }} 筆訂單 · 共 {{ fileCount }} 個可下載檔案 · 累計花費 {{ F.money(spent) }}</p>
      </div>
    </div>

    <!-- 篩選工具列 -->
    <div class="card-pad history-toolbar">
      <div class="filter-bar">
        <div class="fb-group">
          <div class="fb-field" style="flex:1 1 auto;">
            <label class="fb-label">購買時間</label>
            <n-date-picker
              v-model:value="dateRange"
              type="daterange"
              clearable
              start-placeholder="起始日期"
              end-placeholder="結束日期" />
          </div>
        </div>
      </div>
    </div>

    <div v-if="!filtered.length" class="card-pad" style="text-align:center; padding:48px 24px;">
      <span class="kpi-ic" style="background:var(--c-violet); margin:0 auto 14px;"><app-icon name="note" :size="22" /></span>
      <div style="font-weight:700; font-size:15px;">尚無購買紀錄</div>
      <div style="font-size:13px; color:var(--text-faint); margin-top:4px;">
        沒有符合所選日期範圍的訂單，試試調整或重設篩選條件。
      </div>
    </div>

    <div v-else class="pur-list">
      <div v-for="pu in list" :key="pu.orderId" class="pur-row">
        <product-thumb :product="pu.product" :glyph-size="30" :show-cat="false" hide-label />
        <div class="pr-body">
          <div class="pr-title">{{ pu.product.title }}</div>
          <div class="pr-meta">
            <span style="display:inline-flex; align-items:center; gap:6px;">
              <span class="avatar" :style="{ background: pu.product.avatar, width:'18px', height:'18px', fontSize:'9px' }">{{ F.initials(pu.product.creator) }}</span>
              {{ pu.product.creator }}
            </span>
            <span style="color:var(--text-faint)">·</span>
            <span style="font-family:var(--oj-mono); font-size:12px;">{{ pu.product.formats.join(' · ') }} · {{ pu.product.totalSize }}</span>
            <span style="color:var(--text-faint)">·</span>
            <span style="font-family:var(--oj-mono); font-size:12px; color:var(--text-faint);">{{ F.date(pu.date) }} 購買</span>
          </div>
        </div>
        <div style="display:flex; flex-direction:column; align-items:flex-end; gap:10px; flex:none;">
          <span class="price" style="font-size:16px;">{{ pu.price === 0 ? '免費' : '$' + pu.price }}</span>
          <div style="display:flex; gap:8px;">
            <n-popover trigger="click" placement="bottom-end" :show-arrow="false" to=".oj-root">
              <template #trigger><n-button size="small" tertiary>{{ pu.product.contents.length }} 個檔案 <template #icon><app-icon name="chevronD" :size="14" /></template></n-button></template>
              <div style="display:grid; gap:2px; min-width:240px; padding:2px;">
                <button v-for="(f, i) in pu.product.contents" :key="i" class="menu-item" style="justify-content:space-between;">
                  <span style="display:flex; align-items:center; gap:9px; min-width:0;"><app-icon name="file" :size="16" /> <span style="overflow:hidden; text-overflow:ellipsis; white-space:nowrap;">{{ f.name }}</span></span>
                  <span style="font-family:var(--oj-mono); font-size:11px; color:var(--text-faint); flex:none;">{{ f.size }}</span>
                </button>
              </div>
            </n-popover>
            <n-button size="small" type="primary">
              <template #icon><app-icon name="download" :size="15" /></template>
              下載
            </n-button>
          </div>
        </div>
      </div>

      <div class="pur-pager">
        <n-pagination
          :page="page"
          :page-count="totalPages"
          @update:page="(p: number) => (page = p)" />
      </div>
    </div>
  </div>
</template>

<style scoped>
/* 對齊 admin 頁面（AuditLogView）的 10px 圓角 */
.history-toolbar {
  margin-bottom: 16px;
  border-radius: 10px;
}

.history-toolbar :deep(.n-date-picker),
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

/* 篩選列：兩組各佔一半，單行並排（共四欄平均分布），不足時整組換行成最多兩行 */
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

/* 欄位：標籤在上、控制項在下，撐滿配置的 flex 寬度 */
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

/* 各筆購買紀錄圓角對齊 admin 頁面的 10px */
.pur-row {
  border-radius: 10px;
}

.pur-pager {
  display: flex;
  justify-content: flex-end;
  padding: 12px 4px 4px;
}
</style>
