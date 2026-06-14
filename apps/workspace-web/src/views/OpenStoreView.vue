<script setup lang="ts">
import { onMounted, reactive, ref, computed } from 'vue'
import { useMessage } from 'naive-ui'
import { storeToRefs } from 'pinia'
import { useStoreApplicationStore } from '@/stores/storeApplication'
import { StoreApplicationStatus, StoreStatus } from '@/api/store-service'

// 申請審核狀態 → 顯示用標籤
const APP_STATUS = {
  [StoreApplicationStatus.Pending]:   { label: '審核中', type: 'warning' },
  [StoreApplicationStatus.Approved]:  { label: '已核准', type: 'success' },
  [StoreApplicationStatus.Rejected]:  { label: '已駁回', type: 'error' },
  [StoreApplicationStatus.Withdrawn]: { label: '已撤回', type: 'default' },
}
const STORE_STATUS = {
  [StoreStatus.Active]:    { label: '營運中', type: 'success' },
  [StoreStatus.Suspended]: { label: '已停權', type: 'warning' },
  [StoreStatus.Closed]:    { label: '已關閉', type: 'error' },
}

// 與後端 StoreSlugValidator 對齊：3–30 字小寫英數 + 連字號，不可開頭/結尾為連字號
const SLUG_RE = /^[a-z0-9]([a-z0-9-]{1,28}[a-z0-9])?$/

const message = useMessage()
const store = useStoreApplicationStore()
const { applications, stores, loading, submitting, latestApplication, hasPending, hasStore } =
  storeToRefs(store)

const form = reactive({ storeName: '', storeSlug: '' })
const showForm = ref(false)

const slugValid = computed(() => SLUG_RE.test(form.storeSlug))
const canSubmit = computed(
  () => form.storeName.trim().length >= 1 && slugValid.value && !submitting.value,
)

// 是否處於「可提交新申請」的狀態（沒有商店、且沒有待審申請）
const canApply = computed(() => !hasStore.value && !hasPending.value)

function statusOf(s?: StoreApplicationStatus) {
  return (s != null && APP_STATUS[s]) || { label: '—', type: 'default' }
}
function storeStatusOf(s?: StoreStatus) {
  return (s != null && STORE_STATUS[s]) || { label: '—', type: 'default' }
}
function fmtDate(v?: string | null) {
  return v ? new Date(v).toLocaleString('zh-TW', { hour12: false }) : '—'
}

async function onSubmit() {
  if (!canSubmit.value) return
  const ok = await store.submit({
    storeName: form.storeName.trim(),
    storeSlug: form.storeSlug.trim().toLowerCase(),
  })
  if (ok) {
    message.success('開店申請已送出，請等待審核。')
    form.storeName = ''
    form.storeSlug = ''
    showForm.value = false
  } else {
    message.error(store.error ?? '提交失敗')
  }
}

async function onWithdraw(id?: string) {
  if (!id) return
  const ok = await store.withdraw(id)
  if (ok) message.success('已撤回申請。')
  else message.error(store.error ?? '撤回失敗')
}

onMounted(store.load)
</script>

