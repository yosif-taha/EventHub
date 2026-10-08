using AutoMapper;
using EventHub.Application.Common.Dtos.Events;
using EventHub.Application.Features.Events.Create_Event;
using EventHub.Domin.Models;
using EventHub.Domin.Enums;


namespace EventHub.Application.Common.Mapping.Events
{
    public class EventProfile : Profile
    {
        public EventProfile()
        {
            CreateMap<CreateEventCommand, Event>()
                .ForMember(dest => dest.OrganizerId ,opt => opt.Ignore());

            CreateMap<Event, EventDto>()
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name))
                .ForMember(dest => dest.RemainingSlots, opt => opt.MapFrom(src => src.MaxAttendees - src.CurrentAttendeesCount))
                .ForMember(dest => dest.PaymentRequired, opt => opt.MapFrom(src => src.Price > 0));

            CreateMap<Event,EventAvailabilityDto>()
                .ForCtorParam(nameof(EventAvailabilityDto.RemainingSlots), opt => opt.MapFrom(src => src.MaxAttendees - src.CurrentAttendeesCount))
                .ForCtorParam(nameof(EventAvailabilityDto.IsAvailable), opt => opt.MapFrom(src =>
                    src.Status == EventStatus.Scheduled &&
                    src.EventDate > DateTime.UtcNow &&
                    src.CurrentAttendeesCount < src.MaxAttendees))
                .ForCtorParam(nameof(EventAvailabilityDto.IsCancelled), opt => opt.MapFrom(src => src.Status == EventStatus.Canceled));
        }
    }
}
