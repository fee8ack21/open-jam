using AutoMapper;
using StoreService.Data.Entities;
using StoreService.Models;

namespace StoreService.Mapping;

/// <summary>StoreService 的 Entity → DTO 對應設定。</summary>
public class StoreMappingProfile : Profile
{
    /// <summary>建立對應規則。</summary>
    public StoreMappingProfile()
    {
        CreateMap<StoreApplication, StoreApplicationDto>();

        CreateMap<StoreFollower, StoreFollowerDto>();

        // AvatarUrl / BannerUrl 需 async 查詢資產 StorageKey，由 service map 後補值。
        CreateMap<Store, StoreDto>()
            .ForMember(d => d.AvatarUrl, o => o.Ignore())
            .ForMember(d => d.BannerUrl, o => o.Ignore());
    }
}
