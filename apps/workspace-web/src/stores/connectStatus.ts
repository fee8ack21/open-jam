import { ref } from 'vue';
import { defineStore } from 'pinia';
import { paymentApi } from '@/api';

/**
 * 商店 Stripe Connect 收款狀態（匿名 status 端點，僅布林旗標）。
 * 供付費商品上架動作在前端主動擋下（disabled + tooltip）；
 * 查詢失敗時維持 null（不擋，後端上架閘門為安全網）。
 */
export const useConnectStatusStore = defineStore('connectStatus', () => {
  /** 是否已可收款；null = 尚未載入或查詢失敗。 */
  const chargesEnabled = ref<boolean | null>(null);
  let pending: Promise<void> | null = null;

  /** 查詢商店收款狀態（每次呼叫皆重新取得，避免完成 onboarding 後快取過期）。 */
  async function load(storeId: string) {
    if (!storeId) return;
    if (pending) return pending;
    pending = (async () => {
      try {
        const res = await paymentApi.connectAccounts.getStatus(storeId);
        chargesEnabled.value = res.data.chargesEnabled ?? false;
      } catch {
        chargesEnabled.value = null;
      } finally {
        pending = null;
      }
    })();
    return pending;
  }

  return { chargesEnabled, load };
});
