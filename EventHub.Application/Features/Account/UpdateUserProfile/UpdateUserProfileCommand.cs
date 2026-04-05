using EventHub.Application.Common.Responses;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.Application.Features.Account.UpdateUserProfile
{
    public record UpdateUserProfileCommand(
        string UserId,
        string FullName
        ) : IRequest<RequestResult<bool>>;

}
