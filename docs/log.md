# 日誌 Log

平台有兩種用途完全不同的日誌：**Application Log**（給工程師除錯 / 維運的技術日誌）與 **Audit Log**（給合規 / 安全 / 追溯用的業務事件紀錄）。兩者在讀者、保留期、儲存與特性上都不同，分別設計。

| | Application Log | Audit Log |
|---|---|---|
| 目的 | 除錯、維運、監控 | 追溯「誰在何時對什麼做了什麼」|
| 讀者 | 開發 / SRE | 管理員、用戶、創作者 |
| 保留 | 30 天 | 3 年以上 |
| 特性 | 量大、可遺失、技術性 | 不可竄改、需精準查詢、與業務動作一致 |
| 儲存 | Loki | LogService + 關聯式 DB（分表分庫）|

## Application Log

### 蒐集與儲存

```
各服務 stdout（結構化 JSON）→ Promtail → Loki → Grafana（查詢 / 儀表板 / 告警）
```

- 正式環境記錄 warn / error 以上；開發環境可放寬至 info / debug。
- 每筆日誌帶 **correlation / trace id**，可跨微服務串讀同一請求。
- **嚴禁記錄敏感資料**（密碼、token、完整卡號等）。
- 保留期限：**30 天**。

### 分散式追蹤

- 導入 **OpenTelemetry**，trace id 跨服務傳遞（W3C traceparent）。
- 搭配 **Grafana Tempo** 檢視完整調用鏈，強化微服務除錯。

### 告警

- 以 **Grafana 告警**監看 error 率 / 關鍵錯誤，觸發後接通知管道。

## Audit Log

### 寫入流程（Outbox）

與 [[Auth]]、[[Email]] 一致的 Outbox 模式，確保「業務動作成功」與「稽核紀錄」原子一致：

```
各服務在業務 transaction 內寫 Outbox → 排程 → RabbitMQ → LogService Consumer → 寫入 audit 儲存
```

### 事件 Schema

每筆稽核事件至少包含：

- `who`：操作者（user id / 系統）
- `action`：動作類型
- `target`：對象 / 資源（含 id）
- `result`：成功 / 失敗
- `before / after`：變更前後（適用於修改類動作）
- `ip` / `user-agent`
- `tenant`：所屬租戶（creator）
- `timestamp`
- `correlation id`

### 事件清單

- 安全 / 帳號類事件清單已於 [[Auth]] 定義（登入成功 / 失敗、登出、註冊、改密、重置、改信箱、角色變更、停權、刪除帳號）。
- 其他服務（[[Order]]、[[Catalog]]）的業務事件日後擴充。

### 儲存與索引

- 儲存於 **LogService + 關聯式 DB**。
- MVP 先以**索引**支撐查詢，建立**時間**與**租戶（creator）**雙維度索引，不做分表分庫。
- 資料量大時再評估分表分庫（見 [[Develop]]）。

### 查詢權限

- **管理員**：可查全平台稽核紀錄。
- **用戶**：可查自己帳號的安全活動（呼應 [[Auth]] 裝置管理 / 異常登入偵測）。
- **創作者**：可查自己店鋪的操作紀錄。

### 保留與不可竄改

- 保留期限：**3 年以上**（涉金流，偏向長期追溯）。
- **Append-only**：僅允許 insert，不開放 update / delete（應用層 + DB 權限雙重限制）。

## 技術與架構

- .NET 微服務（LogService），整體結構慣例見 [[Develop]]。
- 事件傳遞採 **RabbitMQ + MassTransit**（與 [[Email]]、[[Storage]] 一致的共用基建）。
- 可觀測性堆疊：Loki（logs）+ Tempo（traces）+ Grafana（查詢 / 儀表板 / 告警），部署見 [[Infra]]。
