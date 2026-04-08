using AutoMapper;
using EventHub.Application.Common.Dtos.Events;
using EventHub.WebAPI.Presentation.ViewModels.Events;

namespace EventHub.WebAPI.Presentation.Mapping
{
    public class EventViewModelProfile : Profile
    {
        public EventViewModelProfile()
        {
            CreateMap<EventDto,GetAllEventsViewModel>();
            CreateMap<EventDto,GetEventByIdViewModel>();
            CreateMap<EventAvailabilityDto, CheckEventAvailabilityViewModel>();
        }
    }
}
