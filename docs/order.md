# 訂單 Order

本頁定義**消費者端的購買與金流**。金流採 **Stripe**：以 **Connect Express** 讓創作者收款並由平台抽成，結帳用 **Payment Intent + 自訂 UI**。涵蓋購買流程、訂單狀態、下載授權（entitlement）、退款、免費追蹤，以及 guest 註冊後的歷史回溯。

## 收款架構（Stripe Connect）

- **Connect Express**：每位創作者擁有各自的 connected account 收款；Stripe 託管其 onboarding 與提領（創作者的提領 / 收款設定屬敏感操作，見 [[Auth]]）。
- **平台抽成**：建立 PaymentIntent 時帶 `application_fee_amount` + `transfer_data.destination`（創作者帳戶），平台抽成、餘額入創作者帳戶。
- **提領（payout）**：由 Stripe 處理 payout 至創作者帳戶。
- **稅務**：整合 **Stripe Tax** 自動計算各地稅負。

## 購買流程

- **結帳**：Payment Intent + 自訂 UI（Stripe Elements），支援登入與未登入（guest）結帳。
- **定價**：依 [[Product]] 的固定價 / 免費 / 折扣碼；結帳時套用折扣碼。
- **免費商品**：憑信箱領取，不需付款流程。
- **履約時機**：以 Stripe **webhook（`payment_intent.succeeded`）為唯一事實來源**，確認後才產生 entitlement 並寄送訂單確認信（[[Email]]）；前端僅顯示結果。
- **成功路徑**：成功頁提供 order id 與下載連結 + 寄送訂單確認信。

## 訂單狀態機

- `pending`（待付款）→ `paid`（已付款）→ `fulfilled`（已履約 / 可下載）→ `refunded`（已退款）/ `failed`（付款失敗）。

## Stripe Webhook

- Webhook 為金流事實來源，處理 `payment_intent.succeeded`、`payment_intent.payment_failed`、`charge.refunded`、Connect 帳戶事件等。
- **冪等**：以 Stripe event id 去重，避免重複 webhook 造成重複履約（與 [[Email]] 冪等同理）。

## 下載授權（Entitlement）

- 付款成功（webhook 確認）後產生 entitlement。
- **範圍：永久下載**——只要商品存在即可重複下載；已售出商品的下載權保留呼應 [[Storage]] / [[Product]] / [[Auth]]。
- **關聯**：以 `user_id`（FK）為主，guest 階段以 email 記錄（資料模型已於 [[Auth]] 定）。
- [[Storage]] 依 entitlement 簽發 signed URL / signed cookie。
- 退款時撤銷 entitlement。

## 退款

- 提供**退款窗口**（如購買後 X 天內可退）。
- 退款經 Stripe 處理（含 application fee 的處理），完成後撤銷 entitlement。

## Guest 購買的信箱問題

未登入購買可能付款成功但信箱輸錯 / 沒保存成功頁，採三路徑兜底：

1. **成功頁**：提供 order id 與下載連結。
2. **訂單確認信**：寄至結帳信箱，可憑信件查詢。
3. **客服追溯**：若成功頁未保存且信箱輸錯，由客服憑 Stripe 交易（卡號等）追溯訂單。

## 免費追蹤（關注 + 新品通知）

- 消費者可**憑信箱追蹤創作者**，於新品 / 動態時寄信通知（[[Email]]），不涉付費。
- 需判斷信箱是否合法、是否重複追蹤、如何取消追蹤（退訂）。
- 登入狀態下預填該用戶信箱，但允許修改。

## Guest → 帳號回溯（Mapping）

- 用戶憑信箱註冊後，以該信箱查找歷史**訂單**與**追蹤**紀錄，掛到其 `user_id`。
- 資料模型原則：`user_id` 為主、email 僅作 guest → 帳號 的初次對應鍵（見 [[Auth]]）。

## 技術與架構

- .NET 微服務，整體結構慣例見 [[Develop]]。
- 跨服務關聯：下載 [[Storage]]、商品與定價 / 折扣 [[Product]]、身份與提領設定 [[Auth]]、訂單信 / 追蹤通知 [[Email]]、稽核 [[Log]]、配額 [[Quota]]。
