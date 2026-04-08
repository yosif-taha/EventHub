using AutoMapper;
using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Domin.Models;
using MediatR;

namespace EventHub.Application.Features.Events.Create_Event
{
    public class CreateEventCommandHandler(IGenericRepository<Event> _repository, IMapper _mapper, IUserContext _userContext) : IRequestHandler<CreateEventCommand, RequestResult<Guid>>
    {
        public async Task<RequestResult<Guid>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var newEvent = _mapper.Map<Event>(request);
            newEvent.OrganizerId = _userContext.UserId;

            await _repository.AddAsync(newEvent, cancellationToken);
            return RequestResult<Guid>.Success(newEvent.Id);
        }
    }
}
