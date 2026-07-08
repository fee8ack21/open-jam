# Open Jam

Open Jam 是面向創作者的台灣數位商品平台，產品型態參考 Gumroad。

創作者可在子網域 `<creator>.openjam.co` 開店、上架商品、管理訂單；消費者無需註冊，憑信箱即可購買，付款走 Stripe Checkout。

## 技術棧

| 層 | 技術 |
|----|------|
| 後端 | .NET 8、ASP.NET Core、EF Core、MassTransit RabbitMQ |
| 前端 | Vue 3、Vite 5、Pinia、Vue Router 4、Naive UI、vue-i18n、pnpm |
| 資料庫 | PostgreSQL、snake_case naming convention |
| 訊息 | RabbitMQ、Outbox pattern |
| 認證 | Ory Hydra、OIDC、JWT Bearer、服務間 client_credentials |
| 金流 | Stripe Checkout + Webhook |
| 儲存 | StorageService 抽象，本地 Local / 雲端 GCS 雙 bucket |
| 文件 | VitePress |

## 專案結構

```text
src/
  Auth/                 # MVC 登入、註冊、忘記密碼；整合 Ory Hydra；使用者列表 API
  LogService/           # Audit Log REST API 與 AuditLogRequestedEvent consumer
  StorageService/       # 檔案上傳 / 下載 URL 簽發與檔案生命週期、用量統計
  StoreService/         # 開店申請、店家、追蹤 REST API
  CatalogService/       # 商品、版本、分類、標籤、評論、收藏 REST API
  QuotaService/         # 租戶資源配額計量（儲存空間 / 上架商品數）
  PaymentService/       # Stripe Checkout 金流與 Webhook
  OrderService/         # 訂單、結帳入口、狀態歷程 REST API
  NotificationService/  # 追蹤者上架 / 公告通知（Email + in-app）
  EmailService/         # RabbitMQ Worker，渲染模板並寄送 Email
  Bootstrap/            # 一次性 seed：Hydra client、Email template、分類、管理員
  Shared/               # 共用 DbContext、Auth、Exception、Events、Outbox model
apps/
  portal-web/           # 市集首頁 / landing SPA，OIDC
  creator-web/          # 創作者店面 SPA，消費者免註冊，含結帳
  workspace-web/        # 創作者後台 + 管理員後台 SPA，OIDC
docs/                   # VitePress 文件站
infra/docker/           # Docker Compose 與本地基礎設施設定
infra/helm/             # GKE 部署 Helm chart（open-jam 應用 + infra Ingress/憑證）
```

