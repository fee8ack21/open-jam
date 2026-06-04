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
- **XML 文件註解**：Entity、Model（DTO / ViewModel / Request / Response）、Service、公開方法、Controller 及其 Action 皆須撰寫完整 `<summary>`，Model 屬性另加 `<example>`。Controller / Model 缺少註解會直接影響 Swagger 文件完整度，屬強制要求。

## 前端注意事項

- **API promise singleton**：同一請求進行中時共用同一個 promise，避免重複發送；搭配 with-pending 狀態機制。
- **lodash debounce**：搜尋 / 輸入等場景去抖。
- **前端多國語系**：i18n。

## Entity Audit 介面

每個 Entity 可選擇實作以下介面，DbContext 在 `SaveChanges` / `SaveChangesAsync` 時自動填入對應欄位。Setter 一律為 `private`，只有 DbContext 攔截邏輯才能寫入，業務層無法直接賦值。

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

在 `AppDbContext`（或共用基底 `BaseDbContext`）覆寫 `SaveChangesAsync`，依介面偵測 entity 並透過 **EF Core Shadow Property** 或反射填入欄位。需注入 `ICurrentUserAccessor` 取得目前使用者 Id。

```csharp
public abstract class BaseDbContext(DbContextOptions options, ICurrentUserAccessor currentUser)
    : DbContext(options)
{
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var userId = currentUser.UserId; // nullable Guid

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity is ICreatedAt ca)
                    entry.Property(nameof(ICreatedAt.CreatedAt)).CurrentValue = now;

                if (entry.Entity is ICreatedBy cb && userId.HasValue)
                    entry.Property(nameof(ICreatedBy.CreatedBy)).CurrentValue = userId.Value;
            }

            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                if (entry.Entity is IUpdatedAt ua)
                    entry.Property(nameof(IUpdatedAt.UpdatedAt)).CurrentValue = now;

                if (entry.Entity is IUpdatedBy ub && userId.HasValue)
                    entry.Property(nameof(IUpdatedBy.UpdatedBy)).CurrentValue = userId.Value;
            }

            // IDeletedAt / IDeletedBy 由業務層顯式呼叫軟刪除方法觸發，DbContext 不自動填入
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
```

> **軟刪除**：`DeletedAt` / `DeletedBy` 由 Entity 自身提供 `SoftDelete(Guid operatorId)` 方法填入，DbContext 不自動處理，以避免誤將 `Modified` 狀態的 entity 一律視為刪除。

### ICurrentUserAccessor

```csharp
public interface ICurrentUserAccessor
{
    Guid? UserId { get; }
}
```

實作注入 `IHttpContextAccessor`，從 JWT Claims 取得 `sub`。背景工作（如 Worker / Saga）可提供獨立實作回傳固定的系統帳號 Id。

## 錯誤處理

### 設計原則

所有 HTTP API 的錯誤統一由 `Shared` 類別庫的 `ExceptionMiddleware` 攔截，轉換為 [RFC 9457 Problem Details](https://www.rfc-editor.org/rfc/rfc9457) 格式回傳。業務層只需拋出語意明確的自定義 Exception，不需自行組裝 response。

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
