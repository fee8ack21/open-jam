/* ============================================================
   Open Jam — current storefront 型別
   欄位對應 StoreService 的 StoreDto（GET /v1/stores/{slug}）。
   creator-web 為單一店家的店面（<creator>.openjam.co），
   店面資料一律由 API 取得（見 stores/shop.ts 的 loadCatalog）。
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

/** API 載入前的空店面初始值（不顯示任何假資料）。 */
export const EMPTY_STORE: Store = {
  id: '',
  storeName: '',
  storeSlug: '',
  description: null,
  avatarUrl: null,
  bannerUrl: null,
  status: 'Active',
  createdAt: '',
  updatedAt: null,
};
