using FluentValidation;

namespace EventHub.Application.Features.Admin.UpdateUserRole
{
    public class UpdateUserRoleCommandValidator : AbstractValidator<UpdateUserRoleCommand>
    {
        public UpdateUserRoleCommandValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Role).IsInEnum();
        }
    }
}
