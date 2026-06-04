# Open Jam

一個面向創作者的數位商品上架與販售平台，參考 [Gumroad](https://gumroad.com)。

創作者可在自己的子網域 `<creator>.openjam.co` 開店、上架商品、管理訂單；消費者無需註冊，憑信箱即可購買。

## 技術棧

| 層 | 技術 |
|----|------|
| 後端 | .NET 8 (C#)，ASP.NET Core，EF Core，MassTransit |
| 前端 | Vue 3，Vite，pnpm workspace |
| 資料庫 | PostgreSQL（snake_case，EF Core Npgsql） |
| 訊息佇列 | RabbitMQ（Outbox pattern） |
| 認證 | Ory Hydra（OIDC） |
| 文件 | VitePress |

## 專案結構

```
src/
  Auth/             # 認證授權（OIDC / Hydra），port 5169
  EmailService/     # 寄信 Worker，消費 RabbitMQ 事件
  LogService/       # Audit Log REST API，port 5170（Swagger /swagger）
  Bootstrap/        # 平台初始化 seed（一次性執行）
  Shared/           # 共用：BaseDbContext、ExceptionMiddleware、Audit 介面、Events
apps/
  workspace-web/    # 用戶後台 SPA
  creator-web/      # 創作者商品空間 SPA
  market-web/       # 平台首頁 SPA
docs/               # VitePress 文件站
```

## 前置需求

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/) + [pnpm](https://pnpm.io/)
- PostgreSQL
- RabbitMQ

## 後端服務

從 `src/<Service>/` 執行：

```bash
dotnet run                  # 開發伺服器
dotnet build                # 建置
dotnet publish -c Release   # 發佈
```

| 服務 | 說明 | 開發 URL |
|------|------|----------|
| Auth | MVC 登入 / 註冊 / 忘記密碼 | http://localhost:5169 |
| LogService | Audit Log REST API | http://localhost:5170 / Swagger: `/swagger` |
| EmailService | Worker（無 HTTP port） | — |
| Bootstrap | Seed，一次性執行後結束 | — |

Docker（從 `src/` 執行）：

```bash
docker build -f Auth/Dockerfile -t open-jam-auth .
```

### 環境設定

各服務的 `appsettings.json` 已包含 localhost 預設值，開發時可直接使用或以 User Secrets 覆蓋。

**Auth**

```json
{
  "ConnectionStrings": {
    "Postgres": "Host=localhost;Database=open_jam_auth;Username=postgres;Password=postgres"
  },
  "RabbitMQ": { "Host": "localhost", "Username": "guest", "Password": "guest" },
  "Hydra": { "AdminUrl": "http://localhost:4445" },
  "App": { "BaseUrl": "https://localhost:7280" }
}
```

**EmailService**

```json
{
  "ConnectionStrings": {
    "Postgres": "Host=localhost;Database=open_jam_email;Username=postgres;Password=postgres"
  },
  "RabbitMQ": { "Host": "localhost", "Username": "guest", "Password": "guest" },
  "Smtp": {
    "Host": "localhost",
    "Port": 1025,
    "UseSsl": false,
    "FromAddress": "noreply@open-jam.tw"
  }
}
```

**LogService**

```json
{
  "ConnectionStrings": {
    "Postgres": "Host=localhost;Database=open_jam_log;Username=postgres;Password=postgres"
  },
  "RabbitMQ": { "Host": "localhost", "Username": "guest", "Password": "guest" }
}
```

## 前端應用

從 `apps/<app>/` 執行：

```bash
pnpm dev    # 開發伺服器（Vite 預設 port 5173，多個並跑時依序遞增）
pnpm build  # 建置
```

| 應用 | 說明 |
|------|------|
| workspace-web | 用戶後台 |
| creator-web | 創作者商品空間 |
| market-web | 平台探索首頁 |

## 文件站

從 `docs/` 執行：

```bash
pnpm dev      # http://localhost:5173
pnpm build
pnpm preview
```

Docker：

```bash
docker build -t open-jam-docs .
docker run -p 8080:80 open-jam-docs
```

## 架構慣例

- **CQRS / Mediator + 軟體三層**
- **時間欄位**：型別 `DateTimeOffset`，命名以 `At` 結尾
- **資料庫命名**：snake_case（由 `BaseDbContext` 自動轉換，無需手動 `[Column]`）
- **XML 文件**：Entity / DTO / Controller Action 皆需 `<summary>`，DTO 屬性另加 `<example>`
- **錯誤處理**：業務層拋 `AppException` 子類，由 `ExceptionMiddleware` 統一轉為 RFC 9457 Problem Details
- **分頁**：一律使用 `offset` / `limit`

詳見 [開發慣例](docs/develop.md)。
