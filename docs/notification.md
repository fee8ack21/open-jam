# 通知 Notification

NotificationService（`src/NotificationService/`）負責**追蹤者通知**：商品上架與商店公告，透過 **Email + 站內（in-app）通知**雙管道送達。REST API + RabbitMQ Consumer，管理通知任務（`NotificationRequest`）與 in-app 通知（`Notification`）。

## 統一排程管線

所有通知——不論即時或預定日期——皆為一筆 `NotificationRequest`：

- `ScheduledAt` 為預定發送時間，即時通知 = 建立當下。
- 狀態機：`Pending → Dispatched`，另有 `Cancelled`（僅 `Pending` 可取消）/ `Failed`（重試達上限）。
- `NotificationDispatcherService`（`IHostedService`）掃描到期任務執行 fan-out：**整個 fan-out 包在單一交易**並以 `FOR UPDATE SKIP LOCKED` 持鎖，失敗整體回滾、下一輪重試；`AttemptCount` 達上限（預設 5）轉 `Failed`。

## 通知類型

| 類型 | 觸發 | 說明 |
|------|------|------|
| `catalog.published` | 消費 `CatalogPublishedEvent` | 僅 `IsFirstPublish`（首次上架）建任務，重新上架不重複通知 |
| `store.announcement` | Owner 經 `POST /v1/notification-requests` 建立 | 可帶 `ScheduledAt` 預定發送（workspace-web 公告排程後台） |

## Fan-out 對象（追蹤者 Ref Table）

- 本地 `store_follower_ref` 參照表（微服務 Ref Table，見 [[Develop]]），資料來源：
  - `StoreFollowerChangedEvent`（StoreService Follow / Unfollow 時發出）同步增刪。
  - `UserRegisteredEvent`（Auth 註冊時發出）回填訪客追蹤者的 `UserId`（信箱一律小寫比對）。
  - Bootstrap `StoreFollowerRefSeeder` 可全量回填（重跑冪等）。
- **已關聯帳號**的追蹤者：建 in-app `Notification`（`(RequestId, RecipientEmail)` 唯一索引防重）+ Email。
- **訪客追蹤者**（未註冊）：只收 Email。
- Email 經 Outbox 發 `EmailRequestedEvent`（模板 `notification.catalog_published` / `notification.store_announcement`）由 [[Email]] 寄送；信中商品連結由 `Notification:CatalogUrlPattern` 樣板組出。

## REST API 契約

開發 URL 5180，Swagger 於 `/swagger`。皆以 JWT `sub` 限縮。

### in-app 通知（Notifications）

| Method | Path | 用途 |
|--------|------|------|
| `GET`  | `/v1/notifications/mine` | 自己的通知（分頁 + `unreadOnly`） |
| `GET`  | `/v1/notifications/mine/unread-count` | 未讀數（前端鈴鐺輪詢） |
| `POST` | `/v1/notifications/{id}/read` | 標記已讀 |
| `POST` | `/v1/notifications/read-all` | 全部已讀 |

### 通知任務（NotificationRequests，Owner）

| Method | Path | 用途 |
|--------|------|------|
| `POST` | `/v1/notification-requests` | 建立公告通知（可帶 `ScheduledAt`） |
| `GET`  | `/v1/notification-requests` | 自己商店的任務列表 |
| `DELETE` | `/v1/notification-requests/{id}` | 取消（僅 `Pending`） |

## 跨服務與冪等

- **`StoreServiceClient`**（named `"store"`）：Owner 驗證（轉發 Bearer token 至 `GET /v1/stores/me`）+ dispatch 時匿名查商店公開資訊（`GET /v1/stores/{id}` 取店名 / slug 渲染通知，不另建 store ref 表）。
- **Consumer 冪等**：`CatalogPublishedConsumer` 以 `ProcessedEvent`（`OutboxMessageId` 唯一）+ `NotificationRequest.SourceEventId` 唯一索引雙層去重；追蹤同步與 UserId 回填為天然冪等 upsert / delete / update。

## 前端整合

- market-web / workspace-web 導覽列**通知鈴鐺**：輪詢 unread-count、下拉列表、點擊通知導向對應商品 / 商店頁。
- workspace-web **公告排程後台**：建立 / 排程 / 取消商店公告。

## 技術與架構

- .NET 微服務，整體結構慣例見 [[Develop]]。
- 跨服務關聯：追蹤者來源 [[Store]]、上架事件 [[Catalog]]、寄信 [[Email]]、身份 [[Auth]]。
