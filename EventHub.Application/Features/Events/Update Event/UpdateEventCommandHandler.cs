using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Domin.Models;
using MediatR;

namespace EventHub.Application.Features.Events.Update_Event
{
    public class UpdateEventCommandHandler(IGenericRepository<Event> _repository, IUserContext _userContext, IUnitOfWork _unitOfWork) : IRequestHandler<UpdateEventCommand, RequestResult<Unit>>
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

                var newevent = new Event { Id = request.Id };
                List<string> propertiesToUpdate = new List<string>();
                newevent.UpdatedAt = DateTime.UtcNow;
                propertiesToUpdate.Add(nameof(newevent.UpdatedAt));

                var eventProperties = typeof(Event).GetProperties().ToDictionary(p => p.Name);

                foreach (var prop in typeof(UpdateEventCommand).GetProperties())
                {
                    var value = prop.GetValue(request);
                    if (value == null)
                        continue;

                    if (!eventProperties.TryGetValue(prop.Name, out var entityProp))
                        continue;

                    entityProp.SetValue(newevent, value);
                    if (entityProp.Name != "Id")
                        propertiesToUpdate.Add(entityProp.Name);
                }

                _repository.SaveInclude(newevent, propertiesToUpdate.ToArray());

                return RequestResult<Unit>.Success(Unit.Value);
            }, cancellationToken);
        }
    }
}
