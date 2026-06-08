# 基礎架構 Infra

本頁描述程式之外的基礎架構：網域、叢集、資料層、CDN、CI/CD、憑證、機密與環境。核心運算在 **GKE**，資料庫用 **Cloud SQL**，物件儲存用 **GCS**，邊緣與網域走 **Cloudflare**。

## 網域與邊緣（Cloudflare）

- 於 Cloudflare 承租網域 **openjam.co**。
- Cloudflare DNS 設定，含 **wildcard `*.openjam.co`**（對應動態的創作者子網域）。
- Cloudflare Turnstile 作為 CAPTCHA（見 [[Auth]]）。
- **Cloudflare Email Routing**：網域收信路由，將 `support@openjam.co` 等地址轉發至內部信箱（僅收信路由，寄出由 SendGrid 負責，見 [[Email]]）。

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

- **地端開發**：docker-compose 起本機依賴（MinIO、SMTP catcher（Mailpit，代替 SendGrid）、RabbitMQ、Redis、PostgreSQL 等，見 [[Develop]]）。
- **正式（prod）**：GKE。
- （MVP 不設獨立 staging，先以地端 + prod 兩套。）

## 備份與災難復原（DR）

- Cloud SQL：自動備份 + PITR。
- GCS：開啟版本控管。

## 成本

- 每月流量評估。
- 成本計算（GKE 資源、Cloud SQL、GCS / Cloud CDN、Transcoder 用量）。
- 規格評估（node 規格、DB 規格）。

## 部署流程（Runbook）

以下記錄正式環境（GKE）從零部署的可重現步驟，依目前已驗證可行的順序排列。

### 1. Cloudflare 購買網域並設定 DNS

1. 於 Cloudflare 購買網域 **openjam.co**。
2. 先建立暫定 DNS 記錄（待第 4 步取得 GCLB 外部 IP 後回填正確值）：
   - `openjam.co`（A）
   - `*.openjam.co`（A，wildcard，涵蓋 `auth` / `workspace` / `docs` / `<creator>` 等子網域）
3. **Proxy 狀態請設為「僅 DNS」（灰雲）**：cert-manager 走 Cloudflare DNS-01 challenge 簽發憑證、且 GKE Ingress 本身已處理 TLS，若開啟 Cloudflare Proxy（橘雲）會干擾驗證與憑證綁定。

### 2. 建立 GKE 叢集與全域靜態 IP

```bash
# 建立 GKE 叢集（依規格評估調整 node pool，省略細節）
gcloud container clusters create open-jam ...

# 建立全域靜態 IP，供 GCE Ingress 使用
gcloud compute addresses create open-jam-ip --global
```

### 3. 部署 open-jam 應用 chart

```bash
helm upgrade --install open-jam infra/helm/open-jam `
  --namespace open-jam `
  --create-namespace `
  -f infra/helm/open-jam/values.prod.yaml
```

> `infra` chart 的 Ingress 直接以 Service 名稱（`open-jam-<component>`）指向本 release 建立的 backend，故須先部署本 chart。

### 4. 安裝 cert-manager

`infra` chart 的 `ClusterIssuer` / `Certificate` 依賴叢集已安裝 cert-manager（含 CRDs）。以官方 Helm chart 安裝至獨立的 `cert-manager` namespace（與應用 release 分開管理生命週期）：

```bash
helm repo add jetstack https://charts.jetstack.io
helm repo update jetstack

helm install cert-manager jetstack/cert-manager `
  --namespace cert-manager `
  --create-namespace `
  --version v1.20.2 `
  --set crds.enabled=true
```

> 若叢集中已殘留同名孤兒 CRD / webhook（例如 namespace 曾被直接刪除而非 `helm uninstall`），只要沿用相同的 release 名稱（`cert-manager`）與 namespace（`cert-manager`），Helm 會辨識既有資源上的 `meta.helm.sh/release-name` 標記並接管（adopt），而非報錯衝突。

### 5. 建立 Cloudflare API Token Secret 並部署 infra chart（Ingress + cert-manager）

```bash
# Token 需具備該 zone 的 Zone:DNS:Edit 權限；ClusterIssuer 為叢集級資源，
# cert-manager 會在其自身所在 namespace（預設 cert-manager）查找此 Secret
kubectl create secret generic cloudflare-api-token `
  --namespace cert-manager `
  --from-literal=api-token=<Cloudflare API Token>

helm upgrade --install open-jam-infra infra/helm/infra `
  --namespace open-jam `
  --create-namespace `
  -f infra/helm/infra/values.prod.yaml
```

### 6. 取得外部 IP，回填 Cloudflare DNS

```bash
kubectl get ingress -n open-jam
```

將第 1 步建立的 `openjam.co` / `*.openjam.co` A 記錄指向此處取得的外部 IP（應與 `open-jam-ip` 相同）。

### 7. 驗證 cert-manager 簽發 wildcard 憑證

```bash
kubectl get certificate -n open-jam
kubectl describe certificaterequest -n open-jam
```

確認 `letsencrypt-cloudflare` ClusterIssuer 透過 DNS-01 challenge 成功簽出涵蓋 `openjam.co` / `*.openjam.co` 的憑證，並寫入 `open-jam-tls` Secret。

### 8. 已知問題與排查紀錄

部署過程中遇到並修正的問題，記錄於此供日後重建環境時參考：

| 問題 | 原因 | 解法 |
|------|------|------|
| GKE Ingress 路由未生效 | GKE 內建 GCE Ingress controller 不採用 `spec.ingressClassName`，須改用 `kubernetes.io/ingress.class` annotation（`gce` / `gce-internal`） | 改以 annotation 指定 ingress class（見 `infra/helm/infra/templates/ingress/ingress.yaml`） |
| Auth 服務 502 Bad Gateway | GCLB 預設探測 `/` 且僅接受 200，但 Auth（MVC）的 `/` 會 302 導向 `/login`，被判定 UNHEALTHY | 新增 `BackendConfig`，將健康檢查路徑改為固定回 200 的 `/favicon.ico`，並透過 `cloud.google.com/backend-config` annotation 套用至 Service（見 `infra/helm/open-jam/templates/auth/backend-config.yaml`） |
| RabbitMQ Pod 啟動後被誤判 unhealthy 而重啟 | liveness probe 時間門檻過緊，叢集啟動較慢時來不及就緒 | 放寬 `livenessProbe`（`initialDelaySeconds: 90`、`periodSeconds: 20`、`timeoutSeconds: 10`、`failureThreshold: 6`），並將 `virtualHost` 改回預設值 `/` 以符合服務連線設定 |
| `api.openjam.co` 路由 | 對應「功能 API」服務尚未就緒 | 暫時於 `infra/helm/infra/values.prod.yaml` 中註解該 host 路由，待服務部署後再啟用 |

### 後續待辦

- `api.openjam.co`：待「功能 API」服務的 Helm chart / Service 就緒後，於 `infra/helm/infra/values.prod.yaml` 取消該路由的註解並調整 `serviceName`。
- 將 Cloudflare API Token 與其他正式環境機密改由 External Secrets Operator 從 GCP Secret Manager 同步，避免明文存於 git（見上方「Secrets 管理」）。
