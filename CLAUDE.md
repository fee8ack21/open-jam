# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Open Jam 是台灣數位商品平台（參考 Gumroad）。創作者可在子網域 `<creator>.openjam.co` 開店；消費者無需註冊，憑信箱即可購買。

## Commands

### 後端服務（從各 `src/<Service>/` 執行）

```bash
dotnet run                  # 開發伺服器
dotnet build                # 建置
dotnet publish -c Release   # 發佈
dotnet ef migrations add <Name>   # 新增 EF Core Migration（需 dotnet-ef tool）
```

| 服務 | 說明 | 開發 URL |
|------|------|----------|
| Auth | MVC 登入 / 註冊 / 忘記密碼 | http://localhost:5169 |
| LogService | Audit Log REST API | http://localhost:5170，Swagger: `/swagger` |
| StorageService | 檔案上傳 / 下載 URL 簽發 REST API | http://localhost:5171，Swagger: `/swagger` |
| StoreService | 開店申請 / 店家 / 追蹤 REST API | http://localhost:5172，Swagger: `/swagger` |
| CatalogService | 商品 / 版本 / 分類 / 標籤 / 評論 / 收藏 REST API | http://localhost:5176，Swagger: `/swagger` |
| QuotaService | 租戶資源配額計量 REST API | http://localhost:5177，Swagger: `/swagger` |
| PaymentService | Stripe Checkout 金流 REST API | http://localhost:5178，Swagger: `/swagger` |
| OrderService | 訂單 / 結帳 / 狀態歷程 REST API | http://localhost:5179，Swagger: `/swagger` |
| NotificationService | 通知（追蹤者上架 / 公告，Email + in-app）REST API | http://localhost:5180，Swagger: `/swagger` |
| ContentService | 法律文件（服務條款 / 隱私權政策）版本 + 常見問題（FAQ）REST API | http://localhost:5181，Swagger: `/swagger` |
| EmailService | RabbitMQ Worker（無 HTTP port） | — |
| Bootstrap | Seed，一次性執行後結束 | — |

Docker（從 `src/` 執行）：

```bash
docker build -f Auth/Dockerfile -t open-jam-auth .
```

### 前端應用（從各 `apps/<app>/` 執行）

皆為 Vue 3 + Vite 5 + Pinia + Vue Router 4 + Naive UI、pnpm 管理的 SPA。

| App | 用途 | 認證 |
|-----|------|------|
| portal-web | 市集首頁 / landing | OIDC（oidc-client-ts） |
| creator-web | 創作者店面：商品列表 / 詳情 / 結帳（Stripe Checkout 導轉）/ 訂單下載頁（`/orders/:orderId`，訪客憑訂單 ID 下載） | 無（消費者免註冊） |
| workspace-web | 創作者後台（開店、商品 / 訂單 / 公告管理、上架、設定、購買紀錄與下載）+ 管理員後台（會員 / 商店 / 商品 / 訂單 / 資源用量 / 稽核） | OIDC（oidc-client-ts） |

```bash
pnpm dev    # Vite dev server（預設 5173，多個並跑依序遞增）
pnpm build
```

#### 前端串接後端 API 流程（強制）

前端**不手寫** API 呼叫、**不 hard code** 任何 endpoint 路徑、URL、query string 或 request / response 型別。一律走自動產生流程：

1. **OpenAPI 來源**：各後端服務的 Swagger 文件放在該 app 的 `openapi/` 資料夾（如 `workspace-web/openapi/catalog-service.json`、`log-service.json`），或直接指向開發伺服器的 `/swagger/v1/swagger.json`。
2. **產生 client**：以 `swagger-typescript-api` 透過 `package.json` 的 `gen:api`（及各 `gen:api:<service>`）script，將型別與 API class 產生到 `src/api/<service>.ts`。**產生檔不手動編輯**；後端 API 變更時更新 `openapi/` 文件並重跑 `pnpm gen:api`。
3. **統一進入點**：`src/api/index.ts` 匯入產生的 `Api` / `HttpClient`，設定 `baseUrl`（取自 `environment.ts` 的環境變數，非寫死）並以 `customFetch` wrapper 注入 OIDC Bearer token，匯出 `storeApi` / `catalogApi` / `logApi` 等實例。
4. **業務使用**：所有 component / store 一律 `import` `src/api/index.ts` 匯出的實例呼叫，**不直接 `new` 產生出來的 class、不自行 `fetch`**。

詳見 [docs/develop.md](docs/develop.md) 的「前端串接後端 API」。

### 文件站（從 `docs/` 執行）

```bash
pnpm dev      # http://localhost:5173
pnpm build
pnpm preview
```

## 架構慣例

