using AutoMapper;
using Auth.Data.Entities;
using Auth.Models;

namespace Auth.Mapping;

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
