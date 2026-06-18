<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { storeToRefs } from 'pinia'
import { useStoreReviewStore } from '@/stores/storeReview'
import { StoreApplicationStatus } from '@/api/store-service'

const store = useStoreReviewStore()
const { history, loading } = storeToRefs(store)

// 審核結果 → 顯示用標籤
const RESULT = {
  [StoreApplicationStatus.Approved]: { label: '已核准', type: 'success' as const },
  [StoreApplicationStatus.Rejected]: { label: '已駁回', type: 'error' as const },
}
function resultOf(s?: StoreApplicationStatus) {
  return (s != null && RESULT[s as keyof typeof RESULT]) || { label: '—', type: 'default' as const }
}

// ── 篩選 / 排序狀態 ──────────────────────────────────────────
const keyword = ref('')
/** 審核結果篩選：all | Approved | Rejected */
const statusFilter = ref<'all' | StoreApplicationStatus>('all')
/** 排序欄位 + 方向 */
const sortKey = ref<'reviewedAt' | 'createdAt' | 'storeName'>('reviewedAt')
const sortDesc = ref(true)

const statusOptions = [
  { label: '全部結果', value: 'all' },
  { label: '已核准', value: StoreApplicationStatus.Approved },
  { label: '已駁回', value: StoreApplicationStatus.Rejected },
]
const sortOptions = [
  { label: '審核時間', value: 'reviewedAt' },
  { label: '申請時間', value: 'createdAt' },
  { label: '商店名稱', value: 'storeName' },
]

function fmtDate(v?: string | null) {
  return v ? new Date(v).toLocaleString('zh-TW', { hour12: false }) : '—'
}
function initial(email?: string | null) {
  return (email?.charAt(0) || '?').toUpperCase()
}

/** 套用關鍵字、結果篩選與排序後的清單。 */
const visible = computed(() => {
  const q = keyword.value.trim().toLowerCase()
  let list = history.value.slice()

  if (statusFilter.value !== 'all') {
    list = list.filter((a) => a.status === statusFilter.value)
  }
  if (q) {
    list = list.filter((a) =>
      [a.storeName, a.storeSlug, a.email]
        .some((f) => (f ?? '').toLowerCase().includes(q)),
    )
  }

  const dir = sortDesc.value ? -1 : 1
  list.sort((a, b) => {
    const key = sortKey.value
    const av = (a[key] ?? '') as string
    const bv = (b[key] ?? '') as string
    if (key === 'storeName') return av.localeCompare(bv, 'zh-Hant') * dir
    // 時間欄位以 ISO 字串可直接字典序比較
    return (av < bv ? -1 : av > bv ? 1 : 0) * dir
  })
  return list
})

onMounted(store.load)
</script>

<template>
  <div data-screen-label="審核紀錄">
    <div class="page-head">
      <div>
        <p class="h-eyebrow">平台管理</p>
        <h1 class="h-title">審核紀錄</h1>
        <p class="h-sub">共 {{ history.length }} 筆已審核申請</p>
      </div>
    </div>

    <!-- 篩選 / 排序工具列 -->
    <div class="card-pad" style="margin-bottom:16px;">
      <div style="display:flex; flex-wrap:wrap; gap:12px; align-items:center;">
        <n-input
          v-model:value="keyword"
          clearable
          placeholder="搜尋商店名稱、子網域或信箱"
          style="flex:1; min-width:220px;">
          <template #prefix><app-icon name="search" :size="16" /></template>
        </n-input>

        <n-select
          v-model:value="statusFilter"
          :options="statusOptions"
          style="width:140px; flex:none;" />

        <n-select
          v-model:value="sortKey"
          :options="sortOptions"
          style="width:140px; flex:none;" />

        <n-button tertiary style="flex:none;" @click="sortDesc = !sortDesc"
                  :title="sortDesc ? '改為遞增' : '改為遞減'">
          <template #icon><app-icon :name="sortDesc ? 'chevronD' : 'chevronU'" :size="16" /></template>
          {{ sortDesc ? '遞減' : '遞增' }}
        </n-button>
      </div>
    </div>

    <n-spin :show="loading">
      <!-- 無符合紀錄 -->
      <div v-if="!loading && !visible.length" class="card-pad" style="text-align:center; padding:48px 24px;">
        <span class="kpi-ic" style="background:var(--c-violet); margin:0 auto 14px;"><app-icon name="receipt" :size="22" /></span>
        <div style="font-weight:700; font-size:15px;">
          {{ history.length ? '沒有符合條件的紀錄' : '尚無審核紀錄' }}
        </div>
        <div style="font-size:13px; color:var(--text-faint); margin-top:4px;">
          {{ history.length ? '試試調整關鍵字或篩選條件。' : '完成核准／駁回後，紀錄會顯示於此。' }}
        </div>
      </div>

      <!-- 紀錄列表 -->
      <div v-else class="card-pad">
        <div v-for="a in visible" :key="a.id"
             style="display:flex; align-items:flex-start; gap:12px; padding:14px 0; border-bottom:1.5px solid var(--border);">
          <span class="avatar" style="width:36px; height:36px; font-size:15px; flex:none;">{{ initial(a.email) }}</span>
          <div style="flex:1; min-width:0;">
            <div style="display:flex; align-items:center; gap:10px;">
              <div style="font-weight:600; font-size:14px;">{{ a.storeName }}</div>
              <n-tag :type="resultOf(a.status).type" size="small" round>{{ resultOf(a.status).label }}</n-tag>
            </div>
            <div style="font-family:var(--oj-mono); font-size:12px; color:var(--text-faint); margin-top:3px;">
              {{ a.storeSlug }}.openjam.co · {{ a.email }}
            </div>
            <div style="font-family:var(--oj-mono); font-size:12px; color:var(--text-faint); margin-top:2px;">
              申請於 {{ fmtDate(a.createdAt) }} · 審核於 {{ fmtDate(a.reviewedAt) }}
            </div>
            <div v-if="a.reviewComment" style="font-size:12.5px; color:var(--text-faint); margin-top:6px;">
              駁回原因：{{ a.reviewComment }}
            </div>
          </div>
        </div>
      </div>
    </n-spin>
  </div>
</template>