- **軟體三層 + Service DI**：Controller 只做 HTTP（路由、model binding、狀態碼），業務邏輯一律下放至 `Services/<Feature>/` 的 `I<Feature>Service` + 實作，於 `Program.cs` 以 `AddScoped` 註冊（詳見 `docs/develop.md`）
- **API Model**：Request / Response / DTO 依功能集中於 `Models/<Feature>Models.cs`（一功能一檔，非一型別一檔），namespace `<Service>.Models`
- **Model 驗證**：REST API 服務一律以 **FluentValidation** 驗證輸入 model。驗證器置於 `Validators/`（`AbstractValidator<TRequest>`），於 `Program.cs` 呼叫 `AddOpenJamValidation(typeof(Program).Assembly)`（`Shared/Web/ValidationExtensions.cs`）註冊。**僅無狀態輸入驗證**（格式 / 長度 / 必填 / 範圍）放驗證器；需查 DB 或跨服務的檢查（唯一性、存在性、Owner、狀態轉移）留在 service 層拋 `AppException` 子類。`ValidationActionFilter` 將驗證失敗統一拋 `ValidationException`（422，含欄位 `errors`），不使用 FluentValidation 內建 auto-validation（避免回 400 破壞契約）。分頁範圍重用 `Shared.Web.PaginationRules` 的 `ValidOffset()` / `ValidLimit()`
- **Model 轉換**：Entity ↔ DTO 一律以 **AutoMapper** 轉換。`Profile` 置於 `Mapping/`，於 `Program.cs` 呼叫 `AddOpenJamMapping(typeof(Program).Assembly)`（`Shared/Web/MappingExtensions.cs`）註冊；業務層注入 `IMapper`，IQueryable 投影用 `ProjectTo<TDto>(mapper.ConfigurationProvider)`。AutoMapper 只做扁平欄位對應，需 async 查詢補值的欄位（資產 URL、標籤清單、目前版本等）以 `Ignore()` 標記，由 service map 後補上
- **分頁**：一律 `offset` / `limit`（非 `page` / `pageSize`）
- **時間欄位**：型別 `DateTimeOffset`，命名以 `At` 結尾
- **資料庫命名**：snake_case；C# Entity 保持 PascalCase，由 `BaseDbContext` 自動套用 EF Core naming convention，不需手動 `[Column]`
- **XML 文件**：Entity、DTO、Controller Action 皆須 `<summary>`；DTO 屬性另加 `<example>`（影響 Swagger 完整度，強制要求）
- **錯誤處理**：業務層拋 `AppException` 子類。REST API 服務（LogService / StorageService / StoreService 等）以 `ExceptionMiddleware` 轉為 RFC 9457 Problem Details；MVC 服務（Auth）以 ASP.NET Core 內建 `UseExceptionHandler` 導頁至 Error Page，不掛載 `ExceptionMiddleware`
- **微服務 Ref Table**：本地保留其他服務資源的參照表，資源變更時發 Event 同步

## Shared 類別庫（`src/Shared/`）

所有服務共用，不含 HTTP 路由邏輯。

**`BaseDbContext`**（`Shared/Data/`）— 覆寫 `SaveChangesAsync`，自動填入 Audit 欄位（`ICreatedAt/By`、`IUpdatedAt/By`）。Audit 欄位 setter 皆為 `private set`，僅 DbContext 攔截邏輯可寫入。**軟刪除**：業務層照常呼叫 `DbSet.Remove()`，`BaseDbContext` 攔截 `IDeletedAt` entity 的 `Deleted` 狀態轉為 `Modified` 並填入 `DeletedAt/By`；`OnModelCreating` 對所有 `IDeletedAt` 套用全域 Query Filter（預設排除已刪除）。**硬刪除**需 `IgnoreQueryFilters().ExecuteDeleteAsync()` 繞過追蹤（見 `docs/develop.md`）。

**`ICurrentUserAccessor`**（`Shared/Auth/`）— 取得目前使用者 `Guid? UserId`。HTTP 服務注入 `HttpContextUserAccessor`（從 JWT Claims 讀 `sub`）；design-time / migration 等無 HTTP 情境注入 `NullCurrentUserAccessor`（永遠回傳 null）。

**`AddOpenJamJwtAuth`**（`Shared/Auth/JwtBearerExtensions.cs`）— REST API 服務於 `Program.cs` 呼叫，加入 JwtBearer 驗證（驗 Hydra JWKS）與 `"Admin"` 授權 policy（`RequireRole("Admin")`）。

**服務間認證**（`Shared/Auth/ServiceAuth*.cs`）— 內部 API（如 PaymentService checkout-session）不對外開放時使用。呼叫端 `AddOpenJamServiceTokenClient` 註冊 `ServiceTokenClient`（singleton，向 Hydra 以 `client_credentials` 換 token 並快取至到期前一分鐘，client 為 Bootstrap seed 的 `open-jam-service`）；被呼叫端 `AddOpenJamInternalServicePolicy` 加入 `"InternalService"` policy（要求 JWT `sub` == `ServiceAuth:ClientId`）。設定取自 `ServiceAuth` 區段（`TokenUrl` / `ClientId` / `ClientSecret`）。

**`AppException` 子類** — `NotFoundException(404)` / `ForbiddenException(403)` / `ConflictException(409)` / `ValidationException(422)` / `UnauthorizedException(401)`。

**`ExceptionMiddleware`** — 僅用於 **REST API 服務**。在 `Program.cs` 以 `app.UseExceptionMiddleware()` 掛載，需排在所有其他 middleware 之前。MVC 服務不使用此 middleware，改以 `app.UseExceptionHandler(...)` 處理。

**Events**（`Shared/Events/`）— `EmailRequestedEvent`、`AuditLogRequestedEvent`、`FileReadyEvent`、`PaymentSucceededEvent`、`OrderCompletedEvent`、`CatalogPublishedEvent`、`StoreFollowerChangedEvent`、`UserRegisteredEvent`：各服務在業務 transaction 內寫入 Outbox，由 `OutboxRelayService` 排程搬入 RabbitMQ。

## Outbox 模式

業務 transaction 內寫入 `OutboxMessage` 表，由各服務的 `OutboxRelayService`（`IHostedService`）定期掃描並 publish 到 RabbitMQ。冪等鍵為 `OutboxMessageId`，Consumer 端需去重。

## Auth 服務（`src/Auth/`）

ASP.NET Core 8 MVC，整合 Ory Hydra（OIDC）。

