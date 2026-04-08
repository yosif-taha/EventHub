using EventHub.Application.Common.Dtos.Category;
using EventHub.Application.Common.Responses;
using MediatR;

namespace EventHub.Application.Features.Category.Get_Category_By_Id
{
    public record GetCategoryByIdQuery(Guid Id) : IRequest<RequestResult<CategoryDto>>;
}
