# 平台初始化 Bootstrap

Bootstrap 負責**預建平台運行所需的資料**。因架構採 **DB per service**（見 [[Infra]]），各服務自管自己 DB 的 seed；Bootstrap 不直接寫各服務的 DB，而是負責**跨服務協調**（執行順序）與**跨服務 / 基建共用資料**。以**冪等 Job** 於部署時自動執行，seed 比照 migration 版本控管。

## 職責邊界

- **各服務自管 seed**：各微服務 seed 自己 DB 的資料（尊重 DB per service 邊界）。
- **Bootstrap 負責**：
  - 跨服務執行順序協調。
  - 跨服務 / 基建共用資料（如 Hydra Client、子網域保留字）。

## 執行模型

- 以 **K8s Job / Argo CD hook** 在部署時自動執行（見 [[Infra]]）。
- **冪等**：已存在則跳過，可安全重跑。
- seed 比照 **migration 版本控管**，可增量套用（保留字、分類、方案等會演進）。

## 預建資料清單

### 平台必要（prod）

| 資料 | 歸屬 | 來源 |
|------|------|------|
| 子網域保留字 | Bootstrap（共用）| [[Catalog]] / [[Auth]] / [[Quota]] |
| Hydra Client（各 SPA / API 的 OIDC client）| Bootstrap（基建）| [[Auth]] |
| RBAC 角色與權限（Consumer / Creator / Admin）| Auth | [[Auth]] |
| 初始 Admin 帳號與角色 | Auth | [[Auth]] |
| Email Template | EmailService | [[Email]] |
| 平台固定分類 | Catalog | [[Catalog]] |
| 固定配額額度（設定值，不分方案）| Quota | [[Quota]] |

### 開發假資料（dev only）

- 預設使用者（消費者 & 創作者）。
- 測試商品。

## 相依順序

- Hydra Client → 各 SPA / API。
- RBAC 角色 → 初始 Admin。
- 創作者方案 → 配額額度。
- 平台固定分類 → （商品）。

## 技術與架構

- 部署方式（Argo hook / Job）見 [[Infra]]。
- 各服務的 migration 與 seed 慣例見 [[Develop]]。
