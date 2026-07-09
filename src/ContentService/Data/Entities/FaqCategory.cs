namespace ContentService.Data.Entities;

/// <summary>常見問題主題分類（對應 portal-web FAQ 頁的主題分頁）。</summary>
public enum FaqCategory
{
    /// <summary>認識平台。</summary>
    Platform = 0,

    /// <summary>購買與下載。</summary>
    Buying = 1,

    /// <summary>開店與上架。</summary>
    Selling = 2,

    /// <summary>金流與結算。</summary>
    Payments = 3,
}
