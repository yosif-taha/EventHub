using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Domin.Models;
using MediatR;

namespace EventHub.Application.Features.Category.Update_Category
{
    public class UpdateCategoryCommandHandler(IGenericRepository<EventCategory> _repository, IUnitOfWork _unitOfWork) : IRequestHandler<UpdateCategoryCommand, RequestResult<Unit>>
    {
        public async Task<RequestResult<Unit>> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
        {
            return await _unitOfWork.ExecuteAsync(async () =>
            {
                bool categoryExists = await _repository.AnyAsync(x => x.Id == request.Id, cancellationToken);
                if (!categoryExists)
                    return RequestResult<Unit>.Failure(ErrorCode.CategoryNotFound);

                bool isNameDuplicate = await _repository.AnyAsync(c => c.Name == request.Name, cancellationToken);
                if (isNameDuplicate)
                    return RequestResult<Unit>.Failure(ErrorCode.CategoryAlreadyExist);

                var category = new EventCategory
                {
                    Id = request.Id,
                    Name = request.Name,
                    UpdatedAt = DateTime.UtcNow
                };

                _repository.SaveInclude(category, nameof(EventCategory.Name), nameof(EventCategory.UpdatedAt));


                return RequestResult<Unit>.Success(Unit.Value);
            }, cancellationToken);

        }
    }
}
