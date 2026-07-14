# ArgoCD（GitOps CD）

以 App-of-Apps 模式讓 ArgoCD 持續把 GKE 叢集狀態收斂到本 repo 宣告的狀態。
git = 單一真相；推 `main` 即部署。

> **目前現況**：ArgoCD 尚未 bootstrap（尚未執行下方「Bootstrap」步驟）。正式環境目前由
> `.github/workflows/release.yml` 的 `deploy` job 以 `helm upgrade --install` 直接部署到
> GKE（過渡方案，見該檔案 `deploy` job 上方註解）——`values.prod.yaml` 因 `.gitignore`
> 未進 git，全文改存於 GitHub Actions secret `HELM_VALUES_PROD`，部署時還原成檔案；
> image tag 以 `--set` 於部署當下覆寫，不寫回檔案。本文件其餘內容描述的是完成「前置
> 需求」、執行 Bootstrap 後要切換過去的目標 GitOps 流程，尚未生效。

```
infra/argocd/
├── root.yaml           # App-of-Apps 根 Application（手動 apply 一次）
└── apps/
    ├── open-jam.yaml   # 應用 chart（wave 1，release name 必須為 open-jam）
    └── infra.yaml      # Ingress + cert-manager（wave 2）
```

## Bootstrap（一次性）

```bash
# 1. 安裝 ArgoCD
kubectl create namespace argocd
kubectl apply -n argocd -f https://raw.githubusercontent.com/argoproj/argo-cd/stable/manifests/install.yaml

# 2. 套用 root（之後所有變更都走 git，不再手動 apply）
kubectl apply -n argocd -f infra/argocd/root.yaml

# 3. 取得 admin 初始密碼 / 開 UI
kubectl -n argocd get secret argocd-initial-admin-secret \
  -o jsonpath='{.data.password}' | base64 -d; echo
kubectl -n argocd port-forward svc/argocd-server 8080:443
```

Private repo 需先讓 ArgoCD 有讀取權（`argocd repo add` 或 repo-credentials Secret）。

## 前置需求（尚未完成，導入前必做）

### 1. Secret：External Secrets Operator + GCP Secret Manager ⚠️

`infra/helm/open-jam/values.prod.yaml`（及 `infra/helm/infra/values.prod.yaml`）目前有
**明文密碼**（postgres / sendgrid / hydra / rabbitmq…）。這兩份檔案已列在 `.gitignore`，
**從未進過 git 歷史**，故不需為此輪換密碼；但也因此 ArgoCD（從 GitHub clone repo）目前
**讀不到**這兩份檔案——`apps/open-jam.yaml` / `apps/infra.yaml` 的 `helm.valueFiles` 指向
的檔案在遠端 repo 上並不存在，Application 會同步失敗。這是導入 GitOps 前必須解決的**阻塞
問題**，不只是安全性建議：

- 安裝 External Secrets Operator，配 GKE Workload Identity 存取 GCP Secret Manager。
- 密碼寫進 Secret Manager，git 只放 `ExternalSecret` CR（引用，不含值）。
- open-jam chart 改為讀「既有 Secret」而非 `.Values.secrets.*` 明文。
- `values.prod.yaml` 完成遷移、不再含明文密碼後，才可移出 `.gitignore` 並提交進
  git，讓 ArgoCD 讀得到其餘非機敏設定值；機敏值改由叢集中既有的 `ExternalSecret`
  同步出的 Secret 提供。
- cert-manager 用的 `cloudflare-api-token` Secret 同理由 External Secrets 同步。

### 2. Image tag：CI write-back

`values.yaml` 目前所有服務 `tag: latest`；GitOps 需 pinned tag 才能靠 git diff 觸發同步。
待 ArgoCD 正式啟用後，`.github/workflows/release.yml` 建置成功後需追加一步：以 `yq`
將剛發佈的版本寫回 `values.prod.yaml` 對應服務的 `tag`，並 commit 回 `main` → ArgoCD
偵測 git 變化自動同步，如此 git log 即完整部署史。

> 現行過渡方案（見上方「目前現況」）不做這件事——`deploy` job 是以 `--set` 在部署當下
> 覆寫 tag，不寫回 `values.prod.yaml`（該檔本就不進 git）。等 secret 遷移完成、
> `values.prod.yaml` 可進 git 之後，才會改成這裡描述的 write-back 流程。

## 已知後續（本次未動 chart templates）

- **一次性 Job**（`bootstrap/job.yaml`、`hydra/migrate-job.yaml`）完成後會讓 Application
  一直顯示 OutOfSync。建議加 `argocd.argoproj.io/hook: PreSync`（migration）/ 設為
  `Sync` hook 並搭配 `hook-delete-policy`，或標 `Replace=true`。
- **cert-manager 本體 / CRDs** 需先於 infra chart 存在，可再加一個更早 wave 的
  Application（或 Helm dependency）納管。
