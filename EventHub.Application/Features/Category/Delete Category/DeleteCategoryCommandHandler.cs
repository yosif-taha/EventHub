using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Application.Features.Common.Queries.CheckEventExists;
using EventHub.Domin.Models;
using MediatR;

namespace EventHub.Application.Features.Category.Delete_Category
{
    public class DeleteCategoryCommandHandler(
        IGenericRepository<EventCategory> _categoryRepository,
        IUnitOfWork _unitOfWork,
        IMediator _mediator) : IRequestHandler<DeleteCategoryCommand, RequestResult<Unit>>
    {
        public async Task<RequestResult<Unit>> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
        {
            return await _unitOfWork.ExecuteAsync(async () =>
            {
                bool categoryExists = await _categoryRepository.AnyAsync(x => x.Id == request.Id, cancellationToken);
                if (!categoryExists)
                    return RequestResult<Unit>.Failure(ErrorCode.CategoryNotFound);

                bool hasRelatedEvents = await _mediator.Send(new ChekEventExistsByCategoryIdQuery(request.Id), cancellationToken);
                if (hasRelatedEvents)
                    return RequestResult<Unit>.Failure(ErrorCode.CategoryInUse);

                 _categoryRepository.SoftDelete(new EventCategory { Id = request.Id});
                return RequestResult<Unit>.Success(Unit.Value);
            }, cancellationToken);
        }
    }
}
