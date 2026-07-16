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
| `openjam.co` | 平台探索頁 | portal-web |
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

- **GCS**（雲端）+ 地端**本地檔案系統**（見 [[Storage]]）。
- **雙 bucket**：
  - `open-jam-public`：公開讀取資產（商店 Avatar/Banner、商品縮圖等 `IsPublic` 物件），整個 bucket 開匿名讀取（StorageService 啟動時由 `EnsurePublicReadPolicyAsync` 套用 IAM 繫結）。
  - `open-jam-private`：私有付費檔，僅透過短效 signed URL 授權存取。
  - 兩個 bucket 皆須啟用 **uniform bucket-level access**。StorageService 依 `StoredFile.IsPublic` 旗標選 bucket，物件鍵值一律 `creators/{creatorId}/{fileId}/{originalName}`。
  - **CORS（必設，否則瀏覽器上傳全失敗）**：前端（workspace-web）以 StorageService 簽發的 signed URL **從瀏覽器直接 HTTP PUT 到 `storage.googleapis.com`**，屬跨源請求，bucket 未設 CORS 時瀏覽器會擋下 PUT（preflight 被拒、`net::ERR_FAILED`）。伺服器端呼叫不受 CORS 限制，故僅真實瀏覽器上傳會踩到。兩個 bucket 都須套用下列 CORS：

    ```jsonc
    // cors.json — 上傳只從 workspace-web 發起（下載檔進 private、頭像/橫幅/縮圖進 public）；
    // 買家下載私有檔以 <a href target="_blank"> 導向 signed GET URL（非 fetch），不受 CORS，故下載端免設定。
    // 注意：GCS CORS 的 origin 為精確比對、不支援萬用子網域（*.openjam.co 無效），新增上傳來源須逐一列入。
    [{ "origin": ["https://workspace.openjam.co"], "method": ["PUT", "GET", "HEAD"],
       "responseHeader": ["Content-Type", "ETag"], "maxAgeSeconds": 3600 }]
    ```

    ```bash
    gcloud storage buckets update gs://open-jam-public  --cors-file=cors.json
    gcloud storage buckets update gs://open-jam-private --cors-file=cors.json
    # 驗證：gcloud storage buckets describe gs://open-jam-private --format="value(cors_config)"
    ```
- **GCP Cloud CDN**：與 GCS 原生整合，原生支援 **signed URL / signed cookie**（對應 [[Storage]] 付費內容與 HLS 串流授權）。`Storage:PublicBaseUrl` 指向公開資產對外網址（MVP 直接用 `https://storage.googleapis.com/open-jam-public`，未來換 CDN 網域如 `assets.openjam.co`）。

## 平台服務元件

- **Hydra**（OIDC server，見 [[Auth]]）：部署於 GKE。
- **GCP Transcoder API**：影片轉碼 HLS（見 [[Storage]]）。
- **ClamAV**：檔案掃毒 worker，部署於 GKE（見 [[Storage]]）。

## 可觀測性

- **Loki + Promtail**（logs）、**Tempo**（traces）、**Grafana**（查詢 / 儀表板 / 告警），部署於 GKE，詳見 [[Log]]。

## CI / CD

- **CI**：GitHub Actions 於 release（推 `chore(release)` commit 到 `main`）時建置映像，推送至 **GCP Artifact Registry**，並將結果彙整推播 Discord。詳細流程（Workflow Identity 設定、由 commit 訊息清單決定建置範圍、完整 workflow）見 [[CI]]。
- **CD**：**GitHub Actions + Helm Umbrella Chart**。同一個 release workflow 於映像建置成功後，直接以 `helm upgrade --install open-jam ... --atomic` 部署至 GKE（`--atomic` 失敗自動回滾）。
- 流程：CI build / push image → helm 以本次 release 清單的 image tag 覆寫（`--set`）+ `values.prod.yaml` 疊上 Secret Manager 機密 → 部署 GKE。
- 未來可考慮改採 **Argo CD（GitOps）** 接管部署（見「後續待辦」）。

