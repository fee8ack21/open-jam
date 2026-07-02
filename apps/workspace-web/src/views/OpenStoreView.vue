<script setup lang="ts">
import { onMounted, reactive, ref, computed } from 'vue'
import { useMessage } from 'naive-ui'
import { useI18n } from 'vue-i18n'
import { storeToRefs } from 'pinia'
import { useStoreApplicationStore } from '@/stores/storeApplication'
import { StoreApplicationStatus, StoreStatus } from '@/api/store-service'

const { t, locale } = useI18n()

// 申請審核狀態 → 顯示用標籤
const APP_STATUS = {
  [StoreApplicationStatus.Pending]:   { labelKey: 'appStatus.pending', type: 'warning' },
  [StoreApplicationStatus.Approved]:  { labelKey: 'appStatus.approved', type: 'success' },
  [StoreApplicationStatus.Rejected]:  { labelKey: 'appStatus.rejected', type: 'error' },
  [StoreApplicationStatus.Withdrawn]: { labelKey: 'appStatus.withdrawn', type: 'default' },
}
const STORE_STATUS = {
  [StoreStatus.Active]:    { labelKey: 'storeStatus.active', type: 'success' },
  [StoreStatus.Suspended]: { labelKey: 'storeStatus.suspended', type: 'warning' },
  [StoreStatus.Closed]:    { labelKey: 'storeStatus.closed', type: 'error' },
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

function statusOf(s?: StoreApplicationStatus) {
  return (s != null && APP_STATUS[s]) || { labelKey: 'appStatus.unknown', type: 'default' }
}
function storeStatusOf(s?: StoreStatus) {
  return (s != null && STORE_STATUS[s]) || { labelKey: 'appStatus.unknown', type: 'default' }
}
function fmtDate(v?: string | null) {
  return v ? new Date(v).toLocaleString(locale.value, { hour12: false }) : '—'
}

async function onSubmit() {
  if (!canSubmit.value) return
  const ok = await store.submit({
    storeName: form.storeName.trim(),
    storeSlug: form.storeSlug.trim().toLowerCase(),
  })
  if (ok) {
    message.success(t('openStore.msgSubmitted'))
    form.storeName = ''
    form.storeSlug = ''
    showForm.value = false
  } else {
    message.error(store.error ?? t('openStore.msgSubmitFailed'))
  }
}

async function onWithdraw(id?: string) {
  if (!id) return
  const ok = await store.withdraw(id)
  if (ok) message.success(t('openStore.msgWithdrawn'))
  else message.error(store.error ?? t('openStore.msgWithdrawFailed'))
}

onMounted(store.load)
</script>

<template>
  <div :data-screen-label="t('route.openStore')">

    <n-spin :show="loading">
      <div class="dash-grid" style="gap:18px;">

        <!-- 已擁有商店 -->
        <template v-if="hasStore">
        <div v-for="m in stores" :key="m.store?.id" class="card-pad applications-card">
          <div style="display:flex; align-items:center; gap:14px;">
            <span class="kpi-ic" style="background:var(--c-violet)"><app-icon name="box" :size="20" /></span>
            <div style="flex:1;">
              <div style="display:flex; align-items:center; gap:10px;">
                <div style="font-weight:700; font-size:15px;">{{ m.store?.storeName }}</div>
                <n-tag :type="storeStatusOf(m.store?.status).type" size="small" round>
                  {{ t(storeStatusOf(m.store?.status).labelKey) }}
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
        <div v-else-if="hasPending && latestApplication" class="card-pad applications-card">
          <div style="display:flex; align-items:center; gap:14px;">
            <span class="kpi-ic" style="background:var(--c-amber, #f0a020)"><app-icon name="receipt" :size="20" /></span>
            <div style="flex:1;">
              <div style="display:flex; align-items:center; gap:10px;">
                <div style="font-weight:700; font-size:15px;">{{ latestApplication.storeName }}</div>
                <n-tag :type="statusOf(latestApplication.status).type" size="small" round>
                  {{ t(statusOf(latestApplication.status).labelKey) }}
                </n-tag>
              </div>
              <div style="font-family:var(--oj-mono); font-size:12px; color:var(--text-faint); margin-top:3px;">
                {{ latestApplication.storeSlug }}.openjam.co · {{ t('openStore.submittedAt', { time: fmtDate(latestApplication.createdAt) }) }}
              </div>
            </div>
            <n-popconfirm @positive-click="onWithdraw(latestApplication.id)">
              <template #trigger><n-button size="small" tertiary>{{ t('openStore.withdraw') }}</n-button></template>
              {{ t('openStore.withdrawConfirm') }}
            </n-popconfirm>
          </div>
        </div>

        <!-- 申請表單（無商店、無待審申請） -->
        <div v-else class="card-pad applications-card">
          <div class="set-grid">
            <div class="sg-k">
              <div class="sgk-t">{{ t('openStore.applyTitle') }}</div>
              <div class="sgk-d">{{ t('openStore.applyDesc') }}</div>
            </div>
            <div>
              <n-alert
                v-if="latestApplication && latestApplication.status === StoreApplicationStatus.Rejected && latestApplication.reviewComment"
                type="error" :show-icon="true" style="margin-bottom:16px;" :title="t('openStore.rejectedTitle')">
                {{ latestApplication.reviewComment }}
              </n-alert>

              <div class="field">
                <label class="field-label">{{ t('openStore.storeName') }}</label>
                <n-input v-model:value="form.storeName" size="large" maxlength="100" show-count
                         :placeholder="t('openStore.storeNamePlaceholder')" />
              </div>
              <div class="field">
                <label class="field-label">{{ t('openStore.subdomain') }}</label>
                <n-input v-model:value="form.storeSlug" size="large" placeholder="xiaoming-shop">
                  <template #suffix><span style="color:var(--text-faint)">.openjam.co</span></template>
                </n-input>
                <div style="font-size:11.5px; margin-top:6px; font-family:var(--oj-mono);"
                     :style="{ color: form.storeSlug && !slugValid ? 'var(--c-rose, #d03050)' : 'var(--text-faint)' }">
                  {{ t('openStore.slugHint') }}
                </div>
              </div>

              <div style="display:flex; justify-content:flex-end; margin-top:8px;">
                <n-button type="primary" size="large" strong
                          :disabled="!canSubmit" :loading="submitting" @click="onSubmit">
                  {{ t('openStore.submit') }}
                </n-button>
              </div>
            </div>
          </div>
        </div>

        <!-- 申請紀錄 -->
        <div v-if="applications.length" class="card-pad applications-card">
          <div style="font-weight:700; font-size:14px; margin-bottom:12px;">{{ t('openStore.historyTitle') }}</div>
          <div v-for="(a, i) in applications" :key="a.id"
               :style="`display:flex; align-items:center; gap:12px; padding:12px 0; border-bottom:${i === applications.length - 1 ? 'none' : '1.5px solid var(--border)'};`">
            <div style="flex:1;">
              <div style="font-weight:600; font-size:14px;">{{ a.storeName }}</div>
              <div style="font-family:var(--oj-mono); font-size:12px; color:var(--text-faint); margin-top:2px;">
                {{ a.storeSlug }}.openjam.co · {{ fmtDate(a.createdAt) }}
              </div>
            </div>
            <n-tag :type="statusOf(a.status).type" size="small" round>{{ t(statusOf(a.status).labelKey) }}</n-tag>
          </div>
        </div>

      </div>
    </n-spin>
  </div>
</template>

<style scoped>
/* 開店頁所有卡片（狀態區塊與申請紀錄）圓角統一 10px，對齊 admin 頁面 */
.applications-card {
  border-radius: 10px;
}
</style>
