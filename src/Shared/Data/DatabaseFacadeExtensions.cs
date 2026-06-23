using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Shared.Data;

/// <summary>
/// 交易輔助方法：啟用 <c>EnableRetryOnFailure</c> 後，EF Core 的重試 execution strategy
/// 不允許直接呼叫 <c>BeginTransaction</c>，必須把整段交易包進
/// <c>CreateExecutionStrategy().ExecuteAsync(...)</c> 才能在 transient 失敗時整體重放。
/// 此處集中該樣板，交易內的 Commit / Rollback 仍由呼叫端的委派自行控制。
/// </summary>
public static class DatabaseFacadeExtensions
{
    /// <summary>
    /// 在重試 execution strategy 下開啟交易並執行 <paramref name="operation"/>，回傳其結果。
    /// 交易物件以參數傳入委派；委派需自行 Commit / Rollback。
    /// 重試時整段委派（含重新 BeginTransaction）會重新執行，故委派內容須具冪等性。
    /// </summary>
    public static async Task<T> ExecuteInTransactionAsync<T>(
        this DatabaseFacade database,
        Func<IDbContextTransaction, Task<T>> operation,
        CancellationToken ct = default)
    {
        var strategy = database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await database.BeginTransactionAsync(ct);
            return await operation(tx);
        });
    }

    /// <summary>無回傳值版本，語意同 <see cref="ExecuteInTransactionAsync{T}"/>。</summary>
    public static Task ExecuteInTransactionAsync(
        this DatabaseFacade database,
        Func<IDbContextTransaction, Task> operation,
        CancellationToken ct = default)
        => database.ExecuteInTransactionAsync<object?>(async tx =>
        {
            await operation(tx);
            return null;
        }, ct);
}
