# 資源配額 Quota

QuotaService 負責**租戶（creator）資源配額的計量與扣量**。簽發上傳 signed URL 階段**不計配額**；使用者提交確認、功能 API 建立檔案 reference 時才原子地檢查並扣量，與 [[Storage]] 的上傳流程銜接，確保並發下不超額。

本頁聚焦資源配額；租戶識別（JWT claim）見 [[Auth]]、子網域路由見 [[Infra]]，不在本頁範圍。

## 範圍與前提（MVP）

- **不實作創作者訂閱方案**：所有商店共用一組**固定額度**，由 `appsettings` 設定，非依方案動態變動。日後要做付費方案時再把固定值換成依方案查詢即可，API 與資料模型不變。
- **租戶識別**：`tenant_id` 即**創作者使用者 ID**，取自 JWT 的 `sub`（`ICurrentUserAccessor`），與 [[Storage]] 檔案紀錄的 `CreatorId` 一致。MVP 為 1 創作者 : 1 商店。

## 計量資源

| 資源 | 性質 | 維護方式 |
|------|------|----------|
| 帳號總儲存空間（bytes） | 帳號總用量 | 確認時原子扣量（核心對象） |
| 上架商品數量 | 原子計數器 | 同樣的原子條件式增減 |
| 單檔大小上限 | 即時檢查 | 扣量當下檢查，不維護狀態 |
| 單商品總量上限 | 即時檢查 | 扣量當下檢查（見「即時上限檢查」） |

固定額度設定（預設值，可於 `appsettings` 覆蓋）：

```jsonc
{
  "Quota": {
    "MaxAccountStorageBytes": 53687091200,   // 帳號總量 50 GiB
    "MaxFileSizeBytes": 2147483648,          // 單檔 2 GiB
    "MaxProductTotalBytes": 10737418240,     // 單商品總量 10 GiB
    "MaxPublishedProducts": 100              // 上架商品數
  }
}
```

## 用量資料模型

- **租戶用量（`tenant_usage`）**：`tenant_id`（PK）、`quota`（建列當下從設定快照）、`used`（bytes）、`reserved`（bytes，舊制預扣殘量，僅遞減至 0）、`product_count`、`updated_at`。
  - **建列時機**：首次 `charge` 時 **lazy upsert**（不依賴開店事件），`quota` 取當下設定值快照。
- **扣量紀錄（`reservation`）**：`id`（PK，Guid，即 `ChargeId`，慣例 = 檔案 ID）、`tenant_id`、`product_id`、`size`、`status`、`created_at`。
  - 新流程一律直接以 `committed` 入帳；`reserved` / `released` 為舊制預扣的歷史資料（由 sweeper 收斂）。
  - `id` 作為冪等鍵與跨服務關聯鍵（= StorageService `FileId` = CatalogService `AssetId`）。

## 配額扣量流程（上傳確認）

呼叫方為**功能 API（[[Catalog]] / CatalogService）**，QuotaService 不被 [[Storage]] 直接呼叫——[[Storage]] 維持為純儲存原語。

```
CatalogService 上傳流程：
  1. 簽 URL         ──upload-url──▶ StorageService（不計配額、不建資產）
  2. 回前端直傳（PUT signed URL）
  3. 使用者提交確認 ──confirm──▶ CatalogService
       3a. confirm 檔案  ──POST /v1/files/{id}/confirm──▶ StorageService（確保 Ready、取得實際大小）
       3b. 扣配額        ──POST /v1/charges──▶ QuotaService（ChargeId = FileId，409 / 422 擋下）
       3c. 建立資產 reference（CatalogAsset / CatalogVersionAsset）
       3d. 標記檔案已使用 ──POST /v1/files/{id}/reference──▶ StorageService
```

1. 簽發 signed URL 階段**完全不涉及配額**；上傳後未確認的檔案不計量，逾期由 [[Storage]] 清理排程回收。
2. 使用者提交確認時，CatalogService 以 `ChargeId = FileId` 向 QuotaService `charge` 實際大小並做即時上限檢查；配額不足（409）或超上限（422）時，資產 reference 不會建立。
3. `charge` 通過後才建立資產 reference，並回頭將檔案標記為已使用（referenced）——只有已標記檔案計入配額對帳。
4. 整條 confirm 管線每一步皆冪等，前端重試安全。
5. **刪除資產**：CatalogService 同步軟刪 [[Storage]] 檔案；`used` 由每日對帳釋放（不即時退款）。

### 即時上限檢查（無預扣狀態）

- **單檔上限**：`S <= MaxFileSizeBytes`，於 `charge` 同時驗證，超過回 422。
- **單商品總量上限**：`charge` 帶 `product_id`，QuotaService 加總該商品**已 committed 扣量的 size（含本次）**，超過 `MaxProductTotalBytes` 則拒絕。

### 商品數計數（product_count）

