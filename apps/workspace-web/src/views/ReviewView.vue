<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useMessage } from 'naive-ui'
import { storeToRefs } from 'pinia'
import { useStoreReviewStore } from '@/stores/storeReview'

const message = useMessage()
const store = useStoreReviewStore()
const { items, loading, pendingCount } = storeToRefs(store)

// 每筆申請的處理中狀態（避免重複點擊）與駁回意見草稿
const busyId = ref<string | null>(null)
const rejectComment = ref<Record<string, string>>({})

function fmtDate(v?: string | null) {
  return v ? new Date(v).toLocaleString('zh-TW', { hour12: false }) : '—'
}
function initial(email?: string | null) {
  return (email?.charAt(0) || '?').toUpperCase()
}

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

    <n-spin :show="loading">
      <!-- 無待審核申請 -->
      <div v-if="!loading && !items.length" class="card-pad" style="text-align:center; padding:48px 24px;">
        <span class="kpi-ic" style="background:var(--c-violet); margin:0 auto 14px;"><app-icon name="shield" :size="22" /></span>
        <div style="font-weight:700; font-size:15px;">目前沒有待審核的申請</div>
        <div style="font-size:13px; color:var(--text-faint); margin-top:4px;">新的開店申請送出後會顯示於此。</div>
      </div>

      <!-- 待審核列表 -->
      <div v-else class="dash-grid" style="gap:14px;">
        <div v-for="a in items" :key="a.id" class="card-pad">
          <div style="display:flex; align-items:center; gap:14px;">
            <span class="avatar" style="width:40px; height:40px; font-size:16px; flex:none;">{{ initial(a.email) }}</span>
            <div style="flex:1; min-width:0;">
              <div style="display:flex; align-items:center; gap:10px;">
                <div style="font-weight:700; font-size:15px;">{{ a.storeName }}</div>
                <n-tag type="warning" size="small" round>審核中</n-tag>
              </div>
              <div style="font-family:var(--oj-mono); font-size:12px; color:var(--text-faint); margin-top:3px;">
                {{ a.storeSlug }}.openjam.co · {{ a.email }} · 送出於 {{ fmtDate(a.createdAt) }}
              </div>
            </div>
            <div style="display:flex; gap:8px; flex:none;">
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
          </div>
        </div>
      </div>
    </n-spin>
  </div>
</template>
