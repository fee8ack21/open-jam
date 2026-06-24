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
| CatalogService | 商品 / 版本 / 分類 / 標籤 REST API | http://localhost:5176，Swagger: `/swagger` |
| OrderService | 訂單 / 結帳 / 狀態歷程 REST API | http://localhost:5179，Swagger: `/swagger` |
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
| market-web | 市集首頁 / landing | OIDC（oidc-client-ts） |
| creator-web | 創作者店面：商品列表 / 詳情 / 結帳 | 無（消費者免註冊） |
| workspace-web | 創作者後台：開店、商品 / 訂單管理、上架、設定 | OIDC（oidc-client-ts） |

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

**`AppException` 子類** — `NotFoundException(404)` / `ForbiddenException(403)` / `ConflictException(409)` / `ValidationException(422)` / `UnauthorizedException(401)`。

**`ExceptionMiddleware`** — 僅用於 **REST API 服務**。在 `Program.cs` 以 `app.UseExceptionMiddleware()` 掛載，需排在所有其他 middleware 之前。MVC 服務不使用此 middleware，改以 `app.UseExceptionHandler(...)` 處理。

**Events**（`Shared/Events/`）— `EmailRequestedEvent`、`AuditLogRequestedEvent`、`FileReadyEvent`：各服務在業務 transaction 內寫入 Outbox，由 `OutboxRelayService` 排程搬入 RabbitMQ。

## Outbox 模式

業務 transaction 內寫入 `OutboxMessage` 表，由各服務的 `OutboxRelayService`（`IHostedService`）定期掃描並 publish 到 RabbitMQ。冪等鍵為 `OutboxMessageId`，Consumer 端需去重。

## Auth 服務（`src/Auth/`）

ASP.NET Core 8 MVC，整合 Ory Hydra（OIDC）。

**核心服務**：
- `UserService` — 註冊、登入、密碼重設、email 驗證
- `HydraService` — 包裝 Hydra Admin API（accept/reject login/consent）
- `Argon2idHasher` — 密碼雜湊
- `OutboxRelayService` — Outbox → RabbitMQ

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
- **背景**：`OrphanCleanupService` 清理保留期過後的孤兒檔案（硬刪除）。

## StoreService（`src/StoreService/`）

REST API，管理開店申請、店家與追蹤。Controller（`StoreApplications` / `Stores` / `StoreFollowers`）僅轉接，業務在 `Services/<Feature>/` 的 `IStoreApplicationService` / `IStoreManager` / `IStoreFollowerService`。

- **`StorageServiceClient`**：以 `HttpClient`（named `"storage"`，BaseUrl 由 `ServiceOptions` 設定）呼叫 StorageService 簽發 Avatar / Banner 上傳 URL。
- **`StoreSlugValidator` / `StoreAuthorization`**：子網域 slug 驗證與店家成員授權。
- **`AuditLogPublisher`** + `OutboxRelayService`：經 Outbox 發 `AuditLogRequestedEvent`。

## CatalogService（`src/CatalogService/`）

REST API，管理數位商品（`Catalog`）、版本（`CatalogVersion`）、平台分類（`CatalogCategory`，以 `ParentId` 自我參照多層子分類）與標籤（`CatalogTag`，名稱強制小寫、維護 `UsageCount`）。Controller（`Catalogs` / `CatalogVersions` / `CatalogCategories` / `CatalogTags`）僅轉接，業務在 `Services/<Feature>/` 的 `ICatalogManager` / `ICatalogVersionService` / `ICatalogCategoryService` / `ICatalogTagService`。

