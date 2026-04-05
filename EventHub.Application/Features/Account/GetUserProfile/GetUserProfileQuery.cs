using EventHub.Application.Common.Dtos.Account;
using EventHub.Application.Common.Responses;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.Application.Features.Account.GetUserProfile
{
    public record GetUserProfileQuery(
        string UserId
        ) : IRequest<RequestResult<UserProfileResponse>>;

}
