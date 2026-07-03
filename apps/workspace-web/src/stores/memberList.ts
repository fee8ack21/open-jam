import { ref } from 'vue';
import { defineStore } from 'pinia';
import { authApi } from '@/api';
import i18n from '@/i18n';
import { UserRole, UserStatus, type UserSummaryDto } from '@/api/auth-service';

/** 由後端 RFC 9457 Problem Details 取出可顯示的錯誤訊息。 */
function messageOf(err: unknown, fallback = i18n.global.t('storeError.actionFailed')): string {
  if (typeof err === 'string') return err;
  const problem = (err as { error?: { detail?: string; title?: string } })?.error
    ?? (err as { detail?: string; title?: string } | null | undefined);
  return problem?.detail ?? problem?.title ?? fallback;
}

/** 查詢條件（皆為選填；空字串 / null 視為未篩選）。 */
export interface MemberListFilter {
  search?: string;
  role?: UserRole | null;
  status?: UserStatus | null;
}

const PAGE_SIZE = 10;

/**
 * 平台管理員的會員列表 store：分頁載入並檢視全平台會員。
 * 串接 Auth 服務 `GET /v1/users`（Admin，支援 Search / Role / Status 過濾）。僅 Admin 使用。
 */
export const useMemberListStore = defineStore('memberList', () => {
  const items = ref<UserSummaryDto[]>([]); // 目前頁次的會員
  const totalCount = ref(0);               // 目前篩選條件下的總筆數
  const offset = ref(0);
  const loading = ref(false);
  const error = ref<string | null>(null);
  const filter = ref<MemberListFilter>({ search: '', role: null, status: null });

  /** 啟用中會員總數（跨頁），供側欄徽章顯示；由獨立 count 查詢維護。 */
  const activeCount = ref(0);
  /** 平台管理員總數（跨頁）。 */
  const adminCount = ref(0);

  /** 以獨立查詢取得啟用中 / 管理員總數（不受目前分頁影響）。 */
  async function refreshCounts() {
    try {
      const [activeRes, adminRes] = await Promise.all([
        authApi.users.list({ Status: UserStatus.Active, Offset: 0, Limit: 1 }),
        authApi.users.list({ Role: UserRole.Admin, Offset: 0, Limit: 1 }),
      ]);
      activeCount.value = activeRes.data.totalCount ?? 0;
      adminCount.value = adminRes.data.totalCount ?? 0;
    } catch {
      /* 徽章數字非關鍵，靜默失敗 */
    }
  }

  /** 載入目前 offset / 篩選條件下的一頁會員。 */
  async function load() {
    loading.value = true;
    error.value = null;
    try {
      const res = await authApi.users.list({
        Offset: offset.value,
        Limit: PAGE_SIZE,
        Search: filter.value.search?.trim() || undefined,
        Role: filter.value.role ?? undefined,
        Status: filter.value.status ?? undefined,
      });
      items.value = res.data.items ?? [];
      totalCount.value = res.data.totalCount ?? items.value.length;
    } catch (err) {
      error.value = messageOf(err, i18n.global.t('storeError.loadMembersFailed'));
      items.value = [];
      totalCount.value = 0;
    } finally {
      loading.value = false;
    }
    await refreshCounts();
  }

  /** 套用新的篩選條件並回到第一頁重新載入。 */
  async function applyFilter(next: MemberListFilter) {
    filter.value = { ...filter.value, ...next };
    offset.value = 0;
    await load();
  }

  /** 跳至指定頁（1-based）。 */
  async function goPage(page: number) {
    offset.value = Math.max(0, (page - 1) * PAGE_SIZE);
    await load();
  }

  return {
    items,
    totalCount,
    offset,
    pageSize: PAGE_SIZE,
    loading,
    error,
    filter,
    activeCount,
    adminCount,
    load,
    applyFilter,
    goPage,
  };
});
