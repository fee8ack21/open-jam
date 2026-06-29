# Open Jam Helm Chart

Open Jam 台灣數位商品平台的 Kubernetes 部署套件，對應 `infra/docker/docker-compose.yaml` 的完整服務組合。

## 前置需求

- Kubernetes 1.25+
- Helm 3.10+
- 各應用程式 Docker image 已推送至 registry

## 快速開始

### 1. 建置並推送 image

```bash
# 從專案根目錄執行
docker build -f src/Auth/Dockerfile        -t open-jam/auth:latest        src/
docker build -f src/LogService/Dockerfile  -t open-jam/log-service:latest src/
docker build -f src/EmailService/Dockerfile -t open-jam/email-service:latest src/
docker build -f src/Bootstrap/Dockerfile   -t open-jam/bootstrap:latest   src/
docker build -f apps/creator-web/Dockerfile   -t open-jam/creator-web:latest   apps/creator-web/
docker build -f apps/market-web/Dockerfile    -t open-jam/market-web:latest    apps/market-web/
docker build -f apps/workspace-web/Dockerfile -t open-jam/workspace-web:latest apps/workspace-web/
docker build -f docs/Dockerfile               -t open-jam/docs:latest          docs/
```

### 2. 安裝 Chart

```bash
helm install open-jam infra/helm/open-jam \
  --namespace open-jam --create-namespace \
  --set secrets.postgres.password=<強密碼> \
  --set secrets.redis.password=<強密碼> \
  --set secrets.rabbitmq.password=<強密碼> \
  --set secrets.hydra.systemSecret=<至少32字元的隨機字串> \
  --set secrets.hydra.pairwiseSalt=<隨機鹽值> \
  --set config.externalUrl=https://openjam.co/ \
  --set config.authPageUrl=https://openjam.co/auth/
```

### 3. 升級

```bash
helm upgrade open-jam infra/helm/open-jam \
  --namespace open-jam \
  -f my-values.yaml
```

### 4. 移除

```bash
helm uninstall open-jam --namespace open-jam
```

> **注意**：StatefulSet 的 PersistentVolumeClaim（postgres / redis / rabbitmq 資料）不會隨 `helm uninstall` 一併刪除，需手動清除。

---

## 服務一覽

### Infrastructure

| 服務 | 種類 | 說明 |
|------|------|------|
| postgres | StatefulSet | PostgreSQL 16，含 init.sql 建立四個資料庫 |
| redis | StatefulSet | Redis 7，requirepass 模式 |
| rabbitmq | StatefulSet | RabbitMQ 3.13 + Management UI |
| hydra | Deployment | Ory Hydra v2.2 OIDC Provider |
| mailpit | Deployment | 本地 SMTP 攔截器（僅開發 / Staging） |

### Application

| 服務 | 種類 | Port | 說明 |
|------|------|------|------|
| auth | Deployment | 8080 | ASP.NET Core MVC 登入 / 註冊 |
| log-service | Deployment | 8080 | Audit Log REST API |
| email-service | Deployment | — | RabbitMQ Worker，無 HTTP |
| storage-service | Deployment | 8080 | 檔案上傳 / 下載 URL 簽發 REST API |
| store-service | Deployment | 8080 | 開店申請 / 店家 / 追蹤 REST API |
| creator-web | Deployment | 80 | 創作者前台（Vite + nginx） |
| market-web | Deployment | 80 | 消費者市集（Vite + nginx） |
| workspace-web | Deployment | 80 | 工作台（Vite + nginx） |
| docs | Deployment | 80 | VitePress 文件站 |

### Job

| 服務 | 種類 | 說明 |
|------|------|------|
| bootstrap | Job (post-install) | 一次性 seed：Hydra OIDC Client + Email Template |

---

## 目錄結構

```
infra/helm/open-jam/
├── Chart.yaml
├── values.yaml
└── templates/
    ├── _helpers.tpl
    ├── NOTES.txt
    ├── secret.yaml
    ├── postgres/         configmap.yaml · statefulset.yaml · service.yaml
    ├── redis/            statefulset.yaml · service.yaml
    ├── rabbitmq/         statefulset.yaml · service.yaml
    ├── hydra/            deployment.yaml · service.yaml
    ├── mailpit/          deployment.yaml · service.yaml
    ├── auth/             deployment.yaml · service.yaml
    ├── log-service/      deployment.yaml · service.yaml
    ├── email-service/    deployment.yaml
    ├── storage-service/  deployment.yaml · service.yaml
    ├── store-service/    deployment.yaml · service.yaml
    ├── creator-web/      deployment.yaml · service.yaml
    ├── market-web/       deployment.yaml · service.yaml
    ├── workspace-web/    deployment.yaml · service.yaml
    ├── docs/             deployment.yaml · service.yaml
    └── bootstrap/        job.yaml
```