<template>
  <div data-screen-label="開店">
    <div class="page-head">
      <div>
        <p class="h-eyebrow">創作者</p>
        <h1 class="h-title">開店</h1>
      </div>
    </div>

    <n-spin :show="loading">
      <div class="dash-grid" style="gap:18px;">

        <!-- 已擁有商店 -->
        <template v-if="hasStore">
        <div v-for="m in stores" :key="m.store?.id" class="card-pad">
          <div style="display:flex; align-items:center; gap:14px;">
            <span class="kpi-ic" style="background:var(--c-violet)"><app-icon name="box" :size="20" /></span>
            <div style="flex:1;">
              <div style="display:flex; align-items:center; gap:10px;">
                <div style="font-weight:700; font-size:15px;">{{ m.store?.storeName }}</div>
                <n-tag :type="storeStatusOf(m.store?.status).type" size="small" round>
                  {{ storeStatusOf(m.store?.status).label }}
                </n-tag>
              </div>
              <div style="font-family:var(--oj-mono); font-size:12px; color:var(--text-faint); margin-top:3px;">
                {{ m.store?.storeSlug }}.openjam.co
              </div>
            </div>
          </div>
        </div>
        </template>

        <!-- 審核中的申請 -->
        <div v-else-if="hasPending && latestApplication" class="card-pad">
          <div style="display:flex; align-items:center; gap:14px;">
            <span class="kpi-ic" style="background:var(--c-amber, #f0a020)"><app-icon name="receipt" :size="20" /></span>
            <div style="flex:1;">
              <div style="display:flex; align-items:center; gap:10px;">
                <div style="font-weight:700; font-size:15px;">{{ latestApplication.storeName }}</div>
                <n-tag :type="statusOf(latestApplication.status).type" size="small" round>
                  {{ statusOf(latestApplication.status).label }}
                </n-tag>
              </div>
              <div style="font-family:var(--oj-mono); font-size:12px; color:var(--text-faint); margin-top:3px;">
                {{ latestApplication.storeSlug }}.openjam.co · 送出於 {{ fmtDate(latestApplication.createdAt) }}
              </div>
            </div>
            <n-popconfirm @positive-click="onWithdraw(latestApplication.id)">
              <template #trigger><n-button size="small" tertiary>撤回</n-button></template>
              撤回後即可重新申請，確定撤回？
            </n-popconfirm>
          </div>
        </div>

        <!-- 申請表單（無商店、無待審申請） -->
        <div v-else class="card-pad">
          <div class="set-grid">
            <div class="sg-k">
              <div class="sgk-t">申請開店</div>
              <div class="sgk-d">填寫商店名稱與子網域，送出後由平台審核。核准後即可在你的子網域開店。</div>
            </div>
            <div>
              <n-alert
                v-if="latestApplication && latestApplication.status === StoreApplicationStatus.Rejected && latestApplication.reviewComment"
                type="error" :show-icon="true" style="margin-bottom:16px;" title="上次申請被駁回">
                {{ latestApplication.reviewComment }}
              </n-alert>

              <div class="field">
                <label class="field-label">商店名稱</label>
                <n-input v-model:value="form.storeName" size="large" maxlength="100" show-count
                         placeholder="例如：小明的數位商店" />
              </div>
              <div class="field">
                <label class="field-label">子網域</label>
                <n-input v-model:value="form.storeSlug" size="large" placeholder="xiaoming-shop">
                  <template #suffix><span style="color:var(--text-faint)">.openjam.co</span></template>
                </n-input>
                <div style="font-size:11.5px; margin-top:6px; font-family:var(--oj-mono);"
                     :style="{ color: form.storeSlug && !slugValid ? 'var(--c-rose, #d03050)' : 'var(--text-faint)' }">
                  3–30 字小寫英數與連字號，不可開頭/結尾為連字號
                </div>
              </div>

              <div style="display:flex; justify-content:flex-end; margin-top:8px;">
                <n-button type="primary" size="large" strong
                          :disabled="!canSubmit" :loading="submitting" @click="onSubmit">
                  送出申請
                </n-button>
              </div>
            </div>
          </div>
        </div>

        <!-- 申請紀錄 -->
        <div v-if="applications.length" class="card-pad">
          <div style="font-weight:700; font-size:14px; margin-bottom:12px;">申請紀錄</div>
          <div v-for="a in applications" :key="a.id"
               style="display:flex; align-items:center; gap:12px; padding:12px 0; border-bottom:1.5px solid var(--border);">
            <div style="flex:1;">
              <div style="font-weight:600; font-size:14px;">{{ a.storeName }}</div>
              <div style="font-family:var(--oj-mono); font-size:12px; color:var(--text-faint); margin-top:2px;">
                {{ a.storeSlug }}.openjam.co · {{ fmtDate(a.createdAt) }}
              </div>
            </div>
            <n-tag :type="statusOf(a.status).type" size="small" round>{{ statusOf(a.status).label }}</n-tag>
          </div>
        </div>

      </div>
    </n-spin>
  </div>
</template>
