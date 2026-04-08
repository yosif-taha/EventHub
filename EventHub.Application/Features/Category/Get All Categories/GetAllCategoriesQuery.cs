using EventHub.Application.Common.Dtos.Category;
using EventHub.Application.Common.Responses;
using MediatR;

namespace EventHub.Application.Features.Category.Get_All_Categories
{
    public record GetAllCategoriesQuery() : IRequest<RequestResult<IEnumerable<CategoryDto>>>;
}
