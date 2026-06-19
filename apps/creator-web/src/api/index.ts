// ============================================================
// API service 進入點（creator-web）
//
// 後端 client 一律由 swagger-typescript-api 從各服務的 OpenAPI
// 自動產生（見 package.json 的 `gen:api`），此處只負責設定 baseUrl 並
// 以 customFetch 注入 OIDC Bearer token。creator-web 多數瀏覽走公開
// （匿名）端點；少數需登入的操作（如商品收藏 wishlist）會帶上 token。
// 業務程式碼一律 import 這裡匯出的實例，不直接 new 產生出來的 class。
// ============================================================
import { Api as CatalogApi, HttpClient as CatalogHttpClient } from '@/api/catalog-service';
import { Api as StoreApi, HttpClient as StoreHttpClient } from '@/api/store-service';
import { env } from '@/environment';
import { userManager } from '@/oidc/auth';

/**
 * 包一層 fetch，於請求帶上目前 OIDC 使用者的 access token（若已登入）。
 * 公開端點未登入時不帶 token、照常匿名呼叫；token 可能因 silent renew 更新，故即時讀取不快取。
 */
const authFetch: typeof fetch = async (input, init = {}) => {
  const user = await userManager.getUser();
  const headers = new Headers(init.headers ?? {});
  if (user?.access_token && !headers.has('Authorization')) {
    headers.set('Authorization', `Bearer ${user.access_token}`);
  }
  return fetch(input, { ...init, headers });
};

const catalogHttp = new CatalogHttpClient({ baseUrl: env.CATALOG_API_URL, customFetch: authFetch });
const storeHttp = new StoreHttpClient({ baseUrl: env.STORE_API_URL, customFetch: authFetch });

/** CatalogService API client（商品瀏覽公開；收藏 wishlist 需登入）。 */
export const catalogApi = new CatalogApi(catalogHttp);

/** StoreService API client（公開商店資訊：GET /v1/stores/{slug}）。 */
export const storeApi = new StoreApi(storeHttp);
