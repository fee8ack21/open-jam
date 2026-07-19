# 認證授權 Auth

Auth Service 負責 Open Jam 的帳號、認證與授權，以 OIDC（Hydra）為核心，並透過 Outbox 機制與 [[Email]]、[[Log]] 等服務串接。本頁分為**功能規格**（面向使用者的流程）與**技術設計**（基礎建設與實作策略）兩部分。

> **實作現況（MVP）**：本頁多數內容為規劃規格。**已實作**：註冊（含 Pending 帳號覆蓋重註冊）/ Email 驗證 / 登入 / 忘記密碼 / 重置密碼、Hydra Login & Consent Provider、Argon2id 雜湊、條款同意與 re-consent、`UserRegisteredEvent`、`GET /v1/users`（Admin）、店面子網域 OIDC redirect URI 註冊（消費 `StoreProvisionedEvent`）、JWT role claim（`User.Role` enum：`User` / `Admin`）。**尚未實作**：CAPTCHA（Turnstile）、登入失敗鎖定與 rate limit（`UserStatus.Locked` 僅預留）、過期未驗證帳號清理排程、修改密碼 / 修改信箱 / 帳號停用刪除（GDPR）、2FA、多裝置管理與異常登入偵測、Role–Permission 關聯表與 Redis permission cache、front-channel / back-channel 全域登出；SPA token 實際採 oidc-client-ts silent renew（非下述「refresh token 放 HttpOnly cookie」策略）。

## 功能規格

### 帳號狀態機

帳號狀態以單一 enum 表示，作為各流程判斷的共同依據，並對應 DB schema 設計：

- `pending`：註冊後尚未點擊開通信，不允許登入。
- `active`：正常使用狀態。
- `locked`：因登入失敗暫時鎖定，逾時或解鎖後回到 `active`。
- `suspended`：遭管理員停權，session 失效並禁止登入。
- `deactivated`：用戶自行停用，可評估是否允許重新啟用。
- `deleted`：依 GDPR 處理刪除（軟刪除）；創作者的商品 / 訂單依策略保留或下架。

### 註冊

- 表單欄位：信箱、密碼、確認密碼、服務條款與隱私權勾選、CAPTCHA。
- 密碼規則：
  - 長度 8～20 字。
  - 至少各包含一個大寫、一個小寫字母。
  - 至少包含一個數字。
  - 至少包含一個特殊字元。
  - 不可使用全形字元，不可包含空白字元。
- 註冊完成後寄發帳號開通信，須於時效內點擊才算完成；時效過期須提供重新寄信流程。
- 未驗證的帳號不允許登入。
- 信箱於資料庫 schema 設為 unique，從資料庫層級限制不可重複。
- 帳號列舉防護：註冊時允許提示「信箱已被使用」（UX 取捨），但忘記密碼一律回統一訊息。
- 未驗證帳號占用信箱（squatting）的處理：
  - 定期清除過期未驗證帳號，並允許該信箱重新註冊。
  - 重複註冊同一信箱時，覆蓋舊的未驗證資料並重發驗證信。
  - 目的：避免惡意者註冊後不驗證，藉此卡位真正擁有者的信箱。

### 登入

- 表單欄位：信箱、密碼、CAPTCHA。
- 未經帳號開通驗證的帳號不允許登入。
- 登入失敗鎖定採 **IP + 帳號雙維度 + CAPTCHA 升級**：
  - 失敗先觸發 CAPTCHA，持續失敗才暫時鎖定一段時間。
  - 雙維度可避免攻擊者單以帳號維度鎖死受害者（DoS）。
- 跨分頁 session 同步：任一分頁登入或登出時，其他分頁自動同步狀態（BroadcastChannel / storage event）。
- 多裝置：允許多裝置同時登入，並提供「查看已登入裝置 / 遠端登出」管理功能。
- 異常登入偵測：記錄登入裝置與 IP，於新裝置或異地登入時即時寄信提醒。

### 忘記密碼

- 表單欄位：信箱。
- 送出後依信箱寄發重置密碼信，須於時效內點擊才算完成。
- 帳號列舉防護：無論信箱是否存在，一律回統一訊息「若信箱存在，已寄出重置信」。
- 重置 token 為單次使用；重新申請即作廢舊 token；用過即失效。
- 重置成功後登出該用戶所有既有 session，並導頁回登入頁。

### 重置密碼

- 表單欄位：新密碼、確認密碼（套用與註冊相同的密碼規則）。

### 登出

- 支援多分頁同時登出（與登入同步機制一致）。

### 服務條款與隱私權

- 需做版本紀錄，用戶註冊時記錄其勾選的版本。
- 上架新版本後，用戶下次登入須再次確認同意。

### 兩階段驗證（2FA）

