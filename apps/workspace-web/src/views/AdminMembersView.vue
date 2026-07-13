<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { storeToRefs } from 'pinia'
import { useMemberListStore } from '@/stores/memberList'
import { UserRole, UserStatus } from '@/api/auth-service'

const { t, locale } = useI18n()
const store = useMemberListStore()
const { items, totalCount, loading } = storeToRefs(store)

onMounted(store.load)

// 帳號狀態 → 顯示用標籤
const STATUS = {
  [UserStatus.Active]:      { labelKey: 'memberStatus.active', type: 'success' as const },
  [UserStatus.Pending]:     { labelKey: 'memberStatus.pending', type: 'info' as const },
  [UserStatus.Locked]:      { labelKey: 'memberStatus.locked', type: 'warning' as const },
  [UserStatus.Suspended]:   { labelKey: 'memberStatus.suspended', type: 'warning' as const },
  [UserStatus.Deactivated]: { labelKey: 'memberStatus.deactivated', type: 'default' as const },
  [UserStatus.Deleted]:     { labelKey: 'memberStatus.deleted', type: 'error' as const },
}
function statusOf(s?: UserStatus) {
  return (s != null && STATUS[s]) || { labelKey: 'appStatus.unknown', type: 'default' as const }
}
function roleOf(r?: UserRole) {
  return r === UserRole.Admin
    ? { labelKey: 'memberRole.admin', type: 'info' as const }
    : { labelKey: 'memberRole.user', type: 'default' as const }
}

// ── 篩選狀態（伺服器端分頁）──────────────────────────────────
const keyword = ref('')
/** 角色篩選：all | User | Admin */
const roleFilter = ref<'all' | UserRole>('all')
/** 狀態篩選：all | <UserStatus> */
const statusFilter = ref<'all' | UserStatus>('all')
const page = ref(1)

const roleOptions = computed(() => [
  { label: t('members.filterAllRoles'), value: 'all' },
  { label: t('memberRole.user'), value: UserRole.User },
  { label: t('memberRole.admin'), value: UserRole.Admin },
])
const statusOptions = computed(() => [
  { label: t('members.filterAllStatuses'), value: 'all' },
  { label: t('memberStatus.active'), value: UserStatus.Active },
  { label: t('memberStatus.pending'), value: UserStatus.Pending },
  { label: t('memberStatus.locked'), value: UserStatus.Locked },
  { label: t('memberStatus.suspended'), value: UserStatus.Suspended },
  { label: t('memberStatus.deactivated'), value: UserStatus.Deactivated },
  { label: t('memberStatus.deleted'), value: UserStatus.Deleted },
])
const columns = [
  { key: 'email', labelKey: 'members.colMember', hideSm: false },
  { key: 'createdAt', labelKey: 'members.colRegisteredAt', hideSm: true },
  { key: 'updatedAt', labelKey: 'stores.colUpdatedAt', hideSm: true },
] as const

function fmtDate(v?: string | null) {
  return v ? new Date(v).toLocaleString(locale.value, { hour12: false }) : '—'
}
function initial(email?: string | null) {
  return (email?.charAt(0) || '?').toUpperCase()
}

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / store.pageSize)))
const hasFilter = computed(() =>
  keyword.value.trim() !== '' || roleFilter.value !== 'all' || statusFilter.value !== 'all',
)

async function applyFilter() {
  page.value = 1
  await store.applyFilter({
    search: keyword.value,
    role: roleFilter.value === 'all' ? null : roleFilter.value,
    status: statusFilter.value === 'all' ? null : statusFilter.value,
  })
}
// 關鍵字改由「搜尋」按鈕 / Enter 觸發；下拉條件維持即時套用
watch([roleFilter, statusFilter], () => { applyFilter() })
async function changePage(p: number) { page.value = p; await store.goPage(p) }
async function changePageSize(size: number) { page.value = 1; await store.setPageSize(size) }
</script>

