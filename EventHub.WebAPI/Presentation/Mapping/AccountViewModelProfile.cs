using AutoMapper;
using EventHub.Application.Common.Dtos.Account;
using EventHub.WebAPI.Presentation.ViewModels.Account;

namespace EventHub.WebAPI.Presentation.Mapping
{
    public class AccountViewModelProfile : Profile
    {
        public AccountViewModelProfile()
        {
            CreateMap<UserProfileResponse,AccountResponseViewModel>();
        }
    }
}
