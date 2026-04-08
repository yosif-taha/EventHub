using FluentValidation;

namespace EventHub.Application.Features.Events.Check_Event_Availability
{
    public class CheckEventAvailabilityQueryValidator : AbstractValidator<CheckEventAvailabilityQuery>
    {
        public CheckEventAvailabilityQueryValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Event ID is required.");  
        }
    }
}
