<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useMessage } from 'naive-ui'
import { storeToRefs } from 'pinia'
import { useStoreReviewStore } from '@/stores/storeReview'

const message = useMessage()
const store = useStoreReviewStore()
const { items, loading, pendingCount } = storeToRefs(store)

// 每筆申請的處理中狀態（避免重複點擊）與駁回意見草稿
const busyId = ref<string | null>(null)
const rejectComment = ref<Record<string, string>>({})
const keyword = ref('')
const sortKey = ref<'storeName' | 'storeSlug' | 'email' | 'createdAt'>('createdAt')
const sortDesc = ref(true)

const columns = [
  { key: 'storeName', label: '商店名稱', hideSm: false },
  { key: 'storeSlug', label: '子網域', hideSm: false },
  { key: 'email', label: '申請人 Email', hideSm: true },
  { key: 'createdAt', label: '送出時間', hideSm: true },
] as const

function fmtDate(v?: string | null) {
  return v ? new Date(v).toLocaleString('zh-TW', { hour12: false }) : '—'
}

function toggleSort(key: typeof sortKey.value) {
  if (sortKey.value === key) {
    sortDesc.value = !sortDesc.value
    return
  }
  sortKey.value = key
  sortDesc.value = key === 'createdAt'
}

const visible = computed(() => {
  const q = keyword.value.trim().toLowerCase()
  const list = q
    ? items.value.filter((a) =>
      [a.storeName, a.storeSlug, a.email]
        .some((field) => (field ?? '').toLowerCase().includes(q)),
    )
    : items.value.slice()

  const dir = sortDesc.value ? -1 : 1
  return list.sort((a, b) => {
    const key = sortKey.value
    const av = (a[key] ?? '') as string
    const bv = (b[key] ?? '') as string
    if (key === 'createdAt') return (av < bv ? -1 : av > bv ? 1 : 0) * dir
    return av.localeCompare(bv, 'zh-Hant') * dir
  })
})

async function onApprove(id?: string) {
  if (!id) return
  busyId.value = id
  const ok = await store.approve(id)
  busyId.value = null
  if (ok) message.success('已核准開店申請，商店已建立。')
  else message.error(store.error ?? '核准失敗')
}

async function onReject(id?: string) {
  if (!id) return
  const comment = (rejectComment.value[id] ?? '').trim()
  if (!comment) {
    message.warning('請填寫駁回原因。')
    return
  }
  busyId.value = id
  const ok = await store.reject(id, comment)
  busyId.value = null
  if (ok) {
    message.success('已駁回開店申請。')
    delete rejectComment.value[id]
  } else {
    message.error(store.error ?? '駁回失敗')
  }
}

onMounted(store.load)
</script>

<template>
  <div data-screen-label="店家審核">
    <div class="page-head">
      <div>
        <p class="h-eyebrow">平台管理</p>
        <h1 class="h-title">待審核商店</h1>
        <p class="h-sub">共 {{ pendingCount }} 筆待審核開店申請</p>
      </div>
    </div>

    <div class="card-pad review-toolbar">
      <n-input
        class="review-search"
        v-model:value="keyword"
        clearable
        placeholder="搜尋商店名稱、子網域或申請人 Email">
        <template #prefix><app-icon name="search" :size="16" /></template>
      </n-input>
    </div>

    <n-spin :show="loading">
      <!-- 無待審核申請 -->
      <div v-if="!loading && !visible.length" class="card-pad" style="text-align:center; padding:48px 24px;">
        <span class="kpi-ic" style="background:var(--c-violet); margin:0 auto 14px;"><app-icon name="shield" :size="22" /></span>
        <div style="font-weight:700; font-size:15px;">
          {{ items.length ? '沒有符合條件的申請' : '目前沒有待審核的申請' }}
        </div>
        <div style="font-size:13px; color:var(--text-faint); margin-top:4px;">
          {{ items.length ? '試試調整關鍵字。' : '新的開店申請送出後會顯示於此。' }}
        </div>
      </div>

      <!-- 待審核表格 -->
      <div v-else class="card-pad review-table-card" style="padding:8px 8px 4px;">
        <div class="review-table-wrap">
          <table class="tbl review-table">
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
                <th style="width:170px; text-align:right;">操作</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="a in visible" :key="a.id">
                <td>
                  <div class="prod-cell">
                    <span class="review-rank">審</span>
                    <div style="min-width:0;">
                      <div class="pc-title">{{ a.storeName }}</div>
                      <div class="pc-meta">待審核開店申請</div>
                    </div>
                  </div>
                </td>
                <td>
                  <span class="review-mono">{{ a.storeSlug }}.openjam.co</span>
                </td>
                <td class="hide-sm">
                  <span class="review-mono">{{ a.email }}</span>
                </td>
                <td class="hide-sm">
                  <span class="review-mono">{{ fmtDate(a.createdAt) }}</span>
                </td>
                <td class="hide-sm">
                  <n-tag type="warning" size="small" round>審核中</n-tag>
                </td>
                <td>
                  <div class="row-actions review-actions">
                    <!-- 駁回（需填原因） -->
                    <n-popover trigger="click" placement="bottom-end" :show-arrow="false" to=".oj-root">
                      <template #trigger>
                        <n-button size="small" tertiary :disabled="busyId === a.id">
                          <template #icon><app-icon name="close" :size="15" /></template>
                          駁回
                        </n-button>
                      </template>
                      <div style="display:grid; gap:10px; width:280px; padding:4px;">
                        <div style="font-weight:600; font-size:13px;">駁回原因</div>
                        <n-input
                          v-model:value="rejectComment[a.id!]"
                          type="textarea"
                          :rows="3"
                          maxlength="500"
                          show-count
                          placeholder="說明駁回原因，將附於通知信中。" />
                        <div style="display:flex; justify-content:flex-end;">
                          <n-button size="small" type="error" :loading="busyId === a.id" @click="onReject(a.id)">
                            確認駁回
                          </n-button>
                        </div>
                      </div>
                    </n-popover>

                    <!-- 核准 -->
                    <n-popconfirm @positive-click="onApprove(a.id)">
                      <template #trigger>
                        <n-button size="small" type="primary" :disabled="busyId === a.id" :loading="busyId === a.id">
                          <template #icon><app-icon name="check" :size="15" /></template>
                          核准
                        </n-button>
                      </template>
                      核准後將建立商店並指派申請人為 Owner，確定核准？
                    </n-popconfirm>
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
.review-toolbar {
  margin-bottom: 16px;
  border-radius: 10px;
}

.review-search,
.review-search :deep(.n-input-wrapper) {
  border-radius: 10px;
}

.review-search :deep(.n-input__border),
.review-search :deep(.n-input__state-border) {
  border-radius: 10px;
}

.review-table-wrap {
  overflow-x: auto;
}

.review-table {
  min-width: 860px;
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

.review-table thead th {
  font-size: 12.5px;
  padding-top: 12px;
  vertical-align: middle;
}

.review-table-card {
  border-radius: 10px;
}

.review-table thead th + th {
  border-left: 1.5px solid var(--border);
}

.review-table tbody td + td {
  border-left: 1.5px solid var(--border);
}

.review-rank {
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

.review-mono {
  font-family: var(--oj-mono);
  color: var(--text-soft);
}
</style>
