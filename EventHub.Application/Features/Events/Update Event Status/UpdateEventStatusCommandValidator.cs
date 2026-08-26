using EventHub.Domin.Enums;
using FluentValidation;

namespace EventHub.Application.Features.Events.Update_Event_Status
{
    public class UpdateEventStatusCommandValidator : AbstractValidator<UpdateEventStatusCommand>
    {
        public UpdateEventStatusCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Status)
                .Must(status => status is EventStatus.Completed or EventStatus.Canceled)
                .WithMessage("Events can only transition from scheduled to completed or canceled.");
        }
    }
}
