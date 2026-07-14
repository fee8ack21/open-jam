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

    /// <summary>其他二進位下載檔（ZIP / 音訊 / 設計檔等），不做轉碼或預覽處理，僅供買家授權下載。</summary>
    Other,
}
