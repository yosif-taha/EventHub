using FluentValidation;

namespace EventHub.Application.Features.Registerations.CancelRegistrationForEvent
{
    public class CancelRegistrationCommandValidator : AbstractValidator<CancelRegistrationCommand>
    {
        public CancelRegistrationCommandValidator()
        {
            RuleFor(x => x.RegistrationId)
                  .NotEmpty()
                  .WithMessage("Event ID is required and cannot be empty.");
        }
    }
}
