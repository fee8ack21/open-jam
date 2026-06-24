using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Data.Entities;
using OrderService.Models;
using Shared.Exceptions;

namespace OrderService.Services.Orders;

/// <summary>訂單業務邏輯實作。</summary>
public class OrderManager(
    OrderDbContext db,
    IMapper mapper,
    AuditLogPublisher auditLog) : IOrderManager
{
    /// <inheritdoc/>
    public async Task<OrderResponse> CreateAsync(CreateOrderRequest request, Guid? userId, CancellationToken ct)
    {
        var currency = request.Currency.ToLowerInvariant();

        var order = new Order
        {
            Id          = Guid.NewGuid(),
            OrderNumber = OrderNumberGenerator.Next(),
            BuyerUserId = userId,
            BuyerEmail  = request.BuyerEmail,
            Currency    = currency,
            Status      = OrderStatus.Pending,
            Items       = request.Items.Select(i => new OrderItem
            {
                Id               = Guid.NewGuid(),
                CatalogId        = i.CatalogId,
                CatalogVersionId = i.CatalogVersionId,
                CatalogName      = i.CatalogName,
                UnitPrice        = i.UnitPrice,
            }).ToList(),
        };

        order.TotalAmount = order.Items.Sum(i => i.UnitPrice);
        order.StatusHistory.Add(NewHistory(null, OrderStatus.Pending, "Order created"));

        db.Orders.Add(order);
        auditLog.Add(userId, "order.create", "Order", order.Id, tenant: null);

        await db.SaveChangesAsync(ct);

        return await GetAsync(order.Id, ct);
    }

    /// <inheritdoc/>
    public Task<OrderResponse> GetAsync(Guid id, CancellationToken ct) =>
        ToResponseAsync(db.Orders.Where(o => o.Id == id), ct);

    /// <inheritdoc/>
    public Task<OrderResponse> GetByNumberAsync(string orderNumber, CancellationToken ct) =>
        ToResponseAsync(db.Orders.Where(o => o.OrderNumber == orderNumber), ct);

    /// <inheritdoc/>
    public async Task<ListOrdersResponse> ListAsync(ListOrdersRequest request, CancellationToken ct)
    {
        var query = db.Orders.AsNoTracking().AsQueryable();

        if (request.BuyerUserId is { } buyerUserId)
            query = query.Where(o => o.BuyerUserId == buyerUserId);

        if (!string.IsNullOrWhiteSpace(request.BuyerEmail))
        {
            var email = request.BuyerEmail.Trim();
            query = query.Where(o => o.BuyerEmail == email);
        }

        if (request.Status is { } status)
            query = query.Where(o => o.Status == status);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip(request.Offset)
            .Take(request.Limit)
            .ProjectTo<OrderSummaryDto>(mapper.ConfigurationProvider)
            .ToListAsync(ct);

        return new ListOrdersResponse { TotalCount = total, Items = items };
    }

    /// <inheritdoc/>
    public async Task<OrderResponse> CancelAsync(Guid id, CancelOrderRequest request, Guid? userId, CancellationToken ct)
    {
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new NotFoundException("找不到訂單。");

        // 具名買家的訂單僅本人可取消；匿名（Email）訂單憑訂單 ID 即可取消。
        if (order.BuyerUserId is { } buyer && buyer != userId)
            throw new ForbiddenException("無權取消此訂單。");

        if (order.Status != OrderStatus.Pending)
            throw new ConflictException("僅未付款的訂單可取消。");

        TransitionTo(order, OrderStatus.Cancelled, request.Reason ?? "Cancelled by buyer");
        auditLog.Add(userId, "order.cancel", "Order", order.Id, tenant: null);

        await db.SaveChangesAsync(ct);

        return await GetAsync(order.Id, ct);
    }

    /// <inheritdoc/>
    public async Task CompleteFromPaymentAsync(Guid orderId, DateTimeOffset paidAt, CancellationToken ct)
    {
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null)
            throw new NotFoundException($"找不到訂單 {orderId}。");

        // 冪等：訂單已完成（或已取消 / 退款）時不再處理，避免重複的 PaymentSucceededEvent 造成重複狀態。
        if (order.Status is not (OrderStatus.Pending or OrderStatus.Paid))
            return;

        order.CompletedAt = paidAt;
        TransitionTo(order, OrderStatus.Completed, "Payment succeeded");
        auditLog.Add(order.BuyerUserId, "order.complete", "Order", order.Id, tenant: null);

        await db.SaveChangesAsync(ct);
    }

    private static void TransitionTo(Order order, OrderStatus next, string? reason)
    {
        order.StatusHistory.Add(NewHistory(order.Status, next, reason));
        order.Status = next;
    }

    private static OrderStatusHistory NewHistory(OrderStatus? oldStatus, OrderStatus newStatus, string? reason) =>
        new()
        {
            Id        = Guid.NewGuid(),
            OldStatus = oldStatus,
            NewStatus = newStatus,
            Reason    = reason,
        };

    private async Task<OrderResponse> ToResponseAsync(IQueryable<Order> query, CancellationToken ct)
    {
        var order = await query
            .AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("找不到訂單。");

        var response = mapper.Map<OrderResponse>(order);
        response.StatusHistory = response.StatusHistory.OrderBy(h => h.CreatedAt).ToList();
        return response;
    }
}
