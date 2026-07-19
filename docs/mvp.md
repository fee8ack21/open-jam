# MVP 範圍與 Roadmap

本頁說明 Open Jam **MVP 階段**已涵蓋的功能邊界，以及尚未完成、規劃中的工作項目。各功能的完整規格見對應的功能頁；本頁只談「做到哪、還沒做什麼」。

## MVP 目標

以最小可運行的範圍，跑通一條完整的創作者商業流程：

> 註冊登入 → 申請開店 → 上架數位商品（含檔案上傳、版本、資產、配額）→ 在子網域店面展示 → 消費者瀏覽、下單付款（Stripe Checkout）→ 完成訂單、取得下載 → 評論與追蹤通知。

並具備支撐上述流程的平台基礎建設：認證授權、檔案儲存、金流、配額、通知、信件、稽核日誌、平台內容（法律文件 / FAQ）、事件驅動同步（Outbox / RabbitMQ）。

## 已涵蓋功能

### 後端服務

| 服務 | MVP 範圍 | 規格 |
|------|----------|------|
| **Auth** | 註冊 / 登入 / 忘記密碼 / Email 驗證，整合 Ory Hydra（OIDC）；Argon2id 密碼雜湊；法律文件同意流程（註冊同意 + 登入 re-consent，同意紀錄 `UserLegalConsent`，文件本身由 ContentService 管理）；使用者列表 API（Admin）；註冊發 `UserRegisteredEvent` | [[Auth]] |
| **StoreService** | 開店申請與管理員審核、商店資料（名稱 / 描述 / Avatar / Banner / 狀態）、商店成員（僅 Owner）、憑信箱追蹤（含註冊後 UserId 回填）、全平台商店列表（Admin） | [[Store]] |
| **CatalogService** | 數位商品 CRUD、版本、平台多層分類、創作者標籤、展示型與下載型資產、商品狀態機、評論（限已購買者）、收藏、瀏覽 / 銷量計數、精選策展、買家下載授權 | [[Catalog]] |
| **OrderService** | 訂單建立（免註冊憑信箱、伺服器端核價）、結帳單一入口（串 PaymentService）、免費訂單直接履約（`TotalAmount == 0` 不走 Stripe，建單即完成）、狀態歷程、買家 / 賣家 / Admin 三視角列表、購買驗證、取消、訪客訂單註冊後回填（消費 `UserRegisteredEvent` 補 `BuyerUserId`）、訂單完成信（含下載頁連結） | [[Order]] |
| **PaymentService** | Stripe Checkout Session、Stripe Connect 分帳（一店一 Express 帳戶、destination charge + application fee、託管 onboarding、Connect webhook 同步收款狀態）、Webhook 兩段式處理（落地→背景處理）、`PaymentSucceededEvent`、付款交易紀錄 | [[Order]] |
| **QuotaService** | 帳號總儲存空間 / 單檔 / 單商品總量 / 上架商品數固定額度，confirm 時原子扣量、每日對帳 | [[Quota]] |
| **StorageService** | 簽發上傳 / 下載 URL（不計配額）、本地 / GCS 雙後端（雙 bucket）、confirm / reference 生命週期、用量統計 API、`FileReadyEvent`、孤兒檔清理 | [[Storage]] |
| **NotificationService** | 追蹤者上架通知、商店公告（可排程），Email + in-app 雙管道、統一排程管線 | [[Notification]] |
| **EmailService** | 消費 `EmailRequestedEvent`，DB 模板渲染 → SMTP（地端）/ SendGrid（正式）寄送，指數退避重試 + 補償排程 | [[Email]] |
| **LogService** | 消費 `AuditLogRequestedEvent` 寫入 `audit_log`，提供分頁查詢 REST API | [[Log]] |
| **ContentService** | 平台內容管理：法律文件版本（服務條款 / 隱私權政策，`Draft → Active → Inactive`、永不刪除、匿名撈啟用版本）、常見問題分類與問答（`FaqCategory` / `FaqItem`，匿名撈已發布項目、Admin CRUD） | [[Content]] |
| **Bootstrap** | 一次性 Seed：Hydra OIDC client（Web + Service）、Email 模板、管理員、平台分類、法律文件初始啟用版本、FAQ 分類與初始問答、追蹤者 ref 回填、dev 假帳號 / 假店家 | [[Bootstrap]] |

### 前端應用

| App | MVP 範圍 | 認證 |
|-----|----------|------|
| **portal-web** | 市集首頁 / landing、平台探索入口、常見問題頁（FAQ，依主題分頁）、服務條款 / 隱私權政策頁、關於頁、通知鈴鐺 | OIDC |
| **creator-web** | 創作者子網域店面：商品列表（搜尋 / 篩選）/ 詳情（評論、收藏、追蹤商店）/ 結帳（Stripe Checkout 導轉、結果頁）/ 訂單下載頁（`/orders/:orderId`，訪客憑訂單 ID 下載） | 無（消費者免註冊） |
| **workspace-web** | 創作者後台：開店、商品 / 版本 / 資產管理、上架、訂單、公告排程、收款設定（Stripe Connect onboarding）、購買紀錄與下載、收藏清單、商店設定；管理員後台：會員 / 商店（開店審核 + 審核紀錄）/ 商品 / 訂單 / 金流付款 / 商品分類 / 條款管理 / 常見問題（分類 + 問答）/ 資源用量 / 稽核 | OIDC |

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

- [ ] **退款**：退款窗口、經 Stripe 退款並撤銷下載權（`Refunded` 狀態已預留）。
- [ ] **Stripe Tax**：各地稅負自動計算。
- [ ] **定價進階**：折扣碼 / 優惠碼。

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

- [ ] **Redis**：快取 / Session / 配額計數。2026-07-20 起 compose 與 helm **皆預設停用**（無程式使用，省叢集資源）；設定保留，要用時 compose `--profile redis`、helm `redis.enabled: true` 即可（見 [[Infra]]）。
- [ ] **Argo CD GitOps（未來可考慮，短期不做）**：目前 CD 為 GitHub Actions release build / push + 直接 `helm upgrade` 部署 GKE（見 [[CI]]），運作正常。未來若要宣告式同步 / self-heal，可評估改由 Argo CD 接管、移除 CI 直接 helm 步驟。
- [ ] **可觀測性**：Loki + Promtail（Application Log）、OpenTelemetry / Tempo 整合。
- [ ] **多商店成員**：StoreMember 角色擴充（如 Staff），目前僅 Owner。

## 相關頁面

- [[Develop]] — 開發慣例與專案結構
- [[Infra]] — 網域、站點、雲端資源、部署
- 各功能規格：[[Auth]]、[[Store]]、[[Catalog]]、[[Order]]、[[Storage]]、[[Notification]]、[[Email]]、[[Quota]]、[[Log]]、[[Content]]
