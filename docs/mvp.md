# MVP 範圍與 Roadmap

本頁說明 Open Jam **MVP 階段**已涵蓋的功能邊界，以及尚未完成、規劃中的工作項目。各功能的完整規格見對應的功能頁；本頁只談「做到哪、還沒做什麼」。

## MVP 目標

以最小可運行的範圍，跑通一條完整的創作者商業流程：

> 註冊登入 → 申請開店 → 上架數位商品（含檔案上傳、版本、資產、配額）→ 在子網域店面展示 → 消費者瀏覽、下單付款（Stripe Checkout）→ 完成訂單、取得下載 → 評論與追蹤通知。

並具備支撐上述流程的平台基礎建設：認證授權、檔案儲存、金流、配額、通知、信件、稽核日誌、事件驅動同步（Outbox / RabbitMQ）。

## 已涵蓋功能

### 後端服務

| 服務 | MVP 範圍 | 規格 |
|------|----------|------|
| **Auth** | 註冊 / 登入 / 忘記密碼 / Email 驗證，整合 Ory Hydra（OIDC）；Argon2id 密碼雜湊；使用者列表 API（Admin）；註冊發 `UserRegisteredEvent` | [[Auth]] |
| **StoreService** | 開店申請與管理員審核、商店資料（名稱 / 描述 / Avatar / Banner / 狀態）、商店成員（僅 Owner）、憑信箱追蹤（含註冊後 UserId 回填）、全平台商店列表（Admin） | [[Store]] |
| **CatalogService** | 數位商品 CRUD、版本、平台多層分類、創作者標籤、展示型與下載型資產、商品狀態機、評論（限已購買者）、收藏、瀏覽 / 銷量計數、精選策展、買家下載授權 | [[Catalog]] |
| **OrderService** | 訂單建立（免註冊憑信箱）、結帳單一入口（串 PaymentService）、狀態歷程、買家 / 賣家 / Admin 三視角列表、購買驗證、取消 | [[Order]] |
| **PaymentService** | Stripe Checkout Session、Webhook 兩段式處理（落地→背景處理）、`PaymentSucceededEvent`、付款交易紀錄 | [[Order]] |
| **QuotaService** | 帳號總儲存空間 / 單檔 / 單商品總量 / 上架商品數固定額度，confirm 時原子扣量、每日對帳 | [[Quota]] |
| **StorageService** | 簽發上傳 / 下載 URL（不計配額）、本地 / GCS 雙後端（雙 bucket）、confirm / reference 生命週期、用量統計 API、`FileReadyEvent`、孤兒檔清理 | [[Storage]] |
| **NotificationService** | 追蹤者上架通知、商店公告（可排程），Email + in-app 雙管道、統一排程管線 | [[Notification]] |
| **EmailService** | 消費 `EmailRequestedEvent`，DB 模板渲染 → SMTP（地端）/ SendGrid（正式）寄送，指數退避重試 + 補償排程 | [[Email]] |
| **LogService** | 消費 `AuditLogRequestedEvent` 寫入 `audit_log`，提供分頁查詢 REST API | [[Log]] |
| **Bootstrap** | 一次性 Seed：Hydra OIDC client（Web + Service）、Email 模板、管理員、平台分類、追蹤者 ref 回填、dev 假帳號 / 假店家 | [[Bootstrap]] |

### 前端應用

| App | MVP 範圍 | 認證 |
|-----|----------|------|
| **portal-web** | 市集首頁 / landing、平台探索入口、通知鈴鐺 | OIDC |
| **creator-web** | 創作者子網域店面：商品列表 / 詳情 / 結帳（Stripe Checkout 導轉、結果頁） | 無（消費者免註冊） |
| **workspace-web** | 創作者後台：開店、商品 / 版本 / 資產管理、上架、訂單、公告排程、購買紀錄與下載、商店設定；管理員後台：會員 / 商店 / 商品 / 訂單 / 資源用量 / 稽核 | OIDC |

### 平台共通能力

- **微服務三層架構 + Service DI**：Controller 僅處理 HTTP，業務邏輯落在 `Services/<Feature>/`。
- **統一輸入驗證**：FluentValidation（422 + 欄位 `errors`）。
- **Entity ↔ DTO 轉換**：AutoMapper。
- **錯誤處理**：REST API 走 RFC 9457 Problem Details，MVC（Auth）走 Error Page。
- **軟刪除 + Audit 欄位**：由 `BaseDbContext` 自動處理。
- **事件驅動同步**：Outbox 模式 → RabbitMQ，Consumer 端冪等去重。
- **服務間認證**：內部 API 以 Hydra `client_credentials` service token + `"InternalService"` policy 保護。
- **前端 API 全自動產生**：以 `swagger-typescript-api` 從各服務 OpenAPI 產生 client，不手寫 endpoint。
- **TypeScript + i18n**：三個前端皆為 TypeScript，並導入 vue-i18n（Excel→JSON 翻譯流程）。

## 未涵蓋 / Future TODO

以下為 MVP 尚未實作、規劃於後續階段的項目。

### 金流進階

- [ ] **Stripe Connect Express**：創作者各自收款、平台抽成（`application_fee_amount`）、payout；目前收款進平台帳戶未分帳。
- [ ] **退款**：退款窗口、經 Stripe 退款並撤銷下載權（`Refunded` 狀態已預留）。
- [ ] **Stripe Tax**：各地稅負自動計算。
- [ ] **定價進階**：折扣碼 / 優惠碼、免費商品領取流程。
- [ ] **Guest 訂單回溯**：消費者日後註冊時，自動回溯歷史訂單（追蹤回填已實作）。

### 儲存進階

- [ ] **Malware Scan**：上傳檔案掃毒（ClamAV）。
- [ ] **影音轉碼**：HLS 轉碼 pipeline 與雙層預覽衍生檔。
- [ ] **CDN 加速**：公開資產走 CDN。

### 配額進階

- [ ] **方案制配額**：免費 / 付費訂閱方案對應不同上限（目前為固定額度，API 與資料模型已預留）。

### 內容治理

- [ ] **檢舉機制**：任何人（含未登入）可檢舉不宜內容。
- [ ] **檢舉審核流程**：成立則下架商品 / 停權創作者（Admin 停權 / 下架操作本身已實作）。

### 訂閱與社群

- [ ] **創作者訂閱方案**：平台抽成 + 創作者付費方案的計費。

### 平台與維運

- [ ] **Redis**：快取 / Session / 配額計數（compose 已就緒，尚未使用）。
- [ ] **CI/CD 自動化**：GitHub Actions + Argo CD（目前手動 build / push / helm upgrade）。
- [ ] **可觀測性**：Loki + Promtail（Application Log）、OpenTelemetry / Tempo 整合。
- [ ] **多商店成員**：StoreMember 角色擴充（如 Staff），目前僅 Owner。

## 相關頁面

- [[Develop]] — 開發慣例與專案結構
- [[Infra]] — 網域、站點、雲端資源、部署
- 各功能規格：[[Auth]]、[[Store]]、[[Catalog]]、[[Order]]、[[Storage]]、[[Notification]]、[[Email]]、[[Quota]]、[[Log]]
