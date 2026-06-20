using AutoMapper;
using EventHub.Application.Common.Extensions;
using EventHub.Application.Common.Models;
using EventHub.Application.Features.Registerations.CancelRegistrationForEvent;
using EventHub.Application.Features.Registerations.GetUserRegistrations;
using EventHub.Application.Features.Registerations.RegisterationForEvent;
using EventHub.WebAPI.Presentation.ViewModels.Registrations;
using EventHub.WebAPI.Presentation.ViewModels.Respponse;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.WebAPI.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    [Authorize]
    public class RegistrationController(IMediator _mediator, IMapper _mapper) : ControllerBase
    {
        [HttpPost("{eventId}")]
        public async Task<ResponseViewModel> RegisterForEvent(Guid eventId, CancellationToken ct)
        {
            var result = await _mediator.Send(new RegisterationCommand(eventId), ct);
            if (!result.IsSuccess)
                return new FailedResponseViewModel(result.ErrorCode, result.ErrorCode.GetDescription());
            var data = _mapper.Map<RegistrationResultViewModel>(result.Data);
            return new SuccessResponseViewModelT<RegistrationResultViewModel>(data);
        }

        [HttpPost("{registrationId}")]
        public async Task<ResponseViewModel> CancelRegisterForEvent(Guid registrationId, CancellationToken ct)
        {
            var result = await _mediator.Send(new CancelRegistrationCommand(registrationId), ct);
            if (!result.IsSuccess)
                return new FailedResponseViewModel(result.ErrorCode, result.ErrorCode.GetDescription());
            return new SuccessResponseViewModel( "Registration Canceled Successfuly");
        }

        [HttpGet]
        public async Task<ResponseViewModel> GetRegistrations(CancellationToken ct)
        {
            var result = await _mediator.Send(new GetMyRegistrationsQuery(),ct);
            if (!result.IsSuccess)
                return new FailedResponseViewModel(result.ErrorCode, result.ErrorCode.GetDescription());

            var data = _mapper.Map<List<GetUserRegistrationsViewModel>>(result.Data!.Items);
            var paginatedData = new PaginatedList<GetUserRegistrationsViewModel>(data, result.Data.TotalCount, result.Data.PageNumber, 10);

            return new SuccessResponseViewModelT<PaginatedList<GetUserRegistrationsViewModel>>(paginatedData);
        }
    }
}
