using EventHub.Application.Contracts;
using EventHub.Domin.Models;
using MediatR;

namespace EventHub.Application.Features.Common.Queries.CheckEventExists
{
    public record ChekEventExistsByCategoryIdQuery(Guid CategoryId) : IRequest<bool>;

    public record ChekEventExistsByCategoryIdQueryHandler(IGenericRepository<Event> _eventRepository) : IRequestHandler<ChekEventExistsByCategoryIdQuery, bool>
    {
        public async Task<bool> Handle(ChekEventExistsByCategoryIdQuery request, CancellationToken cancellationToken)
        {
            return await _eventRepository.AnyAsync(e => e.CategoryId == request.CategoryId, cancellationToken);
        }

    }
}
