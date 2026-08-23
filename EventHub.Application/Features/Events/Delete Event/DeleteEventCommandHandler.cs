using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Domin.Models;
using EventHub.Domin.Constants;
using MediatR;

namespace EventHub.Application.Features.Events.Delete_Event
{
    public class DeleteEventCommandHandler(IGenericRepository<Event> _repository, IUserContext _userContext, IUnitOfWork _unitOfWork) : IRequestHandler<DeleteEventCommand, RequestResult<Unit>>
    {
        public async Task<RequestResult<Unit>> Handle(DeleteEventCommand request, CancellationToken cancellationToken)
        {
            if (!_userContext.IsInRole(RoleNames.Admin))
                return RequestResult<Unit>.Failure(ErrorCode.Forbidden);

            return await _unitOfWork.ExecuteAsync(async () =>
            {
                bool existingEvent = await _repository.AnyAsync(x => x.Id == request.Id, cancellationToken);
                if (!existingEvent)
                    return RequestResult<Unit>.Failure(ErrorCode.EventNotFound);

                _repository.SoftDelete(new Event { Id = request.Id });
                return RequestResult<Unit>.Success(Unit.Value);
            }, cancellationToken);
        }
    }
}
