using AutoMapper;
using OrderService.Data.Entities;
using OrderService.Models;

namespace OrderService.Mapping;

public class OrderMappingProfile : Profile
{
    public OrderMappingProfile()
    {
        CreateMap<Order, OrderResponse>();
        CreateMap<Order, OrderSummaryDto>();
        CreateMap<OrderItem, OrderItemResponse>();
        CreateMap<OrderStatusHistory, OrderStatusHistoryResponse>();
    }
}
