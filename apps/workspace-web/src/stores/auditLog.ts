import { ref } from 'vue';
import { defineStore } from 'pinia';
import { logApi } from '@/api';
import type { AuditLogDto } from '@/api/log-service';

/** 由後端 RFC 9457 Problem Details 取出可顯示的錯誤訊息。 */
function messageOf(err: unknown, fallback = '載入稽核日誌失敗，請稍後再試。'): string {
  const response = err as { error?: { detail?: string; title?: string } } | null;
  const problem = response?.error;
  return problem?.detail ?? problem?.title ?? fallback;
}

/** 查詢條件（皆為選填；空字串視為未篩選）。 */
export interface AuditLogFilter {
  action?: string;
  target?: string;
}

const PAGE_SIZE = 20;

/**
 * 平台管理員的稽核日誌 store：分頁查詢 LogService `/v1/audit-logs`。
 * 僅 Admin 使用，支援以動作類型 / 對象資源類型篩選。
 */
export const useAuditLogStore = defineStore('auditLog', () => {
  const items = ref<AuditLogDto[]>([]);
  const totalCount = ref(0);
  const offset = ref(0);
  const loading = ref(false);
  const error = ref<string | null>(null);
  const filter = ref<AuditLogFilter>({});

  /** 載入目前 offset / 篩選條件下的一頁稽核日誌。 */
  async function load() {
    loading.value = true;
    error.value = null;
    try {
      const res = await logApi.auditLog.get({
        Offset: offset.value,
        Limit: PAGE_SIZE,
        Action: filter.value.action?.trim() || undefined,
        Target: filter.value.target?.trim() || undefined,
      });
      items.value = res.data.items ?? [];
      totalCount.value = res.data.totalCount ?? 0;
    } catch (err) {
      error.value = messageOf(err);
      items.value = [];
      totalCount.value = 0;
    } finally {
      loading.value = false;
    }
  }

  /** 套用新的篩選條件並回到第一頁重新載入。 */
  async function applyFilter(next: AuditLogFilter) {
    filter.value = { ...next };
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
    load,
    applyFilter,
    goPage,
  };
});