**核心服務**：
- `UserService` — 註冊、登入、密碼重設、email 驗證；註冊成功經 Outbox 發 `UserRegisteredEvent`（供 StoreService / NotificationService 回填訪客 UserId）；註冊交易內寫入當下啟用中條款版本的 `UserLegalConsent`（啟用文件清單於交易前向 ContentService 即時取得）
- `HydraService` — 包裝 Hydra Admin API（accept/reject login/consent）
- `ContentServiceClient` + `LegalConsentService` — 法律文件本身由 **ContentService** 管理，Auth 僅保留**同意紀錄**（`UserLegalConsent`，`LegalDocumentId` 為跨服務參照、無外鍵）。`ContentServiceClient`（named HttpClient `"content"`）呼叫 ContentService 匿名端點 `GET /v1/legal-documents/active` 取啟用版本；`LegalConsentService` 以此比對本地同意紀錄算出「未同意清單」（`GetPendingConsentAsync`）、寫入同意（`RecordConsentsAsync`）。註冊時 ContentService 不可用則中止註冊（fail-closed，不建帳號）。
- `Argon2idHasher` — 密碼雜湊
- `OutboxRelayService` — Outbox → RabbitMQ

**REST API**：`GET /v1/users`（Admin，`UsersController`）——全平台使用者分頁列表，供 workspace-web 管理員後台會員列表。（法律文件管理 API 已移至 ContentService。）

**條款同意流程**：註冊頁 dialog 內容由 ContentService 啟用版本渲染（`_LegalModal.cshtml` + `LegalContentHelper`，內容慣例「## 」章節 /「- 」列點），`form-ui.js` 強制兩份文件都點過「我了解了」才可勾選同意；登入（含 Hydra skip 路徑）時檢查使用者對啟用版本的 `UserLegalConsent`，缺少者導向 `LegalReconsent` 頁重新勾選同意（subject / challenge 以 time-limited Data Protection 票證保護，15 分鐘失效）後才 AcceptLogin。

**錯誤處理**：使用 ASP.NET Core 內建 `app.UseExceptionHandler("/Home/Error")`（非開發環境）導頁至 Error Page。不掛載 `ExceptionMiddleware`（JSON 輸出不適用於 MVC 畫面流程）。

**流程**：所有 POST 使用 `[ValidateAntiForgeryToken]` + PRG pattern（成功 → `RedirectToAction`，失敗 → `return View(model)` 帶 ModelState errors）。Email 透過 `TempData["Email"]` 在 POST → GET 間傳遞。

**前端（無 build step）**：

script 載入順序（`_Layout.cshtml`）：jQuery → jquery-validation → jquery-validation-unobtrusive → `validation-setup.js` → `form-ui.js`

- `validation-setup.js`：自訂驗證規則（`mustbetrue`、`minpasswordstrength`）+ unobtrusive adapters + `$.validator.setDefaults`（`.err` class on `.input-shell`）
- `form-ui.js`：密碼強度 meter、visibility toggle、checkbox sync、legal modal、resend timer

表單欄位一律以內聯 HTML 撰寫，搭配 `IconHelper` 渲染 SVG icon；不使用額外 Partial View 封裝輸入元件。

## EmailService（`src/EmailService/`）

RabbitMQ Worker，消費 `EmailRequestedEvent`，從 DB 讀取模板渲染後以 SendGrid API 寄出（地端開發用 SMTP catcher）。失敗採指數退避重試（最多 5 次），並有 `EmailRetryService` 補償排程。

## LogService（`src/LogService/`）

消費 `AuditLogRequestedEvent` 寫入 `audit_log` 表，並提供分頁查詢 REST API（`/swagger`）。

## StorageService（`src/StorageService/`）

REST API，簽發上傳 / 下載 URL 並管理檔案生命週期。

- **儲存後端**：`IStorageProvider` 抽象，依 `Storage:Provider` 設定切換地端 `LocalStorageProvider`（本地檔案系統，預設存於 `Files/` 資料夾）或雲端 `GcsStorageProvider`（GCS，signed URL 用服務帳戶金鑰或 GKE Workload Identity 的 IAM SignBlob）。
- **流程**：`FilesController` 簽發上傳 / 下載 URL（本地由 `BlobUrlSigner` 以 HMAC 簽章，取代 presigned URL）→ 客戶端直傳；本地經 `BlobController` 接收上傳寫入 `LocalFileStore` 後即觸發 `StorageEventService` → `FileProcessingService` 處理 → publish `FileReadyEvent`（GCS 則由 bucket notification 觸發）。
- **本地 blob 端點**：`BlobController`（`/v1/files/blob/{**key}`）負責本地儲存的 PUT 上傳 / GET 下載；`public/` 前綴免簽章供匿名讀取，其餘須帶有效 `expires` + `sig`。
- **檔案生命週期與配額銜接**：簽發 signed URL **不計配額**。`POST /v1/files/{id}/confirm` 驗證物件存在後觸發處理 pipeline（Local / GCS 共用同一條 confirm 路徑）；`POST /v1/files/{id}/reference` 由功能 API 於資產 reference 建立後標記檔案「已被使用」——只有 referenced 檔案計入配額對帳，未 referenced 者逾期由清理排程回收。
- **用量查詢**：`GET /v1/files/usage?creatorId=`（回該租戶 Ready 且 referenced 檔案 size 總和，QuotaService 每日對帳用）、`GET /v1/files/usage/summary`（Admin，平台儲存用量彙總，workspace-web 資源頁）。
- **背景**：`OrphanCleanupService` 清理保留期過後的孤兒檔案（硬刪除）。

## StoreService（`src/StoreService/`）

REST API，管理開店申請、店家與追蹤。Controller（`StoreApplications` / `Stores` / `StoreFollowers`）僅轉接，業務在 `Services/<Feature>/` 的 `IStoreApplicationService` / `IStoreManager` / `IStoreFollowerService`。

