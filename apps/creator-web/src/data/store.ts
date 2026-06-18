/* ============================================================
   Open Jam — current storefront（mock）
   欄位對應 StoreService 的 StoreDto（GET /v1/stores/{slug}）。
   creator-web 為單一店家的店面（<creator>.openjam.co），此處先以
   假資料代表「目前這間店」，待串接後端後改由 API 取得。
   ============================================================ */

/** 商店狀態（對應 StoreService StoreStatus enum）。 */
export type StoreStatus = 'Active' | 'Suspended' | 'Closed';

/** 商店基本資訊（對應 StoreService StoreDto）。 */
export interface Store {
  /** 商店唯一識別碼。 */
  id: string;
  /** 商店顯示名稱。 */
  storeName: string;
  /** 商店子網域代稱。 */
  storeSlug: string;
  /** 商店描述。 */
  description: string | null;
  /** 商店頭像公開 URL；null 表示尚未設定。 */
  avatarUrl: string | null;
  /** 商店橫幅公開 URL；null 表示尚未設定。 */
  bannerUrl: string | null;
  /** 商店狀態。 */
  status: StoreStatus;
  /** 建立時間（ISO 8601）。 */
  createdAt: string;
  /** 最後更新時間（ISO 8601）；null 表示尚未更新。 */
  updatedAt: string | null;
}

/** 目前店面（mock）。 */
export const STORE: Store = {
  id: '3fa85f64-5717-4562-b3fc-2c963f66afa6',
  storeName: '小明的數位商店',
  storeSlug: 'xiaoming-shop',
  description: '專注於數位插畫與素材販售。',
  avatarUrl: null,
  bannerUrl: null,
  status: 'Active',
  createdAt: '2026-01-15T08:30:00+00:00',
  updatedAt: null,
};
