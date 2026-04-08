using EventHub.Application.Common.Responses;
using MediatR;

namespace EventHub.Application.Features.Category.Create_Category
{
    public record CreateCategoryCommand(
        string Name
        ) : IRequest<RequestResult<Guid>>;

}