- **商品狀態**：`Draft → Published`（需先有版本）↔ `Archived`；`Suspended` 由 Admin 停權 / 解除。`CatalogStatus` 索引化以利瀏覽過濾。
- **資產分兩類**：展示型 `CatalogAsset`（縮圖 / 截圖 / 預覽影音，公開讀取，`public/` 前綴）；版本可下載檔 `CatalogVersionAsset`（買家實際取得內容，私有，須授權簽發下載 URL）。Asset `Id` 與 StorageService 簽發的 `FileId` 同值。
- **`StorageServiceClient`**（named `"storage"`）：簽發上傳 / 下載 URL。**`StoreServiceClient`**（named `"store"`）：轉發呼叫者 Bearer token 至 StoreService `GET /v1/stores/me` 驗證商品所屬商店的 Owner 身分（`CatalogAuthorization.LoadOwnedCatalogAsync`）。
- **`CatalogSlugValidator`**：slug 格式驗證；商品 slug 於同一商店內唯一、分類 slug 全域唯一。
- **`AuditLogPublisher`** + `OutboxRelayService`：經 Outbox 發 `AuditLogRequestedEvent`。

## OrderService（`src/OrderService/`）

REST API，管理商品訂單（`Order`）、訂單項目（`OrderItem`）與狀態變更歷程（`OrderStatusHistory`，append-only）。Controller（`Orders`）僅轉接，業務在 `Services/Orders/` 的 `IOrderManager`。消費者免註冊，憑 Email 即可下單；登入則由 JWT `sub` 帶入 `BuyerUserId`。

- **訂單狀態**：`Pending → Paid → Completed`，另有 `Cancelled` / `Refunded`。`OrderStatus` 索引化以利過濾。數位商品付款成功即一次轉移為 `Completed` 並填 `CompletedAt`（`Paid` 保留供未來人工審核流程）。每次狀態轉移寫一筆 `OrderStatusHistory`（`OldStatus` 可為 null 表建立）。
- **訂單編號**：`OrderNumberGenerator` 產生人類可讀且全域唯一的 `OrderNumber`（格式 `OJ-yyyyMMdd-XXXXXXXX`）。
- **金流整合**：訂單先建立（`Pending`），PaymentService 以 `OrderId` 建立 Stripe Checkout Session；付款成功後 `PaymentSucceededConsumer` 消費 `PaymentSucceededEvent`（`Shared/Events/`）→ `CompleteFromPaymentAsync` 履約完成訂單（以訂單既有狀態做冪等判斷，搭配 MassTransit 指數退避重試）。履約（發放下載權限）由 CatalogService 另行消費同事件，OrderService 僅追蹤訂單狀態。
- **`AuditLogPublisher`** + `OutboxRelayService`：經 Outbox 發 `AuditLogRequestedEvent`（`order.create` / `order.cancel` / `order.complete`）。

## Bootstrap（`src/Bootstrap/`）

一次性 seed 工具，執行 `HydraClientSeeder`（註冊 Hydra OIDC client）與 `EmailTemplateSeeder`（寫入郵件模板）後結束。

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
  "App": { "BaseUrl": "https://localhost:7280" }
}
// EmailService 額外需要（地端開發用 SMTP catcher；正式以 SendGrid API Key 替換）
{
  "Smtp": { "Host": "localhost", "Port": 1025, "SecureSocket": "Auto", "FromAddress": "noreply@openjam.co" },
  "SendGrid": { "ApiKey": "<prod-only，由 GCP Secret Manager 注入>" }
}
// StorageService / StoreService / OrderService 額外需要（REST API，驗 Hydra 簽發的 JWT）
{
  "Hydra": { "Issuer": "http://localhost:4444/" }
}
// StorageService 額外需要（地端本地檔案儲存；正式切 Provider: "Gcs"）
{
  "Storage": {
    "Provider": "Local",
    "Bucket": "open-jam", "SoftDeleteRetentionDays": 30,
    "PublicBaseUrl": "http://localhost:5171/v1/files/blob",  // public/* 匿名讀取網址前綴
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
// CatalogService 額外需要（簽發資產上傳 URL + 驗證商店 Owner 身分）
{
  "Storage": { "PublicBaseUrl": "http://localhost:5171/v1/files/blob" },  // 展示型資產公開讀取前綴
  "Services": {
    "StorageService": { "BaseUrl": "http://localhost:5171" },
    "StoreService": { "BaseUrl": "http://localhost:5172" }
  }
}
```
