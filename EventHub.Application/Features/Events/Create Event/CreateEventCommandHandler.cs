using AutoMapper;
using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Application.Features.Common.Queries.CheckCategoryExists;
using EventHub.Domin.Models;
using MediatR;

namespace EventHub.Application.Features.Events.Create_Event
{
    public class CreateEventCommandHandler(
        IGenericRepository<Event> _repository,
        IMapper _mapper, IUserContext _userContext,
        IMediator _mediator,
        IUnitOfWork _unitOfWork
        )
        : IRequestHandler<CreateEventCommand, RequestResult<Guid>>
    {

        public async Task<RequestResult<Guid>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
        {
            return await _unitOfWork.ExecuteAsync(async () =>
            {
                 var categoryExists = await _mediator.Send(new CheckCategoryExistsQuery(request.CategoryId), cancellationToken);
                 if (!categoryExists)
                     return RequestResult<Guid>.Failure(ErrorCode.CategoryNotFound);

                 var newEvent = _mapper.Map<Event>(request);
                 newEvent.OrganizerId = _userContext.UserId;
                 newEvent.CreatedAt = DateTime.UtcNow;

                 await _repository.AddAsync(newEvent, cancellationToken);
                 return RequestResult<Guid>.Success(newEvent.Id);
            }, cancellationToken);

        }
    }
}
