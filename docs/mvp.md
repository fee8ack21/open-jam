# MVP 範圍與 Roadmap

本頁說明 Open Jam **MVP 階段**已涵蓋的功能邊界，以及尚未完成、規劃中的工作項目。各功能的完整規格見對應的功能頁；本頁只談「做到哪、還沒做什麼」。

## MVP 目標

以最小可運行的範圍，跑通一條完整的創作者商業流程骨架：

> 註冊登入 → 申請開店 → 上架數位商品（含檔案上傳、版本、資產）→ 在子網域店面展示 → 消費者瀏覽商品。

並具備支撐上述流程的平台基礎建設：認證授權、檔案儲存、信件、稽核日誌、事件驅動同步（Outbox / RabbitMQ）。

## 已涵蓋功能

### 後端服務

| 服務 | MVP 範圍 | 規格 |
|------|----------|------|
| **Auth** | 註冊 / 登入 / 忘記密碼 / Email 驗證，整合 Ory Hydra（OIDC）；Argon2id 密碼雜湊 | [[Auth]] |
| **StoreService** | 開店申請與管理員審核、商店資料（名稱 / 描述 / Avatar / Banner / 狀態）、商店成員（僅 Owner）、憑信箱追蹤 | [[Store]] |
| **CatalogService** | 數位商品 CRUD、版本（CatalogVersion）、平台多層分類、創作者標籤、展示型與下載型資產、商品狀態機（Draft / Published / Archived / Suspended） | [[Catalog]] |
| **StorageService** | 簽發上傳 / 下載 URL、本地檔案儲存後端（LocalStorageProvider）、HMAC 簽章直傳、上傳確認、`FileReadyEvent`、孤兒檔清理 | [[Storage]] |
| **EmailService** | 消費 `EmailRequestedEvent`，DB 模板渲染 → SMTP（地端）/ SendGrid（正式）寄送，指數退避重試 + 補償排程 | [[Email]] |
| **LogService** | 消費 `AuditLogRequestedEvent` 寫入 `audit_log`，提供分頁查詢 REST API | [[Log]] |
| **Bootstrap** | 一次性 Seed：Hydra OIDC client、Email 模板、dev 用預設使用者 | [[Bootstrap]] |

### 前端應用

| App | MVP 範圍 | 認證 |
|-----|----------|------|
| **market-web** | 市集首頁 / landing，平台探索入口 | OIDC |
| **creator-web** | 創作者子網域店面：商品列表 / 詳情（結帳流程骨架） | 無（消費者免註冊） |
| **workspace-web** | 創作者後台：開店、商品 / 版本 / 資產管理、上架、商店設定 | OIDC |

### 平台共通能力

- **微服務三層架構 + Service DI**：Controller 僅處理 HTTP，業務邏輯落在 `Services/<Feature>/`。
- **統一輸入驗證**：FluentValidation（422 + 欄位 `errors`）。
- **Entity ↔ DTO 轉換**：AutoMapper。
- **錯誤處理**：REST API 走 RFC 9457 Problem Details，MVC（Auth）走 Error Page。
- **軟刪除 + Audit 欄位**：由 `BaseDbContext` 自動處理。
- **事件驅動同步**：Outbox 模式 → RabbitMQ，Consumer 端冪等去重。
- **前端 API 全自動產生**：以 `swagger-typescript-api` 從各服務 OpenAPI 產生 client，不手寫 endpoint。
- **TypeScript 化**：workspace-web、creator-web 已轉 TS。

## 未涵蓋 / Future TODO

以下為 MVP 尚未實作、規劃於後續階段的項目。

### 金流與訂單（最高優先）

- [ ] **OrderService**：訂單建立、未登入憑信箱購買、付款狀態機。
- [ ] **Stripe 串接**：Checkout、Webhook 對帳、退款。
- [ ] **下載授權**：付款成功後簽發私有版本資產的下載 URL（憑 Order ID / 信件雙路徑查詢）。
- [ ] **註冊回溯**：消費者日後註冊時，自動回溯歷史訂單與追蹤。
- [ ] **定價進階**：折扣碼 / 優惠碼、免費商品領取流程。
- 規格見 [[Order]]。

### 資源配額（Quota）

- [ ] **QuotaService**：單檔大小、單一商品總量、帳號總儲存空間上限。
- [ ] **方案制配額**：免費 / 付費訂閱方案對應不同上限。
- [ ] **簽發前檢查**：在 StorageService 簽發上傳 URL 前進行配額檢查與用量計量。
- 規格見 [[Quota]]。

### 儲存進階

- [ ] **雲端後端正式化**：GCS Provider 端到端驗證（雙 bucket `open-jam-public` / `open-jam-private`；憑證先走 k8s secret 掛載的服務帳戶金鑰檔在本地簽 V4 signed URL，未來可改 Workload Identity 免金鑰）。
- [ ] **Malware Scan**：上傳檔案掃毒。
- [ ] **影音轉碼**：HLS 轉碼 pipeline 與雙層預覽衍生檔。
- [ ] **CDN 加速**：公開資產走 CDN。

### 內容治理

- [ ] **檢舉機制**：任何人（含未登入）可檢舉不宜內容。
- [ ] **管理員審核後台**：成立則下架商品 / 停權創作者，並記錄 Audit Log。

### 訂閱與社群

- [ ] **創作者訂閱方案**：平台抽成 + 創作者付費方案的計費。
- [ ] **追蹤者通知**：新商品上架通知追蹤者。

### 平台與維運

- [ ] **Redis**：快取 / Session / 配額計數。
- [ ] **正式部署**：GKE + Argo CD + Helm Umbrella Chart、Cloudflare DNS。
- [ ] **可觀測性**：Loki + Promtail（Application Log）整合。
- [ ] **多商店成員**：StoreMember 角色擴充（如 Staff），目前僅 Owner。

## 相關頁面

- [[Develop]] — 開發慣例與專案結構
- [[Infra]] — 網域、站點、雲端資源、部署
- 各功能規格：[[Auth]]、[[Store]]、[[Catalog]]、[[Order]]、[[Storage]]、[[Email]]、[[Quota]]、[[Log]]
