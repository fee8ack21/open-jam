# CI：映像建置與推送

本頁描述 **GitHub Actions** 的持續整合流程：發布（release）時自動建置 Docker 映像、推送至 **GCP Artifact Registry**，並將成功 / 失敗結果推播至 **Discord**。基礎架構全貌與 CD（Argo CD / Helm）見 [[Infra]]。

## 目標

| 項目 | 值 |
|------|-----|
| Registry Host | `asia-east1-docker.pkg.dev` |
| GCP Project | `open-jam-498418` |
| Artifact Registry Repo | `open-jam`（region `asia-east1`） |
| 映像命名 | `asia-east1-docker.pkg.dev/open-jam-498418/open-jam/<image>` |
| 映像 tag | 取自 git tag（`v0.1.0` → `.../auth:v0.1.0`），另打 `latest` |

映像清單與命名對齊 `infra/helm/open-jam/values.prod.yaml`（如 `auth`、`catalog-service`、`creator-web`）。

## 觸發策略

以**推 git tag** 觸發，tag 名直接作為映像 tag：

```yaml
on:
  push:
    tags: ['v*']
```

推 `v0.1.0` 即建置並推送對應版本的映像。此作法對齊既有的 `v0.0.x` 語意化版號慣例，比監聽 commit 訊息可靠。

## 建置範圍：由 release commit 決定

一次 release 不一定重建全部映像。**要建哪些服務由 release commit 訊息宣告**——在 commit 訊息放一行 `images:`：

```
release: v0.1.0

images: auth, creator-web, catalog-service
```

Workflow 拆兩段：`setup` 解析 `images:` 行產出動態 matrix，`build` 以 `fromJson` 消費該 matrix 只建被列出的服務。key 對應下表（context 與 Dockerfile 路徑）：

| 服務 / 前端 | image key | build context | Dockerfile |
|------|-----------|---------------|------------|
| Auth | `auth` | `src` | `src/Auth/Dockerfile` |
| LogService | `log-service` | `src` | `src/LogService/Dockerfile` |
| EmailService | `email-service` | `src` | `src/EmailService/Dockerfile` |
| StorageService | `storage-service` | `src` | `src/StorageService/Dockerfile` |
| StoreService | `store-service` | `src` | `src/StoreService/Dockerfile` |
| CatalogService | `catalog-service` | `src` | `src/CatalogService/Dockerfile` |
| QuotaService | `quota-service` | `src` | `src/QuotaService/Dockerfile` |
| PaymentService | `payment-service` | `src` | `src/PaymentService/Dockerfile` |
| OrderService | `order-service` | `src` | `src/OrderService/Dockerfile` |
| NotificationService | `notification-service` | `src` | `src/NotificationService/Dockerfile` |
| ContentService | `content-service` | `src` | `src/ContentService/Dockerfile` |
| Bootstrap | `bootstrap` | `src` | `src/Bootstrap/Dockerfile` |
| portal-web | `portal-web` | `apps/portal-web` | `apps/portal-web/Dockerfile` |
| creator-web | `creator-web` | `apps/creator-web` | `apps/creator-web/Dockerfile` |
| workspace-web | `workspace-web` | `apps/workspace-web` | `apps/workspace-web/Dockerfile` |

> 後端 12 個服務共用 `src/` 為 build context（Dockerfile 內 `COPY` 皆相對 `src/`），前端各自以 app 目錄為 context。

## 認證：Workload Identity Federation

CI 以 **WIF（免長期金鑰）** 換取 GCP 憑證，非 SA JSON key。一次性設定：

```bash
PROJECT=open-jam-498418
POOL=github-pool
PROVIDER=github-provider
REPO=<owner>/open-jam                       # 換成實際 GitHub owner/repo
SA=gha-pusher@$PROJECT.iam.gserviceaccount.com

# 1) Artifact Registry repo（若尚未建立）
gcloud artifacts repositories create open-jam \
  --repository-format=docker --location=asia-east1 --project=$PROJECT

# 2) CI 用 Service Account + 授 AR 推送權
gcloud iam service-accounts create gha-pusher --project=$PROJECT
gcloud artifacts repositories add-iam-policy-binding open-jam \
  --location=asia-east1 --project=$PROJECT \
  --member="serviceAccount:$SA" --role="roles/artifactregistry.writer"

# 3) Workload Identity Pool
gcloud iam workload-identity-pools create $POOL \
  --project=$PROJECT --location=global --display-name="GitHub Actions"

# 4) OIDC Provider，attribute-condition 限定只有本 repo 能換憑證（必要，否則任何 repo 皆可冒充）
gcloud iam workload-identity-pools providers create-oidc $PROVIDER \
  --project=$PROJECT --location=global --workload-identity-pool=$POOL \
  --display-name="GitHub OIDC" \
  --attribute-mapping="google.subject=assertion.sub,attribute.repository=assertion.repository" \
  --attribute-condition="assertion.repository=='$REPO'" \
  --issuer-uri="https://token.actions.githubusercontent.com"

# 5) 允許本 repo 冒充 SA
PN=$(gcloud projects describe $PROJECT --format='value(projectNumber)')
gcloud iam service-accounts add-iam-policy-binding $SA --project=$PROJECT \
  --role="roles/iam.workloadIdentityUser" \
  --member="principalSet://iam.googleapis.com/projects/$PN/locations/global/workloadIdentityPools/$POOL/attributes.repository/$REPO"

# 6) 取得 provider 完整資源名（填入 GitHub Secret GCP_WIF_PROVIDER）
gcloud iam workload-identity-pools providers describe $PROVIDER \
  --project=$PROJECT --location=global --workload-identity-pool=$POOL \
  --format='value(name)'
```

