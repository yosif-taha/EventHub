using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.Application.Features.Auth.ConfirmEmail
{
    public class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
    {
        public ConfirmEmailCommandValidator()
        {
            RuleFor(e => e.UserId)
                .NotEmpty()
                .WithMessage("UserId is required.");

            RuleFor(e => e.Code)
                .NotEmpty()
                .WithMessage("Confirmation code is required.")
                .MinimumLength(10)
                .WithMessage("Confirmation code must be at least 10 characters long.");
        }
    }
}
