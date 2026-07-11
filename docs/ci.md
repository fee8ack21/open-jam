# CI：映像建置與推送

本頁描述 **GitHub Actions** 的持續整合流程：發布（release）時自動建置 Docker 映像、推送至 **GCP Artifact Registry**，並將成功 / 失敗結果推播至 **Discord**。基礎架構全貌與 CD（Argo CD / Helm）見 [[Infra]]。

## 目標

| 項目 | 值 |
|------|-----|
| Registry Host | `asia-east1-docker.pkg.dev` |
| GCP Project | `<project-id>` |
| Artifact Registry Repo | `open-jam`（region `asia-east1`） |
| 映像命名 | `asia-east1-docker.pkg.dev/<project-id>/open-jam/<image>` |
| 映像 tag | 取自 git tag 的版本並加 `v` 前綴（`portal-web@1.0.0` → `.../portal-web:v1.0.0`），另打 `latest` |

映像清單與命名對齊 `infra/helm/open-jam/values.prod.yaml`（如 `auth`、`catalog-service`、`creator-web`）。

## 觸發策略

以**推 per-package git tag** 觸發，tag 格式為 `<image-key>@<version>`（例：`portal-web@1.0.0`）。**tag 名稱本身即宣告要建哪個映像與版本**——無需在 commit 訊息另寫宣告：

```yaml
on:
  push:
    tags: ['*@*']
```

此格式對齊 monorepo per-package 版控慣例（各 `apps/<app>/package.json` 各自的 `version`，release commit body 列 `- portal-web@0.0.26` 這類 bullet）。tag 的版本部分（`1.0.0`）**自動補 `v` 前綴**成映像 tag（`v1.0.0`），對齊既有的 `v0.0.x` 映像 tag 慣例。

## 建置範圍：由 tag 名稱決定

一個 tag 建**一個**對應映像。tag 的 `@` 之前為 image key，之後為版本。Workflow 拆兩段：`setup` 從 `github.ref_name` 拆出 key 與版本、查 manifest 得出 context / Dockerfile；`build` 建置並推送該單一映像。key 對應下表（context 與 Dockerfile 路徑）：

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
| docs | `docs` | `docs` | `docs/Dockerfile` |

> 後端 12 個服務共用 `src/` 為 build context（Dockerfile 內 `COPY` 皆相對 `src/`），前端各自以 app 目錄為 context，文件站以 `docs/` 為 context。未知的 image key 會讓 `setup` 明確報錯中止。

## 認證：Workload Identity Federation

CI 以 **WIF（免長期金鑰）** 換取 GCP 憑證，非 SA JSON key。一次性設定：

```bash
PROJECT=<project-id>
POOL=github-pool
PROVIDER=github-provider
REPO=<owner>/open-jam                       # 換成實際 GitHub owner/repo
SA=open-jam-ci@$PROJECT.iam.gserviceaccount.com

# 1) Artifact Registry repo（若尚未建立）
gcloud artifacts repositories create open-jam \
  --repository-format=docker --location=asia-east1 --project=$PROJECT

# 2) CI 用 Service Account + 授 AR 推送權
gcloud iam service-accounts create open-jam-ci --project=$PROJECT
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

Workflow 需要三個 repository secret：

| Secret | 內容 | 取得方式 |
|--------|------|----------|
| `GCP_WIF_PROVIDER` | WIF provider 完整資源名<br>（`projects/<PN>/locations/global/workloadIdentityPools/github-pool/providers/github-provider`） | 上面 WIF 設定第 6 步的輸出 |
| `GCP_SA_EMAIL` | `open-jam-ci@<project-id>.iam.gserviceaccount.com` | 固定值（WIF 設定第 2 步建立的 SA） |
| `DISCORD_WEBHOOK` | `https://discord.com/api/webhooks/<id>/<token>` | Discord 見下方步驟 |

**取得 Discord webhook**：Discord 伺服器 → 目標頻道 → 編輯頻道 → 整合（Integrations）→ 建立 Webhook → 複製 Webhook URL。

**設定 secret（擇一）**：

- **網頁 UI**：Repo → Settings → Secrets and variables → Actions → New repository secret，逐一新增上表三個（名稱須完全一致）。
- **`gh` CLI**（需先 `gh auth login`；`<owner>/open-jam` 換成實際 repo，或在 repo 目錄內省略 `--repo`）：

  ```bash
  gh secret set GCP_WIF_PROVIDER --repo <owner>/open-jam \
    --body "projects/<PN>/locations/global/workloadIdentityPools/github-pool/providers/github-provider"
  gh secret set GCP_SA_EMAIL --repo <owner>/open-jam \
    --body "open-jam-ci@<project-id>.iam.gserviceaccount.com"
  gh secret set DISCORD_WEBHOOK --repo <owner>/open-jam \
    --body "https://discord.com/api/webhooks/<id>/<token>"
  ```

  確認已設定：`gh secret list --repo <owner>/open-jam`。

