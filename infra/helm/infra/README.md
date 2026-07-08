# Open Jam Infra Chart

叢集層基礎設施 Helm Chart：**GKE Ingress** 與 **cert-manager + Let's Encrypt**（Cloudflare DNS-01）。與 [`open-jam`](../open-jam) 應用 chart 為各自獨立的 release，分開管理生命週期（理由：`ClusterIssuer` 為叢集級資源，不適合綁在 namespace 範圍的應用 release 上）。

對應 [[Infra]] 文件中「Ingress / Gateway」與「TLS 憑證」章節所述架構。

## 前置需求

- Kubernetes 1.25+ / GKE
- Helm 3.10+
- 已部署 [`open-jam`](../open-jam) 應用 chart（Ingress 的 backend Service 來自該 release）
- 啟用 cert-manager 時：叢集已安裝 [cert-manager](https://cert-manager.io/docs/installation/)（含 CRDs），並已建立內含 Cloudflare API Token 的 Secret

## 站點與路由

| Host | 對應前端 / 服務 |
|------|----------------|
| `openjam.co` | portal-web |
| `auth.openjam.co` | Auth |
| `workspace.openjam.co` | workspace-web |
| `api.openjam.co` | 功能 API |
| `<creator>.openjam.co`（`*.openjam.co`） | creator-web |

> `api.openjam.co` 對應 docs/infra.md 所述「功能 API」服務，目前 chart 中尚無對應元件，請依實際部署的 Service 名稱調整 `ingress.hosts` 中的 `serviceName`。

## 安裝

```bash
# 1. 建立 GKE 全域靜態 IP
gcloud compute addresses create open-jam-ip --global

# 2. 建立 Cloudflare API Token Secret（cert-manager 安裝所在 namespace，預設 cert-manager）
kubectl create secret generic cloudflare-api-token \
  --namespace cert-manager \
  --from-literal=api-token=<Cloudflare API Token>

# 3. 安裝（namespace 須與 open-jam 應用 chart 相同，backend Service 才能解析）
helm install open-jam-infra infra/helm/infra \
  --namespace open-jam --create-namespace \
  --set ingress.enabled=true \
  --set ingress.staticIpName=open-jam-ip \
  --set certManager.enabled=true \
  --set certManager.email=<維運信箱>

# 4. 查詢 Ingress 取得的外部 IP，於 Cloudflare 設定 DNS 指向
kubectl get ingress -n open-jam
```

> 將 Cloudflare DNS 中 `openjam.co` / `*.openjam.co` 等記錄指向上述外部 IP；建議調整 Proxy 狀態以免影響 Let's Encrypt DNS-01 驗證與憑證綁定。

### 升級 / 移除

```bash
helm upgrade open-jam-infra infra/helm/infra --namespace open-jam -f my-values.yaml
helm uninstall open-jam-infra --namespace open-jam
```

---

## values.yaml 參數說明

### Ingress（GKE）

| 參數 | 預設值 | 說明 |
|------|--------|------|
| `ingress.enabled` | `false` | 是否建立 Ingress 資源 |
| `ingress.className` | `gce` | Ingress class（GKE 原生 Ingress，設定於 `spec.ingressClassName`） |
| `ingress.staticIpName` | `""` | GKE 全域靜態 IP 名稱，對應 `kubernetes.io/ingress.global-static-ip-name` |
| `ingress.annotations` | `{}` | 額外自訂 annotations |
| `ingress.tls.secretName` | `open-jam-tls` | TLS 憑證 Secret 名稱（cert-manager 簽發後寫入） |
| `ingress.hosts` | 見 `values.yaml` | `host` → `serviceName` 對照表，定義路由規則與 TLS hosts |

`hosts` 中的 `serviceName` 須為 **open-jam 應用 chart 在同一 namespace 中實際建立的 Service 名稱**（K8s Ingress backend 不可跨 namespace）。預設值對應 release name 為 `open-jam` 時的命名慣例 `<release>-<component>`，請依實際 release 名稱調整。

### TLS 憑證（cert-manager + Let's Encrypt）

| 參數 | 預設值 | 說明 |
|------|--------|------|
| `certManager.enabled` | `false` | 是否建立 `ClusterIssuer` / `Certificate` 資源 |
| `certManager.acmeServer` | `https://acme-v02.api.letsencrypt.org/directory` | ACME server（測試可改用 [staging endpoint](https://letsencrypt.org/docs/staging-environment/)） |
| `certManager.email` | `""` | 註冊 ACME 帳號用 email（憑證到期通知） |
| `certManager.clusterIssuerName` | `letsencrypt-cloudflare` | `ClusterIssuer` 資源名稱 |
| `certManager.dnsZone` | `openjam.co` | DNS-01 challenge 的 Cloudflare zone |
| `certManager.dnsNames` | `["openjam.co", "*.openjam.co"]` | 簽發憑證涵蓋的網域 |
| `certManager.cloudflare.apiTokenSecretName` | `cloudflare-api-token` | 內含 Cloudflare API Token 的 Secret 名稱（**需自行建立**，見下方說明） |
| `certManager.cloudflare.apiTokenSecretKey` | `api-token` | 上述 Secret 中 Token 的 key 名稱 |

**Cloudflare API Token Secret**：

- `ClusterIssuer` 為叢集級資源，cert-manager 會在其**自身所在的 namespace**（預設 `cert-manager`，由 cert-manager 的 `--cluster-resource-namespace` 旗標決定）查找 `apiTokenSecretRef` 指定的 Secret，**而非** Ingress / Certificate 所在的 `open-jam` namespace。
- Token 需具備該 zone 的 `Zone:DNS:Edit` 權限：

  ```bash
  kubectl create secret generic cloudflare-api-token \
    --namespace cert-manager \
    --from-literal=api-token=<Cloudflare API Token>
  ```

- 正式環境請改由 External Secrets Operator 從 GCP Secret Manager 同步，避免明文進 git（見 [[Infra]] Secrets 管理章節）。

---

## 目錄結構

```
infra/helm/infra/
├── Chart.yaml
├── README.md
├── values.yaml
└── templates/
    ├── _helpers.tpl
    ├── ingress/        ingress.yaml
    └── cert-manager/   cluster-issuer.yaml · certificate.yaml
```
