using AutoMapper;
using Auth.Data.Entities;
using Auth.Models;

namespace Auth.Mapping;

/// <summary>Auth 服務的 Entity → DTO 對應設定。</summary>
public class UserMappingProfile : Profile
{
    /// <summary>建立對應規則。</summary>
    public UserMappingProfile()
    {
        // EmailVerified 由狀態推導：脫離 Pending 即代表已完成信箱驗證。
        // 以可被 EF Core 轉成 SQL 的表達式撰寫，供 ProjectTo 投影使用。
        CreateMap<User, UserSummaryDto>()
            .ForMember(d => d.EmailVerified, o => o.MapFrom(s => s.Status != UserStatus.Pending));
    }
}
