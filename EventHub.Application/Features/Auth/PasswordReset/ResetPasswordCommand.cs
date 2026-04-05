using EventHub.Application.Common.Responses;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.Application.Features.Auth.PasswordReset
{
    public record ResetPasswordCommand(
        string Email,
        string Code,
        string NewPassword
        ) : IRequest<RequestResult<bool>>;

}
