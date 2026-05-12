using EventHub.Application.Contracts;
using EventHub.Application.Features.Common.Queries.CheckCategoryExists;
using EventHub.Domin.Models;
using MediatR;

namespace EventHub.Application.Features.Common.Queries.CheckCategoryExists
{
    public record CheckCategoryExistsQuery(Guid Id) : IRequest<bool>;
}

public class CheckCategoryExistsQueryHandler(IGenericRepository<EventCategory> _repository) : IRequestHandler<CheckCategoryExistsQuery, bool>
{
    public async Task<bool> Handle(CheckCategoryExistsQuery request, CancellationToken cancellationToken)
    {
        return await _repository.AnyAsync(x => x.Id == request.Id, cancellationToken);
    }
}