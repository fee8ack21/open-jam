using Shared.Audit;

namespace PaymentService.Data.Entities;

/// <summary>付款交易歷程，記錄每次金流狀態變更。</summary>
public class PaymentTransaction : ICreatedAt
{
    public Guid Id { get; set; }

    /// <summary>關聯的付款 ID。</summary>
    public Guid PaymentId { get; set; }

    /// <summary>交易類型。</summary>
    public TransactionType TransactionType { get; set; }

    /// <summary>第三方金流的交易編號（Stripe Transaction ID / Event ID）。</summary>
    public string? ProviderTransactionId { get; set; }

    /// <summary>原始 webhook payload（JSON 字串）。</summary>
    public string? RawPayload { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; private set; }
}