## 冪等：版本已存在則略過

`build` job 在建置前先查 Artifact Registry 是否已有該版本 tag——以 `google-github-actions/auth` 取得的 GCP access token（`token_format: 'access_token'`）打 registry 的 `GET /v2/<repo>/<image>/manifests/<version>`，回 `200` 即代表版本已存在。此時 login / buildx / build-push 三步皆以 `if: steps.check.outputs.exists != 'true'` 略過，不重建、不覆蓋既有映像；`build` job 仍正常 success，並將 `skipped=true` 傳給 `notify`。重推同一個 tag 是安全的。

## 結果通知（Discord）

`notify` job 以 `needs: [setup, build]` + `if: always()` 讀取 `needs.build.result` 與 `needs.build.outputs.skipped`，推一則帶顏色與 Actions run 連結的 embed，列出本次的 `image:version`：綠色成功、灰色略過（版本已存在）、紅色失敗。

## Workflow（`.github/workflows/release.yml`）

```yaml
name: Release build & push

# 觸發策略：推 per-package git tag，格式 `<image-key>@<version>`（例：portal-web@1.0.0）。
# tag 名稱本身即宣告要建哪個映像與版本，無需在 commit 訊息另寫 images: 行。
on:
  push:
    tags: ['*@*']

env:
  REGISTRY: asia-east1-docker.pkg.dev
  IMAGE_PREFIX: asia-east1-docker.pkg.dev/<project-id>/open-jam

jobs:
  # ── 1. 解析 tag 名稱，查表得出 image / context / dockerfile / version ──
  setup:
    runs-on: ubuntu-latest
    outputs:
      image:      ${{ steps.pick.outputs.image }}
      context:    ${{ steps.pick.outputs.context }}
      dockerfile: ${{ steps.pick.outputs.dockerfile }}
      version:    ${{ steps.pick.outputs.version }}
    steps:
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
            "workspace-web":        { "image": "workspace-web",        "context": "apps/workspace-web", "dockerfile": "apps/workspace-web/Dockerfile" },
            "docs":                 { "image": "docs",                 "context": "docs",               "dockerfile": "docs/Dockerfile" }
          }
          JSON

          TAG="${{ github.ref_name }}"          # 例：portal-web@1.0.0
          KEY="${TAG%@*}"                        # @ 之前：portal-web
          RAW_VERSION="${TAG##*@}"               # 最後一個 @ 之後：1.0.0

          if [ "$KEY" = "$TAG" ] || [ -z "$RAW_VERSION" ]; then
            echo "::error::tag '$TAG' 格式錯誤，需為 <image-key>@<version>（例：portal-web@1.0.0）"
            exit 1
          fi

          VERSION="v$RAW_VERSION"                 # image tag 加 v 前綴：v1.0.0

          ENTRY=$(jq -c --arg k "$KEY" '.[$k] // empty' /tmp/manifest.json)
          if [ -z "$ENTRY" ]; then
            echo "::error::未知的 image key '$KEY'（tag '$TAG'）。合法 key 見 manifest。"
            exit 1
          fi

          echo "image=$(echo "$ENTRY"      | jq -r .image)"      >> "$GITHUB_OUTPUT"
          echo "context=$(echo "$ENTRY"    | jq -r .context)"    >> "$GITHUB_OUTPUT"
          echo "dockerfile=$(echo "$ENTRY" | jq -r .dockerfile)" >> "$GITHUB_OUTPUT"
          echo "version=$VERSION"                                >> "$GITHUB_OUTPUT"

  # ── 2. 建置並推送該映像（版本已存在則略過） ──
  build:
    needs: setup
    runs-on: ubuntu-latest
    permissions:
      contents: read
      id-token: write        # WIF 需要
    outputs:
      skipped: ${{ steps.check.outputs.exists }}
    steps:
      - uses: actions/checkout@v4

      - id: auth
        uses: google-github-actions/auth@v2
        with:
          workload_identity_provider: ${{ secrets.GCP_WIF_PROVIDER }}
          service_account: ${{ secrets.GCP_SA_EMAIL }}
          token_format: 'access_token'        # 產出 steps.auth.outputs.access_token

      # 查 Artifact Registry 是否已有該版本 tag（回 200 = 已存在）
      - id: check
        env:
          IMAGE:   ${{ needs.setup.outputs.image }}
          VERSION: ${{ needs.setup.outputs.version }}
          TOKEN:   ${{ steps.auth.outputs.access_token }}
        run: |
          REPO_PATH="${IMAGE_PREFIX#${REGISTRY}/}"   # <project-id>/open-jam
          URL="https://${REGISTRY}/v2/${REPO_PATH}/${IMAGE}/manifests/${VERSION}"
          CODE=$(curl -s -o /dev/null -w '%{http_code}' \
            -H "Authorization: Bearer ${TOKEN}" \
            -H "Accept: application/vnd.docker.distribution.manifest.v2+json,application/vnd.oci.image.manifest.v1+json,application/vnd.oci.image.index.v1+json,application/vnd.docker.distribution.manifest.list.v2+json" \
            "$URL")
          if [ "$CODE" = "200" ]; then
            echo "exists=true" >> "$GITHUB_OUTPUT"
            echo "::notice::${IMAGE}:${VERSION} 已存在於 Artifact Registry，略過建置"
          else
            echo "exists=false" >> "$GITHUB_OUTPUT"
            echo "${IMAGE}:${VERSION} 不存在（HTTP $CODE），開始建置"
          fi

      - if: steps.check.outputs.exists != 'true'
        uses: docker/login-action@v3
        with:
          registry: ${{ env.REGISTRY }}
          username: oauth2accesstoken
          password: ${{ steps.auth.outputs.access_token }}

      - if: steps.check.outputs.exists != 'true'
        uses: docker/setup-buildx-action@v3

      - if: steps.check.outputs.exists != 'true'
        uses: docker/build-push-action@v6
        with:
          context: ${{ needs.setup.outputs.context }}
          file: ${{ needs.setup.outputs.dockerfile }}
          push: true
          tags: |
            ${{ env.IMAGE_PREFIX }}/${{ needs.setup.outputs.image }}:${{ needs.setup.outputs.version }}
            ${{ env.IMAGE_PREFIX }}/${{ needs.setup.outputs.image }}:latest
          cache-from: type=gha
          cache-to: type=gha,mode=max

  # ── 3. 將結果推播 Discord ──
  notify:
    needs: [setup, build]
    runs-on: ubuntu-latest
    if: always()
    steps:
      - name: Discord notify
        env:
          RESULT:  ${{ needs.build.result }}          # success / failure / cancelled / skipped
          SKIPPED: ${{ needs.build.outputs.skipped }}  # true = 版本已存在
        run: |
          if [ "$RESULT" = "success" ] && [ "$SKIPPED" = "true" ]; then
            COLOR=9807270; STATUS="⏭️ 略過（版本已存在）"
          elif [ "$RESULT" = "success" ]; then
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
                { "name": "Image", "value": "${{ needs.setup.outputs.image }}:${{ needs.setup.outputs.version }}", "inline": true },
                { "name": "Commit", "value": "${{ github.sha }}", "inline": true }
              ],
              "url": "${{ github.server_url }}/${{ github.repository }}/actions/runs/${{ github.run_id }}"
            }]
          }
          EOF
```