## TLS 憑證

- **cert-manager + Let's Encrypt**，透過 Cloudflare **DNS-01** challenge 自動簽發 / 續期。
- 簽發 **wildcard `*.openjam.co`** 憑證以涵蓋創作者子網域。

## Secrets 管理

- 機密集中存於 **GCP Secret Manager**。
- **External Secrets Operator** 同步至 GKE，集中、可稽核、避免明文進 git。
- **GCS 服務帳戶金鑰**：StorageService 簽 V4 signed URL 需可簽章憑證。MVP 先以 **k8s Secret** 提供金鑰檔（`kubectl create secret generic gcs-sa-key --from-file=gcs-sa-key.json`），由 Helm（`storage.gcs.credentialsSecretName`）掛載至 `/var/secrets/gcs/gcs-sa-key.json`，並注入 `Storage:Gcs:CredentialsPath`。該服務帳戶需 `open-jam-public` 的 `roles/storage.admin`（含 bucket IAM 管理）與 `open-jam-private` 的 `roles/storage.objectAdmin`。金鑰檔不進 git（`.gitignore` 已排除）。未來可改 **GKE Workload Identity** 免金鑰（`credentialsSecretName` 留空走 ADC，簽章改用 IAM SignBlob，服務帳戶另需 `iam.serviceAccounts.signBlob`）。

## 環境

- **地端開發**：docker-compose 起本機依賴（SMTP catcher（Mailpit，代替 SendGrid）、RabbitMQ、Redis、PostgreSQL 等，見 [[Develop]]）；檔案儲存改用本地檔案系統，無需額外物件儲存容器。
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
2. 先建立暫定 DNS 記錄（待第 7 步取得 GCLB 外部 IP 後回填正確值）：
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

### 3. 建立 GCS 服務帳戶金鑰 Secret

正式環境 StorageService 以 GCS 為後端，簽 signed URL 需服務帳戶金鑰。`values.prod.yaml` 預設走「掛金鑰檔」模式（`storage.gcs.credentialsSecretName: gcs-sa-key`），此 Secret **不由 Helm 建立**，須先手動建立，否則 storage-service Pod 會卡在 `ContainerCreating`（找不到 volume 對應的 Secret）。

```bash
# 先建立 namespace（第 4 步的 helm --create-namespace 會接管既有 namespace）
kubectl create namespace open-jam

# 以下載的服務帳戶金鑰 JSON 建立 Secret；Secret 內 data key 必須為 gcs-sa-key.json
# （對應 storage.gcs.credentialsSecretKey，供 Helm 掛載至 /var/secrets/gcs/gcs-sa-key.json）
kubectl create secret generic gcs-sa-key `
  --namespace open-jam `
  --from-file=gcs-sa-key.json=<金鑰檔路徑>
```

驗證 Secret 存在且 data key 正確（應含 `gcs-sa-key.json`）：

```bash
kubectl get secret gcs-sa-key -n open-jam -o jsonpath="{.data}"
```

> 服務帳戶需 `open-jam-public` 的 `roles/storage.admin`、`open-jam-private` 的 `roles/storage.objectAdmin`（見上方「Secrets 管理」）。
> 未來改用 **GKE Workload Identity** 可免此 Secret：於 `values.prod.yaml` 設 `storage.gcs.credentialsSecretName: ""` 走 ADC，服務帳戶另需 `iam.serviceAccounts.signBlob`。

### 4. 部署 open-jam 應用 chart

```bash
helm upgrade --install open-jam infra/helm/open-jam `
  --namespace open-jam `
  --create-namespace `
  -f infra/helm/open-jam/values.prod.yaml