<template>
  <div :data-screen-label="t('route.members')">

    <n-spin :show="loading">
      <!-- 篩選列與表格合併為單一卡片：篩選在上、整寬分隔線、表格在下 -->
      <div class="card-pad store-table-card">
        <div class="list-filter">
          <div class="filter-bar">
            <div class="fb-group">
              <div class="fb-field" style="flex:2 1 220px;">
                <label class="fb-label">{{ t('common.keyword') }}</label>
                <n-input
                  v-model:value="keyword"
                  clearable
                  :placeholder="t('members.searchPlaceholder')"
                  @keyup.enter="applyFilter">
                  <template #prefix><app-icon name="search" :size="16" /></template>
                </n-input>
              </div>
              <div class="fb-field" style="flex:1 1 130px;">
                <label class="fb-label">{{ t('members.role') }}</label>
                <n-select v-model:value="roleFilter" :options="roleOptions" />
              </div>
              <div class="fb-field" style="flex:1 1 130px;">
                <label class="fb-label">{{ t('common.status') }}</label>
                <n-select v-model:value="statusFilter" :options="statusOptions" />
              </div>
              <n-button class="fb-search-btn" type="primary" :loading="loading" @click="applyFilter">
                <template #icon><app-icon name="search" :size="16" /></template>
                {{ t('common.search') }}
              </n-button>
            </div>
          </div>
        </div>

        <div class="store-table-wrap">
          <table class="tbl store-table">
            <thead>
              <tr>
                <th v-for="col in columns" :key="col.key" :class="{ 'hide-sm': col.hideSm }">
                  <span>{{ t(col.labelKey) }}</span>
                </th>
                <th class="hide-sm">{{ t('members.role') }}</th>
                <th class="hide-sm">{{ t('common.status') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="!loading && !items.length">
                <td :colspan="columns.length + 2" style="text-align:center; padding:48px 24px;">
                  <span class="kpi-ic" style="background:var(--c-cyan); margin:0 auto 14px;"><app-icon name="users" :size="22" /></span>
                  <div style="font-weight:700; font-size:15px;">
                    {{ hasFilter ? t('members.emptyFilteredTitle') : t('members.emptyTitle') }}
                  </div>
                  <div style="font-size:13px; color:var(--text-faint); margin-top:4px;">
                    {{ hasFilter ? t('members.emptyFilteredDesc') : t('members.emptyDesc') }}
                  </div>
                </td>
              </tr>
              <tr v-for="m in items" v-else :key="m.id">
                <td>
                  <div class="prod-cell">
                    <span class="store-rank">{{ initial(m.email) }}</span>
                    <div style="min-width:0;">
                      <div class="pc-title">
                        {{ m.email }}
                        <app-icon
                          v-if="!m.emailVerified"
                          name="mail"
                          :size="13"
                          :title="t('members.emailUnverified')"
                          style="color:var(--c-orange); vertical-align:-2px; margin-left:4px;" />
                      </div>
                      <div class="pc-meta store-mono">{{ m.id }}</div>
                    </div>
                  </div>
                </td>
                <td class="hide-sm">
                  <span class="store-mono">{{ fmtDate(m.createdAt) }}</span>
                </td>
                <td class="hide-sm">
                  <span class="store-mono">{{ fmtDate(m.updatedAt) }}</span>
                </td>
                <td class="hide-sm">
                  <n-tag :type="roleOf(m.role).type" size="small" round>{{ t(roleOf(m.role).labelKey) }}</n-tag>
                </td>
                <td class="hide-sm">
                  <n-tag :type="statusOf(m.status).type" size="small" round>{{ t(statusOf(m.status).labelKey) }}</n-tag>
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
    </n-spin>
  </div>
</template>

<style scoped>
/* 篩選區段：卡片頂部，底部整寬分隔線與表格分開 */
.list-filter {
  padding: 18px 20px;
  border-bottom: var(--bw) solid var(--border-strong);
  background: var(--bg);
}


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

.fb-field {
  display: flex;
  flex-direction: column;
  gap: 6px;
  min-width: 0;
}

.fb-label {
  font-size: 12px;
  font-weight: 900;
  color: var(--text);
}

/* 搜尋按鈕與輸入框同高、同圓角（Input heightMedium 於 App.vue 覆寫為 42px） */
.fb-search-btn {
  height: 40px;
}

.store-table-wrap {
  overflow-x: auto;
  padding: 0 10px;
}

.store-table {
  min-width: 880px;
}

.store-table-card {
  padding: 0;
  border-radius: var(--r-lg);
  overflow: hidden;
}

.history-pager {
  display: flex;
  justify-content: flex-end;
  padding: 14px 20px;
}

.store-table thead th {
  font-size: 12.5px;
  padding-top: 12px;
  vertical-align: middle;
}




.store-mono {
  font-family: var(--oj-mono);
  color: var(--text-soft);
}
</style>
