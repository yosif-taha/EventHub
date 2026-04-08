using FluentValidation;

namespace EventHub.Application.Features.Category.Get_Category_By_Id
{
    public class GetCategoryByIdQueryValidator : AbstractValidator<GetCategoryByIdQuery>
    {
        public GetCategoryByIdQueryValidator()
        {
            RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Category ID is required.");
        }
    }
}
