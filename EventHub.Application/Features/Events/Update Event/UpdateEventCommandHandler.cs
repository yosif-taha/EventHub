using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Domin.Models;
using MediatR;

namespace EventHub.Application.Features.Events.Update_Event
{
    public class UpdateEventCommandHandler(IGenericRepository<Event> _repository, IUserContext _userContext) : IRequestHandler<UpdateEventCommand, RequestResult<Unit>>
    {
        public async Task<RequestResult<Unit>> Handle(UpdateEventCommand request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _repository.UpdateAsync(
            e => e.Id == request.Id && e.OrganizerId == _userContext.UserId,
            s => s.SetProperty(e => e.Title, request.Title)
                  .SetProperty(e => e.Description, request.Description)
                  .SetProperty(e => e.EventDate, request.EventDate)
                  .SetProperty(e => e.Location, request.Location)
                  .SetProperty(e => e.CategoryId, request.CategoryId)
                  .SetProperty(e => e.MaxAttendees, request.MaxAttendees), cancellationToken);


            return RequestResult<Unit>.Success(Unit.Value);
        }
    }
}
