using FluentValidation;

namespace EventHub.Application.Features.Registerations.GetEventRegistrations
{
    public class GetEventRegistrationsQueryValidator : AbstractValidator<GetEventRegistrationsQuery>
    {
        public GetEventRegistrationsQueryValidator()
        {
            RuleFor(x => x.EventId).NotEmpty();
            RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
        }
    }
}
