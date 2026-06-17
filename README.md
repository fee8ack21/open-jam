# Open Jam

Open Jam 是面向創作者的台灣數位商品平台，產品型態參考 Gumroad。

創作者可在子網域 `<creator>.openjam.co` 開店、上架商品、管理訂單；消費者無需註冊，憑信箱即可購買。

## 技術棧

| 層 | 技術 |
|----|------|
| 後端 | .NET 8、ASP.NET Core、EF Core、MassTransit RabbitMQ |
| 前端 | Vue 3、Vite 5、Pinia、Vue Router 4、Naive UI、pnpm |
| 資料庫 | PostgreSQL、snake_case naming convention |
| 訊息 | RabbitMQ、Outbox pattern |
| 認證 | Ory Hydra、OIDC、JWT Bearer |
| 儲存 | StorageService 抽象，本地 Local / 雲端 GCS |
| 文件 | VitePress |

## 專案結構

```text
src/
  Auth/             # MVC 登入、註冊、忘記密碼；整合 Ory Hydra
  LogService/       # Audit Log REST API 與 AuditLogRequestedEvent consumer
  StorageService/   # 檔案上傳 / 下載 URL 簽發與檔案生命週期
  StoreService/     # 開店申請、店家、追蹤 REST API
  CatalogService/   # 商品、版本、分類、標籤 REST API
  EmailService/     # RabbitMQ Worker，渲染模板並寄送 Email
  Bootstrap/        # 一次性 seed：Hydra client、Email template
  Shared/           # 共用 DbContext、Auth、Exception、Events、Outbox model
apps/
  market-web/       # 市集首頁 / landing SPA，OIDC
  creator-web/      # 創作者店面 SPA，消費者免註冊
  workspace-web/    # 創作者後台 SPA，OIDC
docs/               # VitePress 文件站
infra/docker/       # Docker Compose 與本地基礎設施設定
```

## 前置需求

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/)
- [pnpm](https://pnpm.io/)
- Docker / Docker Compose
- PostgreSQL、RabbitMQ、Ory Hydra。本地可直接使用 `infra/docker/docker-compose.yaml` 啟動。

## Docker Compose

Docker Compose 設定位於 `infra/docker/docker-compose.yaml`，包含 PostgreSQL、Redis、RabbitMQ、Hydra、Mailpit、MinIO，以及目前已納入 compose 的應用服務。

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
| market-web | 5174 | 市集首頁 |
| workspace-web | 5175 | 創作者後台 |
| docs | 5176 | VitePress 文件站 |
| postgres | 5432 | PostgreSQL |
| redis | 6379 | Redis |
| rabbitmq | 5672 / 15672 | RabbitMQ / Management UI |
| hydra | 4444 / 4445 | Ory Hydra Public / Admin |
| mailpit | 1025 / 8025 | SMTP / Web UI |
| minio | 9000 / 9001 | S3 API / Console |

> `CatalogService` 目前存在於專案中，但尚未加入 `infra/docker/docker-compose.yaml`。

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
| CatalogService | 商品 / 版本 / 分類 / 標籤 REST API | http://localhost:5176，Swagger: `/swagger` |
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
| market-web | 市集首頁 / landing | OIDC (`oidc-client-ts`) |
| creator-web | 創作者店面、商品列表、商品詳情、結帳 | 無，消費者免註冊 |
| workspace-web | 創作者後台、開店、商品 / 訂單管理、設定 | OIDC (`oidc-client-ts`) |

`workspace-web` 的 API client 由 `swagger-typescript-api` 從後端 OpenAPI 產生，不手寫 API 型別。

## 文件站

從 `docs/` 目錄執行：

```bash
pnpm dev
pnpm build
pnpm preview
```

## 架構慣例

- Controller 只處理 HTTP 路由、model binding 與狀態碼，業務邏輯放在 `Services/<Feature>/`。
- Request / Response / DTO 依功能集中於 `Models/<Feature>Models.cs`。
- 分頁一律使用 `offset` / `limit`。
- 時間欄位使用 `DateTimeOffset`，命名以 `At` 結尾。
- 資料庫欄位使用 snake_case，由 `BaseDbContext` 自動套用 naming convention。
- Entity、DTO、Controller Action 皆需 XML `<summary>`；DTO 屬性另加 `<example>`。
- REST API 服務以 `ExceptionMiddleware` 將 `AppException` 轉為 RFC 9457 Problem Details。
- Auth 是 MVC 服務，使用 ASP.NET Core `UseExceptionHandler`，不掛載 `ExceptionMiddleware`。
- 服務間資源以本地 ref table 同步；事件透過 Outbox 寫入後由 background relay publish 到 RabbitMQ。

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
| `Services` | StoreService / CatalogService 呼叫其他內部服務的 BaseUrl |

本地開發預設資料庫命名為 `open_jam_<service>`，例如 `open_jam_auth`、`open_jam_store`、`open_jam_catalog`。
