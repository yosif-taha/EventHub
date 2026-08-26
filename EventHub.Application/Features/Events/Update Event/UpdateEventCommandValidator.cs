using FluentValidation;
using EventHub.Domin.Enums;


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

            RuleFor(x => x.Mode)
                .IsInEnum()
                .When(x => x.Mode.HasValue);

            RuleFor(x => x.OnlineMeetingUrl)
                .NotEmpty()
                .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
                .When(x => x.Mode == EventMode.Online)
                .WithMessage("An absolute online meeting URL is required for online events.");

            RuleFor(x => x.OnlineMeetingUrl)
                .Empty()
                .When(x => x.Mode == EventMode.Offline)
                .WithMessage("Offline events cannot include an online meeting URL.");

        }
    }
}
