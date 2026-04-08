using EventHub.Application.Common.Responses;
using MediatR;

namespace EventHub.Application.Features.Category.Delete_Category
{
    public record DeleteCategoryCommand(Guid Id):IRequest<RequestResult<Unit>>;
}
