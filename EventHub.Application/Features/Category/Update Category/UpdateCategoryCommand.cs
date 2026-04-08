using EventHub.Application.Common.Responses;
using MediatR;

namespace EventHub.Application.Features.Category.Update_Category
{
    public record UpdateCategoryCommand(Guid Id, string Name) : IRequest<RequestResult<Unit>>;
}