- **`StorageServiceClient`**：以 `HttpClient`（named `"storage"`，BaseUrl 由 `ServiceOptions` 設定）呼叫 StorageService 簽發 Avatar / Banner 上傳 URL；上傳後前端呼叫 `POST /v1/stores/{id}/avatar|banner/confirm` 轉呼叫 StorageService confirm。
- **全平台商店列表**：`GET /v1/stores`（Admin），供 workspace-web 管理員後台。
- **`StoreSlugValidator` / `StoreAuthorization`**：子網域 slug 驗證與店家成員授權。
- **追蹤者事件**：Follow / Unfollow 經 Outbox 發 `StoreFollowerChangedEvent`（NotificationService 同步 ref 表）；`UserRegisteredConsumer` 消費 `UserRegisteredEvent` 回填訪客追蹤者的 `UserId`。
- **`AuditLogPublisher`** + `OutboxRelayService`：經 Outbox 發 `AuditLogRequestedEvent`。

## CatalogService（`src/CatalogService/`）

REST API，管理數位商品（`Catalog`）、版本（`CatalogVersion`）、平台分類（`CatalogCategory`，以 `ParentId` 自我參照多層子分類）、標籤（`CatalogTag`，名稱強制小寫、維護 `UsageCount`）、評論（`CatalogReview`）與收藏（`CatalogFavorite`）。Controller（`Catalogs` / `CatalogVersions` / `CatalogCategories` / `CatalogTags` / `CatalogReviews` / `CatalogFavorites`）僅轉接，業務在 `Services/<Feature>/` 的 `ICatalogManager` / `ICatalogVersionService` / `ICatalogCategoryService` / `ICatalogTagService` / `ICatalogReviewService` / `ICatalogFavoriteService`。

- **商品狀態**：`Draft → Published`（需先有版本）↔ `Archived`；`Suspended` 由 Admin 停權 / 解除。`CatalogStatus` 索引化以利瀏覽過濾。上 / 下架時呼叫 QuotaService `POST /v1/products/count` ±1 檢查上架商品數上限；首次上架經 Outbox 發 `CatalogPublishedEvent`（`IsFirstPublish`，觸發追蹤者通知）。**付費商品上架閘門**：`Price > 0` 上架（及已上架商品免費改付費）時以 `PaymentServiceClient`（named `"payment"`）匿名查 PaymentService `GET /v1/connect/accounts/{storeId}/status`，商店未完成 Stripe Connect 收款設定（`ChargesEnabled=false`）回 422；免費商品不受限。
- **資產分兩類**：展示型 `CatalogAsset`（縮圖 / 截圖 / 預覽影音，公開讀取，`public/` 前綴）；版本可下載檔 `CatalogVersionAsset`（買家實際取得內容，私有，須授權簽發下載 URL）。Asset `Id` 與 StorageService 簽發的 `FileId` 同值。
- **資產 confirm 管線（配額扣量）**：簽上傳 URL 不計配額；使用者提交確認時依序——StorageService `POST /v1/files/{id}/confirm`（確保 Ready、取實際大小）→ QuotaService `POST /v1/charges`（`ChargeId = FileId` 冪等扣量，超額 409 / 超上限 422 擋下）→ 建立資產 reference → StorageService `POST /v1/files/{id}/reference` 標記已使用。每步冪等，前端重試安全。
- **計數與策展**：`SalesCount`（`OrderCompletedConsumer` 消費 `OrderCompletedEvent` 原子累加，`ProcessedEvent` 去重）、`ViewCount`（`POST /v1/catalogs/{id}/view`，前端以 localStorage 時間窗去重）、`RatingAverage` / `RatingCount`（隨評論維護）、`IsFeatured`（Admin `POST /{id}/feature` / `/unfeature`，首頁精選 × 熱門混合策展）。列表 API 支援排序與價格篩選。
- **評論**：限已購買者（`OrderServiceClient` 轉發 Bearer token 至 OrderService `GET /v1/orders/purchased/{catalogId}` 驗證），每人每商品一則（`PUT /v1/catalogs/{id}/reviews/mine` upsert / `DELETE` 撤下）。
- **買家下載**：`GET /v1/catalogs/{id}/versions/{versionId}/downloads` 回版本可下載檔清單含短效下載 URL。登入買家以購買紀錄授權（同上 `OrderServiceClient`）；訪客憑 `?orderId=`（不可猜測的訂單 ID 作為下載憑證，隨訂單完成信寄出）由 `OrderServiceClient` 匿名查 OrderService 驗證訂單已完成且包含該商品。
- **`StorageServiceClient`**（named `"storage"`）：簽發上傳 / 下載 URL、confirm / reference。**`StoreServiceClient`**（named `"store"`）：轉發呼叫者 Bearer token 至 StoreService `GET /v1/stores/me` 驗證商品所屬商店的 Owner 身分（`CatalogAuthorization.LoadOwnedCatalogAsync`）。**`QuotaServiceClient`**（named `"quota"`）：轉發呼叫者 Bearer token（JWT `sub` = 租戶）扣量與商品數計數。
- **`CatalogSlugValidator`**：slug 格式驗證；商品 slug 於同一商店內唯一、分類 slug 全域唯一。
- **`AuditLogPublisher`** + `OutboxRelayService`：經 Outbox 發 `AuditLogRequestedEvent`。

## QuotaService（`src/QuotaService/`）

REST API，租戶（創作者）資源配額計量與扣量。Controller（`Charges` / `Products` / `Usage`）僅轉接，業務在 `Services/Quotas/` 的 `IQuotaManager`。MVP 不分方案，固定額度取自 `Quota` 設定（帳號總量 / 單檔 / 單商品總量 / 上架商品數）；`tenant_id` 一律由 JWT `sub` 推導，request 不接受外部指定。

