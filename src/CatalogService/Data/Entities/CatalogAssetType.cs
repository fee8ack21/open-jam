namespace CatalogService.Data.Entities;

/// <summary>商品展示型資產類型。</summary>
public enum CatalogAssetType
{
    /// <summary>縮圖（商品列表 / 卡片）。</summary>
    Thumbnail,

    /// <summary>截圖（商品頁圖庫）。</summary>
    Screenshot,

    /// <summary>預覽音訊。</summary>
    PreviewAudio,

    /// <summary>預覽影片。</summary>
    PreviewVideo,
}
