# 資源配額 Quota

QuotaService 負責**租戶（creator）資源配額的計量、預扣與回滾**。在簽發上傳 signed URL 前原子地檢查並預扣，與 [[Storage]] 的上傳流程銜接，確保並發下不超額。

本頁聚焦資源配額；租戶識別（JWT claim）見 [[Auth]]、子網域路由見 [[Infra]]，不在本頁範圍。

## 範圍與前提（MVP）

- **不實作創作者訂閱方案**：所有商店共用一組**固定額度**，由 `appsettings` 設定，非依方案動態變動。日後要做付費方案時再把固定值換成依方案查詢即可，API 與資料模型不變。
- **租戶識別**：`tenant_id` 即**創作者使用者 ID**，取自 JWT 的 `sub`（`ICurrentUserAccessor`），與 [[Storage]] `FileReadyEvent.CreatorId` 一致。MVP 為 1 創作者 : 1 商店。

## 計量資源

| 資源 | 性質 | 維護方式 |
|------|------|----------|
| 帳號總儲存空間（bytes） | 帳號總用量 | 預扣 / commit / release（核心對象） |
| 上架商品數量 | 原子計數器 | 同樣的原子條件式增減 |
| 單檔大小上限 | 即時檢查 | 上傳當下檢查，不維護預扣狀態 |
| 單商品總量上限 | 即時檢查 | 上傳當下檢查（見「即時上限檢查」） |

固定額度設定（預設值，可於 `appsettings` 覆蓋）：

```jsonc
{
  "Quota": {
    "MaxAccountStorageBytes": 53687091200,   // 帳號總量 50 GiB
    "MaxFileSizeBytes": 2147483648,          // 單檔 2 GiB
    "MaxProductTotalBytes": 10737418240,     // 單商品總量 10 GiB
    "MaxPublishedProducts": 100,             // 上架商品數
    "ReservationTtlMinutes": 240             // 預扣有效期（須 ≥ signed URL 時效，涵蓋影片 resumable 上傳）
  }
}
```

## 用量資料模型

- **租戶用量（`tenant_usage`）**：`tenant_id`（PK）、`quota`（建列當下從設定快照）、`used`（bytes）、`reserved`（bytes）、`product_count`、`updated_at`。
  - **建列時機**：首次 `reserve` 時 **lazy upsert**（不依賴開店事件），`quota` 取當下設定值快照。
- **預扣紀錄（`reservation`）**：`id`（PK，Guid，即 `ReservationId`）、`tenant_id`、`size`、`status`（`reserved` / `committed` / `released`）、`expiry`、`created_at`。
  - `id` 作為跨服務關聯鍵，貫穿上傳 → ready → commit 全程。

## 配額檢查與預扣流程（上傳）

呼叫方為**功能 API（[[Catalog]] / CatalogService）**，QuotaService 不被 [[Storage]] 直接呼叫——[[Storage]] 維持為純儲存原語。

```
CatalogService 上傳流程：
  1. reserve(S)  ──POST /v1/reservations──▶ QuotaService（回 ReservationId）
  2. 簽 URL      ──upload-url(ReservationId)──▶ StorageService（存於 file 紀錄）
  3. 回前端直傳
  ※ 步驟 2 失敗 → 主動 release(ReservationId)
```

1. CatalogService 在簽發 upload signed URL **之前**向 QuotaService `reserve` S bytes 並做即時上限檢查。
2. **原子條件式更新**（見下），通過才建立 reservation；`expiry` 取 `now + ReservationTtlMinutes`（須涵蓋 signed URL 時效與大檔 resumable 上傳）。
3. CatalogService 將 `ReservationId` 帶給 StorageService `upload-url`；StorageService 存於 file 紀錄，並於後續 `FileReadyEvent` 回帶。
4. **commit（事件驅動）**：QuotaService 訂閱 [[Storage]] `FileReadyEvent`，以 `ReservationId` 找 reservation，依事件實際 `SizeBytes` commit。
5. **release**：
   - 主要靠 **sweeper**（排程）回滾過期且仍 `reserved` 的 reservation。
   - CatalogService 在「簽章失敗、檔案根本沒上傳」時可主動呼叫 `release` 即時釋放。

### 即時上限檢查（無預扣狀態）