```

> `infra` chart 的 Ingress 直接以 Service 名稱（`open-jam-<component>`）指向本 release 建立的 backend，故須先部署本 chart。

### 5. 安裝 cert-manager

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

### 6. 建立 Cloudflare API Token Secret 並部署 infra chart（Ingress + cert-manager）

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

### 7. 取得外部 IP，回填 Cloudflare DNS

```bash
kubectl get ingress -n open-jam
```

將第 1 步建立的 `openjam.co` / `*.openjam.co` A 記錄指向此處取得的外部 IP（應與 `open-jam-ip` 相同）。

### 8. 驗證 cert-manager 簽發 wildcard 憑證

```bash
kubectl get certificate -n open-jam
kubectl describe certificaterequest -n open-jam
```

確認 `letsencrypt-cloudflare` ClusterIssuer 透過 DNS-01 challenge 成功簽出涵蓋 `openjam.co` / `*.openjam.co` 的憑證，並寫入 `open-jam-tls` Secret。

### 9. 移除資源（Teardown）

依序刪除，避免殘留 finalizer 卡住 namespace 刪除：

```bash
# 1. 刪 Open Jam Helm Release
helm uninstall open-jam -n open-jam
helm uninstall open-jam-infra -n open-jam

# 確認
kubectl get all -n open-jam
```

```bash
# 2. 刪 cert-manager 資源
kubectl delete certificate --all -n open-jam
kubectl delete certificaterequest --all -n open-jam
kubectl delete order --all -n open-jam
kubectl delete challenge --all -n open-jam
```

如果 Challenge 卡住，編輯該資源移除 finalizer：

```bash
kubectl edit challenge <name> -n open-jam
```

刪除以下欄位：

```yaml
finalizers:
- acme.cert-manager.io/finalizer
```

```bash
# 3. 刪 Secret
kubectl delete secret open-jam-tls -n open-jam
kubectl delete secret cloudflare-api-token -n open-jam
```

```bash
# 4. 刪 Namespace（最後才刪）
kubectl delete namespace open-jam

# 確認沒有 open-jam
kubectl get ns
```

### 10. 已知問題與排查紀錄

部署過程中遇到並修正的問題，記錄於此供日後重建環境時參考：

| 問題 | 原因 | 解法 |
|------|------|------|
| GKE Ingress 路由未生效 | GKE 內建 GCE Ingress controller 不採用 `spec.ingressClassName`，須改用 `kubernetes.io/ingress.class` annotation（`gce` / `gce-internal`） | 改以 annotation 指定 ingress class（見 `infra/helm/infra/templates/ingress/ingress.yaml`） |
| Auth 服務 502 Bad Gateway | GCLB 預設探測 `/` 且僅接受 200，但 Auth（MVC）的 `/` 會 302 導向 `/login`，被判定 UNHEALTHY | 新增 `BackendConfig`，將健康檢查路徑改為固定回 200 的 `/favicon.ico`，並透過 `cloud.google.com/backend-config` annotation 套用至 Service（見 `infra/helm/open-jam/templates/auth/backend-config.yaml`） |
| RabbitMQ Pod 啟動後被誤判 unhealthy 而重啟 | liveness probe 時間門檻過緊，叢集啟動較慢時來不及就緒 | 放寬 `livenessProbe`（`initialDelaySeconds: 90`、`periodSeconds: 20`、`timeoutSeconds: 10`、`failureThreshold: 6`），並將 `virtualHost` 改回預設值 `/` 以符合服務連線設定 |
| `api.openjam.co` 路由 | 對應「功能 API」服務尚未就緒 | 暫時於 `infra/helm/infra/values.prod.yaml` 中註解該 host 路由，待服務部署後再啟用 |

### 後續待辦

- `api.openjam.co`：目前已新增 `/store-service`、`/log-service`、`/storage-service` path 路由分別至 StoreService、LogService、StorageService（見 `infra/helm/infra/values.prod.yaml`）。其餘 path（其他「功能 API」服務）待對應 Helm chart / Service 就緒後再依需求新增。
- 將 Cloudflare API Token 與其他正式環境機密改由 External Secrets Operator 從 GCP Secret Manager 同步，避免明文存於 git（見上方「Secrets 管理」）。
- **（未來可考慮）Argo CD GitOps**：改以 Argo CD 接管部署（宣告式同步 / self-heal），取代目前 release workflow 直接跑 `helm upgrade` 的方式。短期不實作。
