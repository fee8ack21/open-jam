<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useMessage } from 'naive-ui'
import { storeToRefs } from 'pinia'
import { useStoreListStore } from '@/stores/storeList'
import { StoreStatus } from '@/api/store-service'

const router = useRouter()
const message = useMessage()
const store = useStoreListStore()
const { items, loading } = storeToRefs(store)

// 每筆商店的處理中狀態（避免重複點擊）
const busyId = ref<string | null>(null)

// 商店狀態 → 顯示用標籤
const STATUS = {
  [StoreStatus.Active]: { label: '營運中', type: 'success' as const },
  [StoreStatus.Suspended]: { label: '已停權', type: 'warning' as const },
  [StoreStatus.Closed]: { label: '已關閉', type: 'default' as const },
}
function statusOf(s?: StoreStatus) {
  return (s != null && STATUS[s as keyof typeof STATUS]) || { label: '—', type: 'default' as const }
}

// ── 篩選 / 排序狀態 ──────────────────────────────────────────
const keyword = ref('')
/** 狀態篩選：all | Active | Suspended | Closed */
const statusFilter = ref<'all' | StoreStatus>('all')
/** 排序欄位 + 方向 */
const sortKey = ref<'storeName' | 'storeSlug' | 'createdAt' | 'updatedAt'>('createdAt')
const sortDesc = ref(true)

const statusOptions = [
  { label: '全部狀態', value: 'all' },
  { label: '營運中', value: StoreStatus.Active },
  { label: '已停權', value: StoreStatus.Suspended },
  { label: '已關閉', value: StoreStatus.Closed },
]
const columns = [
  { key: 'storeName', label: '商店名稱', hideSm: false },
  { key: 'storeSlug', label: '子網域', hideSm: false },
  { key: 'createdAt', label: '建立時間', hideSm: true },
  { key: 'updatedAt', label: '最後更新', hideSm: true },
] as const

function fmtDate(v?: string | null) {
  return v ? new Date(v).toLocaleString('zh-TW', { hour12: false }) : '—'
}
function initial(name?: string | null) {
  return (name?.charAt(0) || '?').toUpperCase()
}

/** 進入該商店的商品列表。 */
function openProducts(id?: string) {
  if (!id) return
  router.push({ name: 'store-products', params: { id } })
}

function toggleSort(key: typeof sortKey.value) {
  if (sortKey.value === key) {
    sortDesc.value = !sortDesc.value
    return
  }
  sortKey.value = key
  sortDesc.value = key === 'createdAt' || key === 'updatedAt'
}

/** 套用關鍵字、狀態篩選與排序後的清單。 */
const visible = computed(() => {
  const q = keyword.value.trim().toLowerCase()
  let list = items.value.slice()

  if (statusFilter.value !== 'all') {
    list = list.filter((s) => s.status === statusFilter.value)
  }
  if (q) {
    list = list.filter((s) =>
      [s.storeName, s.storeSlug, s.description]
        .some((f) => (f ?? '').toLowerCase().includes(q)),
    )
  }

  const dir = sortDesc.value ? -1 : 1
  list.sort((a, b) => {
    const key = sortKey.value
    const av = (a[key] ?? '') as string
    const bv = (b[key] ?? '') as string
    if (key === 'storeName' || key === 'storeSlug') return av.localeCompare(bv, 'zh-Hant') * dir
    // 時間欄位以 ISO 字串可直接字典序比較
    return (av < bv ? -1 : av > bv ? 1 : 0) * dir
  })
  return list
})

async function onSuspend(id?: string) {
  if (!id) return
  busyId.value = id
  const ok = await store.suspend(id)
  busyId.value = null
  if (ok) message.success('已停權商店。')
  else message.error(store.error ?? '停權失敗')
}

async function onUnsuspend(id?: string) {
  if (!id) return
  busyId.value = id
  const ok = await store.unsuspend(id)
  busyId.value = null
  if (ok) message.success('已解除停權，商店恢復營運。')
  else message.error(store.error ?? '解除停權失敗')
}

async function onClose(id?: string) {
  if (!id) return
  busyId.value = id
  const ok = await store.close(id)
  busyId.value = null
  if (ok) message.success('已關閉商店。')
  else message.error(store.error ?? '關閉失敗')
}

onMounted(store.load)
</script>

