using AutoMapper;
using NotificationService.Data.Entities;
using NotificationService.Models;

namespace NotificationService.Mapping;

public class NotificationMappingProfile : Profile
{
    public NotificationMappingProfile()
    {
        CreateMap<Notification, NotificationDto>();
        CreateMap<NotificationRequest, NotificationRequestDto>();
    }
}