> **範圍**：MVP 暫不實作，但於 schema 與流程預留擴充點，不阻礙 MVP 進度。

- 採 TOTP（Google Authenticator 類），由用戶自行開啟。
- 敏感操作（修改信箱、修改密碼、提領設定）未來須重新驗證。

### 帳號管理（登入狀態）

- **修改密碼**：
  - 需驗證舊密碼，並套用相同密碼規則。
  - 修改成功後寄發「密碼已變更」通知信。
  - 可選擇登出其他既有 session。
- **修改信箱**：
  - 需驗證新信箱（寄驗證信，時效內點擊才生效）。
  - 同時通知舊信箱（防盜用）。
  - 訂單 / 訂閱關聯以 `user_id`（FK）為主，email 僅作為 guest → 帳號 的初次對應鍵；因此改信箱不影響已認領的歷史訂單 / 訂閱。
  - 改信箱（即使已驗證新信箱）不自動合併新信箱底下既有的 guest 訂單 / 訂閱，僅保留原帳號歷史。
  - guest → 帳號 的對應規則由 [[Catalog]] 定義。

- **帳號刪除 / 停用（GDPR）**：
  - 用戶可自行停用或刪除帳號。
  - 創作者若有上架商品、進行中訂單或訂閱者，需訂定處理策略（軟刪除、商品下架、已售出商品的下載權保留）。

### 管理員停權 / 封鎖

- 管理員可停權帳號（對應 [[Catalog]] 的不宜內容檢舉）。
- 停權後既有 session 失效，並禁止登入。
- 須記錄 Audit Log（操作者、原因、時間）。

## 技術設計

### OIDC

- 以 Hydra 作為 OAuth / OIDC server。
- 需自行實作 Login / Consent Provider —— Hydra 僅提供授權框架，登入頁與同意頁邏輯由 Auth Service 負責。
- SPA 為 public client，授權碼流程強制使用 PKCE。
- JWT claims 設計需包含 sub、email、tenant / creator 身份、roles。
- 社群登入（Google 等）：MVP 不實作，但架構上預留 external IdP 擴充空間。

### 跨子網域 SSO 與全域登出

四個 SPA 分屬不同子網域（`auth` / `workspace` / `creator` / `market`），透過 Hydra 維護的中央 session 達成單一登入：

- 所有 SPA 共用單一 public OIDC client（`open-jam-web`），皆對 Hydra 走授權碼流程（PKCE）。
- 使用者於 `auth.openjam.co` 登入後由 Hydra 建立 session；其他 SPA 以 silent auth（`prompt=none`）免重登取得 token，無需各自獨立 client。
- 全域登出採 OIDC front-channel / back-channel logout，使所有 client 一併失效，而非僅同瀏覽器分頁登出。
- 需設定 Hydra session cookie 的網域範圍，各 SPA 各自管理自身 token。

### 授權與角色權限（RBAC）

- 採 Role – Permission 關聯表，未來可擴充子帳號 / 團隊。
- 角色至少包含 Consumer、Creator、Admin，單一帳號可同時擁有多個角色。
- Permission 變更時需失效或更新 Redis Cache。
- 管理者不另設 admin portal，共用 workspace 後台登入，以 Admin 角色權限開啟管理功能。
- 第一個 Admin 帳號與角色由 Seeder 預建。

### 開店流程（Consumer → Creator）

- 已驗證用戶於 workspace 後台申請開店。
- 設定子網域 / 店名，並檢查保留字與重複（見 [[Quota]]、[[Bootstrap]]）。
- 可能需經審核或身分 / 金流驗證後，才取得 Creator 角色。

### 資安

- **CSRF**：表單與狀態變更請求需有 CSRF 防護。
- **Rate Limit**：
  - 套用於 POST 登入、註冊、忘記密碼、重置密碼。
  - 同時以 IP 維度與信箱維度限流。
  - 重發信限流（mail-bomb 防護）：重發帳號開通信 / 重置密碼信須有獨立冷卻時間與次數上限，避免被拿來灌爆受害者信箱。
- **密碼雜湊**：
  - 採 Argon2id（OWASP 首選、memory-hard，可抵抗 GPU / ASIC 暴力破解）。
  - .NET 使用 Konscious.Security.Cryptography 套件。
  - 參數自 OWASP 基準起調（memory ≥ 19 MB、iterations ≥ 2、parallelism = 1）。
  - 每筆使用獨立 salt，可評估加上 pepper。
- **CAPTCHA**：
  - 採 Cloudflare Turnstile（與既有 Cloudflare 網域 / DNS 生態一致、隱私友善）。
  - 套用於登入、註冊、忘記密碼，並作為登入失敗的升級驗證。
