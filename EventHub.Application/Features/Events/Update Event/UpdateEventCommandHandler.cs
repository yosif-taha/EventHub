using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Application.Features.Common.Queries.CheckCategoryExists;
using EventHub.Domin.Enums;
using EventHub.Domin.Models;
using MediatR;

namespace EventHub.Application.Features.Events.Update_Event
{
    public class UpdateEventCommandHandler(
        IGenericRepository<Event> _repository,
        IUserContext _userContext,
        IMediator _mediator,
        IUnitOfWork _unitOfWork) : IRequestHandler<UpdateEventCommand, RequestResult<Unit>>
    {
        public async Task<RequestResult<Unit>> Handle(UpdateEventCommand request, CancellationToken cancellationToken)
        {
            return await _unitOfWork.ExecuteAsync(async () =>
            {
                var existingEvent = await _repository.GetByIdAsTrackingAsync(request.Id, cancellationToken);
                if (existingEvent is null)
                    return RequestResult<Unit>.Failure(ErrorCode.EventNotFound);

                var isAdmin = _userContext.IsInRole(EventHub.Domin.Constants.RoleNames.Admin);
                var isOrganizer = _userContext.IsInRole(EventHub.Domin.Constants.RoleNames.Organizer);
                if (!isAdmin && (!isOrganizer || existingEvent.OrganizerId != _userContext.UserId))
                    return RequestResult<Unit>.Failure(ErrorCode.Forbidden);

                if (existingEvent.Status != EventStatus.Scheduled)
                    return RequestResult<Unit>.Failure(ErrorCode.EventInvalidStatusTransition, "Only scheduled events can be updated.");

                if (request.CategoryId.HasValue)
                {
                    var categoryExists = await _mediator.Send(new CheckCategoryExistsQuery(request.CategoryId.Value), cancellationToken);
                    if (!categoryExists)
                        return RequestResult<Unit>.Failure(ErrorCode.CategoryNotFound);
                }

                if (request.MaxAttendees.HasValue && request.MaxAttendees.Value < existingEvent.CurrentAttendeesCount)
                    return RequestResult<Unit>.Failure(ErrorCode.EventCapacityFull, "Maximum attendees cannot be lower than the current attendee count.");

                if (!request.Mode.HasValue && request.OnlineMeetingUrl is not null && existingEvent.Mode != EventMode.Online)
                    return RequestResult<Unit>.Failure(ErrorCode.ValidationError, "Only online events can include an online meeting URL.");

                if (!request.Mode.HasValue && request.OnlineMeetingUrl is not null &&
                    !Uri.TryCreate(request.OnlineMeetingUrl, UriKind.Absolute, out _))
                    return RequestResult<Unit>.Failure(ErrorCode.ValidationError, "An absolute online meeting URL is required for online events.");

                if (request.Title is not null) existingEvent.Title = request.Title;
                if (request.Description is not null) existingEvent.Description = request.Description;
                if (request.EventDate.HasValue) existingEvent.EventDate = request.EventDate.Value;
                if (request.Location is not null) existingEvent.Location = request.Location;
                if (request.CategoryId.HasValue) existingEvent.CategoryId = request.CategoryId.Value;
                if (request.MaxAttendees.HasValue) existingEvent.MaxAttendees = request.MaxAttendees.Value;

                if (request.Mode.HasValue)
                {
                    existingEvent.Mode = request.Mode.Value;
                    existingEvent.OnlineMeetingUrl = request.Mode == EventMode.Offline
                        ? null
                        : request.OnlineMeetingUrl;
                }
                else if (request.OnlineMeetingUrl is not null)
                {
                    existingEvent.OnlineMeetingUrl = request.OnlineMeetingUrl;
                }

                existingEvent.UpdatedAt = DateTime.UtcNow;

                return RequestResult<Unit>.Success(Unit.Value);
            }, cancellationToken);
        }
    }
}
