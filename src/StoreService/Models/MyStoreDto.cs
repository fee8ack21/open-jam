using StoreService.Data.Entities;

namespace StoreService.Models;

/// <summary>登入使用者所屬商店資訊。</summary>
public class MyStoreDto
{
    /// <summary>商店基本資訊。</summary>
    public StoreDto Store { get; set; } = new();

    /// <summary>使用者於此商店的角色。</summary>
    /// <example>Owner</example>
    public StoreMemberRole Role { get; set; }
}
