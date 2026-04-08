using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.Application.Features.Events.Update_Event
{
    public class UpdateEventCommandValidator : AbstractValidator<UpdateEventCommand>
    {
        public UpdateEventCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty();

            RuleFor(x => x.Title)
              .NotEmpty().WithMessage("Event title is required.")
              .MinimumLength(5).WithMessage("Title must be at least 5 characters long.")
              .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Event description is required.")
                .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters.");

            RuleFor(x => x.EventDate)
                .NotEmpty().WithMessage("Event date and time are required.")
                .GreaterThan(DateTime.UtcNow).WithMessage("Event date must be in the future.");

            RuleFor(x => x.Location)
                .NotEmpty().WithMessage("Location is required.");

            RuleFor(x => x.MaxAttendees)
                .GreaterThan(0).WithMessage("Maximum attendees must be at least 1 person.");

            RuleFor(x => x.CategoryId)
                .NotEmpty().WithMessage("Please select a valid event category.");
        }
    }
}
