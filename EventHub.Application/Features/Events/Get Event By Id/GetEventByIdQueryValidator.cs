using FluentValidation;

namespace EventHub.Application.Features.Events.Get_Event_By_Id
{
    public class GetEventByIdQueryValidator : AbstractValidator<GetEventByIdQuery>
    {
        public GetEventByIdQueryValidator()
        {
            RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Event ID is required.");
        }
    }
}
