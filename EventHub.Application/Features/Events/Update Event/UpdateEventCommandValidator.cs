using FluentValidation;

namespace EventHub.Application.Features.Events.Update_Event
{
    public class UpdateEventCommandValidator : AbstractValidator<UpdateEventCommand>
    {
        public UpdateEventCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty();

            RuleFor(x => x.Title)
              .MinimumLength(5)
              .When(x => x.Title != null)
              .WithMessage("Title must be at least 5 characters long.")
              .MaximumLength(200)
              .WithMessage("Title cannot exceed 200 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(2000)
                .When(x => x.Description != null)
                .WithMessage("Description cannot exceed 2000 characters.");

            RuleFor(x => x.EventDate)
                .GreaterThan(DateTime.UtcNow)
                .When(x => x.EventDate.HasValue)
                .WithMessage("Event date must be in the future.");

            RuleFor(x => x.MaxAttendees)
                .GreaterThan(0)
                .When(x => x.MaxAttendees.HasValue)
                .WithMessage("Maximum attendees must be at least 1 person.");

        }
    }
}
