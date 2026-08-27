using FluentValidation;

namespace EventHub.Application.Features.Notifications.SendEventAnnouncement
{
    public class SendEventAnnouncementCommandValidator : AbstractValidator<SendEventAnnouncementCommand>
    {
        public SendEventAnnouncementCommandValidator()
        {
            RuleFor(command => command.EventId).NotEmpty();
            RuleFor(command => command.Subject).NotEmpty().MaximumLength(250);
            RuleFor(command => command.Message).NotEmpty().MaximumLength(1000);
        }
    }
}
