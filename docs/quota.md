# 資源配額 Quota

QuotaService 負責**租戶（creator）資源配額的計量、預扣與回滾**。配額額度依創作者方案而定（見 [[Product]]），在簽發上傳 signed URL 前原子地檢查並預扣，與 [[Storage]] 的上傳流程銜接，確保並發下不超額。

本頁聚焦資源配額；租戶識別（JWT claim）見 [[Auth]]、子網域路由見 [[Infra]]，不在本頁範圍。

## 計量資源

- **儲存空間**：帳號總用量（bytes）—— 預扣 / 回滾的核心對象。
- **商品數量**：上架商品數上限（原子計數器）。
- **單檔上限 / 單商品總量上限**：上傳當下即時檢查（不需維護預扣狀態）。
- 配額額度來源：創作者方案（[[Product]]）；方案變更即更新額度。

## 用量資料模型

- **租戶用量（tenant usage）**：`tenant_id`、`quota`（依方案）、`used`（bytes）、`reserved`（bytes）、`product_count`、`updated_at`。
- **預扣紀錄（reservation）**：`id`、`tenant_id`、`size`、`status`（reserved / committed / released）、`expiry`、`created_at`。

## 配額檢查與預扣流程（上傳）

1. 功能 API / [[Storage]] 在簽發 upload signed URL **之前**，向 QuotaService 請求預扣 S bytes。
2. **原子條件式更新**（見下），通過才建立 reservation；reservation 有效期**對齊 signed URL 時效**。
3. 上傳成功（[[Storage]] ready 事件）→ **commit**：`used += S`、`reserved -= S`，reservation 標記 committed。
4. 上傳失敗 / 取消 / 逾時 → **release**：`reserved -= S`，reservation 標記 released。
5. **sweeper**：排程定期回滾過期仍未 commit 的 reservation，釋放卡住的預扣。

## Atomic 與一致性

- **DB 原子條件式更新為準**：

  ```
  UPDATE tenant_usage
  SET reserved = reserved + S
  WHERE tenant_id = T AND used + reserved + S <= quota
  ```

  以影響行數判斷成敗，無需顯式鎖，天然防並發超額。
- 商品數量上限以同樣的原子條件式增減。
- **Redis 僅作快取顯示用量**（後台呈現用），不作為授權 / 配額判斷依據。

## 統計量維護與對帳

- **跨動計數器**：commit / release 即時增減 `used` / `reserved`，查詢快速。
- **每日對帳**：排程加總實際檔案（[[Storage]]）校正 `used`，修正計數器漂移。
- 不一致時以實際用量為準修正。

## 方案降級

- 降級導致 `used > quota` 時：**禁止新上傳**（預扣檢查必然失敗），但**保留既有檔案不刪**。
- 由創作者自行清理或重新升級後恢復上傳。

## 技術與架構

- .NET 微服務，整體結構慣例見 [[Develop]]。
- 跨服務關聯：上傳流程 [[Storage]]、方案與配額 [[Product]]、租戶識別 [[Auth]]、稽核 [[Log]]。
