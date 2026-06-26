using AutoMapper;
using CatalogService.Data.Entities;
using CatalogService.Models;

namespace CatalogService.Mapping;

/// <summary>CatalogService 的 Entity → DTO 對應設定。需 async 查詢或計算的欄位以 Ignore 標記，由 service map 後補值。</summary>
public class CatalogMappingProfile : Profile
{
    /// <summary>建立對應規則。</summary>
    public CatalogMappingProfile()
    {
        // 縮圖 URL / 目前版本 / 資產清單 / 標籤皆需 async 查詢，由 service 補值。
        CreateMap<Catalog, CatalogDto>()
            .ForMember(d => d.ThumbnailUrl, o => o.Ignore())
            .ForMember(d => d.CurrentVersion, o => o.Ignore())
            .ForMember(d => d.Assets, o => o.Ignore())
            .ForMember(d => d.Tags, o => o.Ignore());

        CreateMap<Catalog, CatalogSummaryDto>()
            .ForMember(d => d.ThumbnailUrl, o => o.Ignore())
            .ForMember(d => d.Tags, o => o.Ignore());

        // Url 由 StorageKey 組合，由 service 補值。
        CreateMap<CatalogAsset, CatalogAssetDto>()
            .ForMember(d => d.Url, o => o.Ignore());

        // Assets 需 async 查詢，由 service 補值。
        CreateMap<CatalogVersion, CatalogVersionDto>()
            .ForMember(d => d.Assets, o => o.Ignore());

        CreateMap<CatalogVersionAsset, CatalogVersionAssetDto>();

        CreateMap<CatalogCategory, CatalogCategoryDto>();

        CreateMap<CatalogTag, CatalogTagDto>();

        CreateMap<CatalogReview, CatalogReviewDto>();
    }
}
