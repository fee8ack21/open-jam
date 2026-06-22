# 電子郵件服務 EmailService

EmailService 是平台的**共用寄信服務**。各 API service（[[Auth]]、[[Order]] 等）將要寄的信透過 **Outbox → RabbitMQ** 投遞，由 EmailService 統一渲染模板、寄送、記錄結果並處理失敗重試。寄送層以 `IEmailSender` 抽象封裝，正式環境接 SendGrid、地端開發用 SMTP catcher。

## 服務範圍

- **交易信（MVP）**：一對一、事件觸發的信（帳號開通、重置密碼、訂單確認等）。
- **大量群發（bulk，預留）**：創作者 → 追蹤者的群發（見 [[Catalog]] 訂閱 / 追蹤）。MVP 不實作，但於架構上預留批次與退訂管理擴充點。

> **寄送層以 `IEmailSender` 抽象隔離**：正式環境接 SendGrid（支援退信 webhook、投訴回報，適合 bulk 擴充），地端開發改接 SMTP catcher，兩者切換不影響其他邏輯。

## 處理流程

1. 來源服務在自身業務 transaction 內寫入 **Outbox**（確保業務動作與寄信意圖原子一致，見 [[Auth]]）。
2. 來源服務以排程掃描 Outbox，寫入 **RabbitMQ**。
3. EmailService 的 **Consumer（MassTransit）** 收到 email message。
4. **冪等去重**：以業務維度鍵將一筆 `EmailRecord` claim 插入（撞 unique 即代表寄過 → 跳過並照樣回報成功）。
5. 依訊息攜帶的**模板類型 + locale** 取對應模板，套入資料 payload 渲染。
6. 透過 `IEmailSender` 寄出（正式為 SendGrid）。
7. 更新 `EmailRecord` 結果（成功 / 失敗、provider 訊息 id、錯誤等）。
8. 向 RabbitMQ 回報 ack。

## 訊息合約（Email Message）

放進 MQ 的訊息只帶資料、不帶渲染後內容，由 EmailService 集中渲染：

- 模板類型（enum）
- locale（由上游發信時攜帶使用者偏好語系）
- 收件人
- 資料 payload（渲染模板所需的變數）
- 業務維度去重鍵（收件人 + 模板 + 來源業務 id）

## 寄送（IEmailSender 抽象）

- 以 `IEmailSender` 介面封裝寄送，實作可替換。
- **正式環境**：SendGrid API，寄件位址 `noreply@openjam.co`。API Key 以 GCP Secret Manager 注入，支援退信（bounce）/ 投訴（complaint）webhook。
- **地端開發**：SMTP catcher（如 Mailpit），不實際外送。
- 寄件網域需於 Cloudflare DNS 設定 **SPF / DKIM / DMARC**（含 SendGrid 的 CNAME 網域驗證記錄）以確保送達率（見 [[Infra]]）。
- **收信路由**：`openjam.co` 網域收到的信（如 `support@openjam.co`）以 **Cloudflare Email Routing** 轉發至內部信箱，與寄出路徑獨立。

## 模板與多國語系

- 信件模板集中存放於 **EmailService**（上游不持有模板）。
- 採**每語系獨立模板檔**（例如 `zh-TW/account-activation.html`、`en/account-activation.html`）。
- 渲染時依訊息攜帶的 locale 選用對應語系模板。
- 模板清單（隨各服務需求擴充）：
  - 帳號開通
  - 重置密碼
  - 密碼已變更通知
  - 信箱變更確認（新信箱）/ 變更通知（舊信箱）
  - 新裝置 / 異常登入提醒
  - 帳號鎖定 / 停權通知
  - 訂單確認信（含 order id 與下載連結，見 [[Order]]）
  - 追蹤新品 / 動態通知（含退訂連結，見 [[Order]]）
  - （其他服務的交易信日後再擴充）

## 冪等去重

- Outbox + MQ 為 **at-least-once** 投遞，同一封信的訊息可能被處理多次（consumer ack 前重啟、Outbox 搬運重複、MassTransit 重試），不防重就會重複寄信。
- 採**業務維度去重鍵**：收件人 + 模板類型 + 來源業務 id（如某筆 order id、某次重置請求 id）。
- 於 `EmailRecord` 對去重鍵建 **unique 約束**。
- **先寫 claim 再寄**：收到訊息先以去重鍵插入 EmailRecord（撞鍵即跳過），再寄信、最後更新結果，避免並發重寄。

## 重試與失敗處理

- **MassTransit 原生重試**：暫時性失敗（連線逾時等）採即時 + 延遲重遞（指數退避）。
- 超過重試上限的訊息進入 **error queue**，並將該 `EmailRecord` 標記為失敗。
- **排程補償**：另建排程定期掃描 `EmailRecord` 中的失敗件並重寄，作為雙保險。
- RabbitMQ queue 須設為持久化（durable queue + persistent message），確保 queue 資料不因重啟遺失。

## EmailRecord（資料表）

記錄每封信的寄送結果，欄位至少包含：

- 去重鍵（unique）
- 收件人
- 模板類型、locale
- 狀態（pending / sent / failed）
- provider 訊息 id
- 嘗試次數
- 最後錯誤訊息
- 來源業務 id / correlation id
- 建立時間、更新時間

## 退信 / 投訴 / 退訂（預留）

- SendGrid 提供退信（bounce）/ 投訴（complaint）webhook，可串接退訂與黑名單處理，MVP 階段先預留實作。
- bulk / 訂閱信需支援 `List-Unsubscribe` 標頭與退訂管理（對應 [[Catalog]] 訂閱 / 追蹤），隨 bulk 功能一併實作。

## 技術與架構

- .NET 微服務，整體結構慣例見 [[Develop]]。
- 訊息傳遞採 **RabbitMQ + MassTransit**（跨服務共用基建）。
- 角色以 **Consumer + 排程**為主（消化訊息、補償重試）。
- Audit / Application Log 串接見 [[Log]]。
