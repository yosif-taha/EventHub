using FluentValidation;

namespace EventHub.Application.Features.Category.Delete_Category
{
    public class DeleteCategoryCommandValidator : AbstractValidator<DeleteCategoryCommand>
    {
        public DeleteCategoryCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty()
                .WithMessage("Category Id is required.");
        }
    }
}
