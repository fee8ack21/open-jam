using AutoMapper;
using LogService.Data.Entities;
using LogService.Models;

namespace LogService.Mapping;

/// <summary>LogService 的 Entity → DTO 對應設定。</summary>
public class LogMappingProfile : Profile
{
    /// <summary>建立對應規則。</summary>
    public LogMappingProfile()
    {
        // 欄位名稱一致，依慣例對應；UserAgent 刻意不納入 DTO。
        CreateMap<AuditLog, AuditLogDto>();
    }
}
