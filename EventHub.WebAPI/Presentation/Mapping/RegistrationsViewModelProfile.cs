using AutoMapper;
using EventHub.Application.Common.Dtos.Registrations;
using EventHub.WebAPI.Presentation.ViewModels.Registrations;

namespace EventHub.WebAPI.Presentation.Mapping
{
    public class RegistrationsViewModelProfile : Profile
    {
        public RegistrationsViewModelProfile()
        {
            CreateMap<UserRegistrationDto, GetUserRegistrationsViewModel>();
        }
    }
}