### GitHub Secrets

| Secret | 內容 |
|--------|------|
| `GCP_WIF_PROVIDER` | 上面第 6 步輸出的 provider 完整資源名 |
| `GCP_SA_EMAIL` | `gha-pusher@open-jam-498418.iam.gserviceaccount.com` |
| `DISCORD_WEBHOOK` | Discord 頻道 → 整合 → Webhook URL |

## 結果通知（Discord）

`notify` job 以 `needs: [setup, build]` + `if: always()` 讀取 `needs.build.result`（整批 matrix 的匯總結果，任一映像失敗即 `failure`），推一則帶顏色與 Actions run 連結的 embed：綠色成功、紅色失敗。embed 亦可列出本次建置的服務清單。

## Workflow（`.github/workflows/release.yml`）

```yaml
name: Release build & push

on:
  push:
    tags: ['v*']

env:
  REGISTRY: asia-east1-docker.pkg.dev
  IMAGE_PREFIX: asia-east1-docker.pkg.dev/open-jam-498418/open-jam

jobs:
  # ── 1. 解析 release commit 的 images: 行，產出動態 matrix ──
  setup:
    runs-on: ubuntu-latest
    outputs:
      matrix: ${{ steps.pick.outputs.matrix }}
      any:    ${{ steps.pick.outputs.any }}
    steps:
      - uses: actions/checkout@v4
        with: { fetch-depth: 2 }

      - id: pick
        run: |
          cat > /tmp/manifest.json <<'JSON'
          {
            "auth":                 { "image": "auth",                 "context": "src", "dockerfile": "src/Auth/Dockerfile" },
            "log-service":          { "image": "log-service",          "context": "src", "dockerfile": "src/LogService/Dockerfile" },
            "email-service":        { "image": "email-service",        "context": "src", "dockerfile": "src/EmailService/Dockerfile" },
            "storage-service":      { "image": "storage-service",      "context": "src", "dockerfile": "src/StorageService/Dockerfile" },
            "store-service":        { "image": "store-service",        "context": "src", "dockerfile": "src/StoreService/Dockerfile" },
            "catalog-service":      { "image": "catalog-service",      "context": "src", "dockerfile": "src/CatalogService/Dockerfile" },
            "quota-service":        { "image": "quota-service",        "context": "src", "dockerfile": "src/QuotaService/Dockerfile" },
            "payment-service":      { "image": "payment-service",      "context": "src", "dockerfile": "src/PaymentService/Dockerfile" },
            "order-service":        { "image": "order-service",        "context": "src", "dockerfile": "src/OrderService/Dockerfile" },
            "notification-service": { "image": "notification-service", "context": "src", "dockerfile": "src/NotificationService/Dockerfile" },
            "content-service":      { "image": "content-service",      "context": "src", "dockerfile": "src/ContentService/Dockerfile" },
            "bootstrap":            { "image": "bootstrap",            "context": "src", "dockerfile": "src/Bootstrap/Dockerfile" },
            "portal-web":           { "image": "portal-web",           "context": "apps/portal-web",    "dockerfile": "apps/portal-web/Dockerfile" },
            "creator-web":          { "image": "creator-web",          "context": "apps/creator-web",   "dockerfile": "apps/creator-web/Dockerfile" },
            "workspace-web":        { "image": "workspace-web",        "context": "apps/workspace-web", "dockerfile": "apps/workspace-web/Dockerfile" }
          }
          JSON

          MSG=$(git log -1 --pretty=%B)
          KEYS=$(printf '%s\n' "$MSG" | grep -iE '^\s*images:' | head -1 \
                 | sed -E 's/^\s*images:\s*//I' | tr ',' '\n' | tr -d ' ' | grep -v '^$')

          if [ -z "$KEYS" ]; then
            echo "commit 訊息無 images: 行，略過建置"
            echo "any=false" >> "$GITHUB_OUTPUT"
            echo 'matrix={"include":[]}' >> "$GITHUB_OUTPUT"
            exit 0
          fi

          KEYS_JSON=$(printf '%s\n' "$KEYS" | jq -R . | jq -s .)
          INCLUDE=$(jq -c --argjson keys "$KEYS_JSON" '[ .[$keys[]] // empty ]' /tmp/manifest.json)
          echo "matrix={\"include\":$INCLUDE}" >> "$GITHUB_OUTPUT"
          echo "any=true" >> "$GITHUB_OUTPUT"

  # ── 2. 平行建置並推送被列出的映像 ──
  build:
    needs: setup
    if: needs.setup.outputs.any == 'true'
    runs-on: ubuntu-latest
    permissions:
      contents: read
      id-token: write        # WIF 需要
    strategy:
      fail-fast: false
      matrix: ${{ fromJson(needs.setup.outputs.matrix) }}
    steps:
      - uses: actions/checkout@v4

      - id: auth
        uses: google-github-actions/auth@v2
        with:
          workload_identity_provider: ${{ secrets.GCP_WIF_PROVIDER }}
          service_account: ${{ secrets.GCP_SA_EMAIL }}

      - uses: docker/login-action@v3
        with:
          registry: ${{ env.REGISTRY }}
          username: oauth2accesstoken
          password: ${{ steps.auth.outputs.access_token }}

      - uses: docker/setup-buildx-action@v3

      - uses: docker/build-push-action@v6
        with:
          context: ${{ matrix.context }}
          file: ${{ matrix.dockerfile }}
          push: true
          tags: |
            ${{ env.IMAGE_PREFIX }}/${{ matrix.image }}:${{ github.ref_name }}
            ${{ env.IMAGE_PREFIX }}/${{ matrix.image }}:latest
          cache-from: type=gha
          cache-to: type=gha,mode=max

  # ── 3. 將整體結果推播 Discord ──
  notify:
    needs: [setup, build]
    runs-on: ubuntu-latest
    if: always()
    steps:
      - name: Discord notify
        env:
          RESULT: ${{ needs.build.result }}   # success / failure / cancelled / skipped
        run: |
          if [ "$RESULT" = "success" ]; then
            COLOR=3066993; STATUS="✅ 成功"
          else
            COLOR=15158332; STATUS="❌ 失敗（$RESULT）"
          fi
          curl -sf -X POST "${{ secrets.DISCORD_WEBHOOK }}" \
            -H "Content-Type: application/json" \
            -d @- <<EOF
          {
            "embeds": [{
              "title": "Open Jam Release ${{ github.ref_name }}",
              "description": "映像建置與推送 $STATUS",
              "color": $COLOR,
              "fields": [
                { "name": "Tag", "value": "${{ github.ref_name }}", "inline": true },
                { "name": "Commit", "value": "${{ github.sha }}", "inline": true }
              ],
              "url": "${{ github.server_url }}/${{ github.repository }}/actions/runs/${{ github.run_id }}"
            }]
          }
          EOF
```

