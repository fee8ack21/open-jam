// ============================================================
// API service 進入點（market-web）
//
// 後端 client 一律由 swagger-typescript-api 從各服務的 OpenAPI
// 自動產生（見 package.json 的 `gen:api`），此處只負責設定 baseUrl。
// market-web 的市集瀏覽走公開（匿名）端點，不需注入 token。
// 業務程式碼一律 import 這裡匯出的實例，不直接 new 產生出來的 class。
// ============================================================
import { Api as CatalogApi, HttpClient as CatalogHttpClient } from '@/api/catalog-service';
import { Api as StoreApi, HttpClient as StoreHttpClient } from '@/api/store-service';
import { env } from '@/environment';

const catalogHttp = new CatalogHttpClient({ baseUrl: env.CATALOG_API_URL });
const storeHttp = new StoreHttpClient({ baseUrl: env.STORE_API_URL });

/** CatalogService API client（公開商品瀏覽：catalogs / categories / tags）。 */
export const catalogApi = new CatalogApi(catalogHttp);

/** StoreService API client（公開商店資訊：GET /v1/stores/{id}，用於補商品的商店名稱）。 */
export const storeApi = new StoreApi(storeHttp);