- **單檔上限**：`S <= MaxFileSizeBytes`，於 `reserve` 同時驗證，超過回 422。
- **單商品總量上限**：`reserve` 帶 `product_id`，QuotaService 加總該商品**目前已 committed reservation 的 size + 本次 S**，超過 `MaxProductTotalBytes` 則拒絕。（reservation 需存 `product_id`。）

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
  SET reserved = reserved + @S, updated_at = now()
  WHERE tenant_id = @T AND used + reserved + @S <= quota
  ```

- 商品數量上限以同樣的原子條件式增減（`product_count + 1 <= MaxPublishedProducts`）。
- **狀態機與冪等規則**（commit 與 sweeper 可能競態，必須冪等）：
  - `reserve`：呼叫方傳入 `ReservationId`（client 端產生 Guid）作冪等鍵；重送同 id 回原結果，不重複預扣。
  - `commit(id, actual)`：`actual` 取自 `FileReadyEvent.SizeBytes`（null 時退回 `reservation.size`）。實際大小可能 ≠ 預扣大小，故 `used` 加實際、`reserved` 釋放當初預扣量：
    - 僅當 `status = reserved` → `used += actual`、`reserved -= reservation.size`、`status = committed`。
    - **遲到 commit**（reservation 已被 sweeper `released`）：`status = released` → 補記 `used += actual`、`status = committed`，**不動 `reserved`**（已於 release 扣回 `reservation.size`）。檔案實際已 ready 存在，必須計入。
    - `status` 已是 `committed`：no-op（事件重送去重）。
  - `release(id)`：僅當 `status = reserved` → `reserved -= size`、`status = released`；其他狀態 no-op。
  - `status` 欄位本身即為去重 / 順序保護的依據。
- **Redis 僅作快取顯示用量**（後台呈現用），不作為授權 / 配額判斷依據。

## 統計量維護與對帳

- **跨動計數器**：commit / release 即時增減 `used` / `reserved`，查詢快速。
- **每日對帳**：排程加總實際檔案校正 `used`，修正計數器漂移；不一致時以實際用量為準。
  - **資料源**：需 [[Storage]] 提供 per-tenant 用量加總端點（`GET /v1/files/usage?creatorId=`，回該租戶 ready 檔案 size 總和）——此為 QuotaService 的相依，StorageService 需新增。

## 方案降級 / 額度調整

- MVP 無方案，但若調降固定額度導致 `used > quota`：**禁止新上傳**（預扣檢查必然失敗），**保留既有檔案不刪**，由創作者自行清理後恢復上傳。

## REST API 契約

遵循 [[Develop]] 三層 + Service DI、`offset`/`limit` 分頁、FluentValidation（422，欄位 `errors`）、AutoMapper、`ExceptionMiddleware` 轉 RFC 9457 Problem Details、`AddOpenJamJwtAuth` 驗 Hydra JWT。開發 URL 待分配（接續 CatalogService 5176，建議 5177），Swagger 於 `/swagger`。

| Method | Path | 用途 | 授權 | 失敗碼 |
|--------|------|------|------|--------|
| `POST` | `/v1/reservations` | 預扣（含單檔 / 單商品即時檢查） | 租戶 JWT | 409 超額 / 422 超單檔或單商品上限 |
| `POST` | `/v1/reservations/{id}/release` | 主動釋放預扣 | 租戶 JWT | 404 |
| `POST` | `/v1/products/count` | 商品數 +1 / -1（上架 / 下架時由 CatalogService 呼叫） | 租戶 JWT | 409 超上限 |
| `GET`  | `/v1/usage/me` | 查目前用量（後台顯示） | 租戶 JWT | — |
| `GET`  | `/v1/usage/{tenantId}` | 查指定租戶用量 | Admin | 404 |

- `tenant_id` 一律由 JWT `sub` 推導（`ICurrentUserAccessor`），request 不接受外部指定 tenant，避免越權。
- `commit` 無 HTTP 端點：QuotaService 作為 RabbitMQ consumer 訂閱 `FileReadyEvent`（與 LogService / EmailService 同 pattern）。
- 超額（總量 / 商品數）回 **409 Conflict**；超單檔 / 單商品上限屬輸入層回 **422**。

## 對其他服務的相依（需配套修改）

1. **`Shared/Events/FileReadyEvent`**：新增 `Guid? ReservationId` 欄位。
2. **StorageService**：`upload-url` request 新增 `ReservationId`，存於 file 紀錄並於 `FileReadyEvent` 回帶；新增 `GET /v1/files/usage` 對帳用加總端點。
3. **CatalogService**：上傳流程插入 `reserve`（簽章前）與失敗 `release`；新增 `QuotaServiceClient`（named `"quota"`）；上 / 下架時呼叫 `/v1/products/count`。

## 技術與架構

- .NET 微服務（`src/QuotaService/`），整體結構慣例見 [[Develop]]，可參考 CatalogService / StoreService。
- 資料庫 `open_jam_quota`（snake_case、`BaseDbContext` audit / 軟刪除慣例）。
- 事件採 RabbitMQ + MassTransit（與 [[Storage]] / [[Email]] 一致）。
- 跨服務關聯：上傳流程 [[Storage]]、商品上下架與額度語意 [[Catalog]]、租戶識別 [[Auth]]、稽核 [[Log]]。