## 使用方式

```bash
# 1) 寫 release commit，body 宣告要建的映像
git commit --allow-empty -m "release: v0.1.0

images: auth, creator-web, catalog-service"

# 2) 打 tag 觸發
git tag v0.1.0
git push origin main --tags
```

流程：`setup` 讀該 commit 的 `images:` 行 → 動態 matrix →
`build` 只平行建那幾個並以 WIF 推送 `.../<image>:v0.1.0` 與 `:latest` →
`notify` 依整體結果推 Discord。

推送完成後的部署（更新 `values.prod.yaml` 的 tag、Argo CD 同步）見 [[Infra]] 的「部署流程（Runbook）」。

## 設計取捨

- **手寫 `images:` 清單 vs 自動偵測變更**：release 場景採手寫清單最明確可控（缺點是漏寫就不會建）。若改為 `git diff --name-only <上個 tag> HEAD` 自動推導，需額外處理 `Shared/` 改動要重建所有後端的情況，對 release 反而更易誤判，故不採用。
- **WIF vs SA JSON key**：採 WIF 免長期金鑰、無外洩風險；`attribute-condition` 限定 repo 是安全關鍵。
- **匯總通知 vs 逐映像通知**：預設一則匯總（較不吵）；要逐映像明細可改在 `build` job 內各自 `if: always()` 發送。