- **扣量** `POST /v1/charges`：由功能 API（CatalogService）於資產 confirm 時呼叫，`ChargeId`（慣例 = FileId）為冪等鍵——同一交易內先 `ON CONFLICT DO NOTHING` 插入 `committed` 扣量紀錄（`reservation` 表），再以 **DB 原子條件式 UPDATE**（`WHERE used + reserved + @S <= quota`，以影響行數判斷成敗）扣 `tenant_usage.used`，天然防並發超額。超總量回 409；超單檔 / 單商品總量上限回 422。`tenant_usage` 於首次 charge 時 lazy upsert（`quota` 快照當下設定值）。
- **商品數計數** `POST /v1/products/count`：CatalogService 上 / 下架時 ±1，原子檢查 `MaxPublishedProducts`（僅計 `Published`，超限 409）。
- **用量查詢**：`GET /v1/usage/me`（租戶後台顯示）、`GET /v1/usage/{tenantId}`（Admin）。
- **背景**：`ReconciliationService` 每日以 StorageService `GET /v1/files/usage`（僅計 Ready 且 referenced 檔案）對帳校正 `used`（含資產刪除後的用量釋放，不即時退款）；`ReservationSweeperService` 收斂舊制預扣（`reserved`）殘量，新流程不再產生預扣。
- 純 REST API 服務，無事件訂閱；`StorageServiceClient` 供對帳查詢。

## PaymentService（`src/PaymentService/`）

REST API，Stripe Checkout 金流整合與 Stripe Connect 分帳。Controller（`Payments` / `ConnectAccounts` / `Webhook`）僅轉接，業務在 `Services/Payments/` 的 `IPaymentManager` / `StripeWebhookHandler` 與 `Services/Connect/` 的 `IConnectAccountService`。

- **建立 Checkout Session**：`POST /v1/payments/checkout-session` 掛 `"InternalService"` policy，僅限 OrderService 以 service token 呼叫（買家不直接接觸本服務）。以 `OrderId` 建 Stripe Checkout Session（`payment_id` / `order_id` 放 Session metadata）；同訂單已有未過期 `Pending` 付款時直接重用既有 Session（含並發 unique violation 時重用勝者），避免重複建立。`GET /v1/payments/{id}`（Admin）查詢付款。
- **Stripe Connect 分帳（destination charge）**：request 帶 `StoreId`，查本地 `ConnectedAccount`（一店一 Stripe Express 帳戶；無帳戶或 `ChargesEnabled=false` 回 422，為上架閘門的最後防線）。Session 帶 `PaymentIntentData.TransferData.Destination` 與 `ApplicationFeeAmount`（`Stripe:PlatformFeePercent` 百分比抽成，四捨五入），款項自動入創作者帳戶、平台留抽成，Stripe 手續費從平台抽成中吸收。`Payment` 快照 `StoreId` / `DestinationAccountId` / `ApplicationFeeAmount`。
- **Connect onboarding**：`POST /v1/connect/accounts/{storeId}/onboarding-link`（Owner，經 `StoreServiceClient` 轉發 Bearer token 驗證）建立（或重用，並發 unique violation 重用勝者）Express 帳戶（僅請求 `transfers` capability）並簽發 Account Link；`GET /v1/connect/accounts/{storeId}`（Owner，`?refresh=true` 向 Stripe 取即時狀態回寫，供 onboarding 導回頁不等 webhook）；`GET /v1/connect/accounts/{storeId}/status`（匿名，僅布林旗標，供 CatalogService 上架閘門）。`account.updated`（Connect webhook 端點 `POST /v1/webhook/stripe/connect`，簽章密鑰 `Stripe:ConnectWebhookSecret` 與平台端點分開）同步 `DetailsSubmitted` / `ChargesEnabled` / `PayoutsEnabled` 旗標。
- **付款狀態**：`Payment` `Pending → Succeeded` / `Failed` / `Expired`；每次轉移寫一筆 `PaymentTransaction`（`Created` / `Success` / `Fail` / `Expired`，留 Stripe raw payload）。
- **Webhook 兩段式處理**：`POST /v1/webhook/stripe` 驗簽後僅將原始事件落地 `ProviderEvent`（以 Stripe event id 去重）即回 200，避免處理中掛掉遺失 webhook；`StripeWebhookProcessorService`（`IHostedService`）排程處理未完成事件——`checkout.session.completed` / `async_payment_succeeded` → `Succeeded` 並經 Outbox 發 `PaymentSucceededEvent`；`async_payment_failed` / `payment_intent.payment_failed` → `Failed`；`checkout.session.expired` → 僅 `Pending` 轉 `Expired`。
- **設定**：`Stripe`（`SecretKey` / `PublishableKey` / `WebhookSecret` / `ConnectWebhookSecret` / `PlatformFeePercent` / `SuccessUrl` / `CancelUrl` / `ConnectRefreshUrl` / `ConnectReturnUrl`，成功 / 取消頁導回前端 checkout result 頁，Connect 導回 URL 指向 workspace-web `/payouts`）、`Services:StoreService`（Owner 驗證）。
- **`AuditLogPublisher`** + `OutboxRelayService`：經 Outbox 發 `AuditLogRequestedEvent`（`payment.checkout.created` 等）。

## OrderService（`src/OrderService/`）

REST API，管理商品訂單（`Order`）、訂單項目（`OrderItem`）與狀態變更歷程（`OrderStatusHistory`，append-only）。Controller（`Orders`）僅轉接，業務在 `Services/Orders/` 的 `IOrderManager`。消費者免註冊，憑 Email 即可下單；登入則由 JWT `sub` 帶入 `BuyerUserId`。

