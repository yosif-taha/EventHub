using FluentValidation;

namespace EventHub.Application.Features.Events.GetAll_Events
{
    public class GetAllEventsQueryValidator : AbstractValidator<GetAllEventsQuery>
    {
        public GetAllEventsQueryValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1).WithMessage("Page number must be at least 1.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 50).WithMessage("Page size must be between 1 and 50.");

            string[] allowedSortColumns = { "Title", "Location" };
            RuleFor(x => x.SortColumn)
                .Must(x => string.IsNullOrEmpty(x) || allowedSortColumns.Contains(x))
                .WithMessage($"Sort column must be one of: {string.Join(", ", allowedSortColumns)}");

            RuleFor(x => x.SortDirection)
                .Must(x => string.IsNullOrEmpty(x) ||
                 x.Equals("asc", StringComparison.OrdinalIgnoreCase) ||
                 x.Equals("desc", StringComparison.OrdinalIgnoreCase))
                .WithMessage("Sort direction must be 'asc' or 'desc'.");

        }
    }
}
