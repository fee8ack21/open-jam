using MassTransit;
using QuotaService.Services.Quotas;
using Shared.Events;

namespace QuotaService.Consumers;

/// <summary>
/// 消費 FileReadyEvent：上傳成功後以 ReservationId 將對應預扣 commit 為實際用量。
/// 以事件回報的實際大小修正，並對重送 / 遲到事件保持冪等（由 reservation status 把關）。
/// </summary>
public class FileReadyConsumer(
    IQuotaManager quota,
    ILogger<FileReadyConsumer> logger) : IConsumer<FileReadyEvent>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<FileReadyEvent> context)
    {
        var evt = context.Message;

        // 未經配額預扣的檔案（如系統衍生檔）不需 commit。
        if (evt.ReservationId is not Guid reservationId)
            return;

        var actualSize = evt.SizeBytes ?? 0;
        await quota.CommitAsync(reservationId, actualSize, context.CancellationToken);

        logger.LogInformation(
            "Quota commit：Reservation={ReservationId}、Size={Size}", reservationId, actualSize);
    }
}
