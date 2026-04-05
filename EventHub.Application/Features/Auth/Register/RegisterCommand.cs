using EventHub.Application.Common.Responses;
using EventHub.Domin.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.Application.Features.Auth.Register
{
    public record RegisterCommand(
        string Email,
        string Password,
        string FullName,
        UserRole Role = UserRole.Attend
        ) : IRequest<RequestResult<Guid>>;
   
}
