# ArgoCD（GitOps CD）

以 App-of-Apps 模式讓 ArgoCD 持續把 GKE 叢集狀態收斂到本 repo 宣告的狀態。
git = 單一真相；推 `main` 即部署。

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

`infra/helm/open-jam/values.prod.yaml` 目前有**明文密碼**（postgres / sendgrid / hydra /
rabbitmq…）。GitOps 下整個 repo 皆為公開真相，**必須先把 secret 移出 git**：

- 安裝 External Secrets Operator，配 GKE Workload Identity 存取 GCP Secret Manager。
- 密碼寫進 Secret Manager，git 只放 `ExternalSecret` CR（引用，不含值）。
- open-jam chart 改為讀「既有 Secret」而非 `.Values.secrets.*` 明文。
- 因這批密碼已進過 git 歷史，導入前先**輪換（rotate）**。
- cert-manager 用的 `cloudflare-api-token` Secret 同理由 External Secrets 同步。

### 2. Image tag：CI write-back

`values.yaml` 目前所有服務 `tag: latest`；GitOps 需 pinned tag 才能靠 git diff 觸發同步。
在 `.github/workflows/release.yml` 建置成功後追加一步：以 `yq` 將剛發佈的版本寫回
`values.prod.yaml` 對應服務的 `tag`，並 commit 回 `main` → ArgoCD 偵測 git 變化自動同步。
如此 git log 即完整部署史。

## 已知後續（本次未動 chart templates）

- **一次性 Job**（`bootstrap/job.yaml`、`hydra/migrate-job.yaml`）完成後會讓 Application
  一直顯示 OutOfSync。建議加 `argocd.argoproj.io/hook: PreSync`（migration）/ 設為
  `Sync` hook 並搭配 `hook-delete-policy`，或標 `Replace=true`。
- **cert-manager 本體 / CRDs** 需先於 infra chart 存在，可再加一個更早 wave 的
  Application（或 Helm dependency）納管。