<template>
  <div data-screen-label="商店列表">
    <div class="page-head">
      <div>
        <p class="h-eyebrow">平台管理</p>
        <h1 class="h-title">商店列表</h1>
        <p class="h-sub">共 {{ items.length }} 間商店，其中 {{ store.activeCount }} 間營運中</p>
      </div>
    </div>

    <!-- 篩選 / 排序工具列 -->
    <div class="card-pad store-toolbar">
      <div class="filter-bar">
        <div class="fb-group">
          <div class="fb-field" style="flex:2 1 220px;">
            <label class="fb-label">關鍵字</label>
            <n-input
              v-model:value="keyword"
              clearable
              placeholder="搜尋商店名稱、子網域或描述">
              <template #prefix><app-icon name="search" :size="16" /></template>
            </n-input>
          </div>
          <div class="fb-field" style="flex:1 1 140px;">
            <label class="fb-label">狀態</label>
            <n-select
              v-model:value="statusFilter"
              :options="statusOptions" />
          </div>
        </div>
      </div>
    </div>

    <n-spin :show="loading">
      <!-- 商店表格：即使無資料仍顯示表頭，空狀態以 tbody 整列呈現 -->
      <div class="card-pad store-table-card" style="padding:8px 8px 4px;">
        <div class="store-table-wrap">
          <table class="tbl store-table">
            <thead>
              <tr>
                <th v-for="col in columns" :key="col.key" :class="{ 'hide-sm': col.hideSm }">
                  <button class="sort-head" type="button" @click="toggleSort(col.key)">
                    <span>{{ col.label }}</span>
                    <app-icon
                      v-if="sortKey === col.key"
                      :name="sortDesc ? 'chevronD' : 'chevronU'"
                      :size="15" />
                  </button>
                </th>
                <th class="hide-sm">狀態</th>
                <th style="width:190px; text-align:right;">操作</th>
              </tr>
            </thead>
            <tbody>
              <!-- 無符合商店：整列佔滿全部欄位 -->
              <tr v-if="!loading && !visible.length">
                <td :colspan="columns.length + 2" style="text-align:center; padding:48px 24px;">
                  <span class="kpi-ic" style="background:var(--c-violet); margin:0 auto 14px;"><app-icon name="home" :size="22" /></span>
                  <div style="font-weight:700; font-size:15px;">
                    {{ items.length ? '沒有符合條件的商店' : '尚無商店' }}
                  </div>
                  <div style="font-size:13px; color:var(--text-faint); margin-top:4px;">
                    {{ items.length ? '試試調整關鍵字或篩選條件。' : '核准開店申請後，商店會顯示於此。' }}
                  </div>
                </td>
              </tr>
              <tr v-for="s in visible" v-else :key="s.id">
                <td>
                  <div class="prod-cell">
                    <img v-if="s.avatarUrl" class="store-avatar" :src="s.avatarUrl" :alt="s.storeName ?? ''" />
                    <span v-else class="store-rank">{{ initial(s.storeName) }}</span>
                    <div style="min-width:0;">
                      <button class="store-name-button" @click="openProducts(s.id)">{{ s.storeName }}</button>
                      <div class="pc-meta">{{ s.description || '尚未填寫商店描述' }}</div>
                    </div>
                  </div>
                </td>
                <td>
                  <span class="store-mono">{{ s.storeSlug }}.openjam.co</span>
                </td>
                <td class="hide-sm">
                  <span class="store-mono">{{ fmtDate(s.createdAt) }}</span>
                </td>
                <td class="hide-sm">
                  <span class="store-mono">{{ fmtDate(s.updatedAt) }}</span>
                </td>
                <td class="hide-sm">
                  <n-tag :type="statusOf(s.status).type" size="small" round>{{ statusOf(s.status).label }}</n-tag>
                </td>
                <td>
                  <div class="row-actions store-actions">
                    <!-- 已關閉為終態，不提供操作 -->
                    <template v-if="s.status === StoreStatus.Closed">
                      <span class="store-closed-hint">已關閉</span>
                    </template>
                    <template v-else>
                      <!-- 停權 / 解除停權 -->
                      <n-popconfirm
                        v-if="s.status === StoreStatus.Active"
                        @positive-click="onSuspend(s.id)">
                        <template #trigger>
                          <n-button size="small" tertiary :disabled="busyId === s.id">
                            <template #icon><app-icon name="lock" :size="15" /></template>
                            停權
                          </n-button>
                        </template>
                        停權後該商店將暫停對外營業，可隨時解除。確定停權？
                      </n-popconfirm>
                      <n-button
                        v-else
                        size="small"
                        tertiary
                        :disabled="busyId === s.id"
                        :loading="busyId === s.id"
                        @click="onUnsuspend(s.id)">
                        <template #icon><app-icon name="refresh" :size="15" /></template>
                        解除停權
                      </n-button>

                      <!-- 關閉（終態不可逆） -->
                      <n-popconfirm @positive-click="onClose(s.id)">
                        <template #trigger>
                          <n-button size="small" type="error" :disabled="busyId === s.id">
                            <template #icon><app-icon name="close" :size="15" /></template>
                            關閉
                          </n-button>
                        </template>
                        關閉後將永久終止此商店且無法復原，確定關閉？
                      </n-popconfirm>
                    </template>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </n-spin>
  </div>
</template>

<style scoped>
.store-toolbar {
  margin-bottom: 16px;
  border-radius: 10px;
}

.store-toolbar :deep(.n-input),
.store-toolbar :deep(.n-input-wrapper),
.store-toolbar :deep(.n-base-selection),
.store-toolbar :deep(.n-base-selection-label) {
  border-radius: 10px;
}

.store-toolbar :deep(.n-input__border),
.store-toolbar :deep(.n-input__state-border),
.store-toolbar :deep(.n-base-selection__border),
.store-toolbar :deep(.n-base-selection__state-border) {
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

.store-table-wrap {
  overflow-x: auto;
}

.store-table {
  min-width: 940px;
}

.sort-head {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 0;
  border: 0;
  background: transparent;
  color: inherit;
  font: inherit;
  cursor: pointer;
}

.store-table thead th {
  font-size: 12.5px;
  padding-top: 12px;
  vertical-align: middle;
}

.store-table-card {
  border-radius: 10px;
}

.store-table thead th + th {
  border-left: 1.5px solid var(--border);
}

.store-table tbody td + td {
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
  font-size: 12px;
  font-weight: 800;
}

.store-avatar {
  width: 30px;
  height: 30px;
  border-radius: 10px;
  object-fit: cover;
  flex: none;
}

.store-mono {
  font-family: var(--oj-mono);
  color: var(--text-soft);
}

.store-name-button {
  border: 0;
  background: transparent;
  color: var(--oj-primary);
  cursor: pointer;
  font-family: var(--oj-display);
  font-size: 14.5px;
  font-weight: 700;
  line-height: 1.25;
  padding: 0;
  text-align: left;
}

.store-name-button:hover {
  color: var(--c-pink);
}

.store-closed-hint {
  font-size: 12.5px;
  color: var(--text-faint);
}
</style>
