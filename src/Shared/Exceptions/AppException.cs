namespace Shared.Exceptions;

/// <summary>所有業務例外的抽象基底，由 ExceptionMiddleware 統一攔截並轉換為 Problem Details。</summary>
public abstract class AppException(string message) : Exception(message)
{
    /// <summary>對應的 HTTP 狀態碼。</summary>
    public abstract int StatusCode { get; }

    /// <summary>錯誤代碼，預設取類別名稱去掉 "Exception" 後綴。</summary>
    public virtual string ErrorCode => GetType().Name.Replace("Exception", string.Empty);
}

/// <summary>資源不存在（404）。</summary>
public class NotFoundException(string message) : AppException(message)
{
    /// <inheritdoc/>
    public override int StatusCode => 404;
}

/// <summary>無權限操作（403）。</summary>
public class ForbiddenException(string message = "Forbidden") : AppException(message)
{
    /// <inheritdoc/>
    public override int StatusCode => 403;
}

/// <summary>資源衝突，如重複 email（409）。</summary>
public class ConflictException(string message) : AppException(message)
{
    /// <inheritdoc/>
    public override int StatusCode => 409;
}

/// <summary>業務規則驗證失敗（422）。</summary>
public class ValidationException(string message, IReadOnlyDictionary<string, string[]>? errors = null)
    : AppException(message)
{
    /// <inheritdoc/>
    public override int StatusCode => 422;

    /// <summary>欄位層級的驗證錯誤明細；null 表示無欄位細節。</summary>
    public IReadOnlyDictionary<string, string[]>? Errors { get; } = errors;
}

/// <summary>未登入或 token 無效（401）。</summary>
public class UnauthorizedException(string message = "Unauthorized") : AppException(message)
{
    /// <inheritdoc/>
    public override int StatusCode => 401;
}
