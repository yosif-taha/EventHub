using FluentValidation;

namespace EventHub.Application.Features.Events.GetManagedEventById;

public sealed class GetManagedEventByIdQueryValidator : AbstractValidator<GetManagedEventByIdQuery>
{
    public GetManagedEventByIdQueryValidator()
    {
        RuleFor(query => query.EventId).NotEmpty();
    }
}
