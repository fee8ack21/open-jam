# 基礎架構 Infra

本頁描述程式之外的基礎架構：網域、叢集、資料層、CDN、CI/CD、憑證、機密與環境。核心運算在 **GKE**，資料庫用 **Cloud SQL**，物件儲存用 **GCS**，邊緣與網域走 **Cloudflare**。

## 網域與邊緣（Cloudflare）

- 於 Cloudflare 承租網域 **openjam.co**。
- Cloudflare DNS 設定，含 **wildcard `*.openjam.co`**（對應動態的創作者子網域）。
- Cloudflare Turnstile 作為 CAPTCHA（見 [[Auth]]）。

## 站點

| 站點 | 用途 | 前端 |
|------|------|------|
| `openjam.co` | 平台探索頁 | market-web |
| `auth.openjam.co` | 登入 / 登出 | （[[Auth]]）|
| `workspace.openjam.co` | 用戶後台 | workspace-web |
| `<creator>.openjam.co` | 創作者商品空間 | creator-web |

## 運算與叢集（GKE）

- 所有微服務（[[Auth]]、EmailService、StorageService、LogService、功能 API）與前端部署於 GKE。
- **Ingress / Gateway**：以 wildcard host 路由，`<creator>.openjam.co` 導向 creator-web。
- 以 HPA 自動擴縮，依負載規劃 node pool。

## 資料層

### Cloud SQL（PostgreSQL）

- **每服務獨立 database**（同一 Cloud SQL 實例下多個 database），兼顧成本與隔離。
- 前置 **PgBouncer 連線池**，解決大量 pod 連線耗盡 max connection 的問題。
- 透過 private IP + Cloud SQL Auth Proxy 連線。
- 啟用**自動備份 + PITR（時間點還原）**。

### Redis（GKE 自建）

- 部署於 GKE，PVC 持久化。
- 用途：[[Auth]] permission cache 等。

### RabbitMQ（GKE 自建）

- 部署於 GKE，PVC 持久化（durable queue + persistent message）。
- 用途：[[Email]]、[[Storage]]、[[Log]] 的事件傳遞（MassTransit）。

## 物件儲存與 CDN

- **GCS**（雲端）+ 地端 **MinIO**（見 [[Storage]]）。
- **GCP Cloud CDN**：與 GCS 原生整合，原生支援 **signed URL / signed cookie**（對應 [[Storage]] 付費內容與 HLS 串流授權）。

## 平台服務元件

- **Hydra**（OIDC server，見 [[Auth]]）：部署於 GKE。
- **GCP Transcoder API**：影片轉碼 HLS（見 [[Storage]]）。
- **ClamAV**：檔案掃毒 worker，部署於 GKE（見 [[Storage]]）。

## 可觀測性

- **Loki + Promtail**（logs）、**Tempo**（traces）、**Grafana**（查詢 / 儀表板 / 告警），部署於 GKE，詳見 [[Log]]。

## CI / CD

- **CI**：GitHub Actions 建置映像，推送至 **GCP Artifact Registry**。
- **CD**：**Argo CD**（GitOps）+ **Helm Umbrella Chart**。
- 流程：CI build / push image → 更新 git 中的 manifest / Helm values → Argo CD 自動同步至 GKE。

## TLS 憑證

- **cert-manager + Let's Encrypt**，透過 Cloudflare **DNS-01** challenge 自動簽發 / 續期。
- 簽發 **wildcard `*.openjam.co`** 憑證以涵蓋創作者子網域。

## Secrets 管理

- 機密集中存於 **GCP Secret Manager**。
- **External Secrets Operator** 同步至 GKE，集中、可稽核、避免明文進 git。

## 環境

- **地端開發**：docker-compose 起本機依賴（MinIO、SMTP catcher、RabbitMQ、Redis、PostgreSQL 等，見 [[Develop]]）。
- **正式（prod）**：GKE。
- （MVP 不設獨立 staging，先以地端 + prod 兩套。）

## 備份與災難復原（DR）

- Cloud SQL：自動備份 + PITR。
- GCS：開啟版本控管。

## 成本

- 每月流量評估。
- 成本計算（GKE 資源、Cloud SQL、GCS / Cloud CDN、Transcoder 用量）。
- 規格評估（node 規格、DB 規格）。
