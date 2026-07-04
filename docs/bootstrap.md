# 平台初始化 Bootstrap

Bootstrap 負責**預建平台運行所需的資料**。因架構採 **DB per service**（見 [[Infra]]），各服務自管自己 DB 的 seed；Bootstrap 不直接寫各服務的 DB，而是負責**跨服務協調**（執行順序）與**跨服務 / 基建共用資料**。以**冪等 Job** 於部署時自動執行，seed 比照 migration 版本控管。

## 職責邊界

- **各服務自管 seed**：各微服務 seed 自己 DB 的資料（尊重 DB per service 邊界）。
- **Bootstrap 負責**：
  - 跨服務執行順序協調。
  - 跨服務 / 基建共用資料（如 Hydra Client、子網域保留字）。

## 執行模型

- 以 **K8s Job / Argo CD hook** 在部署時自動執行（見 [[Infra]]）。
- **冪等**：已存在則跳過，可安全重跑。
- seed 比照 **migration 版本控管**，可增量套用（保留字、分類、方案等會演進）。

## Seeder 執行清單（依序）

| Seeder | 資料 | 說明 |
|--------|------|------|
| `HydraClientSeeder` | Hydra OIDC client | Web client（SPA 登入）+ Service client（服務間 `client_credentials`，`open-jam-service`），設定 key `HydraClients:Web` / `:Service` |
| `EmailTemplateSeeder` | Email 模板 | 寫入 EmailService DB（[[Email]]） |
| `UserSeeder` | 平台管理員 + dev 假帳號 | `AdminUser:Email` / `:Password` 皆有值才 seed 管理員；`MockUsers:Enabled` 控制假帳號（正式須 `false`） |
| `StoreSeeder` | dev 假店家 | `MockStores:Enabled` 控制（正式須 `false`） |
| `CatalogCategorySeeder` | 平台固定分類 | 寫入 CatalogService DB（[[Catalog]]） |
| `StoreFollowerRefSeeder` | 追蹤者參照表回填 | 回填 NotificationService `store_follower_ref`（[[Notification]]），重跑冪等 |

掛載 5 個 DbContext：`AuthConnection` / `EmailConnection` / `CatalogConnection` / `StoreConnection` / `NotificationConnection`（具名連線字串，非共用 `DefaultConnection`）。Helm 由 `templates/bootstrap/job.yaml` 以環境變數注入設定。

### 尚未納入（規劃）

- 子網域保留字（目前為 `StoreSlugValidator` 內建黑名單，非 seed 資料）。
- 固定配額額度（由 QuotaService `appsettings` 提供，見 [[Quota]]）。

## 技術與架構

- 部署方式（Argo hook / Job）見 [[Infra]]。
- 各服務的 migration 與 seed 慣例見 [[Develop]]。
