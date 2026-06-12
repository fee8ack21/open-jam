namespace StorageService.Data.Entities;

/// <summary>媒體類型分類。</summary>
public enum FileType
{
    /// <summary>影片（支援 HLS 轉碼）。</summary>
    Video,

    /// <summary>圖片（支援縮圖生成）。</summary>
    Image,

    /// <summary>PDF 文件（支援預覽頁生成）。</summary>
    Pdf,
}
