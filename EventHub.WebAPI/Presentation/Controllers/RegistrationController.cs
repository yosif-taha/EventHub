using AutoMapper;
using EventHub.Application.Common.Extensions;
using EventHub.Application.Common.Models;
using EventHub.Application.Features.Registerations.CancelRegistrationForEvent;
using EventHub.Application.Features.Registerations.GetUserRegistrations;
using EventHub.Application.Features.Registerations.GetEventRegistrations;
using EventHub.Application.Features.Registerations.RegisterationForEvent;
using EventHub.WebAPI.Presentation.ViewModels.Registrations;
using EventHub.WebAPI.Presentation.ViewModels.Request;
using EventHub.WebAPI.Presentation.ViewModels.Respponse;
using EventHub.Domin.Constants;
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
        [HttpPost("/api/registrations/{eventId:guid}")]
        [Authorize(Roles = RoleNames.Attendee)]
        public async Task<ResponseViewModel> RegisterForEvent(Guid eventId, CancellationToken ct)
        {
            return await RegisterForEventAsync(eventId, ct);
        }

        [HttpPost("/api/registrations")]
        [Authorize(Roles = RoleNames.Attendee)]
        public async Task<ResponseViewModel> RegisterForEvent([FromBody] RegisterForEventRequest request, CancellationToken ct)
        {
            return await RegisterForEventAsync(request.EventId, ct);
        }

        private async Task<ResponseViewModel> RegisterForEventAsync(Guid eventId, CancellationToken ct)
        {
            var result = await _mediator.Send(new RegisterationCommand(eventId), ct);
            if (!result.IsSuccess)
                return new FailedResponseViewModel(result.ErrorCode, result.ErrorCode.GetDescription());
            var data = _mapper.Map<RegistrationResultViewModel>(result.Data);
            return new SuccessResponseViewModelT<RegistrationResultViewModel>(data);
        }

        [HttpPost("{registrationId}")]
        [HttpDelete("/api/registrations/{registrationId:guid}")]
        [Authorize(Roles = RoleNames.Attendee)]
        public async Task<ResponseViewModel> CancelRegisterForEvent(Guid registrationId, CancellationToken ct)
        {
            var result = await _mediator.Send(new CancelRegistrationCommand(registrationId), ct);
            if (!result.IsSuccess)
                return new FailedResponseViewModel(result.ErrorCode, result.ErrorCode.GetDescription());
            return new SuccessResponseViewModel( "Registration Canceled Successfuly");
        }

        [HttpGet]
        [HttpGet("/api/registrations/me")]
        [Authorize(Roles = RoleNames.Attendee)]
        public async Task<ResponseViewModel> GetRegistrations([FromQuery] RequestFilter request, CancellationToken ct)
        {
            var result = await _mediator.Send(new GetMyRegistrationsQuery(request.PageNumber, request.PageSize),ct);
            if (!result.IsSuccess)
                return new FailedResponseViewModel(result.ErrorCode, result.ErrorCode.GetDescription());

            var data = _mapper.Map<List<GetUserRegistrationsViewModel>>(result.Data!.Items);
            var paginatedData = new PaginatedList<GetUserRegistrationsViewModel>(data, result.Data.TotalCount, result.Data.PageNumber, request.PageSize);

            return new SuccessResponseViewModelT<PaginatedList<GetUserRegistrationsViewModel>>(paginatedData);
        }

        [HttpGet("{eventId:guid}")]
        [HttpGet("/api/events/{eventId:guid}/registrations")]
        [Authorize(Roles = RoleNames.AdminOrOrganizer)]
        public async Task<ResponseViewModel> GetEventRegistrations(Guid eventId, [FromQuery] RequestFilter request, CancellationToken ct)
        {
            var result = await _mediator.Send(new GetEventRegistrationsQuery(eventId, request.PageNumber, request.PageSize), ct);
            if (!result.IsSuccess)
                return new FailedResponseViewModel(result.ErrorCode, result.ErrorCode.GetDescription());

            var data = _mapper.Map<List<EventRegistrationViewModel>>(result.Data!.Items);
            var paginatedData = new PaginatedList<EventRegistrationViewModel>(data, result.Data.TotalCount, result.Data.PageNumber, request.PageSize);
            return new SuccessResponseViewModelT<PaginatedList<EventRegistrationViewModel>>(paginatedData);
        }
    }
}
