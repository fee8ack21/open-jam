# 平台初始化 Bootstrap

Bootstrap 負責**預建平台運行所需的資料**。因架構採 **DB per service**（見 [[Infra]]），各服務自管自己 DB 的 seed；Bootstrap 不直接寫各服務的 DB，而是負責**跨服務協調**（執行順序）與**跨服務 / 基建共用資料**。以**冪等 Job** 於部署時自動執行，seed 比照 migration 版本控管。

## 職責邊界

- **各服務自管 seed**：各微服務 seed 自己 DB 的資料（尊重 DB per service 邊界）。
- **Bootstrap 負責**：
  - 跨服務執行順序協調。
  - 跨服務 / 基建共用資料（如 Hydra Client、子網域保留字）。

## 執行模型

- 以 **K8s Job**（Helm post-install hook）在部署時自動執行（見 [[Infra]]）。
- **冪等**：已存在則跳過，可安全重跑。
- seed 比照 **migration 版本控管**，可增量套用（保留字、分類、方案等會演進）。

## Seeder 執行清單（依序）

| Seeder | 資料 | 說明 |
|--------|------|------|
| `HydraClientSeeder` | Hydra OIDC client | Web client（SPA 登入，create-only）+ Service client（服務間 `client_credentials`，`open-jam-service`，**force-update（PUT）**——每次執行覆寫 secret，避免 Hydra 與各服務設定的 secret 漂移），設定 key `HydraClients:Web` / `:Service` |
| `EmailTemplateSeeder` | Email 模板 | 寫入 EmailService DB（[[Email]]） |
| `UserSeeder` | 平台管理員 + dev 假帳號 | `AdminUser:Email` / `:Password` 皆有值才 seed 管理員；`MockUsers:Enabled` 控制假帳號（正式須 `false`） |
| `LegalDocumentSeeder` | 法律文件初始啟用版本 | 寫入 ContentService DB（[[Content]]），服務條款 / 隱私權政策各一筆，該類型已有紀錄即略過 |
| `FaqSeeder` | FAQ 主題分類與初始問答 | 寫入 ContentService DB（[[Content]]）；分類以 slug 冪等 upsert，問答於 FAQ 表已有資料時略過 |
| `StoreSeeder` | dev 假店家 | `MockStores:Enabled` 控制（正式須 `false`） |
| `CatalogCategorySeeder` | 平台固定分類 | 寫入 CatalogService DB（[[Catalog]]） |
| `StoreFollowerRefSeeder` | 追蹤者參照表回填 | 回填 NotificationService `store_follower_ref`（[[Notification]]），重跑冪等 |
| `StorefrontRedirectSeeder` | 店面子網域 OIDC redirect URI 回填 | 將既有店家的 `<slug>.openjam.co` callback / silent-renew / post-logout URI 補進 Hydra web client 白名單，重跑冪等；新店家由 Auth 消費 `StoreProvisionedEvent` 即時註冊（[[Auth]]） |

掛載 6 個 DbContext：`AuthConnection` / `EmailConnection` / `CatalogConnection` / `StoreConnection` / `NotificationConnection` / `ContentConnection`（具名連線字串，非共用 `DefaultConnection`）。Helm 由 `templates/bootstrap/job.yaml` 以環境變數注入設定。

### 尚未納入（規劃）

- 子網域保留字（目前為 `StoreSlugValidator` 內建黑名單，非 seed 資料）。
- 固定配額額度（由 QuotaService `appsettings` 提供，見 [[Quota]]）。

## 技術與架構

- 部署方式（Helm post-install hook / K8s Job）見 [[Infra]]。
- 各服務的 migration 與 seed 慣例見 [[Develop]]。