- **訂單狀態**：`Pending → Paid → Completed`，另有 `Cancelled` / `Refunded`。`OrderStatus` 索引化以利過濾。數位商品付款成功即一次轉移為 `Completed` 並填 `CompletedAt`（`Paid` 保留供未來人工審核流程）。每次狀態轉移寫一筆 `OrderStatusHistory`（`OldStatus` 可為 null 表建立）。
- **伺服器端核價**：`CreateOrderRequest.Items` 只帶 `CatalogId`（數量固定 1、商品不得重複）——名稱 / 單價 / 版本 / 幣別一律由 **`CatalogServiceClient`**（named `"catalog"`）匿名呼叫 CatalogService `GET /v1/catalogs/{id}` 取得快照（未上架 / 不存在回 404 → 422），並驗證商品屬於 `StoreId`、各項幣別一致；不信任 client 傳入價格。
- **賣方歸屬**：`Order.StoreId`（索引化）標記訂單所屬商店，一張訂單對應單一商店，於 `CreateOrderRequest` 帶入。三種列表視角：買家 `GET /v1/orders/mine`（以登入 `sub` 限縮）、賣家 `GET /v1/orders/store/{storeId}`（`ListByStoreAsync` 先以 `StoreServiceClient` 驗證 Owner 再限縮）、Admin `GET /v1/orders`（`ListOrdersRequest.StoreId` / 買家 / 狀態任意過濾）。
- **訪客訂單回填**：`UserRegisteredConsumer` 消費 `UserRegisteredEvent`，以信箱回填訪客訂單（`BuyerUserId` 為 null）的 `BuyerUserId`（冪等 UPDATE），註冊後即可在會員中心看到購買紀錄。
- **購買驗證與取消**：`GET /v1/orders/purchased/{catalogId}` 查登入使用者是否已有該商品的完成訂單（供 CatalogService 評論 / 買家下載授權）；`POST /v1/orders/{id}/cancel` 取消未付款訂單（具名買家僅本人，匿名訂單憑訂單 ID）。
- **`StoreServiceClient`**（named `"store"` HttpClient，BaseUrl 由 `ServiceOptions` 設定）：轉發呼叫者 Bearer token 至 StoreService `GET /v1/stores/me`，驗證賣家視角查詢者為該商店 Owner。
- **訂單編號**：`OrderNumberGenerator` 產生人類可讀且全域唯一的 `OrderNumber`（格式 `OJ-yyyyMMdd-XXXXXXXX`）。
- **金流整合**：結帳單一入口 `POST /v1/orders`——訂單先建立（`Pending`），OrderService 隨即以 **`PaymentServiceClient`**（named `"payment"` HttpClient，帶 `ServiceTokenClient` 取得的 service token）呼叫 PaymentService 以 `OrderId` 建立 Stripe Checkout Session（該端點掛 `"InternalService"` policy 僅限內部服務呼叫，買家 `UserId` 與賣方 `StoreId`——分帳目的地查找依據——由 request body 帶入），付款頁 URL 以 `OrderResponse.CheckoutUrl` 隨建單回應交給前端導向（前端不直接呼叫 PaymentService）；Session 建立失敗訂單保留 `Pending`。**免費訂單**（`TotalAmount == 0`，Stripe 不接受 0 元 Checkout）不呼叫 PaymentService，建單後直接履約完成（與付款成功共用 `FulfillAsync`：轉 `Completed`、發 `OrderCompletedEvent` 與完成信），`CheckoutUrl` 為 null，前端據此直接導向結帳成功頁。付款成功後 `PaymentSucceededConsumer` 消費 `PaymentSucceededEvent`（`Shared/Events/`）→ `CompleteFromPaymentAsync` 履約完成訂單（以訂單既有狀態做冪等判斷，搭配 MassTransit 指數退避重試），並經 Outbox 發 `OrderCompletedEvent`（CatalogService 消費以累加銷量；下載授權由 CatalogService 於下載當下以購買紀錄即時驗證），同交易內另發 `EmailRequestedEvent`（模板 `order.completed`）寄訂單完成信給買家，信中下載頁連結由 `Order:DownloadUrlPattern`（`{storeSlug}` / `{orderId}` 佔位符，store slug 以 `StoreServiceClient.GetStoreAsync` 匿名查得）組出；商店資訊查詢失敗僅記 log 略過寄信，不阻擋履約。
- **`AuditLogPublisher`** + `OutboxRelayService`：經 Outbox 發 `AuditLogRequestedEvent`（`order.create` / `order.cancel` / `order.complete`）。

## NotificationService（`src/NotificationService/`）

REST API + RabbitMQ Consumer，管理通知任務（`NotificationRequest`）與 in-app 通知（`Notification`）。Controller（`Notifications` / `NotificationRequests`）僅轉接，業務在 `Services/<Feature>/` 的 `INotificationManager` / `INotificationRequestService`。

- **統一排程管線**：所有通知（即時與預定日期）皆為一筆 `NotificationRequest`（`ScheduledAt`，即時 = 建立當下；狀態 `Pending → Dispatched`，另有 `Cancelled` / `Failed`）。`NotificationDispatcherService`（`IHostedService`）掃描到期任務，整個 fan-out 包在單一交易並以 `FOR UPDATE SKIP LOCKED` 持鎖：失敗整體回滾、下一輪重試（`AttemptCount` 達上限轉 `Failed`）。
- **通知類型**：`catalog.published`（消費 `CatalogPublishedEvent`，僅 `IsFirstPublish` 建任務）、`store.announcement`（Owner 經 `POST /v1/notification-requests` 建立，可帶 `ScheduledAt` 預定發送，僅 `Pending` 可取消）。
- **fan-out 對象**：本地 `store_follower_ref` 參照表（微服務 Ref Table），由 `StoreFollowerChangedEvent` 同步、`UserRegisteredEvent` 回填訪客 `UserId`（信箱一律小寫）。已關聯帳號者建 in-app `Notification`（`(RequestId, RecipientEmail)` 唯一）；全部追蹤者經 Outbox 發 `EmailRequestedEvent`（模板 `notification.catalog_published` / `notification.store_announcement`）由 EmailService 寄信。訪客追蹤者只收 Email。
- **in-app API**：`GET /v1/notifications/mine`（分頁 + `unreadOnly`）、`GET /v1/notifications/mine/unread-count`（前端鈴鐺輪詢）、`POST /{id}/read`、`POST /read-all`，皆以 JWT `sub` 限縮。
- **`StoreServiceClient`**（named `"store"`）：Owner 驗證（轉發 Bearer token 至 `GET /v1/stores/me`）+ dispatch 時匿名查商店公開資訊（`GET /v1/stores/{id}`，取店名 / slug 渲染通知，不建 store ref 表）。
- **Consumer 冪等**：`CatalogPublishedConsumer` 以 `ProcessedEvent`（`OutboxMessageId` 唯一）+ `NotificationRequest.SourceEventId` 唯一索引去重；追蹤同步與 UserId 回填為天然冪等 upsert / delete / update。

