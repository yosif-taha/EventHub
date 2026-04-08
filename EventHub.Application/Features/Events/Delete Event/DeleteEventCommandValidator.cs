using FluentValidation;

namespace EventHub.Application.Features.Events.Delete_Event
{
    public class DeleteEventCommandValidator : AbstractValidator<DeleteEventCommand>
    {
        public DeleteEventCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Event Id is required.");
        }
    }
}
