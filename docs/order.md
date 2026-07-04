# 訂單與金流 Order

本頁定義**消費者端的購買與金流**，對應 `src/OrderService/` 與 `src/PaymentService/` 兩個微服務：OrderService 管理訂單、結帳入口與狀態歷程；PaymentService 封裝 Stripe Checkout 與 Webhook。消費者免註冊，憑信箱即可下單；登入者由 JWT `sub` 帶入 `BuyerUserId`。

## 結帳流程（單一入口）

結帳只有一個入口 `POST /v1/orders`，前端不直接接觸 PaymentService：

```
creator-web 結帳頁
  1. POST /v1/orders ──▶ OrderService：建立訂單（Pending）
  2. OrderService ──service token──▶ PaymentService POST /v1/payments/checkout-session
       （"InternalService" policy，僅限內部服務呼叫）
  3. PaymentService 建立 Stripe Checkout Session（同訂單未過期 Pending 付款直接重用）
  4. OrderResponse.CheckoutUrl 隨建單回應 ──▶ 前端導向 Stripe 付款頁
  5. Stripe webhook ──▶ PaymentService：Succeeded + 發 PaymentSucceededEvent
  6. OrderService PaymentSucceededConsumer ──▶ 訂單 Completed（冪等）+ 發 OrderCompletedEvent
```

- Checkout Session 建立失敗時訂單保留 `Pending`，可重試。
- 服務間認證走 Hydra `client_credentials`（`ServiceTokenClient`，client 為 Bootstrap seed 的 `open-jam-service`），見 [[Develop]]。
- 付款成功 / 取消由 Stripe 導回前端 `SuccessUrl` / `CancelUrl`（checkout result 頁）。

## 訂單狀態機（OrderService）

- `Pending`（待付款）→ `Paid`（已付款）→ `Completed`（已完成 / 可下載），另有 `Cancelled` / `Refunded`。
- 數位商品付款成功即一次轉移為 `Completed` 並填 `CompletedAt`；`Paid` 保留供未來人工審核流程。
- 每次狀態轉移寫一筆 `OrderStatusHistory`（append-only，`OldStatus` 為 null 表建立）。
- 訂單編號由 `OrderNumberGenerator` 產生（格式 `OJ-yyyyMMdd-XXXXXXXX`，人類可讀且全域唯一）。
- 賣方歸屬：`Order.StoreId` 標記訂單所屬商店，一張訂單對應單一商店。

## 付款狀態機（PaymentService）

- `Payment`：`Pending → Succeeded` / `Failed` / `Expired`；每次轉移寫一筆 `PaymentTransaction`（`Created` / `Success` / `Fail` / `Expired`，保留 Stripe raw payload）。
- **Webhook 兩段式處理**：`POST /v1/webhook/stripe` 驗簽後僅將原始事件落地 `ProviderEvent`（以 Stripe event id 去重）即回應，避免處理中服務掛掉遺失 webhook；`StripeWebhookProcessorService` 背景排程處理未完成事件：
  - `checkout.session.completed` / `async_payment_succeeded` → `Succeeded`，經 Outbox 發 `PaymentSucceededEvent`。
  - `checkout.session.async_payment_failed` / `payment_intent.payment_failed` → `Failed`。
  - `checkout.session.expired` → 僅 `Pending` 轉 `Expired`。
- **冪等**：Stripe event id 去重 + 付款既有狀態判斷，重複 webhook 不重複履約。

## REST API 契約

### OrderService（開發 URL 5179）

| Method | Path | 用途 | 授權 |
|--------|------|------|------|
| `POST` | `/v1/orders` | 結帳建單（回 `CheckoutUrl`） | 公開（匿名憑 Email） |
| `GET`  | `/v1/orders/{id}` / `/v1/orders/by-number/{orderNumber}` | 查單筆訂單 | 公開（憑 ID / 訂單編號） |
| `GET`  | `/v1/orders/mine` | 買家視角列表 | 登入使用者 |
| `GET`  | `/v1/orders/store/{storeId}` | 賣家視角列表（先驗商店 Owner） | Owner |
| `GET`  | `/v1/orders` | 全平台列表（商店 / 買家 / 狀態過濾） | Admin |
| `GET`  | `/v1/orders/purchased/{catalogId}` | 是否已購買某商品（完成訂單） | 登入使用者 |
| `POST` | `/v1/orders/{id}/cancel` | 取消未付款訂單（具名買家僅本人；匿名憑訂單 ID） | 公開 |

### PaymentService（開發 URL 5178）

| Method | Path | 用途 | 授權 |
|--------|------|------|------|
| `POST` | `/v1/payments/checkout-session` | 以 OrderId 建 Stripe Checkout Session | `InternalService`（僅 OrderService） |
| `GET`  | `/v1/payments/{id}` | 查付款 | Admin |
| `POST` | `/v1/webhook/stripe` | Stripe webhook 接收 | Stripe 簽章 |

## 履約（下載授權）

- 不建 entitlement 表：CatalogService 於買家請求下載當下，以 `OrderServiceClient` 轉發 Bearer token 至 `GET /v1/orders/purchased/{catalogId}` **即時驗證購買紀錄**，通過才簽發短效下載 URL（`GET /v1/catalogs/{id}/versions/{versionId}/downloads`），見 [[Catalog]] / [[Storage]]。
- `OrderCompletedEvent` 由 CatalogService 消費累加商品銷量（`SalesCount`）。
- 已購買驗證同時供評論資格檢查（限已購買者評論）。

## Guest 購買的信箱問題

未登入購買可能付款成功但信箱輸錯 / 沒保存成功頁，兜底路徑：

1. **成功頁**：提供訂單編號與下載入口。
2. **憑訂單編號查詢**：`GET /v1/orders/by-number/{orderNumber}`。
3. **客服追溯**：由客服憑 Stripe 交易（`PaymentTransaction` 保留 provider id 與 raw payload）追溯訂單。

## Guest → 帳號回溯（Mapping）

- 用戶憑信箱註冊後（`UserRegisteredEvent`），追蹤紀錄由 StoreService / NotificationService 回填 `UserId`（已實作）；歷史**訂單**回溯掛帳為未來工作。

## 稽核

- OrderService 經 Outbox 發 `AuditLogRequestedEvent`（`order.create` / `order.cancel` / `order.complete`）；PaymentService 同（`payment.checkout.created` 等），見 [[Log]]。

## 未來工作（Roadmap）

- **Stripe Connect Express**：創作者各自 connected account 收款、平台以 `application_fee_amount` 抽成、Stripe 託管 onboarding 與 payout；**Stripe Tax** 稅務。目前 MVP 收款進平台帳戶，未分帳。
- **退款**：退款窗口、經 Stripe 退款並撤銷下載權（`Refunded` 狀態已預留）。
- **折扣碼 / 免費商品領取**：定價進階，見 [[Catalog]]。
- **Guest 歷史訂單回溯**：註冊後以信箱掛帳歷史訂單。

## 技術與架構

- .NET 微服務，整體結構慣例見 [[Develop]]。
- 跨服務關聯：下載與銷量 [[Catalog]]、檔案簽發 [[Storage]]、身份 [[Auth]]、訂單信 [[Email]]、稽核 [[Log]]、追蹤通知 [[Notification]]。
