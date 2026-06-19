# 商店服務 StoreService

StoreService 面向**創作者**，管理開店申請、商店資料、商店成員與追蹤者。商品本身的欄位/狀態機見 [[Product]]，檔案儲存與簽章直傳見 [[Storage]]。

## 服務職責

- 受理「開店申請」並由平台管理員審核，審核通過後建立商店。
- 管理商店基本資料（名稱、描述、Avatar、Banner、狀態）。
- 管理商店成員（目前僅 Owner）。
- 管理商店追蹤者（憑信箱追蹤，呼應 [[Order]] 的未註冊購買模式）。
- Avatar / Banner 圖片透過 [[Storage]] 簽發上傳 URL，落地記錄於自己的 Assets 表。

## 資料模型

### Stores

| 欄位 | 型別 | 說明 |
|---|---|---|
| Id | Guid | PK |
| StoreName | string | 顯示名稱 |
| StoreSlug | string | unique，子網域 `<slug>.openjam.co` 用。小寫英數字 + 連字號，3–30 字，不可開頭/結尾為 `-`，含保留字黑名單（`www`、`api`、`admin`、`app`、`store` 等） |
| Description | string? | |
| AvatarAssetId | Guid? | FK → Assets |
| BannerAssetId | Guid? | FK → Assets |
| Status | StoreStatus | `Active` / `Suspended` / `Closed` |
| CreatedAt / UpdatedAt | DateTimeOffset / DateTimeOffset? | |

### StoreMembers

| 欄位 | 型別 | 說明 |
|---|---|---|
| Id | Guid | PK |
| StoreId | Guid | FK |
| UserId | Guid | |
| Role | StoreMemberRole | 目前僅 `Owner`，enum 預留未來擴充（如 Staff） |

唯一索引 `(StoreId, UserId)`。MVP 不提供成員管理 API，僅在開店申請核准時自動建立一筆 Owner。

### StoreApplications

| 欄位 | 型別 | 說明 |
|---|---|---|
| Id | Guid | PK |
| UserId | Guid | 申請人 |
| StoreName | string | |
| StoreSlug | string | |
| Status | StoreApplicationStatus | `Pending` / `Approved` / `Rejected` / `Withdrawn` |
| CreatedAt | DateTimeOffset | |
| ReviewedAt | DateTimeOffset? | |
| ReviewedBy | Guid? | 審核管理員 |
| ReviewComment | string? | |

- 過濾型唯一索引 `(UserId) WHERE Status = 'Pending'`：同一使用者僅能有一筆 Pending 申請。
- `Rejected` / `Withdrawn` 可重新提交新申請；`Approved` 觸發建立 Store + StoreMember(Owner)。
- **Slug 唯一性檢查範圍**：`Stores.StoreSlug` ∪ `StoreApplications.StoreSlug WHERE Status = 'Pending'`（已結案的申請不佔用 slug）。

### StoreFollowers

| 欄位 | 型別 | 說明 |
|---|---|---|
| Id | Guid | PK（surrogate） |
| StoreId | Guid | FK |
| UserId | Guid? | null 表示尚未關聯帳號（訪客憑信箱追蹤） |
| Email | string | |

唯一索引 `(StoreId, Email)`。`UserId` 補關聯為**未來工作**：Auth 註冊後發出 Event，StoreService consume 後回填，本次 MVP 僅保留欄位、不實作 consumer。

### Assets

| 欄位 | 型別 | 說明 |
|---|---|---|
| Id | Guid | PK，與 [[Storage]] 簽發的 FileId 相同值 |
| CreatedBy | Guid | |
| StorageKey | string | |
| FileName | string | |
| ContentType | string | |
| FileSize | long | |
| CreatedAt | DateTimeOffset | |

獨立表，不對 StorageService 的 StoredFiles 做反向查詢或 FK。

## API 端點

> 所有端點皆以 API 版本前綴 `/v1` 開頭（URL segment versioning，見 `Shared/Web/ApiVersioningExtensions.cs`）。下表路徑已含前綴。

### 開店申請（StoreApplications）

| Method/Path | 權限 | 說明 |
|---|---|---|
| `POST /v1/store-applications` | 登入使用者 | 提交申請 `{StoreName, StoreSlug}`；驗證 slug 格式/保留字/唯一性，檢查無 Pending 申請 |
| `GET /v1/store-applications/me` | 登入使用者 | 自己的申請紀錄（分頁，offset/limit） |
| `POST /v1/store-applications/{id}/withdraw` | 申請人本人 | `Pending` → `Withdrawn` |
| `GET /v1/store-applications` | Admin | 全平台申請列表，可依 Status 篩選（分頁） |
| `POST /v1/store-applications/{id}/approve` | Admin | `Pending` → `Approved`；建立 Store + StoreMember(Owner)；寫入 ReviewedAt/ReviewedBy |
| `POST /v1/store-applications/{id}/reject` | Admin | `Pending` → `Rejected`；body 含 ReviewComment |

