using AutoMapper;
using EventHub.Application.Common.Dtos.Dashboards;
using EventHub.Application.Common.Dtos.Events;
using EventHub.Application.Common.Extensions;
using EventHub.Application.Common.Models;
using EventHub.Application.Features.Dashboards.GetOrganizerDashboard;
using EventHub.Application.Features.Events.GetMyEvents;
using EventHub.Domin.Constants;
using EventHub.WebAPI.Presentation.ViewModels.Events;
using EventHub.WebAPI.Presentation.ViewModels.Request;
using EventHub.WebAPI.Presentation.ViewModels.Respponse;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.WebAPI.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    [Authorize(Roles = RoleNames.Organizer)]
    public class OrganizerController(IMediator _mediator, IMapper _mapper) : ControllerBase
    {
        [HttpGet("/api/organizer/events")]
        public async Task<ResponseViewModel> GetMyEvents([FromQuery] RequestFilter request, Guid? categoryId, CancellationToken ct)
        {
            var result = await _mediator.Send(new GetMyEventsQuery(
                request.SearchValue,
                categoryId,
                request.SortColumn,
                request.SortDirection,
                request.PageNumber,
                request.PageSize), ct);
            if (!result.IsSuccess)
                return new FailedResponseViewModel(result.ErrorCode, result.Message ?? result.ErrorCode.GetDescription());

            var events = _mapper.Map<List<GetAllEventsViewModel>>(result.Data!.Items);
            var paginatedEvents = new PaginatedList<GetAllEventsViewModel>(
                events,
                result.Data.TotalCount,
                result.Data.PageNumber,
                request.PageSize);
            return new SuccessResponseViewModelT<PaginatedList<GetAllEventsViewModel>>(paginatedEvents);
        }

        [HttpGet("/api/organizer/dashboard")]
        public async Task<ResponseViewModel> GetDashboard(CancellationToken ct)
        {
            var result = await _mediator.Send(new GetOrganizerDashboardQuery(), ct);
            if (!result.IsSuccess)
                return new FailedResponseViewModel(result.ErrorCode, result.Message ?? result.ErrorCode.GetDescription());

            return new SuccessResponseViewModelT<OrganizerDashboardDto>(result.Data!);
        }
    }
}
