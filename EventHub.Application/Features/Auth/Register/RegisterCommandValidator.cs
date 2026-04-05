using EventHub.Application.Common.Responses;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.Application.Features.Auth.Register
{
    public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.");
           
            RuleFor(x => x.Password).NotEmpty()
                .MinimumLength(8);
            
            RuleFor(x => x.FullName).NotEmpty()
                .MaximumLength(100);
        }
    }
}