- **傳輸與前端安全**：
  - CSP（Content-Security-Policy）：限制可載入來源以降低 XSS 風險；因 token 存於 localStorage，此項尤其關鍵。
  - HSTS：強制全站走 HTTPS。
  - 跨子網域 CORS：明定各子網域（`auth` / `workspace` / `creator` / `market`）間的允許來源與憑證攜帶政策。

### Session 與 Token 策略

- **JWT 失效機制**：採「短效 access token + refresh token rotation」。
  - access token 設為短效（建議 5～15 分鐘），靠自然過期失效，不查撤銷清單。
  - refresh token 每次使用即輪替（rotation），舊 token 隨即作廢。
  - 取捨：登出後 access token 最多有數分鐘空窗期仍有效（可接受）。
  - refresh token 設絕對上限壽命與閒置逾時，逾時自動失效。
  - 以下事件會撤銷該用戶所有 refresh token：修改密碼、修改信箱、開啟 / 變更 2FA、遭管理員停權。
- **前端 SPA token 存放**：access token 放 memory、refresh token 放 HttpOnly + Secure + SameSite cookie（scope 限 token endpoint）。
  - cookie 不被 JS 讀取，搭配 refresh token rotation，XSS 無法竊取長效憑證。
  - refresh endpoint 需有 CSRF 防護（SameSite + rotation 為主要防線）。

### API Service 驗證

- 各 API service 在本地以 Hydra jwks 驗證 JWT。
- Permission 向 Redis Cache 取得。
- Auth Service 負責更新 Cache；當 Cache 無資料時提供 fallback。

### 日誌（Log）

- **Audit Log**：
  - 須考量資料量龐大時的查詢效能瓶頸（見 [[Log]]）。
  - 稽核事件清單：登入成功、登入失敗、登出、註冊、修改密碼、重置密碼、修改信箱、角色 / 權限變更、停權 / 解除停權、刪除帳號。
- **Application Log**：使用 Loki；正式部署等級為 warn、error 以上。

### 店面子網域 OIDC redirect URI 註冊

Hydra 不支援萬用字元 redirect URI，creator-web 店面（`<slug>.openjam.co`）的 `callback.html` / `silent-renew.html` / post-logout URI 須逐店列入 `open-jam-web` client 白名單：

- Auth 消費 `StoreProvisionedEvent`（[[Store]] 開店核准時發出），由 `StorefrontRedirectService` 註冊。
- Hydra client 更新為整包 read-modify-write，以 PostgreSQL advisory lock（`pg_advisory_xact_lock`）序列化防 lost update；缺漏才追加、冪等。
- URL 樣式取自 `App:StorefrontUrlPattern`（`{storeSlug}` 佔位符）、client id 取自 `Hydra:WebClientId`；存量店家由 Bootstrap `StorefrontRedirectSeeder` 回填（見 [[Bootstrap]]）。

### 使用者列表 REST API

- `GET /v1/users`（Admin）：全平台使用者分頁列表（`UsersController`），供 workspace-web 管理員後台會員列表。Auth 雖為 MVC 服務，此端點為 REST API，掛 JWT Bearer 驗證與 `Admin` role。

### 事件（Outbox）

- **Audit Log Message**：寫入 Outbox Table，由 LogService Consumer 消化。
- **Send Email Message**：寫入 Outbox Table，由 EmailService Consumer 消化。
- **`UserRegisteredEvent`**：註冊成功發出，供 StoreService / NotificationService 依 Email 回填訪客追蹤者的 `UserId`（見 [[Store]]、[[Notification]]）。

### Email

- **Email Service 流程**：
  - 註冊完成後寫入 Outbox，使 create user 動作與帳號通知信落在同一個 transaction。
  - 忘記密碼表單送出後，將重置密碼信寫入 Outbox。
  - Auth 以排程掃描 Outbox 資料表並寫入 message queue。
  - Email Service 的 Consumer 消化 message。
  - 將發信結果記錄至 email record。
  - 須確保 queue 資料持久化。
- **Email Template**：
  - 帳號開通
  - 重置密碼
  - 密碼已變更通知
  - 信箱變更確認（新信箱）/ 變更通知（舊信箱）
  - 新裝置 / 異常登入提醒
  - 帳號鎖定 / 停權通知

### Log Service

- 執行登入、登出、註冊等動作時記錄 Outbox audit log 資料；Auth 以排程掃描 Outbox 寫入 message queue，交由 Log Service consume。

### Seeder

- Auth 相關的預建資料（Hydra Client / Web client `open-jam-web`、Service client `open-jam-service`、RBAC 角色與權限、初始 Admin 帳號）由 [[Bootstrap]] 統一協調與紀錄。

### 多國語系

- 後端採 .resx 資源檔。
