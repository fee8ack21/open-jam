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

> 前端 API client 一律以 `swagger-typescript-api` 從後端 OpenAPI 自動產生（見 `workspace-web/openapi/`），不手寫；搭配 token 注入 wrapper。

### 文件站（從 `docs/` 執行）

```bash
pnpm dev      # http://localhost:5173
pnpm build
pnpm preview
```

## 架構慣例

- **軟體三層 + Service DI**：Controller 只做 HTTP（路由、model binding、狀態碼），業務邏輯一律下放至 `Services/<Feature>/` 的 `I<Feature>Service` + 實作，於 `Program.cs` 以 `AddScoped` 註冊（詳見 `docs/develop.md`）
- **API Model**：Request / Response / DTO 依功能集中於 `Models/<Feature>Models.cs`（一功能一檔，非一型別一檔），namespace `<Service>.Models`
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

- **儲存後端**：`IStorageProvider` 抽象，依 `Storage:Provider` 設定切換地端 `MinioStorageProvider`（S3 相容）或雲端 `GcsStorageProvider`（GCS，signed URL 用服務帳戶金鑰或 GKE Workload Identity 的 IAM SignBlob）。
- **流程**：`FilesController` 簽發 presigned upload URL → 客戶端直傳後端儲存 → `InternalController` 收 storage 事件（webhook）由 `StorageEventService` 確認 → `FileProcessingService` 處理 → publish `FileReadyEvent`。
- **背景**：`OrphanCleanupService` 清理保留期過後的孤兒檔案（硬刪除）。

## StoreService（`src/StoreService/`）

REST API，管理開店申請、店家與追蹤。Controller（`StoreApplications` / `Stores` / `StoreFollowers`）僅轉接，業務在 `Services/<Feature>/` 的 `IStoreApplicationService` / `IStoreManager` / `IStoreFollowerService`。

- **`StorageServiceClient`**：以 `HttpClient`（named `"storage"`，BaseUrl 由 `ServiceOptions` 設定）呼叫 StorageService 簽發 Avatar / Banner 上傳 URL。
- **`StoreSlugValidator` / `StoreAuthorization`**：子網域 slug 驗證與店家成員授權。
- **`AuditLogPublisher`** + `OutboxRelayService`：經 Outbox 發 `AuditLogRequestedEvent`。

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
// StorageService / StoreService 額外需要（REST API，驗 Hydra 簽發的 JWT）
{
  "Hydra": { "Issuer": "http://localhost:4444/" }
}
// StorageService 額外需要（地端 MinIO；正式切 Provider: "Gcs"）
{
  "Storage": {
    "Provider": "Minio", "Endpoint": "localhost:9000",
    "AccessKey": "minioadmin", "SecretKey": "minioadmin",
    "Bucket": "open-jam", "SoftDeleteRetentionDays": 30,
    "Gcs": { "CredentialsPath": "" }  // 留空走 ADC / Workload Identity
  }
}
// StoreService 額外需要（呼叫 StorageService 簽發頭像 / 橫幅上傳 URL）
{
  "Services": { "StorageService": { "BaseUrl": "http://localhost:5171" } }
}
```
