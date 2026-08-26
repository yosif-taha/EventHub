using AutoMapper;
using EventHub.Application.Common.Dtos.Registrations;
using EventHub.Application.Common.Models;
using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Domin.Constants;
using EventHub.Domin.Models;
using MediatR;

namespace EventHub.Application.Features.Registerations.GetEventRegistrations
{
    public class GetEventRegistrationsQueryHandler(
        IGenericRepository<Event> _eventRepository,
        IGenericRepository<Registration> _registrationRepository,
        IUserContext _userContext,
        IMapper _mapper,
        IDbExecutor _executor)
        : IRequestHandler<GetEventRegistrationsQuery, RequestResult<PaginatedList<EventRegistrationDto>>>
    {
        public async Task<RequestResult<PaginatedList<EventRegistrationDto>>> Handle(
            GetEventRegistrationsQuery request,
            CancellationToken cancellationToken)
        {
            var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
            if (@event is null)
                return RequestResult<PaginatedList<EventRegistrationDto>>.Failure(ErrorCode.EventNotFound);

            var isAdmin = _userContext.IsInRole(RoleNames.Admin);
            var isOrganizer = _userContext.IsInRole(RoleNames.Organizer);
            if (!isAdmin && (!isOrganizer || @event.OrganizerId != _userContext.UserId))
                return RequestResult<PaginatedList<EventRegistrationDto>>.Failure(ErrorCode.Forbidden);

            var registrations = _registrationRepository.GetAll()
                .Where(registration => registration.EventId == request.EventId)
                .OrderBy(registration => registration.RegistrationDate);

            var result = await _executor.CreateAsync<Registration, EventRegistrationDto>(
                registrations,
                request.PageNumber,
                request.PageSize,
                _mapper.ConfigurationProvider,
                cancellationToken);

            return RequestResult<PaginatedList<EventRegistrationDto>>.Success(result);
        }
    }
}
