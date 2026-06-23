using Microsoft.EntityFrameworkCore;

namespace Shared.Data;

/// <summary>
/// DbContext 設定 Npgsql 的共用慣例：snake_case 命名、固定 Migration 歷史表，
/// 並啟用連線韌性（EnableRetryOnFailure，採 Npgsql 預設重試次數 / 退避與內建 transient 錯誤碼）。
/// </summary>
public static class NpgsqlDbContextExtensions
{
    /// <summary>
    /// 以 Open Jam 慣例設定 PostgreSQL 連線。
    /// 注意：啟用重試後，所有「使用者自啟的交易」（BeginTransaction）必須以
    /// <c>DbContext.Database.CreateExecutionStrategy()</c> 包覆才能在重試時整體重放，
    /// 本專案統一透過 <see cref="DatabaseFacadeExtensions.ExecuteInTransactionAsync{T}"/> 達成。
    /// </summary>
    public static DbContextOptionsBuilder UseOpenJamNpgsql(
        this DbContextOptionsBuilder options, string? connectionString)
        => options
            .UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history");
                npgsql.EnableRetryOnFailure();
            })
            .UseSnakeCaseNamingConvention();
}
