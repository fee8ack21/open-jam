using AutoMapper;
using ContentService.Data.Entities;
using ContentService.Models;

namespace ContentService.Mapping;

/// <summary>法律文件 Entity → DTO 對應設定。</summary>
public class LegalDocumentMappingProfile : Profile
{
    /// <summary>建立對應規則。</summary>
    public LegalDocumentMappingProfile()
    {
        CreateMap<LegalDocument, LegalDocumentSummaryDto>();
        CreateMap<LegalDocument, LegalDocumentDto>();
    }
}