## 使用方式

一個 tag 建一個映像。要一次 release 多個服務就打多個 tag：

```bash
# 打 per-package tag（<image-key>@<version>）並推送觸發
git tag portal-web@1.0.0
git tag creator-web@0.0.13
git push origin portal-web@1.0.0 creator-web@0.0.13
```

流程：`setup` 從 tag 名稱拆出 key 與版本、查 manifest →
`build` 建置該映像並以 WIF 推送 `.../portal-web:v1.0.0` 與 `:latest` →
`notify` 依結果推 Discord。多個 tag 各自觸發一次 workflow run。

推送完成後的部署（更新 `values.prod.yaml` 的 tag、Argo CD 同步）見 [[Infra]] 的「部署流程（Runbook）」。

## 設計取捨

- **per-package tag vs 單一版本 tag + `images:` 清單**：採 per-package tag（`<image-key>@<version>`）對齊 monorepo 各 app 獨立版控的慣例，tag 名稱自帶「建什麼、建哪版」兩項資訊，最直觀；代價是一次 release 多服務要打多個 tag。
- **tag 版本補 `v` 前綴**：git tag 用 `@1.0.0`（對齊 `package.json` 的 `version`），映像 tag 用 `v1.0.0`（對齊既有映像 tag 慣例），由 `setup` 自動轉換。
- **WIF vs SA JSON key**：採 WIF 免長期金鑰、無外洩風險；`attribute-condition` 限定 repo 是安全關鍵。
- **建置前查存在性（冪等）**：以 registry `/v2/.../manifests/<tag>` 查詢比重建再靠 registry 拒絕更省時、也不會覆蓋既有映像；重推同 tag 安全。查詢用 `auth` 產出的 access token（`token_format: 'access_token'`），無需額外裝 `gcloud`。
- **逐映像通知**：一個 tag 一則 Discord embed，明確標示 `image:version`（成功 / 略過 / 失敗三色）。
