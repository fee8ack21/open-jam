import { ref, computed } from 'vue';
import { defineStore } from 'pinia';
import { authApi } from '@/api';
import { UserRole, UserStatus, type UserSummaryDto } from '@/api/auth-service';

/** 由後端 RFC 9457 Problem Details 取出可顯示的錯誤訊息。 */
function messageOf(err: unknown, fallback = '操作失敗，請稍後再試。'): string {
  if (typeof err === 'string') return err;
  const problem = (err as { error?: { detail?: string; title?: string } })?.error
    ?? (err as { detail?: string; title?: string } | null | undefined);
  return problem?.detail ?? problem?.title ?? fallback;
}

/**
 * 平台管理員的會員列表 store：載入並檢視全平台會員。
 * 串接 Auth 服務 `GET /v1/users`（管理員專屬）。僅 Admin 使用。
 */
export const useMemberListStore = defineStore('memberList', () => {
  const items = ref<UserSummaryDto[]>([]); // 全平台會員，新到舊
  const totalCount = ref(0);
  const loading = ref(false);
  const error = ref<string | null>(null);

  /** 啟用中會員數，供側欄徽章顯示。 */
  const activeCount = computed(
    () => items.value.filter((m) => m.status === UserStatus.Active).length,
  );
  /** 平台管理員人數。 */
  const adminCount = computed(() => items.value.filter((m) => m.role === UserRole.Admin).length);

  /** 載入全平台會員列表。 */
  async function load() {
    loading.value = true;
    error.value = null;
    try {
      const res = await authApi.users.list({ Offset: 0, Limit: 100 });
      items.value = res.data.items ?? [];
      totalCount.value = res.data.totalCount ?? items.value.length;
    } catch (err) {
      error.value = messageOf(err, '載入會員列表失敗。');
    } finally {
      loading.value = false;
    }
  }

  return {
    items,
    totalCount,
    loading,
    error,
    activeCount,
    adminCount,
    load,
  };
});
