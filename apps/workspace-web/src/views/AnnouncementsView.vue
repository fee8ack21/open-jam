<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useMessage, useDialog } from 'naive-ui'
import { useI18n } from 'vue-i18n'
import { storeToRefs } from 'pinia'
import { useAnnouncementsStore } from '@/stores/announcements'
import { useStoreApplicationStore } from '@/stores/storeApplication'
import { requestStatusMeta, requestStatusOptions, formatRequestTime } from '@/utils/announcement'
import { NotificationRequestStatus, type NotificationRequestDto } from '@/api/notification-service'

const { t } = useI18n()
const message = useMessage()
const dialog = useDialog()
const store = useAnnouncementsStore()
const storeApp = useStoreApplicationStore()
const { items, totalCount, loading, submitting, error } = storeToRefs(store)

const statusOptions = computed(() => requestStatusOptions())

// 載入錯誤以彈出 message 呈現，表格維持空狀態
watch(error, (msg) => { if (msg) message.error(msg) })

// 賣家本人商店 ID（開店資料由 router guard 先載入）；變動時重新綁定查詢來源。
const myStoreId = computed(() => storeApp.primaryStore?.id ?? null)
watch(myStoreId, (id) => { if (id) store.bindStore(id) })
onMounted(() => { if (myStoreId.value) store.bindStore(myStoreId.value) })

// ── 建立公告表單 ─────────────────────────────────────────────
const form = ref({ title: '', message: '' })
/** 預定發送時間（epoch ms）；null 表示立即發送。 */
const scheduledAt = ref<number | null>(null)

const canSubmit = computed(() =>
  form.value.title.trim().length > 0 && form.value.message.trim().length > 0 && !submitting.value,
)

/** 預定時間不可早於現在（n-date-picker 以日為粒度停用過去日期）。 */
function disablePastDate(ts: number) {
  return ts < Date.now() - 24 * 60 * 60 * 1000
}

async function onSubmit() {
  if (!canSubmit.value) return
  if (scheduledAt.value !== null && scheduledAt.value < Date.now()) {
    message.error(t('announcements.msgScheduledInPast'))
    return
  }
  const ok = await store.create({
    title: form.value.title.trim(),
    message: form.value.message.trim(),
    scheduledAt: scheduledAt.value === null ? null : new Date(scheduledAt.value).toISOString(),
  })
  if (ok) {
    message.success(scheduledAt.value === null
      ? t('announcements.msgCreated')
      : t('announcements.msgScheduled'))
    form.value = { title: '', message: '' }
    scheduledAt.value = null
  } else {
    message.error(store.error ?? t('announcements.msgCreateFailed'))
  }
}

// ── 列表 ────────────────────────────────────────────────────
const statusFilter = ref<NotificationRequestStatus | null>(null)
const page = ref(1)
const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / store.pageSize)))

watch(statusFilter, async (s) => {
  page.value = 1
  await store.applyFilter(s)
})

async function changePage(p: number) {
  page.value = p
  await store.goPage(p)
}

async function changePageSize(size: number) {
  page.value = 1
  await store.setPageSize(size)
}

/** 由任務 Payload（camelCase JSON）取出標題顯示。 */
function titleOf(r: NotificationRequestDto): string {
  try {
    const payload = JSON.parse(r.payload ?? '{}') as { title?: string }
    return payload.title ?? '—'
  } catch {
    return '—'
  }
}

function onCancel(r: NotificationRequestDto) {
  if (!r.id) return
  dialog.warning({
    title: t('announcements.cancelConfirmTitle'),
    content: t('announcements.cancelConfirmDesc', { title: titleOf(r) }),
    positiveText: t('common.confirm'),
    negativeText: t('common.cancel'),
    onPositiveClick: async () => {
      const ok = await store.cancel(r.id!)
      if (ok) message.success(t('announcements.msgCancelled'))
      else message.error(store.error ?? t('announcements.msgCancelFailed'))
    },
  })
}
</script>