## ContentService（`src/ContentService/`）

REST API，管理平台內容——法律文件（`LegalDocument`，服務條款 / 隱私權政策）、常見問題主題分類（`FaqCategory`）與常見問題（`FaqItem`）。Controller（`LegalDocuments` / `FaqCategories` / `Faqs`）僅轉接，業務在 `Services/<Feature>/` 的 `ILegalDocumentService` / `IFaqCategoryService` / `IFaqService`。純 REST API 服務，無事件訂閱；僅經 Outbox 發 `AuditLogRequestedEvent`（`OutboxRelayService` + `AuditLogPublisher`）。

- **法律文件**（原屬 Auth，已抽離）：每次修訂為一筆新 `LegalDocument`（同類型 `Version` 遞增，狀態 `Draft → Active → Inactive`，僅 Draft 可編輯與刪除（軟刪除，`IDeletedAt`）、**啟用過的版本永不刪除**供歷史比對；partial unique index 保證同類型僅一筆 Active，啟用時自動停用既有版本；版本號以 `IgnoreQueryFilters()` 含已刪草稿計算，避免撞 `(Type, Version)` 唯一索引）。`/v1/legal-documents`（Admin）——分頁列表 / 單筆 / 建草稿 / 改草稿 / activate / deactivate / DELETE（僅 Draft，非 Draft 回 409），另 `GET /v1/legal-documents/active?type=` 匿名公開（portal-web 條款頁 + Auth 註冊 / 登入 re-consent 同意流程撈取）。供 workspace-web 管理員「條款管理」後台。**同意紀錄（`UserLegalConsent`）留在 Auth，本服務不涉及。**
- **常見問題主題分類（FaqCategory）**：由平台維護、可 CRUD 的單層分類（`Name` / `Slug` 全域唯一 / `Description` / `SortOrder`，比照 CatalogService 商品分類但無 `ParentId`）。`/v1/faq-categories`——`GET`（列表）匿名公開（portal-web FAQ 頁建立主題分頁、workspace-web 分類下拉）、`GET {id}` / `POST` / `PATCH` / `DELETE` 僅 Admin（刪除前若仍被 `FaqItem` 引用回 409）。供 workspace-web 管理員「常見問題分類」後台。**取代原本寫死的 `FaqCategory` enum。**
- **常見問題（FAQ）**：`FaqItem`（`CategoryId` 參照 `FaqCategory`（FK，`OnDelete Restrict`）、`Question` / `Answer`、`SortOrder` 同分類排序、`IsPublished` 發布旗標；DTO 另帶 `CategoryName` / `CategorySlug`）。`/v1/faqs`（Admin）——分頁列表 / 單筆 / CRUD（含 DELETE，建立 / 更新時驗證 `CategoryId` 存在），另 `GET /v1/faqs/published?categoryId=` 匿名公開（portal-web FAQ 頁撈取已發布項目，依分類 `SortOrder` + 項目 `SortOrder` 排序）。供 workspace-web 管理員「常見問題」後台；取代 portal-web 原前端寫死的 FAQ 內容。
- **DB**：`open_jam_content`；開發 URL http://localhost:5181，Swagger `/swagger`。

## Bootstrap（`src/Bootstrap/`）

一次性 seed 工具，依序執行 `HydraClientSeeder`（註冊 Hydra OIDC client：Web 與 Service）、`EmailTemplateSeeder`（寫入郵件模板）、`UserSeeder`（建平台管理員，另可選 seed 假帳號）、`LegalDocumentSeeder`（seed 服務條款 / 隱私權政策初始啟用版本至 **ContentService** DB，該類型已有紀錄即略過）、`FaqSeeder`（seed 常見問題主題分類與初始問答至 ContentService DB，取自 portal-web 原前端 FAQ；分類以 slug 冪等 upsert，問答於 FAQ 表已有資料時略過）、`StoreSeeder`（可選 seed 假店家）、`CatalogCategorySeeder`（寫入平台分類）、`StoreFollowerRefSeeder`（回填 NotificationService 追蹤者參照表，重跑冪等）後結束。

掛載 6 個 DbContext：`AuthConnection` / `EmailConnection` / `CatalogConnection` / `StoreConnection` / `NotificationConnection` / `ContentConnection`（具名連線字串，非共用 `DefaultConnection`）。設定 key：`AdminUser:Email` / `:Password`（皆需有值才 seed 管理員）、`MockUsers:Enabled` / `MockStores:Enabled`（正式環境須為 `false`）、`HydraClients:Web` / `:Service`。Helm 由 `templates/bootstrap/job.yaml` 以環境變數注入上述設定。

## 環境設定

各服務 `appsettings.json` 已含 localhost 預設值，可直接使用或以 User Secrets 覆蓋。

