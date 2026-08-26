using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Domin.Constants;
using EventHub.Domin.Models;
using MediatR;

namespace EventHub.Application.Features.Events.Update_Event_Status
{
    public class UpdateEventStatusCommandHandler(
        IGenericRepository<Event> _repository,
        IUserContext _userContext,
        IUnitOfWork _unitOfWork) : IRequestHandler<UpdateEventStatusCommand, RequestResult<Unit>>
    {
        public async Task<RequestResult<Unit>> Handle(UpdateEventStatusCommand request, CancellationToken cancellationToken)
        {
            return await _unitOfWork.ExecuteAsync(async () =>
            {
                var @event = await _repository.GetByIdAsTrackingAsync(request.Id, cancellationToken);
                if (@event is null)
                    return RequestResult<Unit>.Failure(ErrorCode.EventNotFound);

                var isAdmin = _userContext.IsInRole(RoleNames.Admin);
                var isOrganizer = _userContext.IsInRole(RoleNames.Organizer);
                if (!isAdmin && (!isOrganizer || @event.OrganizerId != _userContext.UserId))
                    return RequestResult<Unit>.Failure(ErrorCode.Forbidden);

                if (!@event.CanTransitionTo(request.Status))
                    return RequestResult<Unit>.Failure(ErrorCode.EventInvalidStatusTransition);

                @event.TransitionTo(request.Status);
                @event.UpdatedAt = DateTime.UtcNow;
                return RequestResult<Unit>.Success(Unit.Value);
            }, cancellationToken);
        }
    }
}
