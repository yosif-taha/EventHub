using EventHub.Application.Common.Responses;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.Application.Features.Auth.ConfirmEmail
{
    public record ConfirmEmailCommand(
        string UserId,
        string Code
        ) : IRequest<RequestResult<bool>>;

}
