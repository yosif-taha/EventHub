using EventHub.Application.Common.Dtos.Auth;
using EventHub.Application.Common.Responses;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.Application.Features.Auth.RefreshTokens
{
    public record RefreshTokenCommand(
        string Token,
        string RefreshToken
        ) : IRequest<RequestResult<AuthResponse>>;

}
