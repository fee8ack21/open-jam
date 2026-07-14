# CI：映像建置與推送

本頁描述 **GitHub Actions** 的持續整合流程：發布（release）時自動建置 Docker 映像、推送至 **GCP Artifact Registry**，並將成功 / 失敗結果彙整推播至 **Discord**。基礎架構全貌與 CD（Helm 部署 GKE）見 [[Infra]]。

## 目標

| 項目 | 值 |
|------|-----|
| Registry Host | `asia-east1-docker.pkg.dev` |
| GCP Project | `<project-id>` |
| Artifact Registry Repo | `open-jam`（region `asia-east1`） |
| 映像命名 | `asia-east1-docker.pkg.dev/<project-id>/open-jam/<image>` |
| 映像 tag | 取自 release commit 條列的版本並加 `v` 前綴（`- portal-web@1.0.0` → `.../portal-web:v1.0.0`），另打 `latest` |

映像清單與命名對齊 `infra/helm/open-jam/values.prod.yaml`（如 `auth`、`catalog-service`、`creator-web`）。

## 觸發策略

以**推 `chore(release)` commit 到 `main`** 觸發，**commit body 的 `- <image-key>@<version>` 清單即宣告要建哪些映像與版本**——無需另打 git tag：

```yaml
on:
  push:
    branches: [main]
```

`setup` job 讀 HEAD commit：subject 非 `chore(release)` 即判定非發佈、輸出 `is_release=false` 直接結束（一般 feature / fix commit 不會觸發建置）。是 release commit 時，解析 body 的 `- key@version` 條列產出 build matrix。此清單本就是 monorepo per-package 版控的既有產物（各 `apps/<app>/package.json` 各自的 `version`，release commit body 列 `- portal-web@0.0.26` 這類 bullet，見 [[Develop]] 的「Release Commit」）。條列的版本部分（`1.0.0`）**自動補 `v` 前綴**成映像 tag（`v1.0.0`），對齊既有的 `v0.0.x` 映像 tag 慣例。

> **一個 release commit = 一次 workflow run**：清單列幾個服務就在同一個 run 內以 matrix 平行建置，最後彙整成**一則** Discord 通知（不再是每服務一則）。

## 建置範圍：由 release commit 清單決定

release commit body 每一行 `- <key>@<version>` 建**一個**對應映像。Workflow 拆三段：`setup` 解析清單、逐項查 manifest 得出 context / Dockerfile 併入版本，組出 `strategy.matrix`；`build` 以 matrix 平行建置並推送各映像；`notify` 彙整結果。image key 對應下表（context 與 Dockerfile 路徑）：

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

> 後端 12 個服務共用 `src/` 為 build context（Dockerfile 內 `COPY` 皆相對 `src/`），前端各自以 app 目錄為 context，文件站以 `docs/` 為 context。清單出現未知的 image key 會讓 `setup` 明確報錯中止。

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

每個 matrix entry 在建置前先查 Artifact Registry 是否已有該版本 tag——以 `google-github-actions/auth` 取得的 GCP access token（`token_format: 'access_token'`）打 registry 的 `GET /v2/<repo>/<image>/manifests/<version>`，回 `200` 即代表版本已存在。此時 login / buildx / build-push 三步皆以 `if: steps.check.outputs.exists != 'true'` 略過，不重建、不覆蓋既有映像；該 entry 仍正常 success，並將自己標記為 `skipped`。重推同一個 release commit 是安全的（已存在的服務標略過、其餘照建）。

## 結果通知（Discord）

`build` 各 matrix entry 於結尾（`if: always()`）依 `job.status` 與略過旗標，把自己的結果（`image` / `version` / `status`：`success` / `skipped` / `failed`）寫成一個結果 artifact 上傳。`notify` job 以 `needs: [setup, build]` + `if: always()` 下載全部結果 artifact，在**同一個 run 內**彙整成**一則** embed：每個服務一行（✅ 成功 / ⏭️ 略過 / ❌ 失敗），只要有任一服務失敗（或 `build` job 整體非 success）整體標題即為紅色「部分失敗」，全數成功則綠色「全部成功」，並附 Actions run 連結。因結果記錄與 artifact 上傳皆為 `if: always()`，**建置 / 推送 / 權限（WIF）/ registry 查詢等 build 階段各步驟的失敗都會被該服務的 entry 記為 `failed` 並發出通知**。

### setup 階段失敗的獨立通知

`notify` 以 `needs.setup.outputs.is_release == 'true'` 為前提，故當 **`setup` 本身失敗**（release commit 清單含未知 image key、清單為空、或 checkout / 工具錯誤）時 `is_release` 為空，`notify` 會被 skip——若無補救，會出現「Actions 紅色失敗但 Discord 無聲」。為此另設 `notify-setup-failure` job（`needs: setup` + `if: always() && needs.setup.result == 'failure'`）補發一則紅色「設定階段失敗」embed。

