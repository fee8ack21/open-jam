namespace Auth.Data.Entities;

/// <summary>法律文件狀態。文件不可刪除，停用後仍保留於資料庫供歷史比對。</summary>
public enum LegalDocumentStatus
{
    /// <summary>草稿，可編輯，尚未對外生效。</summary>
    Draft = 0,

    /// <summary>啟用中，對外呈現並作為註冊 / 登入同意的依據；同一類型同時僅一筆啟用。</summary>
    Active = 1,

    /// <summary>已停用（曾啟用或草稿作廢），內容不可再編輯。</summary>
    Inactive = 2,
}