<template>
  <div :data-screen-label="t('route.announcements')">

    <n-spin :show="loading && !items.length">
      <div class="dash-grid" style="gap:18px;">

        <!-- 建立公告：標題 / 內文 / 預定發送時間 -->
        <div class="card-pad set-card">
          <div class="set-grid">
            <div class="sg-k">
              <div class="sgk-t">{{ t('announcements.createTitle') }}</div>
              <div class="sgk-d">{{ t('announcements.createDesc') }}</div>
            </div>
            <div>
              <div class="field">
                <label class="field-label">{{ t('announcements.fieldTitle') }}</label>
                <n-input v-model:value="form.title" size="large" maxlength="200" show-count
                         :placeholder="t('announcements.fieldTitlePlaceholder')" />
              </div>
              <div class="field">
                <label class="field-label">{{ t('announcements.fieldMessage') }}</label>
                <n-input v-model:value="form.message" type="textarea" size="large"
                         :autosize="{ minRows: 3, maxRows: 8 }" maxlength="4000" show-count
                         :placeholder="t('announcements.fieldMessagePlaceholder')" />
              </div>
              <div class="field">
                <label class="field-label">{{ t('announcements.fieldScheduledAt') }}</label>
                <n-date-picker v-model:value="scheduledAt" type="datetime" clearable size="large"
                               style="width:100%"
                               :is-date-disabled="disablePastDate"
                               :placeholder="t('announcements.fieldScheduledAtPlaceholder')" />
                <div class="field-hint">{{ t('announcements.fieldScheduledAtHint') }}</div>
              </div>

              <div style="display:flex; justify-content:flex-end; margin-top:16px;">
                <n-button type="primary" size="large" strong
                          :disabled="!canSubmit" :loading="submitting" @click="onSubmit">
                  <template #icon><app-icon name="bell" :size="16" /></template>
                  {{ scheduledAt === null ? t('announcements.submitNow') : t('announcements.submitScheduled') }}
                </n-button>
              </div>
            </div>
          </div>
        </div>

        <!-- 任務列表：狀態篩選 + 表格 + 分頁 -->
        <div class="card-pad history-table-card">
          <div class="list-filter">
            <div class="filter-bar">
              <div class="fb-group">
                <div class="fb-field" style="flex:0 1 220px;">
                  <label class="fb-label">{{ t('common.status') }}</label>
                  <n-select v-model:value="statusFilter"
                            :placeholder="t('requestStatus.all')"
                            :options="statusOptions" />
                </div>
              </div>
            </div>
          </div>

          <div class="history-table-wrap">
            <table class="tbl history-table">
              <thead>
                <tr>
                  <th>{{ t('announcements.colTitle') }}</th>
                  <th class="hide-sm">{{ t('announcements.colScheduledAt') }}</th>
                  <th>{{ t('common.status') }}</th>
                  <th class="hide-sm">{{ t('announcements.colDispatchedAt') }}</th>
                  <th style="width:80px; text-align:right;">{{ t('announcements.colActions') }}</th>
                </tr>
              </thead>
              <tbody>
                <tr v-if="!items.length">
                  <td colspan="5" style="text-align:center; padding:48px 24px;">
                    <span class="kpi-ic" style="background:var(--c-violet); margin:0 auto 14px;"><app-icon name="bell" :size="22" /></span>
                    <div style="font-weight:700; font-size:15px;">{{ t('announcements.emptyTitle') }}</div>
                    <div style="font-size:13px; color:var(--text-faint); margin-top:4px;">
                      {{ t('announcements.emptyDesc') }}
                    </div>
                  </td>
                </tr>
                <tr v-for="r in items" :key="r.id">
                  <td style="max-width:280px;">
                    <div style="font-weight:600; overflow:hidden; text-overflow:ellipsis; white-space:nowrap;" :title="titleOf(r)">{{ titleOf(r) }}</div>
                  </td>
                  <td class="hide-sm"><span class="history-mono" style="font-size:12px;">{{ formatRequestTime(r.scheduledAt) }}</span></td>
                  <td><n-tag :type="requestStatusMeta(r.status).type" size="small" round>{{ t(requestStatusMeta(r.status).labelKey) }}</n-tag></td>
                  <td class="hide-sm"><span class="history-mono" style="font-size:12px;">{{ formatRequestTime(r.dispatchedAt) }}</span></td>
                  <td style="text-align:right;">
                    <n-button v-if="r.status === NotificationRequestStatus.Pending"
                              text type="error" @click="onCancel(r)" :title="t('announcements.cancelAction')">
                      <app-icon name="close" :size="17" />
                    </n-button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

          <div class="history-pager">
            <list-pager
              :page="page"
              :page-count="totalPages"
              :page-size="store.pageSize"
              @update:page="changePage"
              @update:page-size="changePageSize" />
          </div>
        </div>

      </div>
    </n-spin>
  </div>
</template>

<style scoped>
.set-card { border-radius: 10px; }
.field-hint { font-size: 12px; color: var(--text-faint); margin-top: 6px; }
</style>