### 商店（Stores）

| Method/Path | 權限 | 說明 |
|---|---|---|
| `GET /v1/stores/{idOrSlug}` | 公開 | 商店基本資訊（含 Avatar/Banner 公開 URL） |
| `GET /v1/stores/me` | 登入使用者 | 透過 StoreMembers 查自己所屬商店 |
| `PATCH /v1/stores/{id}` | Owner | 更新 StoreName / Description |
| `POST /v1/stores/{id}/suspend` `/unsuspend` | Admin | 平台停權 / 解除停權 |
| `POST /v1/stores/{id}/close` | Owner 或 Admin | `Active`/`Suspended` → `Closed`（終態，不可逆） |

### Avatar / Banner 上傳

| Method/Path | 權限 | 說明 |
|---|---|---|
| `POST /v1/stores/{id}/avatar/upload-url` | Owner | 申請上傳 URL，建立 Asset，設定 `AvatarAssetId` |
| `POST /v1/stores/{id}/banner/upload-url` | Owner | 同上，設定 `BannerAssetId` |

### 追蹤者（StoreFollowers）

| Method/Path | 權限 | 說明 |
|---|---|---|
| `POST /v1/stores/{id}/follow` | 公開 | body `{Email}`；登入使用者另帶 UserId。已存在則 no-op（204） |
| `DELETE /v1/stores/{id}/follow` | 公開 | body `{Email}`，依 `(StoreId, Email)` 取消追蹤 |
| `GET /v1/stores/{id}/followers` | Owner | 分頁列表（offset/limit），回傳 Email/UserId/CreatedAt |

## 業務規則與 Outbox 事件

- 提交申請 → `AuditLogRequestedEvent`（action: `store.application.submit`）
- **Approve**：transaction 內建立 `Store`（Status=Active）+ `StoreMember`（Role=Owner, UserId=申請人）+ 更新 Application（Status=Approved, ReviewedAt, ReviewedBy）→ `AuditLogRequestedEvent`（action: `store.application.approve`, Tenant=新 StoreId）+ `EmailRequestedEvent`（template `email.store_application_approved`）
- **Reject**：更新 Application（Status=Rejected, ReviewedAt, ReviewedBy, ReviewComment）→ `AuditLogRequestedEvent`（action: `store.application.reject`）+ `EmailRequestedEvent`（template `email.store_application_rejected`，帶 ReviewComment）
- `suspend` / `unsuspend` / `close` → `AuditLogRequestedEvent`（action 對應 `store.suspend` / `store.unsuspend` / `store.close`, Tenant=StoreId）
- Follow / Unfollow 為高頻、低風險操作，**不寫 AuditLog**

### 錯誤處理（沿用 `Shared.Exceptions.AppException` 子類）

| 情境 | Exception |
|---|---|
| Slug 格式錯誤 / 保留字 / 已被使用 | `ValidationException(422)` |
| 已有 Pending 申請 | `ValidationException(422)` |
| Avatar/Banner ContentType 不允許 | `ValidationException(422)` |
| Store/Application 不存在 | `NotFoundException(404)` |
| 非 Owner 操作他人商店 / 非 Admin 審核 | `ForbiddenException(403)` |

## Avatar / Banner 上傳流程（含 StorageService 擴充）

[[Storage]] 既有的簽章直傳機制僅支援私有檔案（presigned GET，5 分鐘過期），不適合需長期、可快取顯示的商店 Avatar/Banner。本服務複用 StorageService 的 URL 簽發，但需擴充其支援「公開讀取」物件：

### StorageService 擴充

1. `StorageOptions` 新增 `PublicBaseUrl`（dev: `http://localhost:5171/v1/files/blob`，正式環境為 CDN/GCS public URL）。
2. `RequestUploadUrlRequest` 新增 `bool IsPublic`（預設 false）。
3. `IsPublic=true` 時 `StoredFile.StorageKey` 改用 `public/{creatorId}/{fileId}/{originalName}`（取代 `creators/...`），其餘規則不變。
4. `RequestUploadUrlResponse` 新增 `string StorageKey` 與 `string? PublicUrl`（`IsPublic=true` 時填值：`{PublicBaseUrl}/{StorageKey}`）。
5. `public/*` 前綴的物件供匿名讀取（地端由 `BlobController` 對 `public/` 前綴免簽章放行；雲端 GCS 對 `{bucket}/public/*` 設定 public read）。

### 上傳流程（以 Avatar 為例）

