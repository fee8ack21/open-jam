# 開發慣例 Develop

本頁是 Open Jam 的開發規範：架構慣例、專案結構，以及前後端開發注意事項與 coding style。

## 架構慣例

- **CQRS / Mediator**。
- **軟體三層 + Service DI**。

## 專案結構

```
src/
  Auth/             # 認證授權（OIDC / Hydra）
  EmailService/     # 寄信服務
  StorageService/   # 檔案儲存 / 轉碼 / 預覽
  LogService/       # Audit Log / 日誌
  ProductService/   # 商店與商品上下架
  OrderService/     # 訂單與金流
  QuotaService/     # 資源配額計量
  Bootstrap/        # 平台初始化 seed
  Shared/           # 共用程式庫
apps/
  workspace-web/    # 用戶後台
  creator-web/      # 創作者商品空間
  market-web/       # 平台首頁，探索各產品與創作者
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
- market-web@0.0.2
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
- **後端多國語系**：採 .resx 資源檔。
- **時間欄位命名**：一律以 `At` 結尾（如 `CreatedAt`、`UpdatedAt`），型別統一宣告為 `DateTimeOffset`。
- **資料庫命名慣例**：資料庫、資料表、欄位、Migration history 資料表全部採 `snake_case`；C# Entity 仍保持 `PascalCase`，由 `BaseDbContext` 統一套用 `EF Core` naming convention 自動轉換，開發層不需手動指定 `[Column]` 或 `[Table]`。
- **DbContext 命名**：各服務的 DbContext 一律命名為 `AppDbContext`（置於 `<Service>.Data` namespace），不以服務名稱前綴（如 ~~`EmailDbContext`~~）；服務歸屬由 namespace 表達。Design-time factory 與 Migration snapshot 同步命名為 `AppDbContextFactory`、`AppDbContextModelSnapshot`。若單一專案（如 `Bootstrap`）需同時引用多個服務的 DbContext，以 using alias 區分，例如 `using AuthDbContext = Auth.Data.AppDbContext;`。
- **enum 獨立成檔**：enum 一律單獨一個檔案（一型別一檔），不與 Entity 或其他型別共置同檔，如 `UserStatus.cs`、`EmailStatus.cs`。
- **XML 文件註解**：Entity、Model（DTO / ViewModel / Request / Response）、Service、公開方法、Controller 及其 Action 皆須撰寫完整 `<summary>`，Model 屬性另加 `<example>`。Controller / Model 缺少註解會直接影響 Swagger 文件完整度，屬強制要求。

## 前端注意事項

- **API promise singleton**：同一請求進行中時共用同一個 promise，避免重複發送；搭配 with-pending 狀態機制。
- **lodash debounce**：搜尋 / 輸入等場景去抖。
- **前端多國語系**：i18n。

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
