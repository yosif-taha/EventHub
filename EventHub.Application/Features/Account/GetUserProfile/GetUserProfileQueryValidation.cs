using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.Application.Features.Account.GetUserProfile
{
    public class GetUserProfileQueryValidation : AbstractValidator<GetUserProfileQuery>
    {
        public GetUserProfileQueryValidation()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("UserId is required.")
                .MinimumLength(8).WithMessage("UserId must be at least 8 character long.");
        }
    }
}
