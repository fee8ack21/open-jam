_

# Open Jam

一個面向創作者的數位商品上架與販售平台，參考 [Gumroad](https://gumroad.com)。

## 這是什麼

Open Jam 讓創作者能快速擁有自己的線上商店，上架並販售數位商品（影片、圖片、PDF 等），無須自行處理金流、檔案託管、會員與信件等基礎建設。買家可在不註冊的情況下憑信箱完成購買、訂閱與追蹤，降低消費門檻。

- **網域**：openjam.co
- **定位**：創作者數位商品 SaaS（multi-tenant）
- **商業模式**：平台抽成 + 創作者訂閱方案（依方案給予不同的儲存空間與檔案上限）

## 目標使用者

- **創作者（Creator）**：在自己的子網域 `<creator>.openjam.co` 經營商店、上架商品、管理訂單與訂閱者。
- **消費者（Consumer）**：瀏覽、購買數位商品；可未登入憑信箱購買、訂閱、追蹤，日後註冊時自動回溯歷史訂單與訂閱。

## 核心功能

- **多租戶商店**：子網域隔離、店面 Banner、商品分類與標籤。
- **數位商品販售**：定價、多檔案格式（影片 HLS、圖片、PDF）、簽章直傳、Malware Scan、CDN 加速。
- **金流**：Stripe 串接，支援未登入憑信箱購買，付款成功後提供 Order ID／下載連結與信件查詢雙路徑。
- **訂閱與追蹤**：憑信箱訂閱創作者、合法性與重複性檢查、取消訂閱。
- **帳號系統**：完整註冊／登入／忘記密碼／鎖定流程，OIDC（Hydra）。
- **資源配額**：依方案限制單檔、單一商品與帳號總儲存空間，避免單一用戶吃光雲端空間。
- **內容治理**：不宜內容檢舉。

## 站點

| 站點 | 用途 |
|------|------|
| `openjam.co` | 平台探索頁（market-web） |
| `auth.openjam.co` | 登入／登出（Auth） |
| `workspace.openjam.co` | 用戶後台（workspace-web） |
| `<creator>.openjam.co` | 創作者商品空間（creator-web） |

## 技術概覽

- **後端**：.NET (C#) 微服務，CQRS + Mediator + 三層架構。
- **前端**：Vue SPA。
- **資料／基礎建設**：Cloud SQL、MinIO／Google Cloud Storage、Redis、Message Queue（Outbox pattern）。
- **部署**：GKE、Argo CD、Helm Umbrella Chart、Cloudflare DNS。
- **可觀測性**：Loki + Promtail（Application Log）、Audit Log。

## 規劃筆記

- [[Auth]] — 認證授權（註冊／登入／忘密／OIDC／資安）
- [[Product]] — 商品與商店規格
- [[Order]] — 訂單與金流（Stripe）
- [[Storage]] — 檔案儲存、HLS、安全
- [[Email]] — 信件服務與重試
- [[Quota]] — 資源配額計量與統計
- [[Log]] — Audit Log／Application Log
- [[Infra]] — 網域、站點、雲端資源、部署
- [[Bootstrap]] — 平台運行所需預建資料
- [[Develop]] — 開發慣例、專案結構、實戰情境
