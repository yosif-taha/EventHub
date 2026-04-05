using EventHub.Application.Common.Dtos.Auth;
using EventHub.Application.Common.Responses;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.Application.Features.Auth.Login
{
    public record LoginQuery(
        string Email,
        string Password
        ) : IRequest<RequestResult<AuthResponse>>;
   
}