## 前置需求

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/)
- [pnpm](https://pnpm.io/)
- Docker / Docker Compose
- PostgreSQL、RabbitMQ、Ory Hydra。本地可直接使用 `infra/docker/docker-compose.yaml` 啟動。

## Docker Compose

Docker Compose 設定位於 `infra/docker/docker-compose.yaml`，包含 PostgreSQL、Redis、RabbitMQ、Hydra、Mailpit，以及目前已納入 compose 的應用服務。StorageService 地端採本地檔案系統儲存（無需額外物件儲存容器）。

```bash
cd infra/docker
cp .env.example .env
docker compose up -d
docker compose --profile seed run --rm bootstrap
```

### Compose Port

| 服務 | Port | 說明 |
|------|------|------|
| auth | 5169 | 登入 / 註冊 MVC |
| log-service | 5170 | Audit Log API / Swagger |
| storage-service | 5171 | 檔案簽發與 blob API |
| store-service | 5172 | 開店、店家、追蹤 API |
| creator-web | 5173 | 創作者店面 |
| portal-web | 5174 | 市集首頁 |
| workspace-web | 5175 | 創作者後台 |
| docs | 5176 | VitePress 文件站 |
| quota-service | 5177 | 資源配額 API |
| payment-service | 5178 | Stripe 金流 API |
| order-service | 5179 | 訂單 API |
| notification-service | 5180 | 通知 API |
| postgres | 5432 | PostgreSQL |
| redis | 6379 | Redis |
| rabbitmq | 5672 / 15672 | RabbitMQ / Management UI |
| hydra | 4444 / 4445 | Ory Hydra Public / Admin |
| mailpit | 1025 / 8025 | SMTP / Web UI |

> `CatalogService` 目前存在於專案中，但尚未加入 `infra/docker/docker-compose.yaml`（前端容器以 `CATALOG_API_URL` 指向宿主機的開發伺服器）。

## 後端服務

從各 `src/<Service>/` 目錄執行：

```bash
dotnet run
dotnet build
dotnet publish -c Release
dotnet ef migrations add <Name>
dotnet ef database update
```

| 服務 | 類型 | 開發 URL |
|------|------|----------|
| Auth | MVC 登入 / 註冊 / 忘記密碼 | http://localhost:5169 |
| LogService | Audit Log REST API | http://localhost:5170，Swagger: `/swagger` |
| StorageService | 檔案上傳 / 下載 URL 簽發 REST API | http://localhost:5171，Swagger: `/swagger` |
| StoreService | 開店申請 / 店家 / 追蹤 REST API | http://localhost:5172，Swagger: `/swagger` |
| CatalogService | 商品 / 版本 / 分類 / 標籤 / 評論 / 收藏 REST API | http://localhost:5176，Swagger: `/swagger` |
| QuotaService | 租戶資源配額計量 REST API | http://localhost:5177，Swagger: `/swagger` |
| PaymentService | Stripe Checkout 金流 REST API | http://localhost:5178，Swagger: `/swagger` |
| OrderService | 訂單 / 結帳 / 狀態歷程 REST API | http://localhost:5179，Swagger: `/swagger` |
| NotificationService | 通知（上架 / 公告，Email + in-app）REST API | http://localhost:5180，Swagger: `/swagger` |
| EmailService | RabbitMQ Worker | 無 HTTP port |
| Bootstrap | 一次性 seed 工具 | 無 HTTP port |

Docker image 可從 `src/` 建置，例如：

```bash
docker build -f Auth/Dockerfile -t open-jam-auth .
```

## 前端應用

從各 `apps/<app>/` 目錄執行：

```bash
pnpm dev
pnpm build
pnpm preview
pnpm type-check
```

| App | 用途 | 認證 |
|-----|------|------|
| portal-web | 市集首頁 / landing、通知鈴鐺 | OIDC (`oidc-client-ts`) |
| creator-web | 創作者店面、商品列表 / 詳情、結帳（Stripe Checkout 導轉） | 無，消費者免註冊 |
| workspace-web | 創作者後台（開店、商品 / 訂單 / 公告管理、購買紀錄與下載、設定）+ 管理員後台（會員 / 商店 / 商品 / 訂單 / 資源用量 / 稽核） | OIDC (`oidc-client-ts`) |

三個 app 皆為 TypeScript + vue-i18n；API client 由 `swagger-typescript-api` 從後端 OpenAPI 自動產生（`pnpm gen:api`），不手寫 API 呼叫與型別。

## 文件站

從 `docs/` 目錄執行：

```bash
pnpm dev
pnpm build
pnpm preview
```

## 架構慣例

- Controller 只處理 HTTP 路由、model binding 與狀態碼，業務邏輯放在 `Services/<Feature>/`。
- Request / Response / DTO 依功能集中於 `Models/<Feature>Models.cs`；輸入驗證用 FluentValidation（422 + 欄位 `errors`），Entity ↔ DTO 轉換用 AutoMapper。
- 分頁一律使用 `offset` / `limit`。
- 時間欄位使用 `DateTimeOffset`，命名以 `At` 結尾。
- 資料庫欄位使用 snake_case，由 `BaseDbContext` 自動套用 naming convention；軟刪除與 Audit 欄位亦由其自動處理。
- Entity、DTO、Controller Action 皆需 XML `<summary>`；DTO 屬性另加 `<example>`。
- REST API 服務以 `ExceptionMiddleware` 將 `AppException` 轉為 RFC 9457 Problem Details。
- Auth 是 MVC 服務，使用 ASP.NET Core `UseExceptionHandler`，不掛載 `ExceptionMiddleware`。
- 服務間資源以本地 ref table 同步；事件透過 Outbox 寫入後由 background relay publish 到 RabbitMQ。
- 內部服務間 API（如 PaymentService checkout-session）以 Hydra `client_credentials` service token + `"InternalService"` policy 保護。

更多細節見 [開發慣例](docs/develop.md)。

## 環境設定

各服務 `appsettings.json` 已含 localhost 預設值，可直接執行或用 User Secrets / 環境變數覆蓋。

常見設定群組：

| 群組 | 用途 |
|------|------|
| `ConnectionStrings:DefaultConnection` | PostgreSQL 連線 |
| `RabbitMQ` | Outbox relay / consumer 連線 |
| `Hydra` | Auth 使用 Admin API；REST API 使用 Issuer / Metadata |
| `Smtp` / `SendGrid` | EmailService 寄信 |
| `Storage` | StorageService provider、bucket、public URL、local / GCS 設定 |
| `Services` | 各服務呼叫其他內部服務的 BaseUrl |
| `ServiceAuth` | 服務間 client_credentials（TokenUrl / ClientId / ClientSecret） |
| `Stripe` | PaymentService 金鑰、Webhook secret、成功 / 取消導回頁 |
| `Quota` | QuotaService 固定額度（總量 / 單檔 / 單商品 / 上架商品數） |
| `Notification` | NotificationService 商品連結樣板、dispatch 批次與重試 |

本地開發預設資料庫命名為 `open_jam_<service>`，例如 `open_jam_auth`、`open_jam_store`、`open_jam_catalog`、`open_jam_order`、`open_jam_payment`、`open_jam_quota`。