> **Ingress / TLS（GKE Ingress、cert-manager + Let's Encrypt）已拆分至獨立的 [`infra`](../infra) chart**，與本 chart 分開管理生命週期（`ClusterIssuer` 為叢集級資源，不適合綁在 namespace 範圍的應用 release 上）。請先部署本 chart，再部署 `infra/helm/infra`（其 Ingress backend 會指向本 release 建立的 Service）。

---

## values.yaml 參數說明

### 全域

| 參數 | 預設值 | 說明 |
|------|--------|------|
| `config.externalUrl` | `https://openjam.co/` | 對外根 URL，Hydra issuer 使用 |
| `config.authPageUrl` | `https://openjam.co/auth/` | Auth MVC 對外 URL，Hydra redirect 使用 |
| `dbNamePrefix` | `open_jam` | 資料庫名稱前綴（`<prefix>_auth` / `_log` / `_email` / `_hydra`） |

### Secrets

所有敏感值集中在 `secrets.*`，由 Chart 統一生成一個 Kubernetes Secret（`<release>-secret`）。

| 參數 | 說明 |
|------|------|
| `secrets.postgres.password` | PostgreSQL 超級使用者密碼 |
| `secrets.redis.password` | Redis requirepass 密碼 |
| `secrets.rabbitmq.password` | RabbitMQ 預設使用者密碼 |
| `secrets.hydra.systemSecret` | Hydra 系統加密金鑰（**最少 32 字元**） |
| `secrets.hydra.pairwiseSalt` | Hydra pairwise subject identifier 鹽值 |

### PostgreSQL

| 參數 | 預設值 | 說明 |
|------|--------|------|
| `postgres.enabled` | `true` | 是否部署內建 PostgreSQL |
| `postgres.image` | `postgres:16-alpine` | 映像檔 |
| `postgres.user` | `postgres` | 超級使用者帳號 |
| `postgres.persistence.enabled` | `true` | 啟用 PVC；`false` 改用 emptyDir（資料不持久） |
| `postgres.persistence.storageClass` | `""` | 空字串使用叢集預設 StorageClass |
| `postgres.persistence.size` | `10Gi` | PVC 容量 |

### Redis

| 參數 | 預設值 | 說明 |
|------|--------|------|
| `redis.enabled` | `true` | 是否部署內建 Redis |
| `redis.image` | `redis:7-alpine` | 映像檔 |
| `redis.persistence.enabled` | `true` | 啟用 PVC |
| `redis.persistence.size` | `1Gi` | PVC 容量 |

### RabbitMQ

| 參數 | 預設值 | 說明 |
|------|--------|------|
| `rabbitmq.enabled` | `true` | 是否部署內建 RabbitMQ |
| `rabbitmq.image` | `rabbitmq:3.13-management-alpine` | 映像檔（含 Management UI） |
| `rabbitmq.user` | `guest` | 預設使用者帳號 |
| `rabbitmq.virtualHost` | `open-jam` | 預設 vhost |
| `rabbitmq.persistence.enabled` | `true` | 啟用 PVC |
| `rabbitmq.persistence.size` | `2Gi` | PVC 容量 |

### Hydra

| 參數 | 預設值 | 說明 |
|------|--------|------|
| `hydra.enabled` | `true` | 是否部署 Ory Hydra |
| `hydra.image` | `oryd/hydra:v2.2` | 映像檔 |
| `hydra.ttl.authCode` | `10m` | Authorization Code 有效時間 |
| `hydra.ttl.loginConsentRequest` | `10m` | Login / Consent 請求有效時間 |
| `hydra.oidc.subjectIdentifiers` | `pairwise,public` | Subject Identifier 類型 |
| `hydra.cors.allowedOrigins` | `*` | Public endpoint CORS 允許來源 |

### Mailpit（開發用 SMTP）

| 參數 | 預設值 | 說明 |
|------|--------|------|
| `mailpit.enabled` | `true` | **Production 請設為 `false`** |
| `mailpit.image` | `axllent/mailpit:latest` | 映像檔 |

### SMTP（外部，mailpit.enabled=false 時生效）

