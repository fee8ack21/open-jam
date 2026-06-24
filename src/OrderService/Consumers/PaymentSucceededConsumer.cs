using MassTransit;
using OrderService.Services.Orders;
using Shared.Events;

namespace OrderService.Consumers;

/// <summary>
/// 消費 PaymentSucceededEvent，將對應訂單履約完成（Pending/Paid → Completed）。
/// 以訂單既有狀態做冪等判斷，重複事件會被略過。
/// </summary>
public class PaymentSucceededConsumer(
    IOrderManager orderManager,
    ILogger<PaymentSucceededConsumer> logger) : IConsumer<PaymentSucceededEvent>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<PaymentSucceededEvent> context)
    {
        var evt = context.Message;

        logger.LogInformation(
            "Completing order {OrderId} from payment {PaymentId}.", evt.OrderId, evt.PaymentId);

        await orderManager.CompleteFromPaymentAsync(evt.OrderId, evt.PaidAt, context.CancellationToken);
    }
}
