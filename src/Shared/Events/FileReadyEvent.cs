namespace Shared.Events;

/// <summary>
/// StorageService 在檔案完成掃毒 / 轉碼 / 預覽生成並標記為 Ready 後，
/// 透過 RabbitMQ 發布此事件。功能 API（商品服務等）訂閱後更新商品狀態為可售。
/// </summary>
public record FileReadyEvent(
    /// <summary>檔案 ID。</summary>
    Guid FileId,
    /// <summary>擁有者（創作者）ID。</summary>
    Guid CreatorId,
    /// <summary>所屬商品 ID；null 表示尚未關聯。</summary>
    Guid? ProductId,
    /// <summary>MIME 類型，例如 "video/mp4"。</summary>
    string ContentType,
    /// <summary>媒體類型字串："Video"、"Image" 或 "Pdf"。</summary>
    string FileType,
    /// <summary>檔案大小（bytes）。</summary>
    long? SizeBytes,
    /// <summary>是否為公開預覽衍生檔。</summary>
    bool IsPreview
);