| 參數 | 預設值 | 說明 |
|------|--------|------|
| `smtp.host` | `""` | SMTP 伺服器主機 |
| `smtp.port` | `1025` | SMTP port |
| `smtp.enableSsl` | `false` | 是否啟用 TLS |
| `smtp.fromAddress` | `noreply@openjam.co` | 寄件者地址 |

### 檔案儲存（storage）

| 參數 | 預設值 | 說明 |
|------|--------|------|
| `storage.provider` | `Gcs` | 儲存後端，`Gcs`（雲端，預設）或 `Local`（本地檔案系統） |
| `storage.publicBucket` | `open-jam-public` | 公開讀取資產（Avatar/Banner、縮圖，`public/*`）所屬 GCS bucket |
| `storage.privateBucket` | `open-jam-private` | 私有付費檔所屬 GCS bucket |
| `storage.softDeleteRetentionDays` | `30` | 軟刪除檔案保留天數，超過後由 `OrphanCleanupService` 硬刪除 |
| `storage.gcs.credentialsSecretName` | `gcs-sa-key` | GCS 服務帳戶金鑰的 k8s Secret 名稱；留空則改走 ADC / GKE Workload Identity |
| `storage.gcs.credentialsSecretKey` | `gcs-sa-key.json` | Secret 內金鑰檔的 data key |
| `storage.gcs.mountPath` | `/var/secrets/gcs` | 金鑰檔掛載目錄；`CredentialsPath = mountPath/credentialsSecretKey` |
| `storage.gcs.credentialsPath` | `""` | 直接覆寫 `CredentialsPath`（一般留空，由 `mountPath` + key 組合） |
| `storage.local.rootPath` | `Files` | 本地檔案存放根目錄（`Provider: Local` 時生效，建議掛載 PV） |
| `storage.local.baseUrl` | `""` | 本服務對外 base URL，用於組合 blob URL（`Provider: Local` 時必填） |
| `secrets.storage.localSigningKey` | `changeme-…` | 本地 blob HMAC 簽章密鑰（`Provider: Local` 時使用） |

### 應用服務（auth / logService / emailService / storageService / storeService / creatorWeb / marketWeb / workspaceWeb / docs）

各服務共用相同參數結構：

| 參數 | 預設值 | 說明 |
|------|--------|------|
| `<service>.enabled` | `true` | 是否部署 |
| `<service>.image.repository` | `open-jam/<service>` | Image repository |
| `<service>.image.tag` | `latest` | Image tag |
| `<service>.image.pullPolicy` | `IfNotPresent` | Pull policy |
| `<service>.replicaCount` | `1` | 副本數 |
| `<service>.resources` | `{}` | K8s resource requests / limits |

### Bootstrap Job

| 參數 | 預設值 | 說明 |
|------|--------|------|
| `bootstrap.enabled` | `true` | 是否在首次 install 後執行 seed |
| `bootstrap.ttlSecondsAfterFinished` | `300` | Job 完成後自動清除的秒數 |

---

## Production 部署建議

1. **敏感值**：使用 [External Secrets Operator](https://external-secrets.io/) 或 Vault 注入，避免 `--set` 明文傳遞。

2. **停用 Mailpit**，改用外部 SMTP：
   ```yaml
   mailpit:
     enabled: false
   smtp:
     host: smtp.sendgrid.net
     port: 587
     enableSsl: true
     fromAddress: noreply@openjam.co
   ```

3. **替換 infra 元件**為 managed service（建議生產環境不在 K8s 內自管 stateful 服務）：
   ```yaml
   postgres:
     enabled: false   # 改用 RDS / Cloud SQL
   redis:
     enabled: false   # 改用 ElastiCache / Memorystore
   rabbitmq:
     enabled: false   # 改用 AmazonMQ / CloudAMQP
   ```
   並將 connection strings 直接寫入 Secret 或由 External Secrets 注入。

4. **設定 resource limits**，避免單一 Pod 耗盡節點資源：
   ```yaml
   auth:
     resources:
       requests:
         cpu: 100m
         memory: 128Mi
       limits:
         cpu: 500m
         memory: 512Mi
   ```

5. **Ingress / TLS**：本 chart 不含 Ingress 資源，請另外部署 [`infra/helm/infra`](../infra) chart（GKE Ingress + cert-manager / Let's Encrypt wildcard 憑證），其 backend Service 會指向本 release 建立的 Service，需部署於同一 namespace。
