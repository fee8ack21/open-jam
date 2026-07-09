using AutoMapper;
using ContentService.Data.Entities;
using ContentService.Models;

namespace ContentService.Mapping;

/// <summary>常見問題 Entity → DTO 對應設定。</summary>
public class FaqMappingProfile : Profile
{
    /// <summary>建立對應規則。</summary>
    public FaqMappingProfile()
    {
        CreateMap<FaqItem, FaqItemDto>();
    }
}