如此涵蓋 setup 與 build 兩階段的所有失敗；**唯一無法自我通知的情境是 `notify` job 本身失敗**（例如 `DISCORD_WEBHOOK` secret 設錯導致 `curl -sf` 失敗）——通知者自身故障屬結構性限制，須靠 GitHub Actions 失敗通知信或其他管道補強。

## Workflow（`.github/workflows/release.yml`）

```yaml
name: Release build & push

# 觸發策略：push 到 main，且 HEAD 為 chore(release) commit。
# commit body 的 `- <image-key>@<version>` 清單即宣告要建哪些映像與版本，
# 無需另打 git tag——一個 release commit = 一次 workflow run = 一則 Discord 通知。
on:
  push:
    branches: [main]

env:
  REGISTRY: asia-east1-docker.pkg.dev
  IMAGE_PREFIX: asia-east1-docker.pkg.dev/<project-id>/open-jam

jobs:
  # ── 1. 判斷是否為 release commit，解析清單查表產出 build matrix ──
  setup:
    runs-on: ubuntu-latest
    outputs:
      is_release: ${{ steps.parse.outputs.is_release }}
      matrix:     ${{ steps.parse.outputs.matrix }}
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 1
      - id: parse
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

          # 只看 HEAD 這一筆 commit：subject 非 chore(release) 即非發佈，直接結束
          SUBJECT=$(git log -1 --format=%s)
          case "$SUBJECT" in
            chore\(release\)*) : ;;
            *)
              echo "非 release commit（$SUBJECT），略過"
              echo "is_release=false" >> "$GITHUB_OUTPUT"
              exit 0
              ;;
          esac

          # 解析 body 的 `- key@version` 行 → 查 manifest → 併入 version 組出 include
          INCLUDE='[]'
          while IFS='@' read -r KEY VER; do
            [ -z "$KEY" ] && continue
            ENTRY=$(jq -c --arg k "$KEY" '.[$k] // empty' /tmp/manifest.json)
            if [ -z "$ENTRY" ]; then
              echo "::error::未知的 image key '$KEY'（release commit 清單）。合法 key 見 manifest。"
              exit 1
            fi
            INCLUDE=$(echo "$INCLUDE" | jq -c --argjson e "$ENTRY" --arg v "v$VER" '. + [$e + {version:$v}]')
          done < <(git log -1 --format=%b | grep -oE '^- [a-z0-9-]+@[0-9]+\.[0-9]+\.[0-9]+' | sed 's/^- //')

          if [ "$(echo "$INCLUDE" | jq 'length')" -eq 0 ]; then
            echo "::error::release commit 未列出任何 '- <key>@<version>' 項目"
            exit 1
          fi
          echo "is_release=true" >> "$GITHUB_OUTPUT"
          echo "matrix={\"include\":$INCLUDE}" >> "$GITHUB_OUTPUT"

  # ── 2. 平行建置各映像（版本已存在則略過），各自輸出結果 artifact ──
  build:
    needs: setup
    if: needs.setup.outputs.is_release == 'true'
    runs-on: ubuntu-latest
    permissions:
      contents: read
      id-token: write        # WIF 需要
    strategy:
      fail-fast: false       # 單一服務失敗不中斷其他服務
      matrix: ${{ fromJSON(needs.setup.outputs.matrix) }}
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
          IMAGE:   ${{ matrix.image }}
          VERSION: ${{ matrix.version }}
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
          context: ${{ matrix.context }}
          file: ${{ matrix.dockerfile }}
          push: true
          tags: |
            ${{ env.IMAGE_PREFIX }}/${{ matrix.image }}:${{ matrix.version }}
            ${{ env.IMAGE_PREFIX }}/${{ matrix.image }}:latest
          cache-from: type=gha
          cache-to: type=gha,mode=max

      # 各 entry 記錄自己的結果（success / skipped / failed），供 notify 彙整
      - if: always()
        env:
          IMAGE:   ${{ matrix.image }}
          VERSION: ${{ matrix.version }}
          SKIPPED: ${{ steps.check.outputs.exists }}
          JOB:     ${{ job.status }}
        run: |
          if [ "$JOB" = success ] && [ "$SKIPPED" = true ]; then STATUS=skipped
          elif [ "$JOB" = success ]; then STATUS=success
          else STATUS=failed; fi
          jq -nc --arg i "$IMAGE" --arg v "$VERSION" --arg s "$STATUS" \
            '{image:$i,version:$v,status:$s}' > "${IMAGE}.json"

      - if: always()
        uses: actions/upload-artifact@v4
        with:
          name: result-${{ matrix.image }}
          path: ${{ matrix.image }}.json

  # ── 3a. setup 階段失敗（未知 image key／清單為空／前置工具錯誤）也要通知 ──
  notify-setup-failure:
    needs: setup
    if: always() && needs.setup.result == 'failure'
    runs-on: ubuntu-latest
    steps:
      - name: Discord notify (setup failed)
        run: |
          curl -sf -X POST "${{ secrets.DISCORD_WEBHOOK }}" \
            -H "Content-Type: application/json" \
            -d "{\"embeds\":[{\"title\":\"Open Jam Release — ❌ 設定階段失敗\",\"description\":\"release commit 清單解析或前置步驟失敗（未知 image key／清單為空／工具錯誤），未進入建置。請查看 Actions log。\",\"color\":15158332,\"url\":\"${{ github.server_url }}/${{ github.repository }}/actions/runs/${{ github.run_id }}\"}]}"

  # ── 3b. 彙整所有結果，發一則 Discord ──
  notify:
    needs: [setup, build]
    if: always() && needs.setup.outputs.is_release == 'true'
    runs-on: ubuntu-latest
    steps:
      - uses: actions/download-artifact@v4
        with:
          path: results
          pattern: result-*
          merge-multiple: true

      - name: Discord notify
        run: |
          # 組出每行 "✅ `image:version`"（依 image 名排序）
          LINES=$(cat results/*.json | jq -rs 'sort_by(.image)[] |
            (if .status=="skipped" then "⏭️"
             elif .status=="success" then "✅"
             else "❌" end) + " `" + .image + ":" + .version + "`"')
          FAILED=$(cat results/*.json | jq -s '[.[]|select(.status=="failed")]|length')
          if [ "${{ needs.build.result }}" != "success" ] || [ "$FAILED" -gt 0 ]; then
            COLOR=15158332; TITLE="部分失敗"
          else
            COLOR=3066993; TITLE="全部成功"
          fi
          DESC=$(printf '%s' "$LINES" | jq -Rs .)
          curl -sf -X POST "${{ secrets.DISCORD_WEBHOOK }}" \
            -H "Content-Type: application/json" \
            -d "{\"embeds\":[{\"title\":\"Open Jam Release — $TITLE\",\"description\":$DESC,\"color\":$COLOR,\"url\":\"${{ github.server_url }}/${{ github.repository }}/actions/runs/${{ github.run_id }}\"}]}"
```

