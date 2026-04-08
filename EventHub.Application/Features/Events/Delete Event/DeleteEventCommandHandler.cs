using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Domin.Models;
using MediatR;

namespace EventHub.Application.Features.Events.Delete_Event
{
    public class DeleteEventCommandHandler(IGenericRepository<Event> _repository) : IRequestHandler<DeleteEventCommand, RequestResult<Unit>>
    {
        public async Task<RequestResult<Unit>> Handle(DeleteEventCommand request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var existingEvent = await _repository.GetByIdAsync(request.Id,cancellationToken);
            if (existingEvent == null)
                return RequestResult<Unit>.Failure(ErrorCode.EventNotFound);           

            await _repository.SoftDeleteAsync(request.Id,cancellationToken);
            return RequestResult<Unit>.Success(Unit.Value);
        }
    }
}
