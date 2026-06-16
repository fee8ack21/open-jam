using CatalogService.Data.Entities;
using Shared.Exceptions;

namespace CatalogService.Services;

/// <summary>展示型資產的內容類型驗證與 StorageService 媒體分類對應。</summary>
public static class CatalogAssetContentTypes
{
    // StorageService 目前僅支援下列 MIME（無音訊）；逾此範圍會在簽發上傳 URL 時被 StorageService 拒絕。
    private static readonly HashSet<string> Images =
        new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/gif", "image/webp" };

    private static readonly HashSet<string> Videos =
        new(StringComparer.OrdinalIgnoreCase) { "video/mp4", "video/quicktime", "video/x-msvideo", "video/webm" };

    /// <summary>
    /// 驗證指定展示型資產類型的 ContentType 是否受支援，並回傳對應的 StorageService 媒體分類。
    /// 不支援時拋出 <see cref="ValidationException"/>。
    /// </summary>
    public static StorageFileType Resolve(CatalogAssetType type, string contentType)
    {
        switch (type)
        {
            case CatalogAssetType.Thumbnail:
            case CatalogAssetType.Screenshot:
                if (!Images.Contains(contentType))
                    throw new ValidationException($"{type} 僅支援圖片格式（jpeg/png/gif/webp）：{contentType}");
                return StorageFileType.Image;

            case CatalogAssetType.PreviewVideo:
            case CatalogAssetType.PreviewAudio:
                // StorageService 尚未支援獨立音訊分類，預覽影音皆以 Video 分類儲存。
                if (!Videos.Contains(contentType))
                    throw new ValidationException($"{type} 僅支援影片格式（mp4/quicktime/avi/webm）：{contentType}");
                return StorageFileType.Video;

            default:
                throw new ValidationException($"未知的資產類型：{type}");
        }
    }

    /// <summary>由可下載檔案的 ContentType 推導 StorageService 媒體分類。</summary>
    public static StorageFileType ResolveDownloadable(string contentType)
    {
        if (Images.Contains(contentType))
            return StorageFileType.Image;
        if (Videos.Contains(contentType))
            return StorageFileType.Video;
        if (string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
            return StorageFileType.Pdf;

        throw new ValidationException($"不支援的下載檔案類型：{contentType}");
    }
}
