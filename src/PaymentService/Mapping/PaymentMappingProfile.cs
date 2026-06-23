using AutoMapper;
using PaymentService.Data.Entities;
using PaymentService.Models;

namespace PaymentService.Mapping;

public class PaymentMappingProfile : Profile
{
    public PaymentMappingProfile()
    {
        CreateMap<Payment, PaymentResponse>();
    }
}
