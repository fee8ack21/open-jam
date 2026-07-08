# 開發慣例 Develop

本頁是 Open Jam 的開發規範：架構慣例、專案結構，以及前後端開發注意事項與 coding style。

## 架構慣例

- **軟體三層 + Service DI**。

## 專案結構

```
src/
  Auth/                 # 認證授權（OIDC / Hydra）
  EmailService/         # 寄信服務
  StorageService/       # 檔案儲存 / 轉碼 / 預覽
  LogService/           # Audit Log / 日誌
  StoreService/         # 開店申請 / 商店 / 追蹤
  CatalogService/       # 商品 / 版本 / 分類 / 標籤 / 評論 / 收藏
  OrderService/         # 訂單 / 結帳入口 / 狀態歷程
  PaymentService/       # Stripe Checkout 金流 / Webhook
  QuotaService/         # 資源配額計量
  NotificationService/  # 追蹤者通知（上架 / 公告）
  Bootstrap/            # 平台初始化 seed
  Shared/               # 共用程式庫
apps/
  workspace-web/    # 用戶後台
  creator-web/      # 創作者商品空間
  portal-web/       # 平台首頁，探索各產品與創作者
scripts/
  publish/
infra/
  docker/
  helm/
docs/
```

## Commit 規範

採用 [Conventional Commits v1.0.0](https://www.conventionalcommits.org/en/v1.0.0/) 規範。

### 格式

```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

### Type 列表

| Type | 用途 |
|------|------|
| `feat` | 新功能（SemVer MINOR） |
| `fix` | 修正錯誤（SemVer PATCH） |
| `refactor` | 既非修正也非新增功能的程式碼異動 |
| `docs` | 僅文件異動 |
| `style` | 格式調整、補分號等（不影響邏輯） |
| `test` | 新增或修正測試 |
| `chore` | 建置流程、工具鏈、依賴套件升級 |
| `ci` | CI/CD 設定 |
| `perf` | 效能優化 |
| `build` | 影響建置系統的異動 |

### 規則

1. Type 必填，須為名詞（`feat`、`fix` 等）。
2. Scope 選填，以括號標註受影響範圍，例如 `feat(auth):`。
3. type/scope 後須接冒號與空白。
4. Description 必填，使用祈使句簡述異動內容。
5. Body 選填，與 description 間以一行空白分隔。
6. Footer 選填，與 body 間以一行空白分隔。
7. 重大變更：在冒號前加 `!` 與／或在 footer 加註 `BREAKING CHANGE: <description>`。
8. `BREAKING CHANGE` 須全大寫；其餘關鍵字大小寫不拘。

### 範例

```
feat(auth): 新增信箱驗證流程

fix(log): 修正分頁查詢邊界條件造成的例外

refactor(shared): 重構 OutboxRelayService 排程邏輯

chore: 升級 dotnet 至 8.0.6

docs(infra): 補充正式環境部署 Runbook
```

### Release Commit

跨服務統一升版時，使用固定 subject `chore(release): 發佈新版本`，body 以 `service-name@version`（kebab-case）條列各服務異動版本，不使用敘述段落：

```
chore(release): 發佈新版本

- auth@0.0.2
- bootstrap@0.0.3
- email-service@0.0.2
- log-service@0.0.2
- storage-service@0.0.2
- creator-web@0.0.2
- portal-web@0.0.2
- workspace-web@0.0.2
- docs@0.0.2
```

> 服務名稱對應：`.csproj` 專案名稱（如 `EmailService`）轉為 kebab-case（`email-service`）；`package.json` 的 `name` 欄位（如 `open-jam-docs`）統一以 `docs` 表示。

> **不必每次都全服務升版**：條列清單只列出「自上次升版以來確實有異動」的服務，逐一比對該服務目錄自己上次版本提交至今的 `git log`。也要留意**間接異動**——若 `Shared/`（或其他共用類別庫）在此期間有變更，所有引用它的服務即視為有異動，即便該服務自身檔案未動到。

## 後端注意事項

- **列表 API 分頁採 `offset` / `limit`**（非 `page` / `rowsPerPage`）。
- **微服務資源參照（Ref Table）**：本地保留其他服務資源的參照表，避免每次跨服務查詢。
- **資源更新發 Event**：資源變更時發事件，讓持有 Ref Table 的服務同步。
- **常用資源查詢快取**（Redis）。
- **LogService 單純 index**：以索引支撐查詢，資料量大再評估分表分庫（見 [[Log]]）。
- **decimal 數值計算**：金額計算避免溢位。
- **設定檔 URL 一律無結尾斜線**：`appsettings.json` 內服務自有的 base URL（`Services:*:BaseUrl`、`Storage:PublicBaseUrl`、`Storage:Local:BaseUrl`、`App:BaseUrl` / `App:WorkspaceUrl`、`Hydra:AdminUrl` 等）結尾不帶 `/`，與前端一致。程式端不信任設定值的斜線狀態，一律正規化後再用：
  - 字串拼接情境以 `.TrimEnd('/')` 去尾，再接帶前導斜線的後綴（如 `$"{baseUrl}/v1/files/blob/{key}"`）。
  - `HttpClient.BaseAddress` 則以 `.TrimEnd('/') + "/"` 強制補尾斜線——.NET 規定 BaseAddress 須以 `/` 結尾、相對請求路徑**不可**以 `/` 開頭，否則最後一段路徑會被丟棄。
  - **例外（外部協定精確比對，維持原樣含結尾斜線，不適用本規則）**：OIDC issuer（`Hydra:Issuer`）須與 token `iss` 完全一致；Hydra OIDC client 的 redirect / post-logout redirect URI 為註冊時的精確比對字串。CORS 的 allowed origins 則依規範僅為 scheme+host+port，本就不帶路徑或結尾斜線。
- **後端多國語系**：採 .resx 資源檔。
- **時間欄位命名**：一律以 `At` 結尾（如 `CreatedAt`、`UpdatedAt`），型別統一宣告為 `DateTimeOffset`。
- **資料庫命名慣例**：資料庫、資料表、欄位、Migration history 資料表全部採 `snake_case`；C# Entity 仍保持 `PascalCase`，由 `BaseDbContext` 統一套用 `EF Core` naming convention 自動轉換，開發層不需手動指定 `[Column]` 或 `[Table]`。
- **DbContext 命名**：各服務的 DbContext 一律命名為 `AppDbContext`（置於 `<Service>.Data` namespace），不以服務名稱前綴（如 ~~`EmailDbContext`~~）；服務歸屬由 namespace 表達。Design-time factory 與 Migration snapshot 同步命名為 `AppDbContextFactory`、`AppDbContextModelSnapshot`。若單一專案（如 `Bootstrap`）需同時引用多個服務的 DbContext，以 using alias 區分，例如 `using AuthDbContext = Auth.Data.AppDbContext;`。
- **enum 獨立成檔**：enum 一律單獨一個檔案（一型別一檔），不與 Entity 或其他型別共置同檔，如 `UserStatus.cs`、`EmailStatus.cs`。
- **XML 文件註解**：Entity、Model（DTO / ViewModel / Request / Response）、Service、公開方法、Controller 及其 Action 皆須撰寫完整 `<summary>`，Model 屬性另加 `<example>`。Controller / Model 缺少註解會直接影響 Swagger 文件完整度，屬強制要求。
- **API 版本（ApiVersion）**：後端 REST API 皆須定義 API 版本，路由以版本前綴呈現（如 `/v1/...`）。新增或破壞性變更端點時遞增版本，舊版本維持並行直到下線；版本前綴由各服務統一掛載，Controller 不個別硬寫。
- **PathBase（Ingress path 前綴剝除）**：正式環境各 REST API 服務以 path 前綴掛在 `api.openjam.co` 之下（如 `api.openjam.co/store-service` → StoreService）。GKE 原生 GCE Ingress **不會剝除** 該前綴，後端會收到 `/store-service/v1/...`，無法比對僅註冊於 `/v1/...` 的 Controller 路由而回 404（前端常表現為 CORS 錯誤，因錯誤回應不帶 `Access-Control-Allow-Origin`）。各服務於 `Program.cs` 以 `app.UseOpenJamPathBase()`（`Shared/Web/PathBaseExtensions.cs`）作為**第一個 middleware**剝除前綴，前綴值由 deployment 的 `PathBase` 環境變數提供（如 `/store-service`）。
  - 須排在所有 middleware（含 `UseExceptionMiddleware`）之前，使後續例外處理、路由、CORS 皆見到已校正的路徑。
  - `UsePathBase` 僅在路徑以前綴開頭時剝除；不以前綴開頭的請求（健康檢查 `/healthz`、叢集內部服務間直連 `/v1/...`）原樣通過，不受影響。
  - 未設定 `PathBase`（本機開發）時不作用，行為不變。

## 前端注意事項

- **Composition API**：前端專案（portal-web / creator-web / workspace-web）一律使用 Vue 3 Composition API（`<script setup>`），不使用 Options API。
- **Pinia setup store**：store 一律採 setup 函式寫法（`defineStore('id', () => { ... })`），與 Composition API 一致；不使用 `state` / `getters` / `actions` options 物件寫法。state 以 `ref`、getter 以 `computed`、action 為一般函式，最後在 `return` 中明確匯出要對外公開的 state / getter / action。需傳參數的 getter 以 `computed(() => (arg) => ...)` 回傳函式。
- **API promise singleton**：同一請求進行中時共用同一個 promise，避免重複發送；搭配 with-pending 狀態機制。
- **lodash debounce**：搜尋 / 輸入等場景去抖。
- **前端多國語系**：i18n。
- **環境變數 URL 一律無結尾斜線**：所有 `environment.ts` 的 URL 設定（API base、頁面導向、外部連結，如 `STORE_API_URL`、`AUTH_PAGE_URL`、`*_PAGE_URL`、`GITHUB_REPO_URL`、`DOCS_URL`）結尾**不帶** `/`；拼接時一律由後綴自帶前導 `/`（如 `${env.AUTH_PAGE_URL}/error`、API client 的 `${baseUrl}/v1/...`）。如此「base 無斜線 + 後綴帶斜線」是唯一心智模型，不會組出雙斜線。直接綁 `:href` / `location.href` 而不接後綴的連結同樣維持無斜線以保持一致。

## 前端串接後端 API

前端串接後端 API 的流程**一律基於 `openapi/` 資料夾內的 Swagger 文件**，透過 `swagger-typescript-api` 套件**自動產生** API service 到 `src/api/`。**嚴禁手寫 API 呼叫、hard code endpoint 路徑 / URL / query string / request / response 型別**。

### 流程

1. **OpenAPI 來源（`openapi/`）**：各後端服務的 Swagger 文件存放於該 app 的 `openapi/` 資料夾（如 `workspace-web/openapi/catalog-service.json`、`log-service.json`、`store-service.json`）。可改指向開發伺服器即時端點（如 `http://localhost:5172/swagger/v1/swagger.json`）；後端 API 變更時，先更新 `openapi/` 內的文件再重新產生。

2. **產生 client（`swagger-typescript-api` → `src/api/`）**：以 `package.json` 的 `gen:api` script（彙整各 `gen:api:<service>`）產生型別與 API class 至 `src/api/<service>.ts`：

   ```bash
   pnpm gen:api          # 一次重產所有服務 client
   pnpm gen:api:store    # 單一服務（store / catalog / log…）
   ```

   ```jsonc
   // package.json scripts（--single-http-client 讓多服務共用注入後的 HttpClient；
   //                       --module-name-first-tag 以 OpenAPI tag 切分 API class）
   "gen:api": "pnpm gen:api:store && pnpm gen:api:catalog && pnpm gen:api:log",
   "gen:api:store":   "swagger-typescript-api generate -p http://localhost:5172/swagger/v1/swagger.json -o src/api -n store-service.ts --single-http-client --module-name-first-tag",
   "gen:api:catalog": "swagger-typescript-api generate -p openapi/catalog-service.json -o src/api -n catalog-service.ts --single-http-client --module-name-first-tag",
   "gen:api:log":     "swagger-typescript-api generate -p openapi/log-service.json -o src/api -n log-service.ts --single-http-client --module-name-first-tag"
   ```

   > `src/api/<service>.ts` 為**產生檔，不得手動編輯**——任何手改都會在下次 `pnpm gen:api` 被覆蓋。要調整 API 形狀請改後端，更新 `openapi/` 文件後重產。

3. **統一進入點（`src/api/index.ts`）**：匯入產生的 `Api` / `HttpClient`，於此處（且**僅此處**）設定 `baseUrl` 與 token 注入，匯出可直接使用的實例：

   - `baseUrl` 一律取自 `environment.ts` 的環境變數（如 `env.STORE_API_URL`），**不寫死網址**。
   - 以 `customFetch` wrapper 在每個請求即時帶上目前 OIDC 使用者的 Bearer token（因 silent renew 會更新，故每次請求即時讀取、不快取）。

   ```ts
   const authFetch: typeof fetch = async (input, init = {}) => {
     const user = await userManager.getUser();
     const headers = new Headers(init.headers ?? {});
     if (user?.access_token && !headers.has('Authorization')) {
       headers.set('Authorization', `Bearer ${user.access_token}`);
     }
     return fetch(input, { ...init, headers });
   };

   const storeHttp = new StoreHttpClient({ baseUrl: env.STORE_API_URL, customFetch: authFetch });
   export const storeApi = new StoreApi(storeHttp);
   ```

4. **業務使用**：所有 component / Pinia store 一律 `import` `src/api/index.ts` 匯出的實例（`storeApi` / `catalogApi` / `logApi`…）呼叫，**不直接 `new` 產生出來的 class、不自行 `fetch`、不重複設定 baseUrl 或 token**。

### 禁止事項

- ❌ 手寫 `fetch('http://localhost:5172/v1/stores')` 或 hard code endpoint 路徑 / query。
- ❌ 自訂 interface 重複宣告 request / response 型別——一律 import 產生檔的型別。
- ❌ 手改 `src/api/<service>.ts` 產生檔。
- ❌ 在 `index.ts` 以外的地方設定 baseUrl 或注入 token。

## Controller 與 Service 分層

REST API 服務遵循「軟體三層 + Service DI」：**Controller 只負責 HTTP（路由、model binding、回傳狀態碼），不得撰寫業務邏輯**；所有業務邏輯下放至 Service 層，Controller 以介面注入呼叫。

- **Controller**：薄薄一層。注入 `I<Feature>Service`，每個 action 將請求轉交 Service 後回傳結果；驗證／查詢／DB 存取一律不寫在 Controller。
- **Service 組織**：依功能分資料夾，介面與實作成對放置：
  ```
  Services/
    <Feature>/
      I<Feature>Service.cs   # 介面，撰寫 <summary>
      <Feature>Service.cs    # 實作，標 <inheritdoc/>
  ```
  namespace 為 `<Service>.Services.<Feature>`（如 `StoreService.Services.StoreFollowers`）。
- **DI 註冊**：於 `Program.cs` 以 `builder.Services.AddScoped<I<Feature>Service, <Feature>Service>()` 註冊。
- **業務例外**：Service 直接拋 `AppException` 子類（見 [錯誤處理](#錯誤處理)），不回傳錯誤碼或在 Controller 判斷。

```csharp
// Controller：只做 HTTP，邏輯全部委派給 Service
[ApiController]
[Route("stores/{id:guid}")]
public class StoreFollowersController(IStoreFollowerService followerService) : ControllerBase
{
    [HttpPost("follow")]
    [AllowAnonymous]
    public async Task<IActionResult> FollowAsync(Guid id, [FromBody] FollowStoreRequest request, CancellationToken ct)
    {
        await followerService.FollowAsync(id, request, ct);
        return NoContent();
    }
}
```

> Webhook / 事件回呼端點同理：Controller 僅做 payload 形狀驗證（如必要欄位缺漏回 400），其餘解析與處理交由 Service（例如 `StorageService` 的 `IStorageEventService`）。

## Entity Audit 介面

每個 Entity 可選擇實作以下介面，`BaseDbContext` 在 `SaveChangesAsync` 時自動填入對應欄位。各 audit 欄位的 setter 一律為 `private set`，只有 DbContext 攔截邏輯能寫入，業務層無法直接賦值。

### 介面定義

```csharp
public interface ICreatedAt
{
    DateTimeOffset CreatedAt { get; }
}

public interface ICreatedBy
{
    Guid CreatedBy { get; }
}

public interface IUpdatedAt
{
    DateTimeOffset? UpdatedAt { get; }
}

public interface IUpdatedBy
{
    Guid? UpdatedBy { get; }
}

public interface IDeletedAt
{
    DateTimeOffset? DeletedAt { get; }
}

public interface IDeletedBy
{
    Guid? DeletedBy { get; }
}
```

### Entity 實作範例

Entity 宣告想追蹤的介面，欄位加上 `private set`：

```csharp
public class Product : ICreatedAt, ICreatedBy, IUpdatedAt, IUpdatedBy, IDeletedAt, IDeletedBy
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = string.Empty;

    // ICreatedAt / ICreatedBy
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }

    // IUpdatedAt / IUpdatedBy
    public DateTimeOffset? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    // IDeletedAt / IDeletedBy
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }
}
```

### DbContext 自動填入

共用基底 `BaseDbContext`（`Shared/Data/BaseDbContext.cs`）覆寫 `SaveChangesAsync`，依介面偵測 entity，透過 EF Core 的 `ChangeTracker` 直接寫入欄位的 `CurrentValue`（即便屬性為 `private set` 也能寫入）。各服務的 `AppDbContext` 繼承自此基底，並注入 `ICurrentUserAccessor` 取得目前使用者 Id。

```csharp
public abstract class BaseDbContext(DbContextOptions options, ICurrentUserAccessor currentUser)
    : DbContext(options)
{
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now    = DateTimeOffset.UtcNow;
        var userId = currentUser.UserId; // nullable Guid

        foreach (var entry in ChangeTracker.Entries())
        {
            // 軟刪除：將 IDeletedAt entity 的刪除動作攔截轉為 Modified，避免資料被真正刪除
            if (entry.State == EntityState.Deleted && entry.Entity is IDeletedAt)
            {
                entry.State = EntityState.Modified;
                entry.Property(nameof(IDeletedAt.DeletedAt)).CurrentValue = now;

                if (entry.Entity is IDeletedBy)
                    entry.Property(nameof(IDeletedBy.DeletedBy)).CurrentValue = userId;
            }

            if (entry.State == EntityState.Added)
            {
                if (entry.Entity is ICreatedAt)
                    entry.Property(nameof(ICreatedAt.CreatedAt)).CurrentValue = now;

                if (entry.Entity is ICreatedBy && userId.HasValue)
                    entry.Property(nameof(ICreatedBy.CreatedBy)).CurrentValue = userId.Value;
            }

            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                if (entry.Entity is IUpdatedAt)
                    entry.Property(nameof(IUpdatedAt.UpdatedAt)).CurrentValue = now;

                if (entry.Entity is IUpdatedBy && userId.HasValue)
                    entry.Property(nameof(IUpdatedBy.UpdatedBy)).CurrentValue = userId.Value;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    // 為所有 IDeletedAt entity 自動套用全域 Query Filter，查詢預設排除已軟刪除資料
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(IDeletedAt).IsAssignableFrom(entityType.ClrType)) continue;
            // 對每個型別套用 e => e.DeletedAt == null 的 HasQueryFilter
        }
    }
}
```

> 子類別覆寫 `OnModelCreating` 時，務必先呼叫 `base.OnModelCreating(modelBuilder)`，否則軟刪除全域過濾器不會套用。

### 軟刪除與硬刪除

- **軟刪除**：業務層直接呼叫 `db.Set.Remove(entity)`（或 `DbSet.Remove`），`BaseDbContext` 會把刪除動作攔截轉為 `Modified`，並自動填入 `DeletedAt` / `DeletedBy`（由 `ICurrentUserAccessor` 取得）。**Entity 不提供 `SoftDelete()` 方法**，audit 欄位皆為 `private set`，業務層不可、也不需手動賦值。

  ```csharp
  var file = await db.StoredFiles.FirstOrDefaultAsync(f => f.Id == id)
      ?? throw new NotFoundException($"檔案 {id} 不存在");

  db.StoredFiles.Remove(file);       // 由 BaseDbContext 自動轉為軟刪除
  await db.SaveChangesAsync(ct);
  ```

- **查詢已軟刪除資料**：全域 Query Filter 預設排除 `DeletedAt != null` 的列，需以 `IgnoreQueryFilters()` 才能查到（例如保留期清理、稽核）。

- **永久硬刪除**：因 `Remove()` 一律被轉為軟刪除，真正的硬刪除需繞過變更追蹤，使用 `ExecuteDeleteAsync()` 直接對資料庫下 `DELETE`（搭配 `IgnoreQueryFilters()`）。

  ```csharp
  // 保留期過後的永久刪除（StorageService.OrphanCleanupService）
  await db.StoredFiles
      .IgnoreQueryFilters()
      .Where(f => ids.Contains(f.Id))
      .ExecuteDeleteAsync(ct);
  ```

### ICurrentUserAccessor

```csharp
public interface ICurrentUserAccessor
{
    Guid? UserId { get; }
}
```

內建兩種實作（皆於 `Shared/Auth/`）：

- **`HttpContextUserAccessor`** — HTTP 服務注入，從 JWT Claims 的 `sub` 取得使用者 Id。
- **`NullCurrentUserAccessor`** — 永遠回傳 `null`，供 EF Core design-time factory（`dotnet ef migrations`）等無 HTTP / 背景情境使用；此時 audit 的 `CreatedBy` / `UpdatedBy` / `DeletedBy` 不會被填入。

背景工作（Worker / Saga）若需固定系統帳號，可另提供回傳固定 Id 的實作。

## 錯誤處理

### 設計原則

**REST API 服務**（LogService、StorageService、StoreService 等）的錯誤統一由 `Shared` 類別庫的 `ExceptionMiddleware` 攔截，轉換為 [RFC 9457 Problem Details](https://www.rfc-editor.org/rfc/rfc9457) 格式回傳。業務層只需拋出語意明確的自定義 Exception，不需自行組裝 response。

> **MVC 服務例外**：`Auth` 為 ASP.NET Core MVC（畫面流程），改用內建 `app.UseExceptionHandler("/Home/Error")` 導頁至錯誤頁，**不掛載** `ExceptionMiddleware`（JSON 輸出不適用於畫面）。

### 自定義 Exception 基底

```csharp
public abstract class AppException(string message) : Exception(message)
{
    public abstract int StatusCode { get; }
    public virtual string ErrorCode => GetType().Name.Replace("Exception", string.Empty);
}
```

### 內建自定義 Exception

| Exception | StatusCode | 用途 |
|-----------|-----------|------|
| `NotFoundException` | 404 | 資源不存在 |
| `ForbiddenException` | 403 | 無權限操作 |
| `ConflictException` | 409 | 資源衝突（如重複 email） |
| `ValidationException` | 422 | 業務規則驗證失敗（非 model binding） |
| `UnauthorizedException` | 401 | 未登入或 token 無效 |

```csharp
public class NotFoundException(string message) : AppException(message)
{
    public override int StatusCode => 404;
}

public class ForbiddenException(string message = "Forbidden") : AppException(message)
{
    public override int StatusCode => 403;
}

public class ConflictException(string message) : AppException(message)
{
    public override int StatusCode => 409;
}

public class ValidationException(string message, IReadOnlyDictionary<string, string[]>? errors = null)
    : AppException(message)
{
    public override int StatusCode => 422;
    public IReadOnlyDictionary<string, string[]>? Errors { get; } = errors;
}

public class UnauthorizedException(string message = "Unauthorized") : AppException(message)
{
    public override int StatusCode => 401;
}
```

### ExceptionMiddleware

放置於 `Shared/Middleware/ExceptionMiddleware.cs`，在 `Program.cs` 以 `app.UseExceptionMiddleware()` 掛載，需排在所有其他 middleware 之前。

```csharp
public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (AppException ex)
        {
            await WriteProblemAsync(context, ex.StatusCode, ex.ErrorCode, ex.Message,
                ex is ValidationException ve ? ve.Errors : null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            await WriteProblemAsync(context, 500, "InternalServerError", "發生非預期錯誤");
        }
    }

    private static async Task WriteProblemAsync(
        HttpContext context,
        int status,
        string errorCode,
        string detail,
        IReadOnlyDictionary<string, string[]>? errors = null)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        var problem = new
        {
            type = $"https://open-jam.dev/errors/{errorCode}",
            title = errorCode,
            status,
            detail,
            errors,
            traceId = Activity.Current?.Id ?? context.TraceIdentifier,
        };

        await context.Response.WriteAsJsonAsync(problem);
    }
}

public static class ExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionMiddleware(this IApplicationBuilder app)
        => app.UseMiddleware<ExceptionMiddleware>();
}
```

### Response 範例

**404 Not Found**

```json
{
  "type": "https://open-jam.dev/errors/NotFound",
  "title": "NotFound",
  "status": 404,
  "detail": "Product 'abc123' 不存在",
  "errors": null,
  "traceId": "00-abc...def-01"
}
```

**422 Validation Error**（含欄位明細）

```json
{
  "type": "https://open-jam.dev/errors/Validation",
  "title": "Validation",
  "status": 422,
  "detail": "輸入資料驗證失敗",
  "errors": {
    "price": ["必須大於 0"],
    "title": ["不可為空白"]
  },
  "traceId": "00-abc...def-01"
}
```

### 業務層使用方式

```csharp
var product = await db.Products.FindAsync(id)
    ?? throw new NotFoundException($"Product '{id}' 不存在");

if (product.OwnerId != currentUser.UserId)
    throw new ForbiddenException();
```

## API Model 慣例（CRUD Model）

### 檔案組織

Model（Request / Response / DTO）依**功能**歸類，同一功能的相關型別集中於單一 `<Feature>Models.cs`，置於 `Models/` 資料夾，namespace 為 `<Service>.Models`。不採「一型別一檔」。

```
Models/
  StoreFollowerModels.cs   # FollowStoreRequest、GetStoreFollowersRequest/Response、StoreFollowerDto…
  StoreApplicationModels.cs
  FileModels.cs            # RequestUploadUrlRequest/Response、GetDownloadUrlResponse、FileDto…
```

> 註：純資料傳輸的 DTO 屬性使用一般 `get; set;` 即可；audit 欄位的 `private set` 規範只適用於 [Entity](#entity-audit-介面)，不適用於 Model。

### 範例

標準 request / response DTO 範例（分頁採 offset / limit）：

```csharp
public class GetUsersRequest
{
    public int Offset { get; set; } = 0;
    public int Limit { get; set; } = 20;
    public string? Keyword { get; set; }
    public string? Status { get; set; }
}

public class CreateUserRequest
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
}

public class UpdateUserRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? Status { get; set; }
}

public class GetUsersResponse
{
    public int TotalCount { get; set; }
    public List<UserListItemDto> Items { get; set; }
}

public class GetUserResponse
{
    public UserDetailDto User { get; set; }
}

public class BulkUpdateUserRequest
{
    public List<UpdateUserItem> Items { get; set; }
}

public class UpdateUserItem
{
    public Guid Id { get; set; }
    public string? Status { get; set; }
}
```

## Model 驗證與轉換（FluentValidation / AutoMapper）

REST API 服務（LogService / StorageService / StoreService / CatalogService 等；`Auth` 為 MVC 不在此列）一律：

- **以 FluentValidation 驗證輸入 model**——不在 Service 內以內聯 `if` 檢查輸入格式。
- **以 AutoMapper 轉換 model**——Entity ↔ DTO 不手寫逐欄位組裝。

### 資料夾位置

`Validators/` 與 `Mapping/` 與 `Models/`、`Services/`、`Controllers/`、`Data/` **平行置於服務根目錄，不內嵌於 `Models/` 之下**：

```
<Service>/
  Controllers/
  Services/
  Models/        # Request / Response / DTO（純資料型別）
  Mapping/       # AutoMapper Profile
  Validators/    # FluentValidation AbstractValidator
  Data/
```

理由：

1. **驗證器與 Profile 都不是 model，且橫跨兩層。** Profile 同時引用 `Data/Entities`（來源）與 `Models`（目的），是 Entity↔DTO 的橋接；驗證器則是規則邏輯。兩者皆非 `Models/` 所界定的「Request / Response / DTO 純資料型別」。
2. **與「角色分層、扁平擺放」一致。** 服務根目錄按技術角色平鋪；`Mapping/` 與 `Validators/` 是被 assembly 掃描註冊的橫切單位（性質接近 `Services/`），擺同一層最一致，namespace 也乾淨（`<Service>.Mapping`、`<Service>.Validators`，而非 `<Service>.Models.Mapping`）。
3. **兩者須一致對待。** `Mapping/` 與 `Validators/` 處境相同，要嘛都平行、要嘛都內嵌；分開放會破壞對稱，全塞進 `Models/` 又會讓 `Models/` 變雜燴。故統一平行於 `Models/`。

### 驗證邊界（哪些檢查放 Validator）

**僅無狀態輸入驗證**放 FluentValidation Validator：格式、長度、必填、數值範圍（如名稱 1–200 字、售價 `>= 0`、slug 格式、幣別 3 碼、MIME 白名單、分頁 `offset`/`limit` 範圍）。

**需查 DB 或跨服務的檢查留在 Service 層**，以 `AppException` 子類拋出：唯一性（slug）、存在性（分類 / 上層分類）、授權（店家 Owner）、狀態轉移規則（如「已停權商品不可自行上架」）。slug 的格式檢查與唯一性檢查因此分家——格式以 `StoreSlugValidator.IsValidFormat` / `CatalogSlugValidator.IsValidFormat`（非拋例外的 `bool`）供 Validator 使用，唯一性 `Ensure...UniqueAsync` 留在 Service。

### 驗證接線（維持 422 契約，不用 auto-validation）

各服務於 `Program.cs` 呼叫 `builder.Services.AddOpenJamValidation(typeof(Program).Assembly)`（`Shared/Web/ValidationExtensions.cs`），它會：

- `AddValidatorsFromAssembly` 註冊該 assembly 內所有 `AbstractValidator<T>`；
- 掛上 `ValidationActionFilter`：在 action 執行前對每個已綁定參數解析 `IValidator<>` 並執行，失敗時彙整欄位錯誤拋出 **`ValidationException`（422，含 `errors`）**，由 [`ExceptionMiddleware`](#exceptionmiddleware) 統一轉為 RFC 9457 Problem Details。

> **刻意不使用** FluentValidation 內建的 auto-validation（`AddFluentValidationAutoValidation`）：它會把輸入驗證失敗回成 **400 ValidationProblemDetails**，與既有「業務 / 輸入驗證皆走 422 + `errors`」的契約不一致，也會影響前端 codegen 消費的錯誤形狀。

分頁欄位範圍重用 `Shared/Web/PaginationRules.cs` 的 `ValidOffset()`（`>= 0`）/ `ValidLimit()`（1–100），不再於 Service 以 `Math.Clamp` 靜默截斷——超界一律回 422。

```csharp
// Validators/GetAuditLogsRequestValidator.cs
public class GetAuditLogsRequestValidator : AbstractValidator<GetAuditLogsRequest>
{
    public GetAuditLogsRequestValidator()
    {
        RuleFor(x => x.Offset).ValidOffset();
        RuleFor(x => x.Limit).ValidLimit();
        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From)
            .When(x => x.From.HasValue && x.To.HasValue)
            .WithMessage("查詢結束時間不得早於起始時間。");
    }
}
```

### 轉換接線（AutoMapper 只做扁平對應，async 補值留 Service）

各服務於 `Program.cs` 呼叫 `builder.Services.AddOpenJamMapping(typeof(Program).Assembly)`（`Shared/Web/MappingExtensions.cs`，內部 `AddAutoMapper`），Profile 置於 `Mapping/`。業務層注入 `IMapper`：

- **AutoMapper 只負責 Entity → DTO 的扁平欄位對應。** 需 async 查詢或計算的欄位（資產 URL、標籤清單、目前版本、子資產清單等）在 Profile 以 `.ForMember(..., o => o.Ignore())` 標記，由 Service 在 `Map` 後補值。
- **IQueryable 投影用 `ProjectTo<TDto>(mapper.ConfigurationProvider)`**，讓欄位選取下推到 SQL（取代 `.Select(x => new TDto { ... })`）。

```csharp
// Mapping/CatalogMappingProfile.cs
public class CatalogMappingProfile : Profile
{
    public CatalogMappingProfile()
    {
        CreateMap<Catalog, CatalogDto>()
            .ForMember(d => d.ThumbnailUrl, o => o.Ignore())   // async 查詢
            .ForMember(d => d.CurrentVersion, o => o.Ignore())
            .ForMember(d => d.Assets, o => o.Ignore())
            .ForMember(d => d.Tags, o => o.Ignore());
    }
}

// Service：扁平用 AutoMapper，async 欄位 map 後補上
var dto = mapper.Map<CatalogDto>(catalog);
dto.ThumbnailUrl   = await GetAssetUrlAsync(catalog.ThumbnailAssetId, ct);
dto.CurrentVersion = await GetCurrentVersionDtoAsync(catalog.CurrentVersionId, ct);
```

### 套件版本

固定 **AutoMapper `13.*`** 與 **FluentValidation `11.*`**（最後採寬鬆授權的版本：AutoMapper 14+ / FluentValidation 12+ 已轉商業授權）。兩者及 `FluentValidation.DependencyInjectionExtensions` 集中宣告於 `Shared.csproj`，由 ProjectReference 傳遞至各服務。AutoMapper 13.x 的 NU1903（GHSA-rvv3-g6hj-g44x，需數萬層巢狀物件的 DoS，不適用於自有 Entity→DTO 轉換）已於 `Shared.csproj` 以 `NoWarn` 抑制並註明原因。
