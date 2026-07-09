// ============================================================
// API service 進入點（portal-web）
//
// 後端 client 一律由 swagger-typescript-api 從各服務的 OpenAPI
// 自動產生（見 package.json 的 `gen:api`），此處只負責設定 baseUrl。
// portal-web 的市集瀏覽走公開（匿名）端點，不需注入 token；
// in-app 通知（notificationApi）需登入身分，以 customFetch 注入 OIDC Bearer token。
// 業務程式碼一律 import 這裡匯出的實例，不直接 new 產生出來的 class。
// ============================================================
import { Api as CatalogApi, HttpClient as CatalogHttpClient } from '@/api/catalog-service';
import { Api as StoreApi, HttpClient as StoreHttpClient } from '@/api/store-service';
import {
  Api as NotificationApi,
  HttpClient as NotificationHttpClient,
} from '@/api/notification-service';
import { Api as ContentApi, HttpClient as ContentHttpClient } from '@/api/content-service';
import { env } from '@/environment';
import { userManager } from '@/oidc/auth';

/**
 * 包一層 fetch，於每個請求帶上目前 OIDC 使用者的 access token。
 * token 可能因 silent renew 而更新，故每次請求即時讀取，不快取。
 */
const authFetch: typeof fetch = async (input, init = {}) => {
  const user = await userManager.getUser();
  const headers = new Headers(init.headers ?? {});
  if (user?.access_token && !headers.has('Authorization')) {
    headers.set('Authorization', `Bearer ${user.access_token}`);
  }
  return fetch(input, { ...init, headers });
};

const catalogHttp = new CatalogHttpClient({ baseUrl: env.CATALOG_API_URL });
const storeHttp = new StoreHttpClient({ baseUrl: env.STORE_API_URL });
const notificationHttp = new NotificationHttpClient({
  baseUrl: env.NOTIFICATION_API_URL,
  customFetch: authFetch,
});

/** CatalogService API client（公開商品瀏覽：catalogs / categories / tags）。 */
export const catalogApi = new CatalogApi(catalogHttp);

/** StoreService API client（公開商店資訊：GET /v1/stores/{id}，用於補商品的商店名稱）。 */
export const storeApi = new StoreApi(storeHttp);

/** NotificationService API client（in-app 通知列表 / 未讀數 / 已讀，需登入）。 */
export const notificationApi = new NotificationApi(notificationHttp);

const contentHttp = new ContentHttpClient({ baseUrl: env.CONTENT_API_URL });

/** ContentService REST API client（目前啟用中的條款 / 隱私權政策、已發布 FAQ，皆匿名公開）。 */
export const contentApi = new ContentApi(contentHttp);
