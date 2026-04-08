using AutoMapper;
using EventHub.Application.Common.Dtos.Events;
using EventHub.Application.Features.Events.Create_Event;
using EventHub.Domin.Models;

namespace EventHub.Application.Common.Mapping.Events
{
    public class EventProfile : Profile
    {
        public EventProfile()
        {
            CreateMap<CreateEventCommand, Event>()
                .ForMember(dest => dest.OrganizerId ,opt => opt.Ignore());

            CreateMap<Event, EventDto>()
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name));

            CreateMap<Event,EventAvailabilityDto>()
                .ForMember(dest => dest.RemainingSlots, opt => opt.MapFrom(src => src.MaxAttendees - src.CurrentAttendeesCount))
                .ForMember(dest => dest.IsAvailable, opt => opt.MapFrom(src => !src.IsCancelled && src.CurrentAttendeesCount < src.MaxAttendees))
                .ForMember(dest => dest.IsCancelled, opt => opt.MapFrom(src => src.IsCancelled));
        }
    }
}
