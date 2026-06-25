// ============================================================
// API service 進入點
//
// 後端 client 一律由 swagger-typescript-api 從各服務的 OpenAPI
// 自動產生（見 package.json 的 `gen:api`），此處只負責：
//   1. 設定 baseUrl（環境變數）
//   2. 以 customFetch 注入 OIDC Bearer token
// 業務程式碼一律 import 這裡匯出的實例，不直接 new 產生出來的 class。
// ============================================================
import { Api as StoreApi, HttpClient as StoreHttpClient } from '@/api/store-service';
import { Api as CatalogApi, HttpClient as CatalogHttpClient } from '@/api/catalog-service';
import { Api as LogApi, HttpClient as LogHttpClient } from '@/api/log-service';
import { Api as OrderApi, HttpClient as OrderHttpClient } from '@/api/order-service';
import { Api as AuthApi, HttpClient as AuthHttpClient } from '@/api/auth-service';
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

const storeHttp = new StoreHttpClient({
  baseUrl: env.STORE_API_URL,
  customFetch: authFetch,
});

const catalogHttp = new CatalogHttpClient({
  baseUrl: env.CATALOG_API_URL,
  customFetch: authFetch,
});

const logHttp = new LogHttpClient({
  baseUrl: env.LOG_API_URL,
  customFetch: authFetch,
});

const orderHttp = new OrderHttpClient({
  baseUrl: env.ORDER_API_URL,
  customFetch: authFetch,
});

const authHttp = new AuthHttpClient({
  baseUrl: env.AUTH_API_URL,
  customFetch: authFetch,
});

/** StoreService API client（store-applications / stores / followers）。 */
export const storeApi = new StoreApi(storeHttp);

/** CatalogService API client（catalog / categories / tags）。 */
export const catalogApi = new CatalogApi(catalogHttp);

/** LogService API client（audit-logs 稽核日誌查詢）。 */
export const logApi = new LogApi(logHttp);

/** OrderService API client（orders 訂單列表 / 明細查詢）。 */
export const orderApi = new OrderApi(orderHttp);

/** Auth service REST API client（users 平台會員列表，管理員專屬）。 */
export const authApi = new AuthApi(authHttp);
