using AutoMapper.Configuration.Annotations;
using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.Application.Features.Auth.ConfirmEmail
{
    public class ResendConfirmationEmailCommandHandler(IAuthService authService) : IRequestHandler<ResendConfirmationEmailCommand, RequestResult<bool>>
    {
        private readonly IAuthService _authService = authService;

        public async Task<RequestResult<bool>> Handle(ResendConfirmationEmailCommand request, CancellationToken cancellationToken)
        {
           var data = await _authService.ResendConfirmationEmailAsync(request.Email, cancellationToken);
            if(!data.IsSuccess)
                return RequestResult<bool>.Failure(data.ErrorCode);
            return RequestResult<bool>.Success(data.Data);
        }
    }
}
