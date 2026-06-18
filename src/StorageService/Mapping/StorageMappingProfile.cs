using AutoMapper;
using StorageService.Data.Entities;
using StorageService.Models;

namespace StorageService.Mapping;

/// <summary>StorageService 的 Entity → DTO 對應設定。</summary>
public class StorageMappingProfile : Profile
{
    /// <summary>建立對應規則。</summary>
    public StorageMappingProfile()
    {
        // 欄位名稱一致，依慣例對應。
        CreateMap<StoredFile, FileDto>();
    }
}