```jsonc
// Auth / LogService / EmailService 共用結構
{
  "ConnectionStrings": { "DefaultConnection": "Host=localhost;Database=open_jam_<service>;Username=postgres;Password=postgres" },
  "RabbitMQ": { "Host": "localhost", "Username": "guest", "Password": "guest" }
}
// Auth 額外需要
{
  "Hydra": { "AdminUrl": "http://localhost:4445" },
  "App": { "BaseUrl": "https://localhost:7280" },
  // 取得啟用中法律文件供註冊 / 登入 re-consent 同意流程
  "Services": { "ContentService": { "BaseUrl": "http://localhost:5181" } }
}
// EmailService 額外需要（地端開發用 SMTP catcher；正式以 SendGrid API Key 替換）
{
  "Smtp": { "Host": "localhost", "Port": 1025, "SecureSocket": "Auto", "FromAddress": "noreply@openjam.co" },
  "SendGrid": { "ApiKey": "<prod-only，由 GCP Secret Manager 注入>" }
}
// 各 REST API 服務（StorageService / StoreService / CatalogService / QuotaService / PaymentService / OrderService / NotificationService / ContentService）額外需要（驗 Hydra 簽發的 JWT）
{
  "Hydra": { "Issuer": "http://localhost:4444/" }
}
// ContentService 僅需上述共用結構（ConnectionStrings / RabbitMQ / Hydra:Issuer），無額外設定；DB 為 open_jam_content
// StorageService 額外需要（地端本地檔案儲存；正式切 Provider: "Gcs"）
{
  "Storage": {
    "Provider": "Local",
    "SoftDeleteRetentionDays": 30,
    "PublicBaseUrl": "http://localhost:5171/v1/files/blob",  // public/* 匿名讀取網址前綴
    // GCS 雙 bucket（Provider: "Gcs" 時必填）：公開資產與私有付費檔分開
    "PublicBucket": "open-jam-public", "PrivateBucket": "open-jam-private",
    "Local": {
      "RootPath": "Files",                  // 檔案存放根目錄（相對工作目錄）
      "BaseUrl": "http://localhost:5171",   // 本服務對外網址，用於組合 blob URL
      "SigningKey": "<HMAC 簽章密鑰，正式以 Secret 覆蓋>"
    },
    "Gcs": { "CredentialsPath": "" }  // 留空走 ADC / Workload Identity
  }
}
// StoreService 額外需要（呼叫 StorageService 簽發頭像 / 橫幅上傳 URL）
{
  "Services": { "StorageService": { "BaseUrl": "http://localhost:5171" } }
}
// CatalogService 額外需要（簽發資產上傳 URL + Owner 驗證 + 配額扣量 + 購買驗證 + 付費商品上架閘門）
{
  "Storage": { "PublicBaseUrl": "http://localhost:5171/v1/files/blob" },  // 展示型資產公開讀取前綴
  "Services": {
    "StorageService": { "BaseUrl": "http://localhost:5171" },
    "StoreService": { "BaseUrl": "http://localhost:5172" },
    "QuotaService": { "BaseUrl": "http://localhost:5177" },
    "OrderService": { "BaseUrl": "http://localhost:5179" },
    "PaymentService": { "BaseUrl": "http://localhost:5178" }
  }
}
// QuotaService 額外需要（固定額度 + 每日對帳查 StorageService 用量）
{
  "Quota": {
    "MaxAccountStorageBytes": 53687091200,   // 帳號總量 50 GiB
    "MaxFileSizeBytes": 2147483648,          // 單檔 2 GiB
    "MaxProductTotalBytes": 10737418240,     // 單商品總量 10 GiB
    "MaxPublishedProducts": 100              // 上架商品數
  },
  "Services": { "StorageService": { "BaseUrl": "http://localhost:5171" } }
}
// PaymentService 額外需要（Stripe 金鑰、分帳與導回頁；正式以 Secret 覆蓋）
{
  "Stripe": {
    "SecretKey": "sk_test_...", "PublishableKey": "pk_test_...", "WebhookSecret": "whsec_...",
    "ConnectWebhookSecret": "whsec_...",   // Connect webhook 端點（account.updated）簽章密鑰
    "PlatformFeePercent": 10,              // 平台抽成百分比（destination charge application fee）
    "SuccessUrl": "http://localhost:5174/checkout/success?session_id={CHECKOUT_SESSION_ID}",
    "CancelUrl": "http://localhost:5174/checkout/cancel",
    // Stripe Connect onboarding 導回 workspace-web 收款設定頁
    "ConnectRefreshUrl": "http://localhost:5175/payouts?refresh=1",
    "ConnectReturnUrl": "http://localhost:5175/payouts?return=1"
  },
  // Connect onboarding 端點驗證商店 Owner 身分
  "Services": { "StoreService": { "BaseUrl": "http://localhost:5172" } }
}
// OrderService 額外需要（賣家視角訂單查詢驗證商店 Owner 身分 + 結帳核價 + 建立 Checkout Session + 訂單完成信）
{
  "Services": {
    "StoreService": { "BaseUrl": "http://localhost:5172" },
    "PaymentService": { "BaseUrl": "http://localhost:5178" },
    "CatalogService": { "BaseUrl": "http://localhost:5176" }
  },
  "Order": {
    "DownloadUrlPattern": "https://{storeSlug}.openjam.co/orders/{orderId}"  // 開發環境覆蓋為 creator-web dev server
  },
  // 呼叫 PaymentService 內部端點用的 service token（Hydra client_credentials）
  "ServiceAuth": {
    "TokenUrl": "http://localhost:4444/oauth2/token",
    "ClientId": "open-jam-service",
    "ClientSecret": "<Bootstrap seed 的 Service client secret，正式以 Secret 覆蓋>"
  }
}
// NotificationService 額外需要（Owner 驗證 + dispatch 時查商店公開資訊 + 信件商品連結）
{
  "Services": { "StoreService": { "BaseUrl": "http://localhost:5172" } },
  "Notification": {
    "CatalogUrlPattern": "https://{storeSlug}.openjam.co/product/{catalogId}",  // 開發環境覆蓋為 creator-web dev server
    "DispatchMaxAttempts": 5,
    "DispatchBatchSize": 500
  }
}
```
