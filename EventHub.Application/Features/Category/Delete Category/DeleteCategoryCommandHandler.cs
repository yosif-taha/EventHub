using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Domin.Models;
using MediatR;

namespace EventHub.Application.Features.Category.Delete_Category
{
    public class DeleteCategoryCommandHandler(
        IGenericRepository<EventCategory> _categoryRepository,
        IGenericRepository<Event> _eventRepository) : IRequestHandler<DeleteCategoryCommand, RequestResult<Unit>>
    {
        public async Task<RequestResult<Unit>> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var category = await _categoryRepository.GetByIdAsync(request.Id, cancellationToken);
            if (category is null)
                return RequestResult<Unit>.Failure(ErrorCode.CategoryNotFound);

            var hasRelatedEvents = await _eventRepository.AnyAsync(e => e.CategoryId == request.Id);
            if (hasRelatedEvents)
                return RequestResult<Unit>.Failure(ErrorCode.CategoryInUse);

            await _categoryRepository.SoftDeleteAsync(request.Id, cancellationToken);
            return RequestResult<Unit>.Success(Unit.Value);
        }
    }
}
