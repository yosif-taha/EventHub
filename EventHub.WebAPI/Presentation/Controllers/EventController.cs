using AutoMapper;
using EventHub.Application.Common.Extensions;
using EventHub.Application.Common.Models;
using EventHub.Application.Features.Events.Check_Event_Availability;
using EventHub.Application.Features.Events.Create_Event;
using EventHub.Application.Features.Events.Delete_Event;
using EventHub.Application.Features.Events.Get_Event_By_Id;
using EventHub.Application.Features.Events.GetAll_Events;
using EventHub.Application.Features.Events.Update_Event;
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
    [Authorize]
    public class EventController(IMediator _mediator, IMapper _mapper) : ControllerBase
    {
        [HttpGet]
        public async Task<ResponseViewModel> GetAllEvents([FromQuery] RequestFilter request, Guid? categoryId, CancellationToken ct)
        {
            var result = await _mediator.Send(new GetAllEventsQuery(request.SearchValue, categoryId, request.SortColumn, request.SortDirection, request.PageNumber, request.PageSize), ct);
            if (!result.IsSuccess)
                return new FailedResponseViewModel(result.ErrorCode, result.ErrorCode.GetDescription());
            var data = _mapper.Map<List<GetAllEventsViewModel>>(result.Data!.Items);
            var paginatedData = new PaginatedList<GetAllEventsViewModel>(
                 data,
                 result.Data.TotalCount,
                 result.Data.PageNumber,
                 request.PageSize);
            return new SuccessResponseViewModelT<PaginatedList<GetAllEventsViewModel>>(paginatedData);
        }
       
        [HttpGet]
        public async Task<ResponseViewModel> GetEventById([FromQuery] Guid id, CancellationToken ct)
        {
            var result = await _mediator.Send(new GetEventByIdQuery(id), ct);
            if (!result.IsSuccess)
                return new FailedResponseViewModel(result.ErrorCode, result.ErrorCode.GetDescription());
            var data = _mapper.Map<GetEventByIdViewModel>(result.Data);
            return new SuccessResponseViewModelT<GetEventByIdViewModel>(data);
        }

        [HttpGet]
        public async Task<ResponseViewModel> CheckEventAvailability([FromQuery] Guid id, CancellationToken ct)
        {
            var result = await _mediator.Send(new CheckEventAvailabilityQuery(id), ct);
            if (!result.IsSuccess)
                return new FailedResponseViewModel(result.ErrorCode, result.ErrorCode.GetDescription());
            var data = _mapper.Map<CheckEventAvailabilityViewModel>(result.Data);
            return new SuccessResponseViewModelT<CheckEventAvailabilityViewModel>(data);
        }

        [HttpPost]
        public async Task<ResponseViewModel> CreateEvent([FromBody] CreateEventRequest request, CancellationToken ct)
        {
            var result = await _mediator.Send(new CreateEventCommand(request.Title, request.Description, request.EventDate,request.Price, request.Location, request.CategoryId, request.MaxAttendees), ct);
            if (!result.IsSuccess)
                return new FailedResponseViewModel(result.ErrorCode, result.Message!);
            return new SuccessResponseViewModelT<Guid>(result.Data, "Event Created Successfuly");
        }

        [HttpPost]
        public async Task<ResponseViewModel> UpdateEvent([FromBody] UpdateEventRequest request, CancellationToken ct)
        {
            var result = await _mediator.Send(new UpdateEventCommand(request.Id, request.Title, request.Description, request.EventDate, request.Location, request.CategoryId, request.MaxAttendees), ct);
            if (!result.IsSuccess)
                return new FailedResponseViewModel(result.ErrorCode, result.Message!);
            return new SuccessResponseViewModelT<Unit>(result.Data, "Event Updated Successfuly");
        }

        [HttpPost]
        public async Task<ResponseViewModel> DeleteEvent([FromQuery] Guid id, CancellationToken ct)
        {
            var result = await _mediator.Send(new DeleteEventCommand(id),ct);
            if (!result.IsSuccess)
                return new FailedResponseViewModel(result.ErrorCode, result.ErrorCode.GetDescription());
            return new SuccessResponseViewModelT<Unit>(result.Data, "Event Deleted Successfuly");
        }
    }
}
