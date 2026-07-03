using Microsoft.EntityFrameworkCore;

namespace NotificationService.Consumers;

/// <summary>PostgreSQL 錯誤判別輔助。</summary>
public static class PostgresErrors
{
    /// <summary>是否為唯一索引違反（SQLSTATE 23505）。</summary>
    public static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505";
}
