using FluentValidation;

namespace EventHub.Application.Features.Admin.GetAllRegistrations
{
    public class GetAllRegistrationsQueryValidator : AbstractValidator<GetAllRegistrationsQuery>
    {
        public GetAllRegistrationsQueryValidator()
        {
            RuleFor(query => query.PageNumber).GreaterThanOrEqualTo(1);
            RuleFor(query => query.PageSize).InclusiveBetween(1, 50);
        }
    }
}
