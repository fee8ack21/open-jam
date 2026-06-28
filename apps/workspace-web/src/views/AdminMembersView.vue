<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { storeToRefs } from 'pinia'
import { useMemberListStore } from '@/stores/memberList'
import { UserRole, UserStatus, type UserSummaryDto } from '@/api/auth-service'

const { t, locale } = useI18n()
const store = useMemberListStore()
const { items, loading } = storeToRefs(store)

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

// ── 篩選 / 排序狀態 ──────────────────────────────────────────
const keyword = ref('')
/** 角色篩選：all | User | Admin */
const roleFilter = ref<'all' | UserRole>('all')
/** 狀態篩選：all | <UserStatus> */
const statusFilter = ref<'all' | UserStatus>('all')
const sortKey = ref<'email' | 'createdAt' | 'updatedAt'>('createdAt')
const sortDesc = ref(true)

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

function toggleSort(key: typeof sortKey.value) {
  if (sortKey.value === key) {
    sortDesc.value = !sortDesc.value
    return
  }
  sortKey.value = key
  sortDesc.value = true
}

/** 套用關鍵字、角色 / 狀態篩選與排序後的清單。 */
const visible = computed<UserSummaryDto[]>(() => {
  const q = keyword.value.trim().toLowerCase()
  let list = items.value.slice()

  if (roleFilter.value !== 'all') list = list.filter((m) => m.role === roleFilter.value)
  if (statusFilter.value !== 'all') list = list.filter((m) => m.status === statusFilter.value)
  if (q) list = list.filter((m) => (m.email ?? '').toLowerCase().includes(q))

  const dir = sortDesc.value ? -1 : 1
  list.sort((a, b) => {
    const key = sortKey.value
    const av = (a[key] ?? '') as string
    const bv = (b[key] ?? '') as string
    if (key === 'email') return av.localeCompare(bv) * dir
    return (av < bv ? -1 : av > bv ? 1 : 0) * dir
  })
  return list
})
</script>

<template>
  <div :data-screen-label="t('route.members')">
    <div class="page-head">
      <div>
        <p class="h-eyebrow">{{ t('sidebar.platformAdmin') }}</p>
        <h1 class="h-title">{{ t('route.members') }}</h1>
        <p class="h-sub">
          {{ t('members.subStats', { total: items.length, active: store.activeCount, admin: store.adminCount }) }}
        </p>
      </div>
    </div>

    <!-- 篩選 / 排序工具列 -->
    <div class="card-pad store-toolbar">
      <div class="filter-bar">
        <div class="fb-group">
          <div class="fb-field" style="flex:2 1 220px;">
            <label class="fb-label">{{ t('common.keyword') }}</label>
            <n-input
              v-model:value="keyword"
              clearable
              :placeholder="t('members.searchPlaceholder')">
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
        </div>
      </div>
    </div>

    <n-spin :show="loading">
      <div class="card-pad store-table-card" style="padding:8px 8px 4px;">
        <div class="store-table-wrap">
          <table class="tbl store-table">
            <thead>
              <tr>
                <th v-for="col in columns" :key="col.key" :class="{ 'hide-sm': col.hideSm }">
                  <button class="sort-head" type="button" @click="toggleSort(col.key)">
                    <span>{{ t(col.labelKey) }}</span>
                    <app-icon
                      v-if="sortKey === col.key"
                      :name="sortDesc ? 'chevronD' : 'chevronU'"
                      :size="15" />
                  </button>
                </th>
                <th class="hide-sm">{{ t('members.role') }}</th>
                <th class="hide-sm">{{ t('common.status') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="!loading && !visible.length">
                <td :colspan="columns.length + 2" style="text-align:center; padding:48px 24px;">
                  <span class="kpi-ic" style="background:var(--c-cyan); margin:0 auto 14px;"><app-icon name="users" :size="22" /></span>
                  <div style="font-weight:700; font-size:15px;">
                    {{ items.length ? t('members.emptyFilteredTitle') : t('members.emptyTitle') }}
                  </div>
                  <div style="font-size:13px; color:var(--text-faint); margin-top:4px;">
                    {{ items.length ? t('members.emptyFilteredDesc') : t('members.emptyDesc') }}
                  </div>
                </td>
              </tr>
              <tr v-for="m in visible" v-else :key="m.id">
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
.store-toolbar :deep(.n-base-selection) {
  border-radius: 10px;
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
  font-size: 12.5px;
  font-weight: 600;
  color: var(--text-soft);
}

.store-table-wrap {
  overflow-x: auto;
}

.store-table {
  min-width: 880px;
}

.store-table-card {
  border-radius: 10px;
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

.store-mono {
  font-family: var(--oj-mono);
  color: var(--text-soft);
}
</style>