## 使用方式

發佈只需正常提交一個 `chore(release)` commit（body 條列本次升版的服務）並推上 `main`，CI 即自動建置清單內所有映像——**無需另打 git tag**：

```bash
# release commit（body 條列 <image-key>@<version>，見 [[Develop]] 的「Release Commit」）
git commit -m "chore(release): 發佈新版本" -m "- portal-web@1.0.0
- creator-web@0.0.13"
git push origin main
```

流程：`setup` 確認 HEAD 為 `chore(release)`、解析 body 清單查 manifest 產出 matrix →
`build` 以 matrix 平行建置各映像並以 WIF 推送 `.../portal-web:v1.0.0` 與 `:latest` →
`notify` 下載各 entry 結果 artifact，彙整成一則 Discord embed。整個發佈是**單一** workflow run。

> 一般 feature / fix commit 推上 `main` 也會觸發 workflow，但 `setup` 判定非 release commit 後即結束，`build` / `notify` 不執行、不建任何映像。

推送完成後的部署（更新 `values.prod.yaml` 的 tag、helm upgrade 至 GKE）見 [[Infra]] 的「部署流程（Runbook）」。

## 設計取捨

- **release commit 清單 vs per-package git tag**：改以 `chore(release)` commit body 的 `- <key>@<version>` 清單為建置來源——該清單本就是 release 流程既有產物（見 [[Develop]]），單一事實來源、不必再逐服務打 tag；同一次發佈的多服務落在**同一個 run**，天然彙整成一則通知（避免跨 run 彙整的競態與延遲取捨）。代價是清單需與各服務真實版本一致（release 流程既有要求）。
- **版本補 `v` 前綴**：清單用 `@1.0.0`（對齊 `package.json` 的 `version`），映像 tag 用 `v1.0.0`（對齊既有映像 tag 慣例），由 `setup` 自動轉換。
- **只看 HEAD commit**：`setup` 僅解析推送後的 tip commit。慣例上 release commit 為該次 push 的最後一筆（先 release 再 push），故一般不漏；若 release commit 非 tip 則不會被偵測到。
- **WIF vs SA JSON key**：採 WIF 免長期金鑰、無外洩風險；`attribute-condition` 限定 repo 是安全關鍵。
- **建置前查存在性（冪等）**：以 registry `/v2/.../manifests/<tag>` 查詢比重建再靠 registry 拒絕更省時、也不會覆蓋既有映像；重推同一個 release commit 安全（已存在者標略過）。查詢用 `auth` 產出的 access token（`token_format: 'access_token'`），無需額外裝 `gcloud`。
- **彙整通知（單一 run 內）**：`build` 各 matrix entry 以結果 artifact 回報自身狀態，`notify` 下載彙整成一則 embed，逐服務標示 `image:version`（✅ 成功 / ⏭️ 略過 / ❌ 失敗），任一失敗即整體標紅。
- **失敗通知涵蓋兩階段**：build 階段各步驟失敗（建置 / 推送 / 權限 / registry 查詢）由 `if: always()` 的結果記錄捕捉並通知；setup 階段失敗（清單解析錯誤等）由獨立的 `notify-setup-failure` job 補發。唯一無聲的殘餘情境是 `notify` job 自身失敗（Webhook 設定錯誤），屬通知者無法自我通知的結構性限制。
