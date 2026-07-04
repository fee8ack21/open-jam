using AutoMapper;
using OrderService.Data.Entities;
using OrderService.Models;

namespace OrderService.Mapping;

public class OrderMappingProfile : Profile
{
    public OrderMappingProfile()
    {
        CreateMap<Order, OrderResponse>()
            .ForMember(d => d.CheckoutUrl, o => o.Ignore()); // 建單時向 PaymentService 取得後補上
        CreateMap<Order, OrderSummaryDto>();
        CreateMap<OrderItem, OrderItemResponse>();
        CreateMap<OrderStatusHistory, OrderStatusHistoryResponse>();
    }
}
