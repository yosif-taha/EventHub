using AutoMapper;
using EventHub.Application.Common.Dtos.Auth;
using EventHub.WebAPI.Presentation.ViewModels.Auth;

namespace EventHub.WebAPI.Presentation.Mapping
{
    public class AuthViewModelProfile : Profile
    {
        public AuthViewModelProfile()
        {
            CreateMap<AuthResponse,AuthResponseViewModel>();
        }
    }
}
