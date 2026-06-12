namespace StorageService.Data.Entities;

/// <summary>檔案處理狀態。</summary>
public enum FileStatus
{
    /// <summary>已簽發上傳 URL，等待直傳完成。</summary>
    Uploading,

    /// <summary>Storage 已收到上傳通知，正在進行掃毒 / 轉碼 / 預覽生成。</summary>
    Processing,

    /// <summary>所有處理完成，可對外提供下載。</summary>
    Ready,

    /// <summary>處理失敗（掃毒不通過 / 轉碼錯誤）或上傳逾時未確認。</summary>
    Failed,
}