- `MaxPublishedProducts` 計的是 **Published 狀態的商品數**（對齊 CatalogService `CatalogStatus`：`Draft / Published / Archived / Suspended`）。
- CatalogService 在狀態轉移時呼叫 `POST /v1/products/count`：
  - 進入 `Published`（`Draft → Published`、`Archived → Published`、`Suspended` 解除回 `Published`）→ `delta = +1`，以原子條件式檢查 `product_count + 1 <= MaxPublishedProducts`，超限回 409。
  - 離開 `Published`（`→ Archived`、`→ Suspended`、刪除）→ `delta = -1`。
- `Draft` 不計數；草稿可任意建立，僅上架受限。

## Atomic 與一致性

- **DB 原子條件式更新為準**（以影響行數判斷成敗，無需顯式鎖，天然防並發超額）：

  ```sql
  UPDATE tenant_usage
  SET used = used + @S, updated_at = now()
  WHERE tenant_id = @T AND used + reserved + @S <= quota
  ```

- 商品數量上限以同樣的原子條件式增減（`product_count + 1 <= MaxPublishedProducts`）。
- **冪等規則**：
  - `charge`：呼叫方傳入 `ChargeId`（慣例 = 檔案 ID）作冪等鍵；同一交易內先冪等插入 `committed` 扣量紀錄（`ON CONFLICT DO NOTHING`，0 rows = 已扣過直接 no-op），再做上限檢查與原子扣量，任一步失敗整體 rollback。
  - 舊制 `reserved` 歷史資料由 **sweeper**（排程）釋放收斂，新流程不再產生。
- **Redis 僅作快取顯示用量**（後台呈現用），不作為授權 / 配額判斷依據。

## 統計量維護與對帳

- **跨動計數器**：charge 即時增加 `used`，查詢快速。
- **每日對帳**：排程加總實際檔案校正 `used`，修正計數器漂移（含資產刪除後的用量釋放）；不一致時以實際用量為準。
  - **資料源**：[[Storage]] per-tenant 用量加總端點（`GET /v1/files/usage?creatorId=`），回該租戶 **Ready 且已被使用（referenced）** 檔案的 size 總和；未確認的上傳不計入。

## 方案降級 / 額度調整

- MVP 無方案，但若調降固定額度導致 `used > quota`：**禁止新上傳**（扣量檢查必然失敗），**保留既有檔案不刪**，由創作者自行清理後恢復上傳。

## REST API 契約

遵循 [[Develop]] 三層 + Service DI、`offset`/`limit` 分頁、FluentValidation（422，欄位 `errors`）、AutoMapper、`ExceptionMiddleware` 轉 RFC 9457 Problem Details、`AddOpenJamJwtAuth` 驗 Hydra JWT。開發 URL 5177，Swagger 於 `/swagger`。

| Method | Path | 用途 | 授權 | 失敗碼 |
|--------|------|------|------|--------|
| `POST` | `/v1/charges` | 扣量（含單檔 / 單商品 / 總量即時檢查；冪等） | 租戶 JWT | 409 超額 / 422 超單檔或單商品上限 |
| `POST` | `/v1/products/count` | 商品數 +1 / -1（上架 / 下架時由 CatalogService 呼叫） | 租戶 JWT | 409 超上限 |
| `GET`  | `/v1/usage/me` | 查目前用量（後台顯示） | 租戶 JWT | — |
| `GET`  | `/v1/usage/{tenantId}` | 查指定租戶用量 | Admin | 404 |

- `tenant_id` 一律由 JWT `sub` 推導（`ICurrentUserAccessor`），request 不接受外部指定 tenant，避免越權。
- 超額（總量 / 商品數）回 **409 Conflict**；超單檔 / 單商品上限屬輸入層回 **422**。

## 對其他服務的相依

1. **StorageService**：`POST /v1/files/{id}/reference` 標記檔案已使用；`GET /v1/files/usage` 對帳用加總端點（僅計 referenced）；未 referenced 檔案由清理排程回收。
2. **CatalogService**：資產 confirm 管線（confirm 檔案 → `charge` → 建 reference → 標記 referenced）；新增 `QuotaServiceClient`（named `"quota"`）；上 / 下架時呼叫 `/v1/products/count`；刪除資產時軟刪 Storage 檔案。

## 技術與架構

- .NET 微服務（`src/QuotaService/`），整體結構慣例見 [[Develop]]，可參考 CatalogService / StoreService。
- 資料庫 `open_jam_quota`（snake_case、`BaseDbContext` audit / 軟刪除慣例）。
- 純 REST API 服務，無事件訂閱（扣量由功能 API 於 confirm 時同步呼叫）。
- 跨服務關聯：上傳流程 [[Storage]]、商品上下架與額度語意 [[Catalog]]、租戶識別 [[Auth]]、稽核 [[Log]]。
