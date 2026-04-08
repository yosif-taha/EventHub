using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Domin.Models;
using MediatR;

namespace EventHub.Application.Features.Category.Update_Category
{
    public class UpdateCategoryCommandHandler(IGenericRepository<EventCategory> _repository) : IRequestHandler<UpdateCategoryCommand, RequestResult<Unit>>
    {
        public async Task<RequestResult<Unit>> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var category = await _repository.GetByIdAsync(request.Id, cancellationToken);
            if (category == null) 
               return RequestResult<Unit>.Failure(ErrorCode.CategoryNotFound);

            var isNameDuplicate = await _repository
                .AnyAsync(c => c.Name == request.Name && c.Id != request.Id);
            if (isNameDuplicate)
                return RequestResult<Unit>.Failure(ErrorCode.CategoryAlreadyExist);

             await _repository.UpdateAsync(
                c => c.Id == request.Id,
                c => c.SetProperty(c => c.Name, request.Name)
             , cancellationToken);

            return RequestResult<Unit>.Success(Unit.Value);

        }
    }
}
