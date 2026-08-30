using FluentValidation;

namespace EventHub.Application.Features.Registerations.GetMyEventMeetingLink;

public sealed class GetMyEventMeetingLinkQueryValidator : AbstractValidator<GetMyEventMeetingLinkQuery>
{
    public GetMyEventMeetingLinkQueryValidator()
    {
        RuleFor(query => query.EventId).NotEmpty();
    }
}
