 using AutoMapper;
using EventHub.Application.Common.Dtos.Events;
using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Domin.Models;
using MediatR;

namespace EventHub.Application.Features.Events.Get_Event_By_Id
{
    public class GetEventByIdQueryHangler(IGenericRepository<Event> _repository, IMapper _mapper) : IRequestHandler<GetEventByIdQuery, RequestResult<EventDto>>
    {
        public async Task<RequestResult<EventDto>> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
        {
            var eventDto = await _repository.GetByIdProjectedAsync<EventDto>(
                e => e.Id == request.Id,
                _mapper.ConfigurationProvider, cancellationToken);

            if (eventDto == null) 
                return RequestResult<EventDto>.Failure(ErrorCode.EventNotFound);
            return RequestResult<EventDto>.Success(eventDto);
        }
    }
}
