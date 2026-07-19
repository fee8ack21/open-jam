<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useMessage } from 'naive-ui'
import { useI18n } from 'vue-i18n'
import { storeToRefs } from 'pinia'
import { useStoreApplicationStore } from '@/stores/storeApplication'
import { paymentApi } from '@/api'
import type { ConnectAccountStatusResponse } from '@/api/payment-service'

const { t } = useI18n()
const message = useMessage()
const route = useRoute()
const router = useRouter()
const storeApp = useStoreApplicationStore()
const { primaryStore } = storeToRefs(storeApp)

const loading = ref(true)
const redirecting = ref(false)
const status = ref<ConnectAccountStatusResponse | null>(null)

/** 由後端 RFC 9457 Problem Details 取出可顯示的錯誤訊息。 */
function messageOf(err: unknown, fallback: string): string {
  const response = err as { error?: { detail?: string; title?: string } } | null
  const problem = response?.error
  return problem?.detail ?? problem?.title ?? fallback
}

/** 整體狀態：none 未建立帳戶 / pending 設定中 / ready 可收款。 */
const state = computed<'none' | 'pending' | 'ready'>(() => {
  if (!status.value?.hasAccount) return 'none'
  return status.value.chargesEnabled ? 'ready' : 'pending'
})

const checklist = computed(() => [
  { key: 'hasAccount', label: t('payouts.stepAccount'), done: !!status.value?.hasAccount },
  { key: 'detailsSubmitted', label: t('payouts.stepDetails'), done: !!status.value?.detailsSubmitted },
  { key: 'chargesEnabled', label: t('payouts.stepCharges'), done: !!status.value?.chargesEnabled },
  { key: 'payoutsEnabled', label: t('payouts.stepPayouts'), done: !!status.value?.payoutsEnabled },
])

async function loadStatus(refresh: boolean) {
  const storeId = primaryStore.value?.id
  if (!storeId) return
  loading.value = true
  try {
    const res = await paymentApi.connectAccounts.get(storeId, refresh ? { refresh: true } : undefined)
    status.value = res.data
  } catch (err) {
    message.error(messageOf(err, t('payouts.msgLoadFailed')))
  } finally {
    loading.value = false
  }
}

const dashboardLoading = ref(false)

/** 開啟 Stripe Express Dashboard（login link 短效，每次點擊即時簽發），查看餘額、撥款排程與交易明細。 */
async function goDashboard() {
  const storeId = primaryStore.value?.id
  if (!storeId || dashboardLoading.value) return
  dashboardLoading.value = true
  try {
    const res = await paymentApi.connectAccounts.createLoginLink(storeId)
    if (res.data.url) {
      window.open(res.data.url, '_blank', 'noopener')
      return
    }
    message.error(t('payouts.msgDashboardFailed'))
  } catch (err) {
    message.error(messageOf(err, t('payouts.msgDashboardFailed')))
  } finally {
    dashboardLoading.value = false
  }
}

/** 前往 Stripe 託管 onboarding（Account Link 短效，每次點擊即時簽發）。 */
async function goOnboarding() {
  const storeId = primaryStore.value?.id
  if (!storeId || redirecting.value) return
  redirecting.value = true
  try {
    const res = await paymentApi.connectAccounts.createOnboardingLink(storeId)
    if (res.data.url) {
      window.location.href = res.data.url
      return
    }
    message.error(t('payouts.msgLinkFailed'))
    redirecting.value = false
  } catch (err) {
    message.error(messageOf(err, t('payouts.msgLinkFailed')))
    redirecting.value = false
  }
}

onMounted(async () => {
  if (!storeApp.hasStore) await storeApp.load()

  // Stripe onboarding 導回（?return=1）時向 Stripe 取即時狀態，不等 webhook；
  // Account Link 逾時導回（?refresh=1）則提示重新前往。
  const fromReturn = route.query.return === '1'
  const fromRefresh = route.query.refresh === '1'
  if (fromReturn || fromRefresh) router.replace({ query: {} })
  if (fromRefresh) message.warning(t('payouts.msgLinkExpired'))

  await loadStatus(fromReturn)
})
</script>

<template>
  <div :data-screen-label="t('route.payouts')">
    <n-spin :show="loading">
      <div class="dash-grid" style="gap:18px;">
        <!-- 收款狀態總覽 -->
        <div class="card-pad set-card">
          <div class="set-grid">
            <div class="sg-k">
              <div class="sgk-t">{{ t('payouts.statusTitle') }}</div>
              <div class="sgk-d">{{ t('payouts.statusDesc') }}</div>
            </div>
            <div>
              <div class="payout-state" :class="state">
                <app-icon :name="state === 'ready' ? 'check' : state === 'pending' ? 'clock' : 'wallet'" :size="18" />
                <span>{{ t(`payouts.state.${state}`) }}</span>
              </div>
              <p class="payout-hint">{{ t(`payouts.hint.${state}`) }}</p>

              <ul class="payout-steps">
                <li v-for="item in checklist" :key="item.key" :class="{ done: item.done }">
                  <app-icon :name="item.done ? 'check' : 'minus'" :size="14" :stroke="2.4" />
                  <span>{{ item.label }}</span>
                </li>
              </ul>

              <div class="payout-actions">
                <!-- 已可收款時主要動作變成看儀表板（餘額 / 撥款排程 / 交易明細），更新資料退居次要 -->
                <n-button v-if="state === 'ready'" type="primary" :loading="dashboardLoading" :disabled="loading" @click="goDashboard">
                  <template #icon><app-icon name="chart" :size="16" /></template>
                  {{ t('payouts.actionDashboard') }}
                </n-button>
                <n-button :type="state === 'ready' ? 'default' : 'primary'" :loading="redirecting" :disabled="loading" @click="goOnboarding">
                  <template #icon><app-icon name="wallet" :size="16" /></template>
                  {{ state === 'none' ? t('payouts.actionStart') : state === 'pending' ? t('payouts.actionContinue') : t('payouts.actionUpdate') }}
                </n-button>
              </div>
            </div>
          </div>
        </div>

        <!-- 說明：分帳與抽成 -->
        <div class="card-pad set-card">
          <div class="set-grid">
            <div class="sg-k">
              <div class="sgk-t">{{ t('payouts.aboutTitle') }}</div>
              <div class="sgk-d">{{ t('payouts.aboutDesc') }}</div>
            </div>
            <div>
              <p class="payout-hint">{{ t('payouts.aboutBody1') }}</p>
              <p class="payout-hint">{{ t('payouts.aboutBody2') }}</p>
            </div>
          </div>
        </div>
      </div>
    </n-spin>
  </div>
</template>

<style scoped>
.payout-state {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: 6px 14px;
  border: 2px solid #1a1a1a;
  border-radius: 999px;
  font-weight: 700;
  background: #fff;
}
.payout-state.ready { background: #d7f5dd; }
.payout-state.pending { background: #ffde00; }
.payout-hint {
  margin: 10px 0 0;
  color: var(--t2, #555);
  font-size: 13.5px;
  line-height: 1.7;
}
.payout-steps {
  list-style: none;
  margin: 14px 0 18px;
  padding: 0;
  display: grid;
  gap: 8px;
}
.payout-steps li {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 13.5px;
  color: var(--t2, #777);
}
.payout-steps li.done { color: var(--t1, #1a1a1a); }
.payout-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
}
</style>
