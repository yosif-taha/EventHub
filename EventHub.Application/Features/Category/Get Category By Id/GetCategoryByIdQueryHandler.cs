using AutoMapper;
using EventHub.Application.Common.Dtos.Category;
using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Domin.Models;
using MediatR;

namespace EventHub.Application.Features.Category.Get_Category_By_Id
{
    public class GetCategoryByIdQueryHandler(IGenericRepository<EventCategory> _repository,
        IMapper _mapper) : IRequestHandler<GetCategoryByIdQuery, RequestResult<CategoryDto>>
    {
        public async Task<RequestResult<CategoryDto>> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var categoryDto = await _repository.GetByIdProjectedAsync<CategoryDto>(c => c.Id == request.Id, _mapper.ConfigurationProvider, cancellationToken);
            if (categoryDto is null)
                return RequestResult<CategoryDto>.Failure(ErrorCode.CategoryNotFound);
            return RequestResult<CategoryDto>.Success(categoryDto);
        }
    }
}
