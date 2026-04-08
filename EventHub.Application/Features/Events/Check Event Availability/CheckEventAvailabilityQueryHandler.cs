using AutoMapper;
using EventHub.Application.Common.Dtos.Events;
using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Domin.Models;
using MediatR;

namespace EventHub.Application.Features.Events.Check_Event_Availability
{
    public class CheckEventAvailabilityQueryHandler(
        IGenericRepository<Event> _repository,
        IMapper _mapper) : IRequestHandler<CheckEventAvailabilityQuery, RequestResult<EventAvailabilityDto>>
    {
        public async Task<RequestResult<EventAvailabilityDto>> Handle(CheckEventAvailabilityQuery request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var eventAvailabilityDto = await _repository.GetByIdProjectedAsync<EventAvailabilityDto>(
                e => e.Id == request.Id,
                _mapper.ConfigurationProvider, cancellationToken);

            if (eventAvailabilityDto is null)
                return RequestResult<EventAvailabilityDto>.Failure(ErrorCode.EventNotFound);
            return RequestResult<EventAvailabilityDto>.Success(eventAvailabilityDto);
        }
    }
}
