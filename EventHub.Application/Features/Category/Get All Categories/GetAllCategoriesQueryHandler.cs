using AutoMapper;
using AutoMapper.QueryableExtensions;
using EventHub.Application.Common.Dtos.Category;
using EventHub.Application.Common.Responses;
using EventHub.Application.Contracts;
using EventHub.Domin.Models;
using MediatR;

namespace EventHub.Application.Features.Category.Get_All_Categories
{
    public class GetAllCategoriesQueryHandler(
        IGenericRepository<EventCategory> _repository,
        IMapper _mapper,
        IDbExecutor _executor) : IRequestHandler<GetAllCategoriesQuery, RequestResult<IEnumerable<CategoryDto>>>
    {
        public async Task<RequestResult<IEnumerable<CategoryDto>>> Handle(GetAllCategoriesQuery request, CancellationToken cancellationToken)
        {
            var categories = _repository.GetAll().ProjectTo<CategoryDto>(_mapper.ConfigurationProvider);
            var categoryList = await _executor.ToListAsync(categories, cancellationToken);
            return RequestResult<IEnumerable<CategoryDto>>.Success(categoryList);
        }
    }
}
