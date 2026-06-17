using AutoMapper;
using EventHub.Application.Common.Dtos.Registrations;
using EventHub.Domin.Models;

namespace EventHub.Application.Common.Mapping.Registrations
{
    public class RegistrationProfile : Profile
    {
        public RegistrationProfile()
        {
            CreateMap<Registration, UserRegistrationDto>()
                .ForMember(des => des.EventTitle, opt => opt.MapFrom(src => src.Event.Title))
                .ForMember(des => des.EventLocation, opt => opt.MapFrom(src => src.Event.Location))
                .ForMember(des => des.EventStartDate, opt => opt.MapFrom(src => src.Event.EventDate));
        }
    }
}
