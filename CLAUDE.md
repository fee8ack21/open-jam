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
| EmailService | RabbitMQ Worker（無 HTTP port） | — |
| Bootstrap | Seed，一次性執行後結束 | — |

Docker（從 `src/` 執行）：

```bash
docker build -f Auth/Dockerfile -t open-jam-auth .
```

### 前端應用（從各 `apps/<app>/` 執行）

```bash
pnpm dev    # Vite dev server（預設 5173，多個並跑依序遞增）
pnpm build
```

### 文件站（從 `docs/` 執行）

```bash
pnpm dev      # http://localhost:5173
pnpm build
pnpm preview
```

## 架構慣例

- **CQRS / Mediator + 軟體三層 + Service DI**
- **分頁**：一律 `offset` / `limit`（非 `page` / `pageSize`）
- **時間欄位**：型別 `DateTimeOffset`，命名以 `At` 結尾
- **資料庫命名**：snake_case；C# Entity 保持 PascalCase，由 `BaseDbContext` 自動套用 EF Core naming convention，不需手動 `[Column]`
- **XML 文件**：Entity、DTO、Controller Action 皆須 `<summary>`；DTO 屬性另加 `<example>`（影響 Swagger 完整度，強制要求）
- **錯誤處理**：業務層拋 `AppException` 子類。REST API 服務（LogService 等）以 `ExceptionMiddleware` 轉為 RFC 9457 Problem Details；MVC 服務（Auth）以 ASP.NET Core 內建 `UseExceptionHandler` 導頁至 Error Page，不掛載 `ExceptionMiddleware`
- **微服務 Ref Table**：本地保留其他服務資源的參照表，資源變更時發 Event 同步

## Shared 類別庫（`src/Shared/`）

所有服務共用，不含 HTTP 路由邏輯。

**`BaseDbContext`** — 覆寫 `SaveChangesAsync`，自動填入 Audit 欄位（`ICreatedAt/By`、`IUpdatedAt/By`）。軟刪除（`IDeletedAt/By`）由 Entity 自身的 `SoftDelete()` 方法觸發，DbContext 不自動處理。

**`ICurrentUserAccessor`** — 取得目前使用者 `Guid? UserId`。HTTP 服務注入 `HttpContextUserAccessor`（從 JWT Claims 讀 `sub`）；背景 Worker 注入 `WorkerCurrentUserAccessor`（固定系統帳號）。

**`AppException` 子類** — `NotFoundException(404)` / `ForbiddenException(403)` / `ConflictException(409)` / `ValidationException(422)` / `UnauthorizedException(401)`。

**`ExceptionMiddleware`** — 僅用於 **REST API 服務**。在 `Program.cs` 以 `app.UseExceptionMiddleware()` 掛載，需排在所有其他 middleware 之前。MVC 服務不使用此 middleware，改以 `app.UseExceptionHandler(...)` 處理。

**Events** — `EmailRequestedEvent`、`AuditLogRequestedEvent`：各服務在業務 transaction 內寫入 Outbox，由 `OutboxRelayService` 排程搬入 RabbitMQ。

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
  "Smtp": { "Host": "localhost", "Port": 1025, "UseSsl": false, "FromAddress": "noreply@openjam.co" },
  "SendGrid": { "ApiKey": "<prod-only，由 GCP Secret Manager 注入>" }
}
```
