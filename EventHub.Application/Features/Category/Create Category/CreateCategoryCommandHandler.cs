using AutoMapper;
using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Domin.Models;
using MediatR;

namespace EventHub.Application.Features.Category.Create_Category
{
    public class CreateCategoryCommandHandler(IGenericRepository<EventCategory> _repository, IMapper _mapper) : IRequestHandler<CreateCategoryCommand, RequestResult<Guid>>
    {
        public async Task<RequestResult<Guid>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var exist = await _repository.AnyAsync(x => x.Name == request.Name);
            if (exist)
                return RequestResult<Guid>.Failure(ErrorCode.CategoryAlreadyExist);

            var category = _mapper.Map<EventCategory>(request);
            await _repository.AddAsync(category, cancellationToken);

            return RequestResult<Guid>.Success(category.Id);
        }
    }
}
