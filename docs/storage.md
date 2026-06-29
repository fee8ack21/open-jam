# 檔案儲存服務 StorageService

StorageService 負責平台數位商品檔案（影片、圖片、PDF）的**上傳、儲存、轉碼、掃毒、預覽與授權下載**。上傳與下載皆採**簽章直傳 / 直取**，避免功能 API 轉傳大檔吃緊記憶體；儲存後端以 `IStorageProvider` 抽象封裝，地端用本地檔案系統，雲端用 Google Cloud Storage。

## 服務職責

- 提供功能 API 簽發上傳 / 下載用的 signed URL。
- 接收直傳檔案，觸發**異步處理 pipeline**（掃毒 → 轉碼 / 預覽生成 → 標記 ready）。
- 授權下載：付費內容須經授權後才能取得。
- 檔案生命週期管理（軟刪除、orphan 清理）。

## 儲存後端（IStorageProvider 抽象）

- 以 `IStorageProvider` 介面封裝儲存操作，實作可替換。
- **地端開發**：本地檔案系統（`LocalStorageProvider`，blob URL 以 HMAC 簽章，實體檔案存於服務的 `Files/` 目錄）。
- **雲端正式**：Google Cloud Storage（GCS）。
- 支援格式：影片、圖片、PDF。

## 上傳流程（簽章直傳）

1. 前端向**功能 API** 請求上傳；功能 API **先檢查配額**（單檔 / 單商品 / 帳號總量上限，見 [[Catalog]]、[[Quota]]）。
2. 通過後簽發 **upload signed URL**（內含大小、content-type 限制與時效）。
3. 前端透過 signed URL **直傳** storage，不經功能 API 轉傳。
4. 大檔（影片）支援 **resumable 上傳**（GCS resumable / S3 multipart），斷線可續傳。
5. 上傳檔案須做**類型驗證**（magic-bytes 比對實際類型，非僅副檔名）與**大小上限**檢查。

## 上傳完成與異步處理 Pipeline

- **上傳完成確認**：
  - **地端 Local**：客戶端 PUT 直傳 `BlobController` → 寫入後直接觸發 `StorageEventService`，不依賴客戶端回報。
  - **雲端 GCS**：採 **confirm 端點**。前端透過 signed URL 直傳成功後，功能 API 呼叫 `POST /v1/files/{id}/confirm`；StorageService 以 `ObjectExistsAsync` 驗證物件確實存在後才觸發處理 pipeline。（原規劃的 GCS Pub/Sub bucket notification 列為未來選項，MVP 先以 confirm 端點實作，免額外 Pub/Sub 基建、且 Local/GCS 共用同一條觸發路徑。）
- 觸發後依序進行異步處理，全部完成才將檔案標記為 **ready（可售）**，並透過 **RabbitMQ + MassTransit** publish `FileReadyEvent` 通知功能 API：

### 掃毒（Malware Scan）

- 採 **ClamAV 自建**（GKE 上的 worker），由上傳事件觸發，在標記 ready 前掃描。
- 掃到惡意檔：拒絕、隔離 / 刪除並通知創作者，該檔不得 ready。

### 影片轉碼（HLS）

- 採 **GCP Transcoder API**，將原始影片轉成多畫質 **HLS（ABR）**。
- 轉碼為異步，需回報狀態，並與商品「處理中 → 可售」狀態銜接。

### 預覽生成

- 依「預覽機制」產出公開可預覽的衍生檔（見下）。

## 下載與授權

- 付費內容**不可公開存取**，下載須授權：
  1. 前端向功能 API 請求下載。
  2. 功能 API 驗證請求者是否擁有該商品（entitlement，見 [[Order]]）。
  3. 通過後簽發授權憑證：
     - **一般檔案**：短效 signed URL。
     - **HLS 串流**：signed cookie，一次涵蓋整個串流的多個 segment。
- **CDN**：透過 CDN 加速分發，signed URL / signed cookie 與 CDN 快取並存。

### 影片防盜

- MVP 採**僅 signed URL / cookie 存取控制**（不加密 segment）。
- 已知取捨：可防止未授權存取，但**無法防止下載後的轉發 / 盜錄**；若日後需更強保護再評估 HLS AES-128 加密或 DRM。

## 預覽機制（雙層）

- 檔案分為兩層：
  - **公開預覽衍生檔**：可未授權瀏覽（影片試看片段、PDF 前幾頁、預覽圖等）。
  - **付費完整檔**：須授權才能下載。
- 預覽內容由**創作者自行指定**（而非一律自動生成）。

## 檔案組織與生命週期

- **路徑 / bucket 結構**：以租戶（creator）維度劃分前綴；**原始檔**與**衍生檔**（HLS、預覽、縮圖）分開存放。雲端 GCS 採**雙 bucket**：公開讀取資產（`public/*`，如商店 Avatar/Banner、商品縮圖）存於 `open-jam-public`，私有付費檔存於 `open-jam-private`；`StorageOptions.BucketFor(key)` 依 key 前綴 `public/` 自動選 bucket。地端 Local 無 bucket 概念，僅以 `public/` 前綴區分匿名讀取。
- **軟刪除**：商品下架 / 刪除採軟刪除。
- **已售出保留下載權**：已購買的商品仍保留買家下載權（對應 [[Auth]] 的 GDPR 與帳號刪除策略）。
- **orphan 清理**：以排程清除無人購買且已軟刪的孤兒檔案。
- 帳號刪除（[[Auth]]）連帶的檔案處理依上述策略執行。

## 技術與架構

- .NET 微服務，整體結構慣例見 [[Develop]]。
- 事件傳遞採 **RabbitMQ + MassTransit**（與 [[Email]] 一致的共用基建）。
- 流量 / 頻寬成本評估見 [[Infra]]。
- Audit / Application Log 串接見 [[Log]]。
