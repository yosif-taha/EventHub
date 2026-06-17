using FluentValidation;

namespace EventHub.Application.Features.Registerations.RegisterationForEvent
{
    public class RegisterationCommandValidator : AbstractValidator<RegisterationCommand>
    {
        public RegisterationCommandValidator()
        {
            RuleFor(x => x.EventId)
                .NotEmpty()
                .WithMessage("Event ID is required and cannot be empty.");
        }
    }
}