1. 前端呼叫 `POST /v1/stores/{id}/avatar/upload-url`，body `{FileName, ContentType, SizeBytes}`。
2. StoreService 驗證呼叫者為該 Store 的 Owner，且 ContentType 為允許的圖片格式（jpeg/png/gif/webp）。
3. StoreService 呼叫 StorageService `POST /v1/files/upload-url`：`CreatorId=ownerId, ProductId=null, FileType=Image, IsPreview=false, IsPublic=true, OriginalName, ContentType, SizeBytes`。
4. StorageService 回傳 `FileId, UploadUrl, StorageKey, PublicUrl, ExpiresAt`。
5. StoreService 寫入 `Assets`：`Id=FileId, CreatedBy=ownerId, StorageKey, FileName, ContentType, FileSize=SizeBytes, CreatedAt=now`，並設 `Stores.AvatarAssetId = Asset.Id`。
6. 回傳 `{AssetId, UploadUrl, PublicUrl, ExpiresAt}` 給前端，前端以 `UploadUrl` 直接 PUT 至儲存後端（地端為 StorageService blob 端點，雲端為 GCS）。

`GET /v1/stores/{idOrSlug}` 回傳的 `AvatarUrl` / `BannerUrl` = `{PublicBaseUrl}/{Asset.StorageKey}`（StoreService 自行組字串，需設定 `Storage:PublicBaseUrl`，與 StorageService 同值）。

舊 Avatar/Banner 的 Asset 記錄與儲存後端物件**不主動清除**（MVP 不做孤兒清理，留待未來）。

## 平台基礎設施：JWT 驗證與 Role

StoreApplications 的審核（approve/reject）需要管理員權限，但目前所有 REST API 服務（[[Log]]、[[Storage]]）都還沒有實際的 JWT Bearer 驗證 middleware（`ICurrentUserAccessor` 只讀 Claims，無 `AddAuthentication`/`AddJwtBearer`）。本服務同時補上這塊平台基礎設施：

1. **[[Auth]] User.Role**：`User` entity 新增 `Role` 屬性，`UserRole` enum（`User` / `Admin`），預設 `User`，新增 EF Core migration（`open_jam_auth`）。Admin 指派方式：MVP 不提供 API/UI，由維運人員直接更新資料庫 `users.role` 欄位。

2. **Hydra：JWT Access Token + role claim**
   - `infra/docker/docker-compose.yaml` 與 helm hydra deployment 加上環境變數 `STRATEGIES_ACCESS_TOKEN=jwt`。
   - `HydraConsentSession`（`HydraModels.cs`）新增 `AccessToken` 屬性（型別同 `IdToken`：`Dictionary<string, object>?`）。
   - `HomeController.Consent`：依 `info.Subject` 查詢 `User`，將 `{"role": user.Role.ToString()}` 放入 `Session.AccessToken`。
   - Hydra 會將自訂 claims 包在 JWT access token 的 `ext` 物件下（即 `ext.role`），而非頂層 claim。

3. **Shared：JwtBearer 驗證 + Admin Policy**
   - 新增 `Shared/Auth/JwtBearerExtensions.cs`，提供 `AddOpenJamJwtAuth(IConfiguration config)`：
     - `AddAuthentication().AddJwtBearer()`：`Authority` = Hydra public issuer URL（新增設定 `Hydra:Issuer`，例如 `http://localhost:4444/`），`RequireHttpsMetadata` 依環境決定，`ValidateAudience = false`（目前無 client 要求 audience）。
     - `MapInboundClaims = false`，保留原始 `sub`、`ext` claim 名稱。
     - `OnTokenValidated` event：解析 `ext` claim（JSON），取出 `role` 並轉成標準 `ClaimTypes.Role` claim加入 principal。
     - `AddAuthorizationBuilder().AddPolicy("Admin", p => p.RequireRole("Admin"))`。
   - 各服務 `Program.cs` 加上 `app.UseAuthentication()` + `app.UseAuthorization()`（目前都缺）。

4. **ICurrentUserAccessor 整併**：`Auth/Services/HttpContextUserAccessor.cs` 與 `StorageService/Services/WorkerCurrentUserAccessor.cs` 中重複的 `HttpContextUserAccessor`（讀 `sub` claim）移至 `Shared/Auth/HttpContextUserAccessor.cs`，各服務改為注入共用版本。

5. **套用範圍**：`AddOpenJamJwtAuth` + `UseAuthentication`/`UseAuthorization` 套用到 LogService、StorageService、StoreService（新）。**不**改變現有 endpoint 的 `[Authorize]` 標註，僅讓中介軟體就緒，供 StoreService 的審核 endpoint 使用 `[Authorize(Policy = "Admin")]`。

## 環境設定

- DB：`open_jam_store`，dev port **5172**（接續 Auth 5169 / LogService 5170 / StorageService 5171）。
- `Storage:PublicBaseUrl`、`Services:StorageService:BaseUrl`（服務間呼叫 StorageService）。
- `Hydra:Issuer`（JWKS 驗證用）。

## 未來工作（不在本次範圍）

- StoreFollowers.UserId 回填：Auth 註冊發出 `UserRegisteredEvent`，StoreService consume 並依 Email 比對回填。
- Avatar/Banner 舊資產的孤兒清理。
- StoreMembers 多角色（Staff 等）與成員管理 API。
