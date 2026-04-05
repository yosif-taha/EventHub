using EventHub.Application.Common.Responses;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.Application.Features.Account.ChangePassword
{
    public record ChangePasswordCommand(
        string UserId,
        string CurrentPassword,
        string NewPassword
        ) : IRequest<RequestResult<bool>>;

}
