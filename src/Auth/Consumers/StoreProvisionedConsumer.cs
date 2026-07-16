using Auth.Services.Storefront;
using MassTransit;
using Shared.Events;

namespace Auth.Consumers;

/// <summary>
/// 消費 StoreProvisionedEvent（開店申請核准），將店面子網域的 OIDC redirect URI
/// 註冊進 Hydra web client。註冊為「缺漏才追加」的冪等操作，重複投遞無副作用，不需 inbox 去重；
/// Hydra 暫時不可用時由 MassTransit 指數退避重試。
/// </summary>
public class StoreProvisionedConsumer(
    IStorefrontRedirectService storefrontRedirect,
    ILogger<StoreProvisionedConsumer> logger) : IConsumer<StoreProvisionedEvent>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<StoreProvisionedEvent> context)
    {
        var evt = context.Message;

        await storefrontRedirect.RegisterAsync(evt.StoreSlug, context.CancellationToken);

        logger.LogInformation(
            "StoreProvisionedEvent：店面 '{Slug}'（商店 {StoreId}）redirect URI 註冊完成。",
            evt.StoreSlug, evt.StoreId);
    }
}
